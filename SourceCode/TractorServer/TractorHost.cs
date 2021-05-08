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
                GameRoom gameRoom = new GameRoom(i, RoomNames[i]);
                RoomStates.Add(gameRoom.CurrentRoomState);
                GameRooms.Add(gameRoom);
                if (!Directory.Exists(gameRoom.LogsByRoomFolder))
                {
                    Directory.CreateDirectory(gameRoom.LogsByRoomFolder);
                }
            }
            SessionIDGameRoom = new Dictionary<string, GameRoom>();
        }

        #region implement interface ITractorHost

        public void PlayerEnterHall(string playerID)
        {
            string clientIP = GetClientIP();
            IPlayer player = OperationContext.Current.GetCallbackChannel<IPlayer>();
            if (!PlayersProxy.Keys.Contains(playerID))
            {
                this.PlayerToIP.Add(playerID, clientIP);
                PlayersProxy.Add(playerID, player);
                log.Debug(string.Format("player {0} entered hall.", playerID));
                GameRoom.LogClientInfo(clientIP, playerID, false);
                UpdateGameHall();
            }
            else if (this.PlayerToIP.ContainsValue(clientIP))
            {
                player.NotifyMessage("之前非正常退出，请重启游戏后再尝试进入大厅");
            }
            else
            {
                player.NotifyMessage("玩家昵称重名，请更改昵称后重试");
            }
        }

        public void PlayerEnterRoom(string playerID, int roomID, int posID)
        {
            string clientIP = GetClientIP();
            string sessionID = GetSessionID();
            IPlayer player = PlayersProxy[playerID];
            if (player != null)
            {
                GameRoom gameRoom = this.GameRooms.Single((room) => room.CurrentRoomState.RoomID == roomID);
                if (gameRoom == null)
                {
                    player.NotifyMessage(string.Format("加入房间失败，房间号【{0}】不存在", roomID));
                }
                else
                {
                    lock (gameRoom)
                    {
                        bool entered = gameRoom.PlayerEnterRoom(playerID, clientIP, player, AllowSameIP, posID);
                        if (entered)
                        {
                            SessionIDGameRoom[sessionID] = gameRoom;
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

        //玩家退出房间
        public void PlayerExitRoom(string playerID)
        {
            string sessionID = GetSessionID();
            if (this.SessionIDGameRoom.ContainsKey(sessionID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[sessionID];
                //如果退出的是正常玩家，则先将旁观玩家移出房间
                if (gameRoom.PlayersProxy.ContainsKey(playerID))
                {
                    List<string> obs = gameRoom.ObserversProxy.Keys.ToList<string>();
                    foreach (string ob in obs)
                    {
                        gameRoom.PlayerQuit(new List<string>() { ob });
                    }
                }

                //再将正常玩家移出房间
                gameRoom.PlayerQuit(new List<string>() { playerID });
                SessionIDGameRoom.Remove(sessionID);
            }
            log.Debug(string.Format("player {0} exited room.", playerID));
            Thread.Sleep(500);
            Thread thr = new Thread(new ThreadStart(this.UpdateGameHall));
            thr.Start();
        }

        //玩家退出游戏
        public IAsyncResult BeginPlayerQuit(string playerID, AsyncCallback callback, object state)
        {
            string clientIP = GetClientIP();
            string sessionID = GetSessionID();
            if (!this.PlayerToIP.ContainsValue(clientIP)) return new CompletedAsyncResult<string>("player not in hall, exit hall with no ops!");
            if (this.SessionIDGameRoom.ContainsKey(sessionID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[sessionID];
                //如果退出的是正常玩家，则先将旁观玩家移出房间
                if (gameRoom.PlayersProxy.ContainsKey(playerID))
                {
                    List<string> obs = gameRoom.ObserversProxy.Keys.ToList<string>();
                    foreach (string ob in obs)
                    {
                        gameRoom.PlayerQuit(new List<string>() { ob });
                    }
                }

                //再将正常玩家移出房间
                gameRoom.PlayerQuit(new List<string>() { playerID });
                SessionIDGameRoom.Remove(sessionID);
            }
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

        public void PlayerIsReadyToStart(string playerID)
        {
            string sessionID = GetSessionID();
            if (this.SessionIDGameRoom.ContainsKey(sessionID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[sessionID];
                gameRoom.PlayerIsReadyToStart(playerID);
            }
        }

        public void PlayerToggleIsRobot(string playerID)
        {
            string sessionID = GetSessionID();
            if (this.SessionIDGameRoom.ContainsKey(sessionID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[sessionID];
                gameRoom.PlayerToggleIsRobot(playerID);
            }
        }

        //player discard last 8 cards
        public void StoreDiscardedCards(int[] cards)
        {
            string sessionID = GetSessionID();
            if (this.SessionIDGameRoom.ContainsKey(sessionID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[sessionID];
                gameRoom.StoreDiscardedCards(cards);
            }
        }

        //亮主
        public void PlayerMakeTrump(Duan.Xiugang.Tractor.Objects.TrumpExposingPoker trumpExposingPoker, Duan.Xiugang.Tractor.Objects.Suit trump, string trumpMaker)
        {
            string sessionID = GetSessionID();
            if (this.SessionIDGameRoom.ContainsKey(sessionID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[sessionID];
                gameRoom.PlayerMakeTrump(trumpExposingPoker, trump, trumpMaker);
            }
        }

        public void PlayerShowCards(CurrentTrickState currentTrickState)
        {
            string sessionID = GetSessionID();
            if (this.SessionIDGameRoom.ContainsKey(sessionID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[sessionID];
                gameRoom.PlayerShowCards(currentTrickState);
            }
        }

        public ShowingCardsValidationResult ValidateDumpingCards(List<int> selectedCards, string playerId)
        {
            string sessionID = GetSessionID();
            if (this.SessionIDGameRoom.ContainsKey(sessionID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[sessionID];
                return gameRoom.ValidateDumpingCards(selectedCards, playerId);
            }
            return new ShowingCardsValidationResult { ResultType = ShowingCardsValidationResultType.Unknown };
        }

        public void RefreshPlayersCurrentHandState()
        {
            string sessionID = GetSessionID();
            if (this.SessionIDGameRoom.ContainsKey(sessionID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[sessionID];
                gameRoom.RefreshPlayersCurrentHandState();
            }
        }

        //随机组队
        public void TeamUp()
        {
            string sessionID = GetSessionID();
            if (this.SessionIDGameRoom.ContainsKey(sessionID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[sessionID];
                gameRoom.TeamUp();
                Thread.Sleep(500);
                Thread thr = new Thread(new ThreadStart(this.UpdateGameHall));
                thr.Start();
            }
        }

        //和下家互换座位
        public void MoveToNextPosition(string playerId)
        {
            string sessionID = GetSessionID();
            if (this.SessionIDGameRoom.ContainsKey(sessionID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[sessionID];
                gameRoom.MoveToNextPosition(playerId);
                Thread.Sleep(500);
                Thread thr = new Thread(new ThreadStart(this.UpdateGameHall));
                thr.Start();
            }
        }

        //旁观：选牌
        public void CardsReady(string playerId, ArrayList myCardIsReady)
        {
            string sessionID = GetSessionID();
            if (this.SessionIDGameRoom.ContainsKey(sessionID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[sessionID];
                gameRoom.CardsReady(playerId, myCardIsReady);
            }
        }

        //读取牌局
        public void RestoreGameStateFromFile(bool restoreCardsShoe)
        {
            string sessionID = GetSessionID();
            if (this.SessionIDGameRoom.ContainsKey(sessionID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[sessionID];
                gameRoom.RestoreGameStateFromFile(restoreCardsShoe);
            }
        }

        //设置从几打起
        public void SetBeginRank(string beginRankString)
        {
            string sessionID = GetSessionID();
            if (this.SessionIDGameRoom.ContainsKey(sessionID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[sessionID];
                gameRoom.SetBeginRank(beginRankString);
            }
        }

        //旁观玩家 by id
        public void ObservePlayerById(string playerId, string observerId)
        {
            string sessionID = GetSessionID();
            if (this.SessionIDGameRoom.ContainsKey(sessionID))
            {
                GameRoom gameRoom = this.SessionIDGameRoom[sessionID];
                gameRoom.ObservePlayerById(playerId, observerId);
            }
        }
        #endregion

        #region Update Client State
        public void UpdateGameHall()
        {
            List<string> names = PlayersProxy.Keys.ToList<string>();
            List<string> namesToCall = new List<string>();
            foreach (var name in names)
            {
                bool isInRoom = false;
                foreach (GameRoom room in this.GameRooms)
                {
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
            if (namesToCall.Count > 0)
            {
                IPlayerInvokeForAll(PlayersProxy, namesToCall, "NotifyGameHall", new List<object>() { this.RoomStates, names });
            }
        }

        #endregion
        public void IPlayerInvokeForAll(Dictionary<string, IPlayer> PlayersProxyToCall, List<string> playerIDs, string methodName, List<object> args)
        {
            List<string> badPlayerIDs = new List<string>();
            foreach (var playerID in playerIDs)
            {
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
            OperationContext context = OperationContext.Current;
            MessageProperties prop = context.IncomingMessageProperties;
            RemoteEndpointMessageProperty endpoint =
                   prop[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
            ip = endpoint.Address;
            return ip;
        }

        private string GetSessionID()
        {
            return OperationContext.Current.SessionId;
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