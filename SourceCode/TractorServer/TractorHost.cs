﻿using Duan.Xiugang.Tractor.Objects;
using Fleck;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Timers;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace TractorServer
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class TractorHost : ITractorHost
    {
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public log4net.ILog illegalOperationLogger;
        public static log4net.ILog transactionLogger;

        private readonly string[] RoomNames;
        internal static GameConfig gameConfig;
        internal int MaxRoom = 0;
        internal bool AllowSameIP = false;
        internal string Webport = "";
        internal string WebportTls = "";
        internal string CertsFoler = "";
        internal string CertsFile = "";
        public string KeyMaxRoom = "maxRoom";
        public string KeyAllowSameIP = "allowSameIP";
        public string KeyWebport = "webport";
        public string KeyWebportTls = "webporttls";
        public string KeyCertsFoler = "certsFoler";
        public string KeyCertsFile = "certsFile";
        public string KeyIsFullDebug = "isFullDebug";
        public EmailSettings emailSettings;

        public List<YuezhanInfo> YuezhanInfos{ get; set; }
        public Dictionary<string, string> PlayerToIP;
        public Dictionary<string, OnlineBonusInfo> PlayerToOnlineBonusInfo;
        public Dictionary<string, IPlayer> PlayersProxy;
        public List<GameRoom> GameRooms { get; set; }
        public List<RoomState> RoomStates { get; set; }
        public Dictionary<string, GameRoom> SessionIDGameRoom { get; set; }
        string[] knownErrors = new string[] {
            "An existing connection was forcibly closed by the remote host",
            "The socket connection was aborted",
            "The I/O operation has been aborted"
        };
        // in milliseconds, allow for 2 seconds to perform clean up before ping again next time
        // timeout configuration NetTcpBinding_ITractorHost sendTimeout only works for non-oneway methods
        // for oneway methods, timeout is defaulted to 20 seconds
        public int PingInterval = 17000;
        public int PingTimeout = 15000;

        public System.Timers.Timer timerPingClients;
        public ConcurrentDictionary<string, System.Timers.Timer> playerIDToTimer;
        public ConcurrentDictionary<System.Timers.Timer,string> timerToPlayerID;

        public List<string> ForbidSayings;

        // 签到提醒
        public HostStatus hostStatus;

        public TractorHost()
        {
            gameConfig = new GameConfig();
            var myreader = new AppSettingsReader();
            try
            {
                MaxRoom = (int)myreader.GetValue(KeyMaxRoom, typeof(int));
                this.RoomNames = new string[MaxRoom];
                for (int i = 0; i < MaxRoom; i++)
                {
                    this.RoomNames[i] = string.Format("{0}", i + 1);
                }
                AllowSameIP = (bool)myreader.GetValue(KeyAllowSameIP, typeof(bool));
                Webport = (string)myreader.GetValue(KeyWebport, typeof(string));
                WebportTls = (string)myreader.GetValue(KeyWebportTls, typeof(string));
                CertsFoler = (string)myreader.GetValue(KeyCertsFoler, typeof(string));
                CertsFile = (string)myreader.GetValue(KeyCertsFile, typeof(string));
                gameConfig.IsFullDebug = (bool)myreader.GetValue(KeyIsFullDebug, typeof(bool));
            }
            catch (Exception ex)
            {
                log.Debug(string.Format("reading config {0} failed with exception: {1}", KeyAllowSameIP, ex.Message));
            }

            PlayerToIP = new Dictionary<string, string>();
            PlayerToOnlineBonusInfo = new Dictionary<string, OnlineBonusInfo>();
            PlayersProxy = new Dictionary<string, IPlayer>();
            YuezhanInfos = new List<YuezhanInfo>();
            GameRooms = new List<GameRoom>();
            RoomStates = new List<RoomState>();
            for (int i = 0; i < this.MaxRoom; i++)
            {
                GameRoom gameRoom = new GameRoom(i, RoomNames[i], this);
                RoomStates.Add(gameRoom.CurrentRoomState);
                GameRooms.Add(gameRoom);
                if (!Directory.Exists(gameRoom.LogsByRoomFullFolder))
                {
                    Directory.CreateDirectory(gameRoom.LogsByRoomFullFolder);
                }
            }
            SessionIDGameRoom = new Dictionary<string, GameRoom>();
            this.SetTimer();

            // initialize newly introduced fields if they are added for the first time
            this.InitNewFieldsFirstTime();

            var threadStartHostWS = new Thread(() =>
            {
                var server = new WebSocketServer("ws://0.0.0.0:" + Webport);
                server.Start(socket =>
                {
                    socket.OnOpen = () => Console.WriteLine("Open!");
                    socket.OnClose = () =>
                    {
                        Console.WriteLine("Close!");
                        var ip = socket.ConnectionInfo.ClientIpAddress;
                        string playerID = getPlayIDByWSProxy(socket);
                        if (!string.IsNullOrEmpty(playerID))
                        {
                            this.handlePlayerDisconnect(playerID);
                        }
                    };
                    socket.OnMessage = (message) =>
                    {
                        WebSocketObjects.WebSocketMessage messageObj = CommonMethods.ReadObjectFromString<WebSocketObjects.WebSocketMessage>(message);
                        if (messageObj == null)
                        {
                            log.Debug(string.Format("failed to unmarshal WS message: {0}", message));
                            return;
                        }

                        var playerProxy = new PlayerWSImpl(socket);

                        if (messageObj.messageType == WebSocketObjects.WebSocketMessageType_PlayerEnterHall)
                        {
                            if (this.PlayersProxy.ContainsKey(messageObj.playerID))
                            {
                                playerProxy.NotifyMessage(new string[] { "此账号已在别处登录", "现已将在别出登录的账号退出", "请重启游戏后再尝试进入大厅" });
                                this.handlePlayerDisconnect(messageObj.playerID);
                                return;
                            }
                            this.PlayersProxy.Add(messageObj.playerID, playerProxy);
                            ProcessOnlineBonusInfo(messageObj.playerID);
                        }

                        WSMessageHandler(messageObj.messageType, messageObj.playerID, messageObj.content);
                    };
                });
            });
            threadStartHostWS.Start();

            var threadStartHostWSS = new Thread(() =>
            {
                var server = new WebSocketServer("wss://0.0.0.0:" + WebportTls);
                server.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                string certFile = string.Format("{0}\\{1}", CertsFoler, CertsFile);
                server.Certificate = new X509Certificate2(certFile);
                server.Start(socket =>
                {
                    socket.OnOpen = () => Console.WriteLine("Open!");
                    socket.OnClose = () =>
                    {
                        Console.WriteLine("Close!");
                        var ip = socket.ConnectionInfo.ClientIpAddress;
                        string playerID = getPlayIDByWSProxy(socket);
                        if (!string.IsNullOrEmpty(playerID))
                        {
                            this.handlePlayerDisconnect(playerID);
                        }
                    };
                    socket.OnMessage = (message) =>
                    {
                        WebSocketObjects.WebSocketMessage messageObj = CommonMethods.ReadObjectFromString<WebSocketObjects.WebSocketMessage>(message);
                        if (messageObj == null)
                        {
                            log.Debug(string.Format("failed to unmarshal WS message: {0}", message));
                            return;
                        }

                        if (messageObj.messageType == WebSocketObjects.WebSocketMessageType_PlayerEnterHall)
                        {
                            var playerProxy = new PlayerWSImpl(socket);
                            string clientIP = socket.ConnectionInfo.ClientIpAddress;

                            string[] validationResult = ValidateClientInfoV3(clientIP, messageObj.playerID, messageObj.content);
                            playerProxy.NotifyMessage(validationResult);
                            // 验证失败，返回错误信息
                            if (validationResult.Length < 1 || !string.Equals(validationResult[0], CommonMethods.loginSuccessFlag, StringComparison.OrdinalIgnoreCase))
                            {
                                socket.Close();
                                return;
                            }

                            if (this.PlayersProxy.ContainsKey(messageObj.playerID))
                            {
                                playerProxy.NotifyMessage(new string[] { "此账号已在别处登录", "现已将在别出登录的账号退出", "请重启游戏后再尝试进入大厅" });
                                this.handlePlayerDisconnect(messageObj.playerID);
                                socket.Close();
                                return;
                            }
                            this.PlayersProxy.Add(messageObj.playerID, playerProxy);
                            ProcessOnlineBonusInfo(messageObj.playerID);
                        }

                        WSMessageHandler(messageObj.messageType, messageObj.playerID, messageObj.content);
                    };
                });
            });
            threadStartHostWSS.Start();
            HashSet<string> regcodes = LoadExistingRegCodes();
            GenerateRegistrationCodes(regcodes);
            LoadEmailSettings();

            string logIllegalOperationFilePath = string.Format("{0}\\{1}.log", GameRoom.LogsFolder, GameRoom.IllegalOperationLogName);
            illegalOperationLogger = LoggerUtil.Setup(GameRoom.IllegalOperationLogName, logIllegalOperationFilePath);
            LoadForbidSayings();

            string transactionLoggerFilePath = string.Format("{0}\\{1}.log", GameRoom.LogsFolder, GameRoom.TransactionLogName);
            transactionLogger = LoggerUtil.Setup(GameRoom.TransactionLogName, transactionLoggerFilePath);

            LoadHostStatus();

            // setup logger
            AppDomain currentDomain = default(AppDomain);
            currentDomain = AppDomain.CurrentDomain;
            // Handler for unhandled exceptions.
            currentDomain.FirstChanceException += GlobalFirstChanceExceptionHandler;
            // Handler for exceptions in threads behind forms.
            System.Windows.Forms.Application.ThreadException += GlobalThreadExceptionHandler;
        }

        private void ProcessOnlineBonusInfo(string playerID)
        {
            DateTime now = DateTime.Now;
            // 如果是新登录玩家，或者玩家离线已经超时，则重置在线记录
            if (!PlayerToOnlineBonusInfo.ContainsKey(playerID) || PlayerToOnlineBonusInfo[playerID].OfflineSince < now.AddMinutes(-1 * CommonMethods.OnlineBonusToleranceMunites))
            {
                if (PlayerToOnlineBonusInfo.ContainsKey(playerID))
                {
                    PlayerToOnlineBonusInfo.Remove(playerID);
                }
                this.PlayerToOnlineBonusInfo.Add(playerID, new OnlineBonusInfo());
                return;
            }
            // 否则将玩家视为一直保持在线状态
            PlayerToOnlineBonusInfo[playerID].OfflineSince = now.AddYears(1);
        }

        public void AwardOnlineBonus(string playerID)
        {
            lock (this)
            {
                DateTime now = DateTime.Now;
                // 如果玩家保持在线达到奖励要求，则颁发奖励
                if (PlayerToOnlineBonusInfo.ContainsKey(playerID) && PlayerToOnlineBonusInfo[playerID].OnlineSince < now.AddMinutes(-1 * (CommonMethods.OnlineBonusMunitesRequired - CommonMethods.OnlineBonusMunitesRequiredBuffer)))
                {
                    PlayerToOnlineBonusInfo[playerID] = new OnlineBonusInfo();

                    Dictionary<string, ClientInfoV3> clientInfoV3Dict = this.LoadClientInfoV3();
                    if (!clientInfoV3Dict.ContainsKey(playerID))
                    {
                        log.Debug(string.Format("fail to find playerID {0} for qiandao!", playerID));
                        return;
                    }
                    ClientInfoV3 curClientInfo = clientInfoV3Dict[playerID];
                    curClientInfo.transactShengbi(CommonMethods.OnlineBonusShengbi, transactionLogger, playerID, "在线奖励");
                    CommonMethods.WriteObjectToFile(clientInfoV3Dict, GameRoom.LogsFolder, GameRoom.ClientinfoV3FileName);
                    DaojuInfo daojuInfo = this.buildPlayerToShengbi(clientInfoV3Dict);
                    PublishDaojuInfo(daojuInfo);
                    UpdateGameHall();
                    string fullMsg = string.Format("玩家【{0}】保持在线【{1}】分钟，获取在线奖励：升币+{2}", playerID, CommonMethods.OnlineBonusMunitesRequired, CommonMethods.OnlineBonusShengbi);
                    this.PlayerSendEmojiWorker("", -1, -1, false, fullMsg, true, true);

                    return;
                }
                illegalOperationLogger.Debug(string.Format("玩家【{0}】获取在线奖励失败！升币【{1}】", playerID, CommonMethods.OnlineBonusShengbi));
            }
        }

        // returns if needs restart
        public bool handlePlayerDisconnect(string playerID)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                if (!gameRoom.handleWSPlayerDisconnect(playerID))
                {
                    return false;
                }
                else
                {
                    PlayerExitRoom(playerID);
                }
            }
            PlayerExitHall(playerID);
            return true;
        }

        private string getPlayIDByWSProxy(IWebSocketConnection proxy)
        {
            if (proxy != null)
            {
                foreach (KeyValuePair<string, IPlayer> entry in this.PlayersProxy)
                {
                    if (entry.Value is PlayerWSImpl && proxy.Equals(((PlayerWSImpl)entry.Value).Socket))
                    {
                        return (string)entry.Key;
                    }
                }
            }
            return "";
        }

        private void WSMessageHandler(string messageType, string playerID, string content)
        {
            switch (messageType)
            {
                case WebSocketObjects.WebSocketMessageType_PlayerEnterHall:
                    this.PlayerEnterHallWS(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_PlayerEnterRoom:
                    this.PlayerEnterRoomWS(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_ReadyToStart:
                    this.PlayerIsReadyToStart(playerID);
                    break;
                case WebSocketObjects.WebSocketMessageType_ToggleIsRobot:
                    this.PlayerToggleIsRobot(playerID);
                    break;
                case WebSocketObjects.WebSocketMessageType_ToggleIsQiangliang:
                    this.PlayerToggleIsQiangliang(playerID);
                    break;
                case WebSocketObjects.WebSocketMessageType_ObserveNext:
                    this.ObservePlayerById(content, playerID, false);
                    break;
                case WebSocketObjects.WebSocketMessageType_ExitRoom:
                    if (string.Equals(content, WebSocketObjects.ExitRoom_REQUEST_TYPE_BootPlayer, StringComparison.OrdinalIgnoreCase))
                    {
                        //玩家被房主请出房间
                        this.PlayerSendEmojiWorker(playerID, -1, -1, false, string.Format("玩家【{0}】被房主请出房间", playerID), true, false);
                    }
                    this.PlayerExitRoom(playerID);
                    break;
                case WebSocketObjects.WebSocketMessageType_ExitAndEnterRoom:
                    this.PlayerExitAndEnterRoom(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_ExitAndObserve:
                    this.PlayerExitAndObserve(playerID);
                    break;
                case WebSocketObjects.WebSocketMessageType_PlayerMakeTrump:
                    this.PlayerMakeTrumpWS(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_StoreDiscardedCards:
                    this.StoreDiscardedCardsWS(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_PlayerShowCards:
                    this.PlayerShowCardsWS(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_ValidateDumpingCards:
                    this.ValidateDumpingCardsWS(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_CardsReady:
                    this.CardsReadyWS(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_SaveRoomSetting:
                    this.SaveRoomSettingWS(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_ResumeGameFromFile:
                    new Thread(() =>
                    {
                        this.ResumeGameFromFile(playerID);
                    }).Start();
                    break;
                case WebSocketObjects.WebSocketMessageType_RandomSeat:
                    this.TeamUp(playerID);
                    break;
                case WebSocketObjects.WebSocketMessageType_SwapSeat:
                    this.SwapSeatWS(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_SendEmoji:
                    this.PlayerSendEmojiWS(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_SendBroadcast:
                    this.PlayerSendBroadcastWS(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_BuyNoDongtuUntil:
                    this.PlayerBuyNoDongtuUntilWS(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_PlayerHasCutCards:
                    new Thread(() =>
                    {
                        this.PlayerHasCutCards(playerID, content);
                    }).Start();
                    break;
                case WebSocketObjects.WebSocketMessageType_NotifyPong:
                    this.PlayerPong(playerID);
                    break;
                case WebSocketObjects.WebSocketMessageType_SgcsPlayerUpdated:
                    this.NotifySgcsPlayerUpdated(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_CreateCollectStar:
                    this.NotifyCreateCollectStar(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_UpdateGobang:
                    this.UpdateGobang(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_GrabStar:
                    this.NotifyGrabStar(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_EndCollectStar:
                    this.NotifyEndCollectStar(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_PlayerQiandao:
                    this.PlayerQiandao(playerID);
                    break;
                case WebSocketObjects.WebSocketMessageType_UsedShengbi:
                    this.UsedShengbi(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_BuyUseSkin:
                    this.BuyUseSkin(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_SendJoinOrQuitYuezhan:
                    this.PlayerSendJoinOrQuitYuezhanWS(playerID, content);
                    break;
                case WebSocketObjects.WebSocketMessageType_SendAwardOnlineBonus:
                    break;
                default:
                    break;
            }
        }

        public void PlayerSendJoinOrQuitYuezhanWS(string playerID, string content)
        {
            lock (this)
            {
                YuezhanInfo yzInfoInput = CommonMethods.ReadObjectFromString<YuezhanInfo>(content);
                if (yzInfoInput == null)
                {
                    log.Debug(string.Format("failed to unmarshal PlayerSendJoinOrQuitYuezhanWS: {0}", content));
                    return;
                }

                bool isFound = false;
                int toDeleteIndex = -1;
                for (int i = 0; i < this.YuezhanInfos.Count; i++)
                {
                    YuezhanInfo yzInfo = this.YuezhanInfos[i];
                    if (string.Equals(yzInfo.owner, yzInfoInput.owner, StringComparison.OrdinalIgnoreCase))
                    {
                        isFound = true;
                        if (yzInfo.participants.Contains(playerID))
                        {
                            yzInfo.participants.Remove(playerID);
                            log.Debug(string.Format("player {0} left yuezhan: {1}", playerID, JsonConvert.SerializeObject(yzInfo)));
                            if (yzInfo.participants.Count == 0)
                            {
                                toDeleteIndex = i;
                            }
                        }
                        else
                        {
                            yzInfo.participants.Add(playerID);
                            log.Debug(string.Format("player {0} joined yuezhan: {1}", playerID, JsonConvert.SerializeObject(yzInfo)));
                        }
                        break;
                    }
                }

                if (toDeleteIndex >= 0)
                {
                    log.Debug(string.Format("{0}'s yuezhan turned empty and was deleted: {1}", this.YuezhanInfos[toDeleteIndex].owner, JsonConvert.SerializeObject(this.YuezhanInfos[toDeleteIndex])));
                    this.YuezhanInfos.RemoveAt(toDeleteIndex);
                }

                if (!isFound)
                {
                    this.YuezhanInfos.Add(yzInfoInput);
                    log.Debug(string.Format("player {0} created yuezhan: {1}", playerID, JsonConvert.SerializeObject(yzInfoInput)));
                }
                UpdateGameHall();
            }
        }

        #region timer to ping clients
        private void SetTimer()
        {
            timerPingClients = new System.Timers.Timer(PingInterval);
            timerPingClients.Elapsed += OnTimedEvent;
            timerPingClients.AutoReset = true;
            timerPingClients.Enabled = true;

            playerIDToTimer = new ConcurrentDictionary<string, System.Timers.Timer>();
            timerToPlayerID = new ConcurrentDictionary<System.Timers.Timer, string>();
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if (TractorHost.gameConfig.IsFullDebug)
            {
                timerPingClients.Enabled = false;
                return;
            }

            // 签到提醒
            DateTime nowDateTime = DateTime.Now;
            DateTime nowDate = nowDateTime.Date;
            if (nowDate > this.hostStatus.PreviousDate)
            {
                lock (this)
                {
                    this.hostStatus.PreviousDate = nowDate;
                    CommonMethods.WriteObjectToFile(this.hostStatus, GameRoom.LogsFolder, GameRoom.HostStatusFileName);

                    this.PlayerSendEmojiWorker("", -1, -1, false, "新的一天开始啦，快去签到吧！", true, true);
                    Dictionary<string, ClientInfoV3> clientInfoV3Dict = this.LoadClientInfoV3();
                    foreach (KeyValuePair<string, ClientInfoV3> entry in clientInfoV3Dict)
                    {
                        entry.Value.ChatQuota = CommonMethods.dailyChatQuota;
                    }
                    DaojuInfo daojuInfo = this.buildPlayerToShengbi(clientInfoV3Dict);
                    PublishDaojuInfoWithSpecificPlayersStatusUpdate(daojuInfo, PlayersProxy.Keys.ToList<string>(), true, false);
                    UpdateGameHall();
                    CommonMethods.WriteObjectToFile(clientInfoV3Dict, GameRoom.LogsFolder, GameRoom.ClientinfoV3FileName);
                }
            }

            // 清理约战
            lock (this)
            {
                List<int> toDeleteIndices = new List<int>();
                for (int i = 0; i < this.YuezhanInfos.Count; i++)
                {
                    YuezhanInfo yzInfo = this.YuezhanInfos[i];
                    DateTime yzDueDate;
                    if (DateTime.TryParse(yzInfo.dueDate, out yzDueDate))
                    {
                        if (yzDueDate < nowDateTime)
                        {
                            toDeleteIndices.Add(i);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unable to parse yzInfo.dueDate: " + yzInfo.dueDate);
                    }
                }
                if (toDeleteIndices.Count > 0)
                {
                    for (int i = toDeleteIndices.Count - 1; i >= 0; i--)
                    {
                        log.Debug(string.Format("{0}'s yuezhan expired and was deleted: {1}", this.YuezhanInfos[toDeleteIndices[i]].owner, JsonConvert.SerializeObject(this.YuezhanInfos[toDeleteIndices[i]])));
                        this.YuezhanInfos.RemoveAt(toDeleteIndices[i]);
                    }
                    UpdateGameHall();
                }
            }

            foreach (KeyValuePair<string, System.Timers.Timer> entry in playerIDToTimer)
            {
                string playerID = entry.Key;
                System.Timers.Timer timerPing = entry.Value;
                timerPing.Enabled = true;
                try
                {
                    IPlayer iplayer = PlayersProxy[playerID];
                    iplayer.NotifyMessage(new string[] { });
                    if (!(iplayer is PlayerWSImpl))
                    {
                        timerPing.Enabled = false;
                    }
                }
                catch (Exception)
                {
                }

            }
        }

        private void OnPingTimeoutEvent(Object source, ElapsedEventArgs e)
        {
            System.Timers.Timer timerSource = (System.Timers.Timer)source;
            timerSource.Enabled = false;
            if (this.timerToPlayerID.ContainsKey(timerSource))
            {
                string playerID = timerToPlayerID[timerSource];
                log.Debug("timed out: " + playerID);
                handlePlayerDisconnect(playerID);
            }
        }

        public void PlayerPong(string playerID)
        {
            if (playerIDToTimer.ContainsKey(playerID))
            {
                System.Timers.Timer timer = playerIDToTimer[playerID];
                timer.Enabled = false;
            }
        }
        #endregion timer to ping clients

        private void GlobalFirstChanceExceptionHandler(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            LogAndAlert(e.Exception.Message + "\n" + e.Exception.StackTrace);
        }

        private void GlobalThreadExceptionHandler(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Exception ex = default(Exception);
            ex = e.Exception;
            LogAndAlert(ex.Message + "\n" + ex.StackTrace);
        }

        private void LogAndAlert(string errMsg)
        {
            foreach (string knownErr in knownErrors)
            {
                if (errMsg.Contains(knownErr)) return;
            }
            log.Error("===game error encountered\n" + errMsg);
            System.Console.Out.WriteLine("游戏出错，请尝试重启游戏");
        }

        #region implement interface ITractorHost
        public void PlayerEnterHall(string playerID)
        {
            string clientIP = GetClientIP();
            IPlayer player = OperationContext.Current.GetCallbackChannel<IPlayer>();
            player.NotifyMessage(new string[] { "感谢您对本游戏的支持！", "Windows版客户端现已下架", "请转至web版客户端加入游戏" });
            return;
            /*
            string[] validationResult = ValidateClientInfo(clientIP, playerID);
            if (validationResult.Length > 0)
            {
                player.NotifyMessage(validationResult);
                return;
            }

            LogClientInfo(clientIP, playerID, false);
            if (!PlayersProxy.Keys.Contains(playerID))
            {
                this.PlayerToIP.Add(playerID, clientIP);
                PlayersProxy.Add(playerID, player);
                if (this.SessionIDGameRoom.ContainsKey(playerID))
                {
                    //断线重连
                    log.Debug(string.Format("player {0} re-entered hall from offline.", playerID));
                    player.NotifyMessage(new string[] { CommonMethods.reenterRoomSignal });
                    Thread.Sleep(2000);

                    GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                    lock (gameRoom)
                    {
                        bool entered = gameRoom.PlayerReenterRoom(playerID, clientIP, player, AllowSameIP);
                        if (entered)
                        {
                            Thread.Sleep(500);
                            Thread thr = new Thread(new ThreadStart(this.UpdateGameHall));
                            thr.Start();
                        }
                    }
                }
                else
                {
                    log.Debug(string.Format("player {0} entered hall.", playerID));
                    UpdateGameHall();
                }
            }
            else if (this.PlayerToIP.ContainsValue(clientIP))
            {
                player.NotifyMessage(new string[] { "之前非正常退出", "请重启游戏后再尝试进入大厅" });
            }
            else
            {
                player.NotifyMessage(new string[] { "玩家昵称重名", "请更改昵称后重试" });
            }
            */
        }

        public void PlayerEnterHallWS(string playerID, string content)
        {
            string clientIP = ((PlayerWSImpl)this.PlayersProxy[playerID]).Socket.ConnectionInfo.ClientIpAddress;
            string cheating = string.Empty;
            List<string> playerIDsWithSameIP = OtherPlayerIDsWithSameIP(playerID, clientIP);
            if (playerIDsWithSameIP.Count > 0)
            {
                cheating = string.Format("player {0} (with other ID {1}) attempted login with IP {2}", playerID, string.Join("; ", playerIDsWithSameIP), clientIP);
            }
            EnterHallInfo enterHallInfo = CommonMethods.ReadObjectFromString<EnterHallInfo>(content);

            DaojuInfo daojuInfo = LogClientInfo(clientIP, playerID, cheating, true, enterHallInfo.clientType);
            Thread.Sleep(500);
            IPlayer player = this.PlayersProxy[playerID];
            this.PlayerToIP[playerID] = clientIP;
            if (this.SessionIDGameRoom.ContainsKey(playerID))
            {
                //断线重连
                string[] reenterMsgs = new string[] { string.Format("玩家【{0}】{1}", playerID, CommonMethods.reenterRoomSignal) };
                player.NotifyMessage(reenterMsgs);
                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                gameRoom.PublishMessage(reenterMsgs);
                Thread.Sleep(2000);

                lock (gameRoom)
                {
                    bool entered = gameRoom.PlayerReenterRoom(playerID, clientIP, player, AllowSameIP);
                    if (entered)
                    {
                        log.Debug(string.Format("player {0} re-entered hall and room {1} from offline as {2}", playerID, gameRoom.CurrentRoomState.RoomID, enterHallInfo.clientType));
                        new Thread(new ThreadStart(() =>
                        {
                            Thread.Sleep(500);
                            this.UpdateGameHall();
                            Thread.Sleep(1000);
                            this.UpdateGameRoomPlayerList(playerID, true, gameRoom.CurrentRoomState.roomSetting.RoomName);
                        })).Start();
                    }
                    else
                    {
                        log.Debug(string.Format("player {0} attempted to re-entered hall and room {1} from offline as {2} but failed.", playerID, gameRoom.CurrentRoomState.RoomID, gameRoom.CurrentRoomState.RoomID));
                    }
                }
            }
            else
            {
                log.Debug(string.Format("player {0} entered hall as {1}", playerID, enterHallInfo.clientType));
                if (daojuInfo != null)
                {
                    PublishDaojuInfoWithSpecificPlayersStatusUpdate(daojuInfo, new List<string> { playerID }, true, true);
                }
            }

            new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(250);
                UpdateGameHall();
                Thread.Sleep(250);
                this.UpdateOnlinePlayerList(playerID, true);
            })).Start();

            new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(PingInterval);
                this.InitTimerForIPlayer(playerID);
            })).Start();
        }

        public void PlayerEnterRoom(string playerID, int roomID, int posID)
        {
            if (!PlayersProxy.ContainsKey(playerID)) return;
            string clientIP = PlayerToIP[playerID];
            IPlayer player = PlayersProxy[playerID];
            if (player != null)
            {
                GameRoom gameRoom = this.GameRooms.Single((room) => room.CurrentRoomState.RoomID == roomID);
                if (gameRoom == null)
                {
                    player.NotifyMessage(new string[] { "加入房间失败", string.Format("房间号【{0}】不存在", roomID) });
                }
                else
                {
                    lock (gameRoom)
                    {
                        bool entered = gameRoom.PlayerEnterRoom(playerID, clientIP, player, AllowSameIP, posID, false);
                        if (entered)
                        {
                            SessionIDGameRoom[playerID] = gameRoom;
                            new Thread(new ThreadStart(() =>
                            {
                                Thread.Sleep(500);
                                this.UpdateGameHall();
                            })).Start();
                        }
                    }
                }
            }
            else
            {
                log.Debug(string.Format("PlayerEnterRoom failed, playerID: {0}, roomID: {1}", playerID, roomID));
            }
        }

        private void PlayerEnterRoomWS(string playerID, string content)
        {
            WebSocketObjects.PlayerEnterRoomWSMessage messageObj = CommonMethods.ReadObjectFromString<WebSocketObjects.PlayerEnterRoomWSMessage>(content);
            if (messageObj == null)
            {
                log.Debug(string.Format("failed to unmarshal PlayerEnterRoomWSMessage: {0}", content));
                return;
            }
            if (!PlayersProxy.ContainsKey(playerID)) return;
            IPlayer player = PlayersProxy[playerID];
            if (player == null)
            {
                log.Debug(string.Format("PlayerEnterRoom failed, playerID: {0}, roomID: {1}", playerID, messageObj.roomID));
                return;
            }

            GameRoom gameRoom = this.GameRooms.Single((room) => room.CurrentRoomState.RoomID == messageObj.roomID);
            if (gameRoom == null)
            {
                player.NotifyMessage(new string[] { "加入房间失败", string.Format("房间号【{0}】不存在", messageObj.roomID) });
                return;
            }
            lock (gameRoom)
            {
                string clientIP = PlayerToIP[playerID];
                bool entered = gameRoom.PlayerEnterRoom(playerID, clientIP, player, AllowSameIP, messageObj.posID, false);
                if (entered)
                {
                    SessionIDGameRoom[playerID] = gameRoom;
                    log.Debug(string.Format("player {0} entered room {1}", playerID, gameRoom.CurrentRoomState.RoomID));
                }
                else
                {
                    log.Debug(string.Format("player {0} attempted to enter room {1} but failed.", playerID, gameRoom.CurrentRoomState.RoomID));
                }
                new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(500);
                    this.UpdateGameHall();
                    this.UpdateGameRoomPlayerList(playerID, true, gameRoom.CurrentRoomState.roomSetting.RoomName);
                })).Start();
            }
        }

        //玩家强退
        public void MarkPlayerOffline(string playerID)
        {
            if (!this.SessionIDGameRoom.ContainsKey(playerID)) return;

            GameRoom gameRoom = this.SessionIDGameRoom[playerID];
            List<string> quitPlayers = new List<string>() { playerID };

            gameRoom.PlayerQuitFromCleanup(quitPlayers);
        }

        //玩家退出房间
        public void PlayerExitRoom(string playerID)
        {
            lock (this)
            {
                if (!this.SessionIDGameRoom.ContainsKey(playerID)) return;

                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                //如果退出的是正常玩家，先记录旁观玩家和离线玩家
                List<string> obs = new List<string>();
                List<string> obsToMove = new List<string>();
                List<string> quitPlayers = new List<string>() { playerID };
                PlayerEntity firstActualPlayer = null;
                if (gameRoom.IsActualPlayer(playerID))
                {
                    obs = gameRoom.ObserversProxy.Keys.ToList<string>();
                    for (int i = 0; i < 4; i++)
                    {
                        PlayerEntity player = gameRoom.CurrentRoomState.CurrentGameState.Players[i];
                        if (player == null)
                        {
                            continue;
                        }
                        if (player.PlayerId != playerID && !player.IsOffline && firstActualPlayer == null)
                        {
                            firstActualPlayer = player;
                        }
                        if (player.IsOffline)
                        {
                            quitPlayers.Add(player.PlayerId);
                            if (player.Observers.Count > 0)
                            {
                                obsToMove.AddRange(player.Observers);
                            }
                        }
                        else if (player != null && !PlayersProxy.ContainsKey(player.PlayerId) && !obs.Contains(player.PlayerId) && !quitPlayers.Contains(player.PlayerId))
                        {
                            obs.Add(player.PlayerId);
                        }
                        if (player.PlayerId == playerID && player.Observers.Count > 0)
                        {
                            obsToMove.AddRange(player.Observers);
                        }
                    }
                }

                //先将退出的玩家和离线玩家移出房间
                gameRoom.PlayerQuit(quitPlayers);
                foreach (string qp in quitPlayers)
                {
                    SessionIDGameRoom.Remove(qp);
                    this.UpdateGameRoomPlayerList(qp, false, gameRoom.CurrentRoomState.roomSetting.RoomName);
                }

                //再将旁观玩家移出房间，这样旁观玩家才能得到最新的handstep的更新
                if (firstActualPlayer == null)
                {
                    gameRoom.PlayerQuit(obs);
                    foreach (string ob in obs)
                    {
                        SessionIDGameRoom.Remove(ob);
                        this.UpdateGameRoomPlayerList(ob, false, gameRoom.CurrentRoomState.roomSetting.RoomName);
                    }
                }
                else
                {
                    foreach (string ob in obsToMove)
                    {
                        this.ObservePlayerById(firstActualPlayer.PlayerId, ob, true);
                    }
                }
                gameRoom.UpdateGameState();
                gameRoom.UpdatePlayersCurrentHandState();

                log.Debug(string.Format("player {0} exited room.", playerID));
                new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(500);
                    this.UpdateGameHall();
                })).Start();
            }
        }

        //玩家已在房间里，直接从旁观加入游戏
        public void PlayerExitAndEnterRoom(string playerID, string content)
        {
            WebSocketObjects.PlayerEnterRoomWSMessage messageObj = CommonMethods.ReadObjectFromString<WebSocketObjects.PlayerEnterRoomWSMessage>(content);
            if (messageObj == null)
            {
                log.Debug(string.Format("failed to unmarshal PlayerEnterRoomWSMessage: {0}", content));
                return;
            }
            if (!PlayersProxy.ContainsKey(playerID)) return;
            IPlayer player = PlayersProxy[playerID];
            if (player == null)
            {
                log.Debug(string.Format("PlayerExitAndEnterRoom failed, playerID: {0}", playerID));
                return;
            }

            lock (this)
            {
                if (!this.SessionIDGameRoom.ContainsKey(playerID)) return;

                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                bool isFromObserver = gameRoom.ObserversProxy.ContainsKey(playerID);
                string clientIP = PlayerToIP[playerID];
                if (gameRoom.PlayerExitAndEnterRoom(playerID, clientIP, player, AllowSameIP, messageObj.posID))
                {
                    log.Debug(string.Format("player {0} took seat {1} successfully in room {2}", playerID, messageObj.posID + 1, gameRoom.CurrentRoomState.RoomID));
                }
                else
                {
                    log.Debug(string.Format("player {0} attempted to take seat {1} in room {2} but failed.", playerID, messageObj.posID + 1, gameRoom.CurrentRoomState.RoomID));
                }
                new Thread(new ThreadStart(() =>
                    {
                        Thread.Sleep(500);
                        this.UpdateGameHall();
                        if (isFromObserver) this.UpdateGameRoomPlayerList(playerID, true, gameRoom.CurrentRoomState.roomSetting.RoomName);
                    })).Start();
            }
        }

        //玩家已在房间里，直接离开座位上树
        public void PlayerExitAndObserve(string playerID)
        {
            lock (this)
            {
                if (!this.SessionIDGameRoom.ContainsKey(playerID)) return;

                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                //如果退出的是正常玩家，先记录旁观玩家和离线玩家
                List<string> obs = new List<string>();
                List<string> obsToMove = new List<string>();
                List<string> quitPlayers = new List<string>() { playerID };
                PlayerEntity firstActualPlayer = null;
                if (gameRoom.IsActualPlayer(playerID))
                {
                    obs = gameRoom.ObserversProxy.Keys.ToList<string>();
                    for (int i = 0; i < 4; i++)
                    {
                        PlayerEntity player = gameRoom.CurrentRoomState.CurrentGameState.Players[i];
                        if (player == null)
                        {
                            continue;
                        }
                        if (player.PlayerId != playerID && !player.IsOffline && firstActualPlayer == null)
                        {
                            firstActualPlayer = player;
                        }
                        if (player.IsOffline)
                        {
                            quitPlayers.Add(player.PlayerId);
                            if (player.Observers.Count > 0)
                            {
                                obsToMove.AddRange(player.Observers);
                            }
                        }
                        else if (player != null && !PlayersProxy.ContainsKey(player.PlayerId) && !obs.Contains(player.PlayerId) && !quitPlayers.Contains(player.PlayerId))
                        {
                            obs.Add(player.PlayerId);
                        }
                        if (player.PlayerId == playerID && player.Observers.Count > 0)
                        {
                            obsToMove.AddRange(player.Observers);
                        }
                    }
                }

                //先将退出的玩家和离线玩家移出房间
                gameRoom.PlayerQuit(quitPlayers);

                //再将旁观玩家移出房间，这样旁观玩家才能得到最新的handstep的更新
                if (firstActualPlayer == null)
                {
                    foreach (string qp in quitPlayers)
                    {
                        SessionIDGameRoom.Remove(qp);
                        this.UpdateGameRoomPlayerList(qp, false, gameRoom.CurrentRoomState.roomSetting.RoomName);
                    }
                    gameRoom.PlayerQuit(obs);
                    foreach (string ob in obs)
                    {
                        SessionIDGameRoom.Remove(ob);
                        this.UpdateGameRoomPlayerList(ob, false, gameRoom.CurrentRoomState.roomSetting.RoomName);
                    }
                }
                else
                {
                    foreach (string ob in obsToMove)
                    {
                        this.ObservePlayerById(firstActualPlayer.PlayerId, ob, true);
                    }
                }

                if (PlayersProxy.ContainsKey(playerID))
                {
                    IPlayer player = PlayersProxy[playerID];
                    if (player != null)
                    {
                        string clientIP = PlayerToIP[playerID];
                        if (gameRoom.PlayerEnterRoom(playerID, clientIP, player, AllowSameIP, -1, true))
                        {
                            log.Debug(string.Format("player {0} exited room {1} and rejoined as observer.", playerID, gameRoom.CurrentRoomState.RoomID));
                        }
                        else
                        {
                            log.Debug(string.Format("player {0} exited room {1} but failed to rejoin as observer.", playerID, gameRoom.CurrentRoomState.RoomID));
                        }
                    }
                }
                gameRoom.UpdateGameState();
                gameRoom.UpdatePlayersCurrentHandState();

                new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(500);
                    this.UpdateGameHall();
                })).Start();
            }
        }

        //玩家退出游戏
        public IAsyncResult BeginPlayerQuit(string playerID, AsyncCallback callback, object state)
        {
            //if (!this.handlePlayerDisconnect(playerID)) return new CompletedAsyncResult<string>(string.Format("BeginPlayerQuit: player {0} marked offline and return", playerID));
            return new CompletedAsyncResult<string>(string.Format("BeginPlayerQuit: player {0} quit and return", playerID));
        }

        private string PlayerExitHall(string playerID)
        {
            CleanupIPlayer(playerID);
            string result = string.Format("player {0} quit.", playerID);
            log.Debug(result);
            new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(500);
                this.UpdateGameHall();
            })).Start();
            return result;
        }

        private void PlayerQiandao(string playerID)
        {
            lock (this)
            {
                Dictionary<string, ClientInfoV3> clientInfoV3Dict = this.LoadClientInfoV3();
                if (!clientInfoV3Dict.ContainsKey(playerID))
                {
                    log.Debug(string.Format("fail to find playerID {0} for qiandao!", playerID));
                    return;
                }
                ClientInfoV3 curClientInfo = clientInfoV3Dict[playerID];
                bool isSuccess = curClientInfo.performQiandao(transactionLogger, playerID);
                CommonMethods.WriteObjectToFile(clientInfoV3Dict, GameRoom.LogsFolder, GameRoom.ClientinfoV3FileName);
                if (isSuccess)
                {
                    DaojuInfo daojuInfo = this.buildPlayerToShengbi(clientInfoV3Dict);
                    List<string> others = PlayersProxy.Keys.ToList<string>();
                    others.Remove(playerID);
                    PublishDaojuInfoWithSpecificPlayersStatusUpdate(daojuInfo, others, false, false);
                    PublishDaojuInfoWithSpecificPlayersStatusUpdate(daojuInfo, new List<string> { playerID }, true, false);
                    UpdateGameHall();
                    string fullMsg = string.Format("玩家【{0}】签到成功，获得福利：升币+{1}", playerID, CommonMethods.qiandaoBonusShengbi);
                    this.PlayerSendEmojiWorker("", -1, -1, false, fullMsg, true, true);
                }
                else
                {
                    illegalOperationLogger.Debug(string.Format("玩家【{0}】签到失败！升币【{1}】", playerID, curClientInfo.Shengbi));
                }
            }
        }

        public void UsedShengbi(string playerID, string content)
        {
            lock (this)
            {
                int shengbiCost = 0;
                switch (content)
                {
                    case CommonMethods.usedShengbiType_Qiangliangka:
                        shengbiCost = CommonMethods.qiangliangkaCost;
                        break;
                    default:
                        return;
                }
                Dictionary<string, ClientInfoV3> clientInfoV3Dict = this.LoadClientInfoV3();
                if (!clientInfoV3Dict.ContainsKey(playerID))
                {
                    log.Debug(string.Format("fail to find playerID {0} for UsedShengbi!", playerID));
                    return;
                }
                if (clientInfoV3Dict[playerID].Shengbi < shengbiCost)
                {
                    log.Debug(string.Format("fail to UsedShengbi for player {0} due to insufficient shengbi: cost: {1}, shengbi: {2}!", playerID, shengbiCost, clientInfoV3Dict[playerID].Shengbi));
                    string warningMsg = string.Format("玩家【{0}】使用道具【抢亮卡】出现异常，游戏已终止", playerID);
                    this.PlayerSendEmojiWorker(playerID, -1, -1, false, warningMsg, true, false);
                    this.PlayerExitRoom(playerID);
                    return;
                }
                clientInfoV3Dict[playerID].transactShengbi(-shengbiCost, transactionLogger, playerID, string.Format("使用道具【{0}】", content));
                CommonMethods.WriteObjectToFile(clientInfoV3Dict, GameRoom.LogsFolder, GameRoom.ClientinfoV3FileName);
                DaojuInfo daojuInfo = this.buildPlayerToShengbi(clientInfoV3Dict);
                this.PublishDaojuInfo(daojuInfo);
                UpdateGameHall();

                string msg = string.Format("玩家【{0}】成功触发道具效果【{1}】，消耗升币：【{2}】", playerID, CommonMethods.daojuToDisplayName[content], shengbiCost);
                this.PlayerSendEmojiWorker(playerID, -1, -1, false, msg, true, false);
            }
        }

        private void BuyUseSkin(string playerID, string content)
        {
            lock (this)
            {
                string skinName = content;
                Dictionary<string, ClientInfoV3> clientInfoV3Dict = this.LoadClientInfoV3();
                if (!clientInfoV3Dict.ContainsKey(playerID))
                {
                    log.Debug(string.Format("fail to find playerID {0} for BuyUseSkin!", playerID));
                    return;
                }
                Dictionary<string, SkinInfo> fullSkinInfo = this.LoadSkinInfo(null);
                if (!fullSkinInfo.ContainsKey(skinName))
                {
                    log.Debug(string.Format("fail to find skin {0} for BuyUseSkin!", skinName));
                    return;
                }

                ClientInfoV3 curClientInfo = clientInfoV3Dict[playerID];
                SkinInfo skinInfoToBuyUse = fullSkinInfo[skinName];
                bool isBuy = false;
                if (!curClientInfo.ownedSkinInfo.Contains(skinName))
                {
                    isBuy = true;
                    if (curClientInfo.Shengbi < skinInfoToBuyUse.skinCost)
                    {
                        log.Debug(string.Format("fail to buy skin {0} for BuyUseSkin due to insufficient shengbi: cost: {1}, shengbi: {2}", skinName, skinInfoToBuyUse.skinCost, curClientInfo.Shengbi));
                        return;
                    }
                    curClientInfo.transactShengbi(-skinInfoToBuyUse.skinCost, transactionLogger, playerID, string.Format("购买皮肤【{0}】", skinInfoToBuyUse.skinDesc));
                    curClientInfo.ownedSkinInfo.Add(skinName);
                }

                curClientInfo.skinInUse = skinName;

                CommonMethods.WriteObjectToFile(clientInfoV3Dict, GameRoom.LogsFolder, GameRoom.ClientinfoV3FileName);
                DaojuInfo daojuInfo = this.buildPlayerToShengbi(clientInfoV3Dict);
                PublishDaojuInfoWithSpecificPlayersStatusUpdate(daojuInfo, PlayersProxy.Keys.ToList<string>(), false, true);
                UpdateGameHall();
                if (isBuy)
                {
                    string msg = string.Format("玩家【{0}】成功解锁新皮肤【{1}】，消耗升币：【{2}】", playerID, skinInfoToBuyUse.skinDesc, skinInfoToBuyUse.skinCost);
                    this.PlayerSendEmojiWorker("", -1, -1, false, msg, true, true);
                }
            }
        }

        public DaojuInfo buildPlayerToShengbi(Dictionary<string, ClientInfoV3> clientInfoV3Dict)
        {
            Dictionary<string, SkinInfo> fullSkinInfo = this.LoadSkinInfo(clientInfoV3Dict);
            Dictionary<string, int> shengbiLeadingBoard = this.BuildshengbiLeadingBoard(clientInfoV3Dict);
            List<string> names = PlayersProxy.Keys.ToList<string>();

            // add offline players
            foreach (GameRoom gameRoom in this.GameRooms)
            {
                foreach (PlayerEntity player in gameRoom.CurrentRoomState.CurrentGameState.Players)
                {
                    if (player != null && player.IsOffline && !names.Contains(player.PlayerId))
                    {
                        names.Add(player.PlayerId);
                    }
                }
            }

            DaojuInfo daojuInfo = new DaojuInfo(fullSkinInfo, shengbiLeadingBoard, new Dictionary<string, DaojuInfoByPlayer>());
            foreach (string pid in names)
            {
                if (!clientInfoV3Dict.ContainsKey(pid)) continue;
                DateTime onlineSince = DateTime.Now;
                if (this.PlayerToOnlineBonusInfo.ContainsKey(pid))
                {
                    onlineSince = this.PlayerToOnlineBonusInfo[pid].OnlineSince;
                }
                daojuInfo.daojuInfoByPlayer.Add(pid,
                    new DaojuInfoByPlayer(clientInfoV3Dict[pid].Shengbi,
                    clientInfoV3Dict[pid].ShengbiTotal,
                    clientInfoV3Dict[pid].lastQiandao,
                    clientInfoV3Dict[pid].ownedSkinInfo,
                    clientInfoV3Dict[pid].skinInUse,
                    clientInfoV3Dict[pid].clientType,
                    clientInfoV3Dict[pid].noDongtuUntil,
                    clientInfoV3Dict[pid].noChatUntil,
                    clientInfoV3Dict[pid].ChatQuota,
                    onlineSince));
            }
            return daojuInfo;
        }

        private Dictionary<string, int> BuildshengbiLeadingBoard(Dictionary<string, ClientInfoV3> clientInfoV3Dict)
        {
            Dictionary<string, int> lb = new Dictionary<string, int>();
            foreach (KeyValuePair< string, ClientInfoV3>  entry in clientInfoV3Dict)
            {
                lb.Add(entry.Key, entry.Value.Shengbi);
            }
            return lb;
        }

        // perform timer init for IPlayer
        private void InitTimerForIPlayer(string playerID)
        {
            if (!PlayersProxy.ContainsKey(playerID)) return;
            System.Timers.Timer timer = new System.Timers.Timer(PingTimeout);
            timer.Elapsed += OnPingTimeoutEvent;
            timer.AutoReset = true;
            playerIDToTimer[playerID] = timer;
            timerToPlayerID[timer] = playerID;
        }

        // perform clean up IPlayer
        public void CleanupIPlayer(string playerID)
        {
            if (playerIDToTimer.ContainsKey(playerID))
            {
                System.Timers.Timer timer = playerIDToTimer[playerID];
                if (!playerIDToTimer.TryRemove(playerID, out timer))
                {
                    log.Debug("concurrence removing playerIDToTimer failed with key: " + playerID);
                }
                string temp = "";
                timerToPlayerID.TryRemove(timer, out temp);
            }
            PlayerToIP.Remove(playerID);
            if (PlayerToOnlineBonusInfo.ContainsKey(playerID))
            {
                PlayerToOnlineBonusInfo[playerID].OfflineSince = DateTime.Now;
            }
            if (PlayersProxy.ContainsKey(playerID))
            {
                PlayersProxy[playerID].Close();
                PlayersProxy.Remove(playerID);
            }
            string roomName = "";
            if (this.SessionIDGameRoom.ContainsKey(playerID))
            {
                roomName = this.SessionIDGameRoom[playerID].CurrentRoomState.roomSetting.RoomName;
            }
            this.UpdateGameRoomPlayerList(playerID, false, roomName);
            this.UpdateOnlinePlayerList(playerID, false);
        }

        public string EndPlayerQuit(IAsyncResult ar)
        {
            CompletedAsyncResult<string> result = ar as CompletedAsyncResult<string>;
            log.Debug(string.Format("EndServiceAsyncMethod called with: \"{0}\"", result.Data));
            return result.Data;
        }

        //尝试检测服务器是否在线
        public IAsyncResult BeginPingHost(string playerID, AsyncCallback callback, object state)
        {
            string result = string.Format("player {0} pinged host.", playerID);
            log.Debug(result);
            return new CompletedAsyncResult<string>(result);
        }
        public string EndPingHost(IAsyncResult ar)
        {
            CompletedAsyncResult<string> result = ar as CompletedAsyncResult<string>;
            log.Debug(string.Format("EndServiceAsyncMethod called with: \"{0}\"", result.Data));
            return result.Data;
        }

        //玩家发起投降提议
        public void SpecialEndGameRequest(string playerID)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                gameRoom.SpecialEndGameRequest(playerID);
            }
            log.Debug(string.Format("player {0} requested surrender.", playerID));
        }

        //玩家投降
        public void SpecialEndGame(string playerID, SpecialEndingType endType)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                gameRoom.SpecialEndGame(playerID, endType);
            }
            log.Debug(string.Format("player {0} surrendered.", playerID));
        }

        //玩家拒绝投降提议
        public void SpecialEndGameDeclined(string playerID)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                gameRoom.SpecialEndGameDeclined(playerID);
            }
            log.Debug(string.Format("player {0} declined surrender.", playerID));
        }

        public void PlayerIsReadyToStart(string playerID)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                gameRoom.PlayerIsReadyToStart(playerID);
            }
        }

        public void PlayerHasCutCards(string playerID, string cutInfo)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                gameRoom.PlayerHasCutCards(cutInfo);
            }
        }

        public void PlayerToggleIsRobot(string playerID)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                gameRoom.PlayerToggleIsRobot(playerID);
            }
        }

        public void PlayerToggleIsQiangliang(string playerID)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                gameRoom.PlayerToggleIsQiangliang(playerID);
            }
        }

        public void PlayerSendEmoji(string playerID, int emojiType, int emojiIndex, bool isCenter)
        {
            PlayerSendEmojiWorker(playerID, emojiType, emojiIndex, isCenter, "", false, false);
        }

        public void PlayerSendEmojiWS(string playerID, string content)
        {
            List<object> args = CommonMethods.ReadObjectFromString<List<object>>(content);

            int emojiType = (int)(long)args[0];
            int emojiIndex = (int)(long)args[1];
            string emojiString = (string)args[2];

            if (!this.ValidateChatMessage(playerID, emojiString, emojiType >= 0 ? 0 : CommonMethods.sendChatMessageCost)) return;

            bool isCenter = false;
            if (args.Count >= 4) isCenter = (bool)args[3];
            this.PlayerSendEmojiWorker(playerID, emojiType, emojiIndex, isCenter, emojiString, false, false);
        }

        public void PlayerSendBroadcastWS(string playerID, string content)
        {
            if (!this.ValidateChatMessage(playerID, content, CommonMethods.sendBroadcastCost)) return;

            string message = string.Format("来自玩家【{0}】的广播消息：{1}", playerID, content);
            this.PlayerSendEmojiWorker(playerID, -1, -1, false, message, false, true);
        }

        private bool ValidateChatMessage(string playerID, string chatMsg, int cost)
        {
            lock (this)
            {
                Dictionary<string, ClientInfoV3> clientInfoV3Dict = this.LoadClientInfoV3();
                if (!clientInfoV3Dict.ContainsKey(playerID))
                {
                    log.Debug(string.Format("fail to find playerID {0} for PlayerSendBroadcastWS!", playerID));
                    return false;
                }

                DateTime dtNow = DateTime.Now;
                if (dtNow < clientInfoV3Dict[playerID].noChatUntil)
                {
                    this.illegalOperationLogger.Debug(string.Format("玩家【{0}】处于禁言中，试图发言：{1}", playerID, chatMsg));
                    return false;
                }

                if (cost > 0 && clientInfoV3Dict[playerID].ChatQuota + clientInfoV3Dict[playerID].Shengbi < cost)
                {
                    illegalOperationLogger.Debug(string.Format("玩家【{0}】余额不足，cost: {1}, shengbi: {2}，试图发言：{3}", playerID, cost, clientInfoV3Dict[playerID].Shengbi, chatMsg));
                    return false;
                }

                if (cost > 0)
                {
                    int remainingFee = cost;
                    if (clientInfoV3Dict[playerID].ChatQuota > 0)
                    {
                        int oldCQ = clientInfoV3Dict[playerID].ChatQuota;
                        int feeCoveredByQuota;
                        if (clientInfoV3Dict[playerID].ChatQuota >= remainingFee)
                        {
                            feeCoveredByQuota = remainingFee;
                            clientInfoV3Dict[playerID].ChatQuota -= remainingFee;
                            remainingFee = 0;
                        }
                        else
                        {
                            feeCoveredByQuota = clientInfoV3Dict[playerID].ChatQuota;
                            remainingFee -= clientInfoV3Dict[playerID].ChatQuota;
                            clientInfoV3Dict[playerID].ChatQuota = 0;
                        }
                        transactionLogger.Debug(string.Format("聊天卡交易记录：玩家：【{0}】，类型：【{1}】，金额：【{2}】，之前【{3}】，之后【{4}】", playerID, CommonMethods.transactionNameChat, -feeCoveredByQuota, oldCQ, clientInfoV3Dict[playerID].ChatQuota));
                    }

                    if (remainingFee > 0)
                    {
                        clientInfoV3Dict[playerID].transactShengbi(-remainingFee, transactionLogger, playerID, CommonMethods.transactionNameChat);
                    }

                    CommonMethods.WriteObjectToFile(clientInfoV3Dict, GameRoom.LogsFolder, GameRoom.ClientinfoV3FileName);
                    DaojuInfo daojuInfo = this.buildPlayerToShengbi(clientInfoV3Dict);
                    this.PublishDaojuInfo(daojuInfo);
                    UpdateGameHall();
                }
            }
            return true;
        }

        public void PlayerBuyNoDongtuUntilWS(string playerID, string content)
        {
            lock (this)
            {
                Dictionary<string, ClientInfoV3> clientInfoV3Dict = this.LoadClientInfoV3();
                if (!clientInfoV3Dict.ContainsKey(playerID))
                {
                    log.Debug(string.Format("fail to find playerID {0} for PlayerBuyNoDongtuUntilWS!", playerID));
                    return;
                }
                if (clientInfoV3Dict[playerID].Shengbi < CommonMethods.buyNoDongtuUntilCost)
                {
                    log.Debug(string.Format("fail to PlayerBuyNoDongtuUntilWS for player {0} due to insufficient shengbi: cost: {1}, shengbi: {2}", playerID, CommonMethods.buyNoDongtuUntilCost, clientInfoV3Dict[playerID].Shengbi));
                    return;
                }

                clientInfoV3Dict[playerID].transactShengbi(-CommonMethods.buyNoDongtuUntilCost, transactionLogger, playerID, string.Format("使用道具【关闭动图】"));
                clientInfoV3Dict[playerID].noDongtuUntil = DateTime.Now.AddHours(CommonMethods.buyNoDongtuUntilDurationHours);
                CommonMethods.WriteObjectToFile(clientInfoV3Dict, GameRoom.LogsFolder, GameRoom.ClientinfoV3FileName);
                DaojuInfo daojuInfo = this.buildPlayerToShengbi(clientInfoV3Dict);
                PublishDaojuInfoWithSpecificPlayersStatusUpdate(daojuInfo, new List<string> { playerID }, false, true);
                this.PublishDaojuInfo(daojuInfo);
                UpdateGameHall();

                string msg = string.Format("玩家【{0}】成功购买道具【关闭动图】，消耗升币：【{1}】", playerID, CommonMethods.buyNoDongtuUntilCost);
                this.PlayerSendEmojiWorker("", -1, -1, false, msg, true, true);
            }
        }

        public void PlayerSendEmojiWorker(string playerID, int emojiType, int emojiIndex, bool isCenter, string msgString, bool noSpeaker, bool isBroadcast)
        {
            if (!string.IsNullOrEmpty(msgString))
            {
                string logPrefix = noSpeaker ? CommonMethods.systemMsgPrefix : string.Format("【{0}】说：", playerID);
                log.Debug(string.Format("{0}{1}", logPrefix, msgString));
            }

            if (isBroadcast)
            {
                IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "NotifyEmoji", new List<object>() { playerID, emojiType, emojiIndex, isCenter, msgString, false });
                return;
            }
            bool isPlayerInGameHall = this.IsInGameHall(playerID);
            if (isPlayerInGameHall)
            {
                isCenter = false;
                List<string> playersToCall = this.getPlayersInGameHall();
                IPlayerInvokeForAll(PlayersProxy, playersToCall, "NotifyEmoji", new List<object>() { playerID, emojiType, emojiIndex, isCenter, msgString, noSpeaker });
            }
            else if (this.SessionIDGameRoom.ContainsKey(playerID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                gameRoom.PublishEmoji(playerID, emojiType, emojiIndex, isCenter, msgString, noSpeaker);
            }
        }

        private List<string> getPlayersInGameHall()
        {
            List<string> playersInGameHall = new List<string>();
            lock (PlayersProxy)
            {
                List<string> names = PlayersProxy.Keys.ToList<string>();
                foreach (var name in names)
                {
                    bool isInRoom = false;
                    foreach (GameRoom room in this.GameRooms)
                    {
                        if (!PlayersProxy.ContainsKey(name)) continue;
                        if (room.PlayersProxy.ContainsValue(PlayersProxy[name]) ||
                            room.ObserversProxy.ContainsValue(PlayersProxy[name]))
                        {
                            isInRoom = true;
                            break;
                        }
                    }

                    if (!isInRoom)
                    {
                        playersInGameHall.Add(name);
                    }
                }
            }
            return playersInGameHall;
        }

        //player discard last 8 cards
        public void StoreDiscardedCards(string playerId, int[] cards)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerId))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerId];
                gameRoom.StoreDiscardedCards(cards);
            }
        }

        //player discard last 8 cards
        public void StoreDiscardedCardsWS(string playerId, string content)
        {
            int[] messageObj = CommonMethods.ReadObjectFromString<int[]>(content);
            this.StoreDiscardedCards(playerId, messageObj);
        }

        //亮主
        public void PlayerMakeTrump(Duan.Xiugang.Tractor.Objects.TrumpExposingPoker trumpExposingPoker, Duan.Xiugang.Tractor.Objects.Suit trump, string trumpMaker, int usedShengbi)
        {
            if (this.SessionIDGameRoom.ContainsKey(trumpMaker))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[trumpMaker];
                gameRoom.PlayerMakeTrump(trumpExposingPoker, trump, trumpMaker, usedShengbi);
            }
        }
        public void PlayerMakeTrumpWS(string trumpMaker, string content)
        {
            List<int> messageObj = CommonMethods.ReadObjectFromString<List<int>>(content);
            this.PlayerMakeTrump((TrumpExposingPoker)messageObj[0], (Suit)messageObj[1], trumpMaker, messageObj[2]);
        }

        public void PlayerShowCards(string playerId, CurrentTrickState currentTrickState)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerId))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerId];
                gameRoom.PlayerShowCards(currentTrickState);
            }
        }

        public void PlayerShowCardsWS(string playerId, string content)
        {
            CurrentTrickState currentTrickState = CommonMethods.ReadObjectFromString<CurrentTrickState>(content);
            this.PlayerShowCards(playerId, currentTrickState);
        }

        public void ValidateDumpingCards(List<int> selectedCards, string playerId)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerId))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerId];
                gameRoom.ValidateDumpingCards(selectedCards, playerId);
            }
        }

        public void ValidateDumpingCardsWS(string playerId, string content)
        {
            List<int> selectedCards = CommonMethods.ReadObjectFromString<List<int>>(content);
            this.ValidateDumpingCards(selectedCards, playerId);
        }

        public void RefreshPlayersCurrentHandState(string playerId)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerId))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerId];
                gameRoom.RefreshPlayersCurrentHandState();
            }
        }

        //随机组队
        public void TeamUp(string playerId)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerId))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerId];
                this.PlayerSendEmojiWorker(playerId, -1, -1, false, "房主发动了技能【随机组队】", true, false);
                gameRoom.TeamUp(true);
            }
        }

        //换座
        public void SwapSeat(string playerId, int offset)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerId))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerId];
                gameRoom.SwapSeat(playerId, offset);
                new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(500);
                    this.UpdateGameHall();
                })).Start();
            }
        }
        public void SwapSeatWS(string playerId, string content)
        {
            int offset = Int32.Parse(content);
            this.SwapSeat(playerId, offset);
        }

        //旁观：选牌
        public void CardsReady(string playerId, ArrayList myCardIsReady)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerId))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerId];
                gameRoom.CardsReady(playerId, myCardIsReady);
            }
        }

        //旁观：选牌
        public void CardsReadyWS(string playerId, string content)
        {
            List<bool> myCardIsReady = CommonMethods.ReadObjectFromString<List<bool>>(content);
            this.CardsReady(playerId, new ArrayList(myCardIsReady));
        }

        //继续牌局
        public void ResumeGameFromFile(string playerId)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerId))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerId];
                gameRoom.ResumeGameFromFile();
                this.PlayerSendEmojiWorker(playerId, -1, -1, false, "房主发动了技能【继续上盘牌局】", true, false);
            }
        }

        //设置从几打起
        public void SetBeginRank(string playerId, string beginRankString)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerId))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerId];
                gameRoom.SetBeginRank(beginRankString);
            }
        }

        //旁观玩家 by id
        public void ObservePlayerById(string playerId, string observerId, bool skipUpdateGameState)
        {
            if (this.SessionIDGameRoom.ContainsKey(observerId))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[observerId];
                gameRoom.ObservePlayerById(playerId, observerId, skipUpdateGameState);
                new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(500);
                    this.UpdateGameHall();
                })).Start();
            }
        }

        //保存房间游戏设置
        public void SaveRoomSetting(string playerId, RoomSetting roomSetting)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerId))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerId];
                gameRoom.SetRoomSetting(roomSetting);
            }
        }

        public void SaveRoomSettingWS(string playerId, string content)
        {
            RoomSetting roomSetting = CommonMethods.ReadObjectFromString<RoomSetting>(content);
            SaveRoomSetting(playerId, roomSetting);
        }
        #endregion

        #region Update Client State
        public void UpdateGameHall()
        {
            lock (PlayersProxy)
            {
                List<string> namesToCall = new List<string>();
                List<string> names = PlayersProxy.Keys.ToList<string>();
                foreach (var name in names)
                {
                    bool isInRoom = false;
                    foreach (GameRoom room in this.GameRooms)
                    {
                        if (!PlayersProxy.ContainsKey(name)) continue;
                        if (room.PlayersProxy.ContainsValue(PlayersProxy[name]) ||
                            room.ObserversProxy.ContainsValue(PlayersProxy[name]))
                        {
                            isInRoom = true;
                            break;
                        }
                    }

                    if (!isInRoom)
                    {
                        namesToCall.Add(name);
                    }
                }
                IPlayerInvokeForAll(PlayersProxy, names, "NotifyGameHall", new List<object>() { this.RoomStates, namesToCall, this.YuezhanInfos });
            }
        }

        public void UpdateOnlinePlayerList(string playerID, bool isJoining)
        {
            lock (PlayersProxy)
            {
                IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "NotifyOnlinePlayerList", new List<object>() { playerID, isJoining });
            }
        }

        public void PublishDaojuInfo(DaojuInfo daojuInfo)
        {
            PublishDaojuInfoWithSpecificPlayersStatusUpdate(daojuInfo, PlayersProxy.Keys.ToList<string>(), false, false);
        }

        public void PublishDaojuInfoWithSpecificPlayersStatusUpdate(DaojuInfo daojuInfo, List<string> playerIDs, bool updateQiandao, bool updateSkin)
        {
            lock (PlayersProxy)
            {
                IPlayerInvokeForAll(PlayersProxy, playerIDs, "NotifyDaojuInfo", new List<object>() { daojuInfo, updateQiandao, updateSkin });
            }
        }

        public void UpdateGameRoomPlayerList(string playerID, bool isJoining, string roomName)
        {
            lock (PlayersProxy)
            {
                IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "NotifyGameRoomPlayerList", new List<object>() { playerID, isJoining, roomName });
            }
        }

        #endregion

        public bool IsInGameHall(string playerID)
        {
            bool isInRoom = false;
            foreach (GameRoom room in this.GameRooms)
            {
                if (!PlayersProxy.ContainsKey(playerID)) continue;
                if (room.PlayersProxy.ContainsValue(PlayersProxy[playerID]) ||
                    room.ObserversProxy.ContainsValue(PlayersProxy[playerID]))
                {
                    isInRoom = true;
                    break;
                }
            }
            return !isInRoom;
        }


        public void IPlayerInvokeForAll(Dictionary<string, IPlayer> PlayersProxyToCall, List<string> playerIDs, string methodName, List<object> args)
        {
            List<string> badPlayerIDs = new List<string>();
            foreach (var playerID in playerIDs)
            {
                if (!PlayersProxyToCall.ContainsKey(playerID)) continue;
                string badPlayerID = IPlayerInvoke(playerID, PlayersProxyToCall[playerID], methodName, args, false);
                if (!string.IsNullOrEmpty(badPlayerID))
                {
                    badPlayerIDs.Add(badPlayerID);
                }
            }
            if (badPlayerIDs.Count > 0)
            {
                foreach (string badID in badPlayerIDs)
                {
                    PlayerExitHall(badID);
                }
            }
        }

        public string IPlayerInvoke(string playerID, IPlayer playerProxy, string methodName, List<object> args, bool performDelete)
        {
            string badPlayerID = null;
            try
            {
                playerProxy.GetType().GetMethod(methodName).Invoke(playerProxy, args.ToArray());
            }
            catch (Exception)
            {
                badPlayerID = playerID;
            }

            if (performDelete && !string.IsNullOrEmpty(badPlayerID))
            {
                PlayerExitHall(badPlayerID);
            }
            return badPlayerID;
        }
        
        public static string GetClientIP()
        {
            string ip = "";
            try
            {
                OperationContext context = OperationContext.Current;
                MessageProperties prop = context.IncomingMessageProperties;
                RemoteEndpointMessageProperty endpoint =
                       prop[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                ip = endpoint.Address;
            }
            catch (Exception)
            {
            }
            return ip;
        }

        private string GetSessionID()
        {
            return OperationContext.Current.SessionId;
        }

        public DaojuInfo LogClientInfo(string clientIP, string playerID, string isCheating, bool publishShengbi, string clientType)
        {
            DaojuInfo daojuInfo = null;
            lock (this)
            {
                Dictionary<string, ClientInfoV3> clientInfoV3Dict = this.LoadClientInfoV3();
                if (!clientInfoV3Dict.ContainsKey(playerID))
                {
                    log.Debug(string.Format("fail to find clientIP {0} for client info logging!", clientIP));
                    return null;
                }

                if (clientIP.StartsWith("127.0") || clientIP.StartsWith("192.168"))
                {
                    isCheating = string.Empty;
                }
                clientInfoV3Dict[playerID].logLogin(clientIP, isCheating);
                if (!string.IsNullOrEmpty(clientType))
                {
                    clientInfoV3Dict[playerID].clientType = clientType;
                }
                CommonMethods.WriteObjectToFile(clientInfoV3Dict, GameRoom.LogsFolder, GameRoom.ClientinfoV3FileName);
                if (publishShengbi)
                {
                    daojuInfo = this.buildPlayerToShengbi(clientInfoV3Dict);
                    // 某人登录进入大厅，对其他人来说，不需要更新签到按钮，也不需要更新皮肤（因为此人肯定还在大厅，不需要显示其皮肤）
                    // 如此分开调用，节约其他玩家的运行资源
                    PublishDaojuInfoWithSpecificPlayersStatusUpdate(daojuInfo, PlayersProxy.Keys.ToList<string>(), false, false);
                }
            }
            return daojuInfo;
        }

        public void InitNewFieldsFirstTime()
        {
            lock (this)
            {
                Dictionary<string, ClientInfoV3> clientInfoV3Dict = this.LoadClientInfoV3();
                foreach (KeyValuePair<string, ClientInfoV3> entry in clientInfoV3Dict)
                {
                    if (string.IsNullOrEmpty(entry.Value.skinInUse))
                    {
                        entry.Value.skinInUse = CommonMethods.defaultSkinInUse;
                    }
                    if (!entry.Value.ownedSkinInfo.Contains(CommonMethods.defaultSkinInUse))
                    {
                        entry.Value.ownedSkinInfo.Add(CommonMethods.defaultSkinInUse);
                    }
                    if (entry.Value.ShengbiTotal == 0)
                    {
                        entry.Value.ShengbiTotal = entry.Value.Shengbi;
                    }
                }
                CommonMethods.WriteObjectToFile(clientInfoV3Dict, GameRoom.LogsFolder, GameRoom.ClientinfoV3FileName);
            }
        }

        public Dictionary<string, SkinInfo> LoadSkinInfo(Dictionary<string, ClientInfoV3> clientInfoV3Dict)
        {
            Dictionary<string, SkinInfo> skinInfo = new Dictionary<string, SkinInfo>();
            string fileName = string.Format("{0}\\{1}", GameRoom.LogsFolder, GameRoom.SkinInfoFileName);
            if (File.Exists(fileName))
            {
                skinInfo = CommonMethods.ReadObjectFromFile<Dictionary<string, SkinInfo>>(fileName);
            }
            else
            {
                skinInfo.Add(CommonMethods.defaultSkinInUse, new SkinInfo(CommonMethods.defaultSkinInUse, "m", "空白皮肤", 0, 0, DateTime.Now.AddYears(999)));
                skinInfo.Add("skin_basicmale", new SkinInfo("skin_basicmale", "m", "免费标准皮肤-男性", 0, 0, DateTime.Now.AddYears(999)));
                skinInfo.Add("skin_basicfemale", new SkinInfo("skin_basicfemale", "f", "免费标准皮肤-女性", 0, 0, DateTime.Now.AddYears(999)));
                CommonMethods.WriteObjectToFile(skinInfo, GameRoom.LogsFolder, GameRoom.SkinInfoFileName);
            }

            // calculate skin owners info
            if (clientInfoV3Dict != null)
            {
                foreach (KeyValuePair<string, ClientInfoV3> entry in clientInfoV3Dict)
                {
                    foreach (string skinName in entry.Value.ownedSkinInfo)
                    {
                        skinInfo[skinName].skinOwners++;
                    }
                }
            }

            return skinInfo;
        }

        public Dictionary<string, ClientInfoV3> LoadClientInfoV3()
        {
            Dictionary<string, ClientInfoV3> clientInfoV3Dict = new Dictionary<string, ClientInfoV3>();
            string fileNameV3 = string.Format("{0}\\{1}", GameRoom.LogsFolder, GameRoom.ClientinfoV3FileName);
            if (File.Exists(fileNameV3))
            {
                clientInfoV3Dict = CommonMethods.ReadObjectFromFile<Dictionary<string, ClientInfoV3>>(fileNameV3);
            }
            return clientInfoV3Dict;
        }

        public string[] ValidateClientInfoV3(string clientIP, string playerID, string content)
        {
            lock (this)
            {
                EnterHallInfo enterHallInfo = CommonMethods.ReadObjectFromString<EnterHallInfo>(content);
                if (!string.Equals(enterHallInfo.clientType, CommonMethods.PlayerClientType_tljapp, StringComparison.OrdinalIgnoreCase))
                {
                    illegalOperationLogger.Debug(string.Format("您当前使用的客户端版本不正确,请访问下面的网址以确保使用正确的客户端: https://bit.ly/tljappold, clientIP: {0}, playerID: {1}, content: {2}", clientIP, playerID, content));
                    return new string[] { "您当前使用的客户端版本不正确", "请访问下面的网址", "https://bit.ly/tljappold" };
                }
                string overridePass = enterHallInfo.password;
                Dictionary<string, ClientInfoV3> clientInfoV3Dict = this.LoadClientInfoV3();
                bool isKnownIDExact = clientInfoV3Dict.ContainsKey(playerID);
                bool isKnownIDIgnoreCase = clientInfoV3Dict.Keys.Contains(playerID, StringComparer.OrdinalIgnoreCase);

                // 找回密码或用户名
                if (!string.IsNullOrEmpty(enterHallInfo.email.Trim()) && string.Equals(enterHallInfo.password, CommonMethods.recoverLoginPassFlag, StringComparison.OrdinalIgnoreCase))
                {
                    string playerEmail = enterHallInfo.email;
                    // 找回用户名
                    if (string.IsNullOrEmpty(playerID))
                    {
                        string actualPlayerID = findPlayerIDByEmail(clientInfoV3Dict, playerEmail);
                        if (string.IsNullOrEmpty(actualPlayerID))
                        {
                            illegalOperationLogger.Debug(string.Format("找回用户名, 用户邮箱输入有误, clientIP: {0}, playerID: {1}, content: {2}", clientIP, playerID, content));
                            return new string[] { "用户邮箱输入有误", "请确认后重试" };
                        }

                        try
                        {
                            string body = string.Format("【{0}】括号内是您的登录用户名，请妥善保存", actualPlayerID);
                            SendEmail(playerEmail, CommonMethods.emailSubjectRevcoverPlayerID, body);
                            return new string[] { "已成功将您的登录用户名发送至指定邮箱", "请查收" };
                        }
                        catch (Exception)
                        {
                            illegalOperationLogger.Debug(string.Format("找回用户名, 用户邮箱输无误, 但发送邮件失败, clientIP: {0}, playerID: {1}, content: {2}", clientIP, playerID, content));
                            return new string[] { "用户邮箱输无误", "但发送邮件失败", "请稍后重试" };
                        }
                    }

                    // 找回密码
                    if (!isKnownIDExact)
                    {
                        illegalOperationLogger.Debug(string.Format("找回密码, 用户名输入有误, clientIP: {0}, playerID: {1}, content: {2}", clientIP, playerID, content));
                        return new string[] { "用户名或用户邮箱输入有误", "请确认后重试" };
                    }
                    ClientInfoV3 info = clientInfoV3Dict[playerID];
                    string playerExpectedEmail = info.PlayerEmail;
                    if (!string.Equals(playerEmail, playerExpectedEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        illegalOperationLogger.Debug(string.Format("找回密码, 用户邮箱输入有误, clientIP: {0}, playerID: {1}, content: {2}", clientIP, playerID, content));
                        return new string[] { "用户名或用户邮箱输入有误", "请确认后重试!" };
                    }

                    try
                    {
                        string body = string.Format("【{0}】括号内是您的登录密码，请妥善保存", info.overridePass);
                        SendEmail(playerEmail, CommonMethods.emailSubjectRevcoverLoginPass, body);
                        return new string[] { "已成功将您的密码发送至指定邮箱", "请查收" };
                    }
                    catch (Exception)
                    {
                        illegalOperationLogger.Debug(string.Format("找回密码, 用户名和用户邮箱输无误, 但发送邮件失败, clientIP: {0}, playerID: {1}, content: {2}", clientIP, playerID, content));
                        return new string[] { "用户名和用户邮箱输无误", "但发送邮件失败", "请稍后重试" };
                    }
                }

                HashSet<string> regcodes = LoadExistingRegCodes();
                // 使用邀请码注册新用户
                if (!string.IsNullOrEmpty(playerID) && !string.IsNullOrEmpty(enterHallInfo.password) && !string.IsNullOrEmpty(enterHallInfo.email))
                {
                    if (playerID.Length > CommonMethods.playerIDMaxLength)
                    {
                        illegalOperationLogger.Debug(string.Format("使用邀请码注册新用户, 用户名过长, clientIP: {0}, playerID: {1}, content: {2}", clientIP, playerID, content));
                        return new string[] { "注册新用户失败：用户名过长", string.Format("请勿超过{0}个字符", CommonMethods.playerIDMaxLength) };
                    }
                    if (!regcodes.Contains(overridePass))
                    {
                        illegalOperationLogger.Debug(string.Format("使用邀请码注册新用户, 邀请码无效, clientIP: {0}, playerID: {1}, content: {2}", clientIP, playerID, content));
                        return new string[] { "注册新用户失败：邀请码无效", "请确认后重试" };
                    }
                    if (isKnownIDIgnoreCase)
                    {
                        illegalOperationLogger.Debug(string.Format("使用邀请码注册新用户, 此玩家昵称已被注册, clientIP: {0}, playerID: {1}, content: {2}", clientIP, playerID, content));
                        return new string[] { "此玩家昵称已被注册（不论大小写）", "请另选一个昵称" };
                    }
                    string regEmail = enterHallInfo.email;
                    if (!CommonMethods.IsValidEmail(regEmail))
                    {
                        illegalOperationLogger.Debug(string.Format("使用邀请码注册新用户, 邮箱地址无效, clientIP: {0}, playerID: {1}, content: {2}", clientIP, playerID, content));
                        return new string[] { "注册新用户失败：邮箱地址无效", "请确认后重试" };
                    }
                    HashSet<string>[] existingPassCodesAndEmails = loadExistingPassCodesAndEmails();
                    HashSet<string> existingPassCodes = existingPassCodesAndEmails[0];
                    HashSet<string> existingEmails = existingPassCodesAndEmails[1];
                    if (existingEmails.Contains(regEmail, StringComparer.OrdinalIgnoreCase))
                    {
                        illegalOperationLogger.Debug(string.Format("使用邀请码注册新用户, 此邮箱已被其他玩家使用, clientIP: {0}, playerID: {1}, content: {2}", clientIP, playerID, content));
                        return new string[] { "此邮箱已被其他玩家使用", "请另选一个邮箱" };
                    }
                    clientInfoV3Dict[playerID] = new ClientInfoV3(clientIP, playerID, overridePass, regEmail, enterHallInfo.clientType);
                    regcodes.Remove(overridePass);
                    GenerateRegistrationCodes(regcodes);
                    CommonMethods.WriteObjectToFile(regcodes, GameRoom.LogsFolder, GameRoom.RegCodesFileName);
                    CommonMethods.WriteObjectToFile(clientInfoV3Dict, GameRoom.LogsFolder, GameRoom.ClientinfoV3FileName);

                    try
                    {
                        string body = string.Format("注册新用户成功！用户名：【{0}】，绑定邮箱：【{1}】。当你丢失用户名或者密码时，请参照使用手册通过绑定邮箱找回。", playerID, regEmail);
                        SendEmail(regEmail, CommonMethods.emailSubjectRegisterNewPlayer, body);
                        return new string[] { CommonMethods.loginSuccessFlag, overridePass, regEmail };
                    }
                    catch (Exception)
                    {
                        illegalOperationLogger.Debug(string.Format("使用邀请码注册新用户, 新用户注册成功, 但发送邮件失败, clientIP: {0}, playerID: {1}, content: {2}", clientIP, playerID, content));
                        return new string[] { "新用户注册成功", "但发送邮件失败", "请确认您的邮箱为有效地址" };
                    }
                }

                // 普通登录
                if (!isKnownIDExact && isKnownIDIgnoreCase)
                {
                    illegalOperationLogger.Debug(string.Format("登录失败, 用户名存在但大小不匹配, clientIP: {0}, playerID: {1}, content: {2}", clientIP, playerID, content));
                    return new string[] { "登录失败", "请确认用户名及密码正确无误!" };
                }
                if (!isKnownIDExact)
                {
                    illegalOperationLogger.Debug(string.Format("登录失败, 用户名不存在, clientIP: {0}, playerID: {1}, content: {2}", clientIP, playerID, content));
                    return new string[] { "登录失败", "请确认用户名及密码正确无误" };
                }
                ClientInfoV3 clientInfoV3 = clientInfoV3Dict[playerID];
                if (string.IsNullOrEmpty(overridePass) || !string.Equals(overridePass, clientInfoV3.overridePass))
                {
                    illegalOperationLogger.Debug(string.Format("登录失败, 密码错误, clientIP: {0}, playerID: {1}, content: {2}", clientIP, playerID, content));
                    return new string[] { "登录失败", "请确认用户名及密码正确无误." };
                }
                if (string.IsNullOrEmpty(clientInfoV3.PlayerEmail))
                {
                    illegalOperationLogger.Debug(string.Format("登录失败, 该用户尚未绑定邮箱, clientIP: {0}, playerID: {1}, content: {2}", clientIP, playerID, content));
                    return new string[] { "该用户尚未绑定邮箱（将用于找回或重设密码）", "请在登录页面中输入邮箱进行绑定后再重新登录" };
                }
                return new string[] { CommonMethods.loginSuccessFlag, clientInfoV3.overridePass, clientInfoV3.PlayerEmail };
            }
        }

        private string findPlayerIDByEmail(Dictionary<string, ClientInfoV3> clientInfoV3Dict, string playerEmail)
        {
            foreach (KeyValuePair<string, ClientInfoV3> entry in clientInfoV3Dict)
            {
                if (string.Equals( entry.Value.PlayerEmail, playerEmail))
                {
                    return entry.Value.PlayerID;
                }
            }
            return string.Empty;
        }

        public void GenerateRegistrationCodes(HashSet<string> regcodes)
        {
            int toGen = CommonMethods.regcodesLength - regcodes.Count;
            if (toGen > 0)
            {
                HashSet<string>[] existingPassCodesAndEmails = loadExistingPassCodesAndEmails();
                HashSet<string> existingPassCodes = existingPassCodesAndEmails[0];
                while (toGen > 0)
                {
                    regcodes.Add(generateNewCode(existingPassCodes, regcodes));
                    toGen--;
                }
                CommonMethods.WriteObjectToFile(regcodes, GameRoom.LogsFolder, GameRoom.RegCodesFileName);
            }
        }

        private static HashSet<string> LoadExistingRegCodes()
        {
            HashSet<string> regcodes = new HashSet<string>();
            string fileName = string.Format("{0}\\{1}", GameRoom.LogsFolder, GameRoom.RegCodesFileName);
            bool fileExists = File.Exists(fileName);
            if (fileExists)
            {
                regcodes = CommonMethods.ReadObjectFromFile<HashSet<string>>(fileName);
            }

            return regcodes;
        }

        private string generateNewCode(HashSet<string> existingPassCodes, HashSet<string> regcodes)
        {
            int attempts = 1;
            string temp = CommonMethods.RandomString(CommonMethods.nickNameOverridePassLengthLower, CommonMethods.nickNameOverridePassLengthUpper);
            while ((existingPassCodes.Contains(temp) || regcodes.Contains(temp)) && attempts < CommonMethods.nickNameOverridePassMaxGetAttempts)
            {
                temp = CommonMethods.RandomString(CommonMethods.nickNameOverridePassLengthLower, CommonMethods.nickNameOverridePassLengthUpper);
                attempts++;
            }

            if (existingPassCodes.Contains(temp) || regcodes.Contains(temp))
            {
                throw new Exception("failed to generate new regcode after maxing out all attempts!");
            }
            return temp;
        }

        private HashSet<string>[] loadExistingPassCodesAndEmails()
        {
            HashSet<string> existingCodes = new HashSet<string>();
            HashSet<string> existingEmails = new HashSet<string>();
            Dictionary<string, ClientInfoV3> clientInfoV3Dict = this.LoadClientInfoV3();
            foreach (KeyValuePair<string, ClientInfoV3> entry in clientInfoV3Dict)
            {
                existingCodes.Add(entry.Value.overridePass);
                if (!string.IsNullOrEmpty(entry.Value.PlayerEmail))
                {
                    existingEmails.Add(entry.Value.PlayerEmail);
                }
            }
            return new HashSet<string>[] { existingCodes, existingEmails };
        }

        public HashSet<string> GetAllNickNameOverridePass(Dictionary<string, ClientInfo> clientInfoDict)
        {
            HashSet<string> allPasses = new HashSet<string>();

            foreach (KeyValuePair<string, ClientInfo> entry in clientInfoDict)
            {
                allPasses.Add(entry.Value.overridePass);
            }
            return allPasses;
        }

        private void LoadEmailSettings()
        {
            string fileName = string.Format("{0}\\{1}", GameRoom.LogsFolder, GameRoom.EmailSettingsFileName);
            bool fileExists = File.Exists(fileName);
            if (!fileExists) throw new Exception("email settings file not found!");
            emailSettings = CommonMethods.ReadObjectFromFile<EmailSettings>(fileName);
        }

        public void SendEmail(string to, string subject, string body)
        {
            var fromAddress = new MailAddress(emailSettings.EmailFromAddress, emailSettings.EmailFromName);
            var toAddress = new MailAddress(to);
            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, emailSettings.EmailFromPass)
            };
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                smtp.Send(message);
            }
        }

        private List<string> OtherPlayerIDsWithSameIP(string PlayerID, string clientIP)
        {
            HashSet<string> others = new HashSet<string>();
            Dictionary<string, ClientInfoV3> clientInfoV3Dict = this.LoadClientInfoV3();
            string fileNameV3 = string.Format("{0}\\{1}", GameRoom.LogsFolder, GameRoom.ClientinfoV3FileName);
            foreach (KeyValuePair<string, ClientInfoV3> entry in clientInfoV3Dict)
            {
                if (string.Equals(entry.Key, PlayerID)) continue;
                if (entry.Value.playerIPList.Contains(clientIP))
                {
                    others.Add(entry.Value.PlayerID);
                }
            }
            return others.ToList();
        }

        public void NotifySgcsPlayerUpdated(string playerID, string content)
        {
            if (!this.SessionIDGameRoom.ContainsKey(playerID)) return;

            GameRoom gameRoom = this.SessionIDGameRoom[playerID];
            gameRoom.NotifySgcsPlayerUpdated(content);
        }

        public void NotifyCreateCollectStar(string playerID, string content)
        {
            if (!this.SessionIDGameRoom.ContainsKey(playerID)) return;
            log.Debug(string.Format("player {0} attempted to start game: {1}", playerID, WebSocketObjects.SmallGameName_CollectStar));

            WebSocketObjects.SGCSState state = CommonMethods.ReadObjectFromString<WebSocketObjects.SGCSState>(content);
            GameRoom gameRoom = this.SessionIDGameRoom[playerID];
            if (!gameRoom.NotifyCreateCollectStar(state))
            {
                this.PlayersProxy[playerID].NotifyMessage(new string[] { "已有其他玩家正在游戏中", "请稍后再试" });
            }
        }

        public void NotifyGrabStar(string playerID, string content)
        {
            if (!this.SessionIDGameRoom.ContainsKey(playerID)) return;
            int[] args = CommonMethods.ReadObjectFromString<int[]>(content);
            int playerIndex = args[0];
            int starIndex = args[1];
            GameRoom gameRoom = this.SessionIDGameRoom[playerID];
            gameRoom.NotifyGrabStar(playerIndex, starIndex);
        }

        public void NotifyEndCollectStar(string playerID, string content)
        {
            if (!this.SessionIDGameRoom.ContainsKey(playerID)) return;
            log.Debug(string.Format("player {0} attempted to end game: {1}", playerID, WebSocketObjects.SmallGameName_CollectStar));

            int playerIndex = int.Parse(content);
            GameRoom gameRoom = this.SessionIDGameRoom[playerID];
            gameRoom.NotifyEndCollectStar(playerIndex);
        }

        public void UpdateGobang(string playerID, string content)
        {
            if (!this.SessionIDGameRoom.ContainsKey(playerID)) return;

            WebSocketObjects.SGGBState state = CommonMethods.ReadObjectFromString<WebSocketObjects.SGGBState>(content);
            GameRoom gameRoom = this.SessionIDGameRoom[playerID];
            if (!gameRoom.UpdateGobang(playerID, state))
            {
                this.PlayersProxy[playerID].NotifyMessage(new string[] { "已有其他玩家正在游戏中", "请稍后再试" });
            }
        }

        private void LoadForbidSayings()
        {
            this.ForbidSayings = new List<string>();
            string fileName = string.Format("{0}\\{1}", GameRoom.LogsFolder, GameRoom.ForbidSayingsFileName);
            bool fileExists = File.Exists(fileName);
            if (fileExists)
            {
                this.ForbidSayings = CommonMethods.ReadObjectFromFile<List<string>>(fileName);
            }
        }

        private void LoadHostStatus()
        {
            string fileName = string.Format("{0}\\{1}", GameRoom.LogsFolder, GameRoom.HostStatusFileName);
            bool fileExists = File.Exists(fileName);
            if (fileExists)
            {
                this.hostStatus = CommonMethods.ReadObjectFromFile<HostStatus>(fileName);
            }
            else
            {
                this.hostStatus = new HostStatus();
                CommonMethods.WriteObjectToFile(this.hostStatus, GameRoom.LogsFolder, GameRoom.HostStatusFileName);
            }
        }
    }

    // Simple async result implementation.
    class CompletedAsyncResult<T> : IAsyncResult
    {
        T data;

        public CompletedAsyncResult(T data)
        { this.data = data; }

        public T Data
        { get { return data; } }

        #region IAsyncResult Members
        public object AsyncState
        { get { return (object)data; } }

        public WaitHandle AsyncWaitHandle
        { get { throw new Exception("The method or operation is not implemented."); } }

        public bool CompletedSynchronously
        { get { return true; } }

        public bool IsCompleted
        { get { return true; } }
        #endregion
    }
}