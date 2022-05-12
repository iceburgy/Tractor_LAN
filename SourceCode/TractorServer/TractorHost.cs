using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Web;
using Duan.Xiugang.Tractor.Objects;
using System.Threading;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.ServiceModel.Channels;
using System.Configuration;


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
        public string KeyMaxRoom = "maxRoom";
        public string KeyAllowSameIP = "allowSameIP";
        public string KeyIsFullDebug = "isFullDebug";

        public CardsShoe CardsShoe { get; set; }

        public Dictionary<string, string> PlayerToIP { get; set; }
        public Dictionary<string, IPlayer> PlayersProxy { get; set; }
        public List<GameRoom> GameRooms { get; set; }
        public List<RoomState> RoomStates { get; set; }
        public Dictionary<string, GameRoom> SessionIDGameRoom { get; set; }
        string[] knownErrors = new string[] {
            "An existing connection was forcibly closed by the remote host",
            "The socket connection was aborted",
            "The I/O operation has been aborted"
        };

        public TractorHost()
        {
            gameConfig = new GameConfig();
            var myreader = new AppSettingsReader();
            try
            {
                MaxRoom = (int)myreader.GetValue(KeyMaxRoom, typeof(int));
                AllowSameIP = (bool)myreader.GetValue(KeyAllowSameIP, typeof(bool));
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

            // setup logger
            AppDomain currentDomain = default(AppDomain);
            currentDomain = AppDomain.CurrentDomain;
            // Handler for unhandled exceptions.
            currentDomain.FirstChanceException += GlobalFirstChanceExceptionHandler;
            // Handler for exceptions in threads behind forms.
            System.Windows.Forms.Application.ThreadException += GlobalThreadExceptionHandler;
        }

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
            LogClientInfo(clientIP, playerID, false);
            IPlayer player = OperationContext.Current.GetCallbackChannel<IPlayer>();
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
        }

        public void PlayerEnterRoom(string playerID, int roomID, int posID)
        {
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
                            Thread.Sleep(500);
                            Thread thr = new Thread(new ThreadStart(this.UpdateGameHall));
                            thr.Start();
                        }
                    }
                }
            }
            else
            {
                log.Debug(string.Format("PlayerEnterRoom failed, playerID: {0}, roomID: {1}", playerID, roomID));
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
            Thread.Sleep(500);
            Thread thr = new Thread(new ThreadStart(this.UpdateGameHall));
            thr.Start();
        }

        //玩家退出游戏
        public IAsyncResult BeginPlayerQuit(string playerID, AsyncCallback callback, object state)
        {
            IAsyncResult iAsyncResult=new CompletedAsyncResult<string>("player not in hall, exit hall with no ops!");
            
            //如果玩家处于离线状态，则不要将其退出
            if (this.SessionIDGameRoom.ContainsKey(playerID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                if (gameRoom.CurrentRoomState.CurrentGameState.Players.Exists(p => p != null && p.PlayerId == playerID))
                {
                    PlayerEntity player = gameRoom.CurrentRoomState.CurrentGameState.Players.Single(p => p != null && p.PlayerId == playerID);
                    if (player.IsOffline && (DateTime.Now - player.OfflineSince).TotalSeconds <= gameRoom.CurrentRoomState.roomSetting.secondsToWaitForReenter) return iAsyncResult;
                }
            }

            if (PlayerToIP.ContainsKey(playerID) && !this.PlayerToIP.ContainsValue(PlayerToIP[playerID]))
            {
                return iAsyncResult;
            }

            PlayerExitRoom(playerID);

            string result = PlayerExitHall(playerID);

            return new CompletedAsyncResult<string>(result);
        }

        private string PlayerExitHall(string playerID)
        {
            PlayerToIP.Remove(playerID);
            PlayersProxy.Remove(playerID);
            string result = string.Format("player {0} quit.", playerID);
            log.Debug(result);
            Thread.Sleep(500);
            Thread thr = new Thread(new ThreadStart(this.UpdateGameHall));
            thr.Start();
            return result;
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
            if (this.SessionIDGameRoom.ContainsKey(playerID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerID];
                gameRoom.PublishEmoji(playerID, emojiType, emojiIndex, isCenter);
            }
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

        //亮主
        public void PlayerMakeTrump(Duan.Xiugang.Tractor.Objects.TrumpExposingPoker trumpExposingPoker, Duan.Xiugang.Tractor.Objects.Suit trump, string trumpMaker)
        {
            if (this.SessionIDGameRoom.ContainsKey(trumpMaker))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[trumpMaker];
                gameRoom.PlayerMakeTrump(trumpExposingPoker, trump, trumpMaker);
            }
        }

        public void PlayerShowCards(string playerId, CurrentTrickState currentTrickState)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerId))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerId];
                gameRoom.PlayerShowCards(currentTrickState);
            }
        }

        public void ValidateDumpingCards(List<int> selectedCards, string playerId)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerId))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerId];
                gameRoom.ValidateDumpingCards(selectedCards, playerId);
            }
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
                Thread.Sleep(500);
                Thread thr = new Thread(new ThreadStart(this.UpdateGameHall));
                thr.Start();
            }
        }

        //换座
        public void SwapSeat(string playerId, int offset)
        {
            if (this.SessionIDGameRoom.ContainsKey(playerId))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[playerId];
                gameRoom.SwapSeat(playerId, offset);
                Thread.Sleep(500);
                Thread thr = new Thread(new ThreadStart(this.UpdateGameHall));
                thr.Start();
            }
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
                Thread.Sleep(500);
                Thread thr = new Thread(new ThreadStart(this.UpdateGameHall));
                thr.Start();
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
                    clientInfoDict[clientIP] = new ClientInfo(clientIP, playerID);
                }
                clientInfoDict[clientIP].logLogin(playerID, isCheating);
                CommonMethods.WriteObjectToFile(clientInfoDict, GameRoom.LogsFolder, GameRoom.ClientinfoFileName);
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