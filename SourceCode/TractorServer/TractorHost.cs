using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Duan.Xiugang.Tractor.Objects;
using System.Threading;
using System.IO;
using System.Collections;
using System.ServiceModel.Channels;
using System.Configuration;
using Fleck;
using System.Security.Cryptography.X509Certificates;
using System.Timers;
using System.Collections.Concurrent;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace TractorServer
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class TractorHost : ITractorHost
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly string[] RoomNames = new string[] { "桃园结义", "五谷丰登", "无中生有", "万箭齐发" };
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

        public CardsShoe CardsShoe { get; set; }

        public Dictionary<string, string> PlayerToIP;
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
        public int PingInterval = 12000;
        public int PingTimeout = 10000;

        public System.Timers.Timer timerPingClients;
        public ConcurrentDictionary<string, System.Timers.Timer> playerIDToTimer;
        public ConcurrentDictionary<System.Timers.Timer,string> timerToPlayerID;

        public TractorHost()
        {
            LoadTakenPlayerIDs();
            gameConfig = new GameConfig();
            var myreader = new AppSettingsReader();
            try
            {
                MaxRoom = (int)myreader.GetValue(KeyMaxRoom, typeof(int));
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

            CardsShoe = new CardsShoe();
            PlayerToIP = new Dictionary<string, string>();
            PlayersProxy = new Dictionary<string, IPlayer>();
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
                                string clientIP = socket.ConnectionInfo.ClientIpAddress;
                                if (this.PlayerToIP.ContainsValue(clientIP))
                                {
                                    playerProxy.NotifyMessage(new string[] { "之前非正常退出", "请重启游戏后再尝试进入大厅" });
                                    this.handlePlayerDisconnect(messageObj.playerID);
                                }
                                else
                                {
                                    playerProxy.NotifyMessage(new string[] { "玩家昵称重名", "请更改昵称后重试" });
                                }
                                return;
                            }
                            this.PlayersProxy.Add(messageObj.playerID, playerProxy);
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

                            string[] validationResult = ValidateClientInfo(clientIP, messageObj.playerID, messageObj.content);
                            // 验证失败，返回错误信息
                            if (validationResult.Length > 1)
                            {
                                playerProxy.NotifyMessage(validationResult);
                                return;
                            }
                            // 首次登录，返回验证码
                            if (validationResult.Length == 1)
                            {
                                playerProxy.NotifyMessage(new string[] { string.Format("【{0}】", validationResult[0]), "括号内是您的昵称验证码", "请妥善保存", "当您更换IP时将使用它来声名昵称所有权", "可在设置页面中再次查看" });
                            }

                            if (this.PlayersProxy.ContainsKey(messageObj.playerID))
                            {
                                if (this.PlayerToIP.ContainsValue(clientIP))
                                {
                                    playerProxy.NotifyMessage(new string[] { "之前非正常退出", "请重启游戏后再尝试进入大厅" });
                                    this.handlePlayerDisconnect(messageObj.playerID);
                                }
                                else
                                {
                                    playerProxy.NotifyMessage(new string[] { "该玩家已在别处登录", "请不要同时在多处登录" });
                                }
                                return;
                            }
                            this.PlayersProxy.Add(messageObj.playerID, playerProxy);
                        }

                        WSMessageHandler(messageObj.messageType, messageObj.playerID, messageObj.content);
                    };
                });
            });
            threadStartHostWSS.Start();

            // setup logger
            AppDomain currentDomain = default(AppDomain);
            currentDomain = AppDomain.CurrentDomain;
            // Handler for unhandled exceptions.
            currentDomain.FirstChanceException += GlobalFirstChanceExceptionHandler;
            // Handler for exceptions in threads behind forms.
            System.Windows.Forms.Application.ThreadException += GlobalThreadExceptionHandler;
        }

        // returns if needs restart
        private bool handlePlayerDisconnect(string playerID)
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
                    this.PlayerEnterHallWS(playerID);
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
                case WebSocketObjects.WebSocketMessageType_ObserveNext:
                    this.ObservePlayerById(content, playerID);
                    break;
                case WebSocketObjects.WebSocketMessageType_ExitRoom:
                    this.PlayerExitRoom(playerID);
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
                case WebSocketObjects.WebSocketMessageType_PlayerHasCutCards:
                    new Thread(() =>
                    {
                        this.PlayerHasCutCards(playerID, content);
                    }).Start();
                    break;
                case WebSocketObjects.WebSocketMessageType_NotifyPong:
                    this.PlayerPong(playerID);
                    break;
                default:
                    break;
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

        public void PlayerEnterHallWS(string playerID)
        {
            string clientIP = ((PlayerWSImpl)this.PlayersProxy[playerID]).Socket.ConnectionInfo.ClientIpAddress;
            LogClientInfo(clientIP, playerID, false);
            IPlayer player = this.PlayersProxy[playerID];
            this.PlayerToIP[playerID] = clientIP;
            if (this.SessionIDGameRoom.ContainsKey(playerID))
            {
                //断线重连
                log.Debug(string.Format("player {0} re-entered hall from offline - web client.", playerID));
                player.NotifyMessage(new string[] { CommonMethods.reenterRoomSignal });
                Thread.Sleep(2000);

                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                lock (gameRoom)
                {
                    bool entered = gameRoom.PlayerReenterRoom(playerID, clientIP, player, AllowSameIP);
                    if (entered)
                    {
                        new Thread(new ThreadStart(() =>
                        {
                            Thread.Sleep(500);
                            this.UpdateGameHall();
                        })).Start();
                    }
                }
            }
            else
            {
                log.Debug(string.Format("player {0} entered hall - web client.", playerID));
                UpdateGameHall();
            }

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
                        bool entered = gameRoom.PlayerEnterRoom(playerID, clientIP, player, AllowSameIP, posID);
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
                bool entered = gameRoom.PlayerEnterRoom(playerID, clientIP, player, AllowSameIP, messageObj.posID);
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
            if (!this.SessionIDGameRoom.ContainsKey(playerID)) return;

            GameRoom gameRoom = this.SessionIDGameRoom[playerID];
            //如果退出的是正常玩家，先记录旁观玩家和离线玩家
            List<string> obs = new List<string>();
            List<string> quitPlayers = new List<string>() { playerID };
            if (gameRoom.IsActualPlayer(playerID))
            {
                obs = gameRoom.ObserversProxy.Keys.ToList<string>();
                foreach (var player in gameRoom.CurrentRoomState.CurrentGameState.Players)
                {
                    if (player == null) continue;
                    if (player.IsOffline)
                    {
                        quitPlayers.Add(player.PlayerId);
                    }
                }
                foreach (PlayerEntity p in gameRoom.CurrentRoomState.CurrentGameState.Players)
                {
                    if (p != null && !PlayersProxy.ContainsKey(p.PlayerId) && !obs.Contains(p.PlayerId) && !quitPlayers.Contains(p.PlayerId))
                    {
                        obs.Add(p.PlayerId);
                    }
                }
            }

            //先将退出的玩家和离线玩家移出房间
            gameRoom.PlayerQuit(quitPlayers);
            foreach (string qp in quitPlayers)
            {
                SessionIDGameRoom.Remove(qp);
            }

            //再将旁观玩家移出房间，这样旁观玩家才能得到最新的handstep的更新
            gameRoom.PlayerQuit(obs);
            foreach (string ob in obs)
            {
                SessionIDGameRoom.Remove(ob);
            }

            log.Debug(string.Format("player {0} exited room.", playerID));
            new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(500);
                this.UpdateGameHall();
            })).Start();
        }

        //玩家退出游戏
        public IAsyncResult BeginPlayerQuit(string playerID, AsyncCallback callback, object state)
        {
            if (!this.handlePlayerDisconnect(playerID)) return new CompletedAsyncResult<string>(string.Format("BeginPlayerQuit: player {0} marked offline and return", playerID));
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
                if(!playerIDToTimer.TryRemove(playerID, out timer))
                {
                    log.Debug("concurrence removing playerIDToTimer failed with key: " + playerID);
                }
                string temp = "";
                timerToPlayerID.TryRemove(timer, out temp);
            }
            PlayerToIP.Remove(playerID);
            PlayersProxy.Remove(playerID);
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

        public void PlayerSendEmoji(string playerID, int emojiType, int emojiIndex, bool isCenter)
        {
            PlayerSendEmojiWorker(playerID, emojiType, emojiIndex, isCenter, "");
        }

        public void PlayerSendEmojiWS(string playerID, string content)
        {
            List<object> args = CommonMethods.ReadObjectFromString<List<object>>(content);

            int emojiType = (int)(long)args[0];
            int emojiIndex = (int)(long)args[1];
            string emojiString = (string)args[2];
            bool isCenter = false;
            if (args.Count >= 4) isCenter = (bool)args[3];
            this.PlayerSendEmojiWorker(playerID, emojiType, emojiIndex, isCenter, emojiString);
        }

        public void PlayerSendEmojiWorker(string playerID, int emojiType, int emojiIndex, bool isCenter, string msgString)
        {
            bool isPlayerInGameHall = this.IsInGameHall(playerID);
            if (isPlayerInGameHall)
            {
                isCenter = false;
                List<string> playersInGameHall = this.getPlayersInGameHall();
                IPlayerInvokeForAll(PlayersProxy, playersInGameHall, "NotifyEmoji", new List<object>() { playerID, emojiType, emojiIndex, isCenter, msgString });
            }
            else if (this.SessionIDGameRoom.ContainsKey(playerID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                gameRoom.PublishEmoji(playerID, emojiType, emojiIndex, isCenter, msgString);
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
        public void PlayerMakeTrump(Duan.Xiugang.Tractor.Objects.TrumpExposingPoker trumpExposingPoker, Duan.Xiugang.Tractor.Objects.Suit trump, string trumpMaker)
        {
            if (this.SessionIDGameRoom.ContainsKey(trumpMaker))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[trumpMaker];
                gameRoom.PlayerMakeTrump(trumpExposingPoker, trump, trumpMaker);
            }
        }
        public void PlayerMakeTrumpWS(string trumpMaker, string content)
        {
            List<int> messageObj = CommonMethods.ReadObjectFromString<List<int>>(content);
            this.PlayerMakeTrump((TrumpExposingPoker)messageObj[0], (Suit)messageObj[1], trumpMaker);
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
                gameRoom.TeamUp();
                new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(500);
                    this.UpdateGameHall();
                })).Start();
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
        public void ObservePlayerById(string playerId, string observerId)
        {
            if (this.SessionIDGameRoom.ContainsKey(observerId))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[observerId];
                gameRoom.ObservePlayerById(playerId, observerId);
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
        #endregion

        #region Update Client State
        public void UpdateGameHall()
        {
            List<string> namesToCall = new List<string>();
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
                        namesToCall.Add(name);
                    }
                }
            }
            if (namesToCall.Count > 0)
            {
                IPlayerInvokeForAll(PlayersProxy, namesToCall, "NotifyGameHall", new List<object>() { this.RoomStates, namesToCall });
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

        public void LogClientInfo(string clientIP, string playerID, bool isCheating)
        {
            lock (this)
            {
                Dictionary<string, ClientInfo> clientInfoDict = new Dictionary<string, ClientInfo>();
                string fileName = string.Format("{0}\\{1}", GameRoom.LogsFolder, GameRoom.ClientinfoFileName);
                if (File.Exists(fileName))
                {
                    clientInfoDict = CommonMethods.ReadObjectFromFile<Dictionary<string, ClientInfo>>(fileName);
                }
                if (!clientInfoDict.ContainsKey(clientIP))
                {
                    log.Debug(string.Format("fail to find clientIP {0} for client info logging!", clientIP));
                    return;
                }
                clientInfoDict[clientIP].logLogin(playerID, isCheating);
                CommonMethods.WriteObjectToFile(clientInfoDict, GameRoom.LogsFolder, GameRoom.ClientinfoFileName);
            }
        }

        public string[] ValidateClientInfo(string clientIP, string playerID, string overridePass)
        {
            lock (this)
            {
                string[] validationResult = new string[] { };
                Dictionary<string, ClientInfo> clientInfoDict = new Dictionary<string, ClientInfo>();
                string fileName = string.Format("{0}\\{1}", GameRoom.LogsFolder, GameRoom.ClientinfoFileName);
                bool fileExists = File.Exists(fileName);
                bool isNewClientID = true;
                bool isKnownIP = !clientInfoDict.ContainsKey(clientIP);
                if (fileExists)
                {
                    clientInfoDict = CommonMethods.ReadObjectFromFile<Dictionary<string, ClientInfo>>(fileName);
                }
                var clientInfo = new ClientInfo(clientIP, overridePass);
                if (isKnownIP)
                {
                    clientInfo = clientInfoDict[clientIP];
                }
                else
                {
                    clientInfoDict[clientIP] = clientInfo;
                }
                isNewClientID = !clientInfo.playerIdList.Contains(playerID);
                if (isKnownIP)
                {
                    if (!clientInfo.playerIdList.Contains(playerID) && clientInfo.playerIdList.Contains(playerID, StringComparer.OrdinalIgnoreCase))
                    {
                        clientInfo.logLoginAttempted(playerID);
                        CommonMethods.WriteObjectToFile(clientInfoDict, GameRoom.LogsFolder, GameRoom.ClientinfoFileName);
                        return new string[] { "检测到仅字母大小写不同的重复昵称", "请正确输入上次登录时所使用的昵称", "（包括正确的字母大小写）" };
                    }
                    if (!clientInfo.playerIdList.Contains(playerID) && clientInfo.playerIdList.Count + 1 > clientInfo.maxIDsAllowed)
                    {
                        clientInfo.logLoginAttempted(playerID);
                        CommonMethods.WriteObjectToFile(clientInfoDict, GameRoom.LogsFolder, GameRoom.ClientinfoFileName);
                        return new string[] { "玩家昵称与历史记录不匹配", "请使用上次登录时所使用的昵称" };
                    }
                }

                Dictionary<string, string> playerIdTakenToIP = LoadTakenPlayerIDs();
                if (isNewClientID && playerIdTakenToIP.ContainsKey(playerID.ToLower()))
                {
                    string oldIP = playerIdTakenToIP[playerID.ToLower()];
                    if (clientInfoDict.ContainsKey(oldIP) && (string.IsNullOrEmpty(clientInfoDict[oldIP].overridePass) || overridePass != clientInfoDict[oldIP].overridePass))
                    {
                        clientInfo.logLoginAttempted(playerID);
                        CommonMethods.WriteObjectToFile(clientInfoDict, GameRoom.LogsFolder, GameRoom.ClientinfoFileName);
                        return new string[] { "此玩家昵称已被注册（不论大小写）", "请另选一个昵称", "如果您的确拥有此昵称", "请尝试输入昵称验证码再次登录" };
                    }
                    if (!clientInfoDict[oldIP].playerIdList.Contains(playerID))
                    {
                        clientInfo.logLoginAttempted(playerID);
                        CommonMethods.WriteObjectToFile(clientInfoDict, GameRoom.LogsFolder, GameRoom.ClientinfoFileName);
                        return new string[] { "昵称验证码正确", "但检测到仅字母大小写不同的重复昵称", "请正确输入上次登录时所使用的昵称", "（包括正确的字母大小写）" };
                    }
                    if (this.PlayersProxy.ContainsKey(playerID))
                    {
                        return new string[] { "该玩家已在别处登录", "请不要同时在多处登录" };
                    }

                    HashSet<string> newIDList = new HashSet<string>();
                    newIDList.Add(playerID);
                    newIDList.UnionWith(clientInfoDict[oldIP].playerIdList);
                    newIDList.UnionWith(clientInfoDict[clientIP].playerIdList);
                    clientInfoDict[oldIP].playerIdList.Clear();
                    clientInfoDict[oldIP].playerIdList.Add(playerID);
                    foreach (string toAddID in newIDList)
                    {
                        if (clientInfoDict[oldIP].playerIdList.Count < clientInfoDict[oldIP].maxIDsAllowed)
                        {
                            clientInfoDict[oldIP].playerIdList.Add(toAddID);
                        }
                        else
                        {
                            break;
                        }
                    }
                    clientInfoDict[oldIP].playerIdListAttempted.UnionWith(clientInfoDict[clientIP].playerIdListAttempted);
                    clientInfoDict[clientIP] = clientInfoDict[oldIP];
                    clientInfoDict[clientIP].IP = clientIP;
                    clientInfoDict.Remove(oldIP);
                    CommonMethods.WriteObjectToFile(clientInfoDict, GameRoom.LogsFolder, GameRoom.ClientinfoFileName);
                    return new string[] { overridePass };
                }
                if (isNewClientID)
                {
                    HashSet<string> allPasses = GetAllNickNameOverridePass(clientInfoDict);
                    string temp = CommonMethods.RandomString(CommonMethods.nickNameOverridePassLength);
                    int attempts = 1;
                    while (allPasses.Contains(temp) && attempts < CommonMethods.nickNameOverridePassMaxGetAttempts)
                    {
                        temp = CommonMethods.RandomString(CommonMethods.nickNameOverridePassLength);
                        attempts++;
                    }
                    if (allPasses.Contains(temp))
                    {
                        return new string[] { "尝试生成昵称验证码失败", "请稍后再试" };
                    }
                    validationResult = new string[] { temp };
                    clientInfo.overridePass = temp;
                }
                if (!fileExists || isNewClientID)
                {
                    CommonMethods.WriteObjectToFile(clientInfoDict, GameRoom.LogsFolder, GameRoom.ClientinfoFileName);
                }
                return validationResult;
            }
        }

        public Dictionary<string, string> LoadTakenPlayerIDs()
        {
            Dictionary<string, string> playerIdTakenToIP = new Dictionary<string, string>();

            string fileName = string.Format("{0}\\{1}", GameRoom.LogsFolder, GameRoom.ClientinfoFileName);
            if (File.Exists(fileName))
            {
                Dictionary<string, ClientInfo> clientInfoDict = CommonMethods.ReadObjectFromFile<Dictionary<string, ClientInfo>>(fileName);
                foreach (KeyValuePair<string, ClientInfo> entry in clientInfoDict)
                {
                    foreach (string idTaken in entry.Value.playerIdList)
                    {
                        playerIdTakenToIP.Add(idTaken.ToLower(), entry.Key);
                    }
                }
            }
            return playerIdTakenToIP;
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