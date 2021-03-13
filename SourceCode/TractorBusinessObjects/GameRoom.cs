using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    public class GameRoom
    {
    //    private static readonly log4net.ILog log = log4net.LogManager.GetLogger
    //(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static string LogsFolder = "logs";
        public string LogsByRoomFolder;

        [DataMember]
        public int RoomID;
        [DataMember]
        public string RoomName;
        [DataMember]
        public GameState CurrentGameState;
        internal CurrentHandState CurrentHandState;
        internal CurrentTrickState CurrentTrickState;
        public CardsShoe CardsShoe { get; set; }

        public Dictionary<string, IPlayer> PlayersProxy { get; set; }
        public Dictionary<string, IPlayer> ObserversProxy { get; set; }

        public GameRoom(int roomID, string roomName)
        {
            RoomID = roomID;
            RoomName = roomName;
            LogsByRoomFolder = string.Format("{0}\\{1}", LogsFolder, roomID);
            CurrentGameState = new GameState();
            CurrentHandState = new CurrentHandState(this.CurrentGameState);
            CardsShoe = new CardsShoe();
            PlayersProxy = new Dictionary<string, IPlayer>();
            ObserversProxy = new Dictionary<string, IPlayer>();

        }

        #region implement interface ITractorHost
        public bool PlayerEnterRoom(string playerID, string clientIP, IPlayer player, bool allowSameIP)
        {
            if (!PlayersProxy.Keys.Contains(playerID) && !ObserversProxy.Keys.Contains(playerID))
            {
                if (PlayersProxy.Count >= 4)
                {
                    //防止双开旁观
                    string msg = string.Format("玩家【{0}】加入旁观", playerID);
                    if (CurrentGameState.Clients.Contains(clientIP))
                    {
                        LogClientInfo(clientIP, playerID, true);
                        //log.Debug(string.Format("observer {0}-{1} attempted double observing.", playerID, clientIP));
                        if (!allowSameIP)
                        {
                            msg += "？？失败";
                            PublishMessage(msg);
                            player.NotifyMessage("已在游戏中，请勿双开旁观");
                            return false;
                        }
                        else
                        {
                        	msg += "？？";
                            PublishMessage(msg);
                        }
                    }
                    else
                    {
                        LogClientInfo(clientIP, playerID, false);
                    }

                    player.NotifyMessage("房间已满，加入旁观");
                    //log.Debug(string.Format("observer {0}-{1} joined.", playerID, clientIP));

                    ObserversProxy.Add(playerID, player);
                    ObservePlayerById(CurrentGameState.Players[0].PlayerId, playerID);
                    return true;
                }

                for (int i = 0; i < 4; i++)
                {
                    if (CurrentGameState.Players[i] == null)
                    {
                        CurrentGameState.Players[i] = new PlayerEntity { PlayerId = playerID, Rank = 0, Team = GameTeam.None, Observers = new HashSet<string>() };
                        break;
                    }
                }
                LogClientInfo(clientIP, playerID, false);
                CurrentGameState.Clients.Add(clientIP);
                PlayersProxy.Add(playerID, player);
                //log.Debug(string.Format("player {0} joined.", playerID));
                if (PlayersProxy.Count == 4)
                {
                    //create team
                    CurrentGameState.Players[0].Team = GameTeam.VerticalTeam;
                    CurrentGameState.Players[2].Team = GameTeam.VerticalTeam;
                    CurrentGameState.Players[1].Team = GameTeam.HorizonTeam;
                    CurrentGameState.Players[3].Team = GameTeam.HorizonTeam;
                    //log.Debug("restart game");
                    foreach (var p in CurrentGameState.Players)
                    {
                        p.Rank = 0;
                    }
                    CurrentHandState.Rank = 0;
                    CurrentGameState.nextRestartID = GameState.RESTART_GAME;
                }
                UpdateGameState();
                return true;
            }
            else
            {
                if (PlayersProxy.Keys.Contains(playerID))
                    PlayersProxy[playerID].NotifyMessage("已在房间里");
                else if (ObserversProxy.Keys.Contains(playerID))
                {
                    List<string> badObs = new List<string>();
                    try
                    {
                        ObserversProxy[playerID].NotifyMessage("已在房间里旁观");
                        string obeserveeId = CurrentGameState.Players.Single(p => p != null && p.Observers.Contains(playerID)).PlayerId;
                        ObservePlayerById(obeserveeId, playerID);
                    }
                    catch (Exception)
                    {
                        badObs.Add(playerID);
                    }
                    RemoveObserver(badObs);
                }
                return false;
            }
        }

        public void PlayerIsReadyToStart(string playerID)
        {
            foreach (PlayerEntity p in CurrentGameState.Players)
            {
                if (p != null && p.PlayerId == playerID)
                {
                    p.IsReadyToStart = true;
                    break;
                }
            }
            UpdateGameState();
            int isReadyToStart = 0;
            foreach (PlayerEntity p in CurrentGameState.Players)
            {
                if (p != null && p.IsReadyToStart)
                {
                    isReadyToStart++;
                }
            }

            if (isReadyToStart == 4)
            {
                switch (CurrentGameState.nextRestartID)
                {
                    case GameState.RESTART_GAME:
                        RestartGame(CurrentHandState.Rank);
                        break;
                    case GameState.RESTART_CURRENT_HAND:
                        RestartCurrentHand();
                        break;
                    case GameState.START_NEXT_HAND:
                        StartNextHand(CurrentGameState.startNextHandStarter);
                        break;
                    default:
                        break;
                }
            }
        }

        public void PlayerToggleIsRobot(string playerID)
        {
            foreach (PlayerEntity p in CurrentGameState.Players)
            {
                if (p != null && p.PlayerId == playerID)
                {
                    p.IsRobot = !p.IsRobot;
                    break;
                }
            }
            UpdateGameState();
        }

        //玩家退出
        public void PlayerQuit(string playerId)
        {
            if (!PlayersProxy.ContainsKey(playerId))
            {
                List<string> badObs = new List<string>();
                badObs.Add(playerId);
                RemoveObserver(badObs);
                UpdateGameState();
                return;
            }
            //log.Debug(playerId + " quit.");
            PlayersProxy.Remove(playerId);
            for (int i = 0; i < 4; i++)
            {
                if (CurrentGameState.Players[i] != null)
                {
                    CurrentGameState.Players[i].Rank = 0;
                    CurrentGameState.Players[i].IsReadyToStart = false;
                    CurrentGameState.Players[i].IsRobot = false;
                    CurrentGameState.Players[i].Team = GameTeam.None;
                    foreach (string ob in CurrentGameState.Players[i].Observers)
                    {
                        ObserversProxy.Remove(ob);
                        // notify exit to hall
                    }
                    if (CurrentGameState.Players[i].PlayerId == playerId)
                    {
                        CurrentGameState.Players[i] = null;
                    }
                }
            }
            UpdateGameState();

            this.CurrentHandState = new CurrentHandState(this.CurrentGameState);
            this.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(this.CurrentGameState.Players.Count);
            CurrentHandState.IsFirstHand = true;
            UpdatePlayersCurrentHandState();

            foreach (var player in PlayersProxy.Values)
            {
                player.StartGame();
            }
        }

        //player discard last 8 cards
        public void StoreDiscardedCards(int[] cards)
        {
            this.CurrentHandState.DiscardedCards = cards;
            this.CurrentHandState.CurrentHandStep = HandStep.DiscardingLast8CardsFinished;
            foreach (var card in cards)
            {
                this.CurrentHandState.PlayerHoldingCards[this.CurrentHandState.Last8Holder].RemoveCard(card);
            }
            var logMsg = "player " + this.CurrentHandState.Last8Holder + " discard 8 cards: ";
            foreach (var card in cards)
            {
                logMsg += card.ToString() + ", ";
            }
            //log.Debug(logMsg);

            //log.Debug(this.CurrentHandState.Last8Holder + "'s cards after discard 8 cards: " + this.CurrentHandState.PlayerHoldingCards[this.CurrentHandState.Last8Holder].ToString());

            UpdatePlayersCurrentHandState();

            //等待5秒，让玩家反底
            var trump = CurrentHandState.Trump;
            Thread.Sleep(2000);
            if (trump == CurrentHandState.Trump) //没有玩家反
            {
                BeginNewTrick(this.CurrentHandState.Starter);
                this.CurrentHandState.CurrentHandStep = HandStep.Playing;
                UpdatePlayersCurrentHandState();
            }
        }

        //亮主
        public void PlayerMakeTrump(Duan.Xiugang.Tractor.Objects.TrumpExposingPoker trumpExposingPoker, Duan.Xiugang.Tractor.Objects.Suit trump, string trumpMaker)
        {
            lock (CurrentHandState)
            {
                //invalid user;
                if (PlayersProxy[trumpMaker] == null)
                    return;
                if (trumpExposingPoker > this.CurrentHandState.TrumpExposingPoker)
                {
                    this.CurrentHandState.TrumpExposingPoker = trumpExposingPoker;
                    this.CurrentHandState.TrumpMaker = trumpMaker;
                    this.CurrentHandState.Trump = trump;
                    if (this.CurrentHandState.IsFirstHand && this.CurrentHandState.CurrentHandStep < HandStep.DistributingLast8Cards)
                        this.CurrentHandState.Starter = trumpMaker;
                    //反底
                    if (this.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8CardsFinished)
                    {
                        this.CurrentHandState.Last8Holder = trumpMaker;
                        this.CurrentHandState.CurrentHandStep = HandStep.Last8CardsRobbed;
                        DistributeLast8Cards();
                    }
                    UpdatePlayersCurrentHandState();
                }
            }
        }

        public void PlayerShowCards(CurrentTrickState currentTrickState)
        {
            string lastestPlayer = currentTrickState.LatestPlayerShowedCard();
            if (PlayersProxy[lastestPlayer] != null)
            {
                this.CurrentTrickState.ShowedCards[lastestPlayer] = currentTrickState.ShowedCards[lastestPlayer];
                string cardsString = "";
                foreach (var card in this.CurrentTrickState.ShowedCards[lastestPlayer])
                {
                    cardsString += card.ToString() + " ";
                }
                //log.Debug("Player " + lastestPlayer + " showed cards: " + cardsString);
                //更新每个用户手中的牌在SERVER
                foreach (int card in this.CurrentTrickState.ShowedCards[lastestPlayer])
                {
                    this.CurrentHandState.PlayerHoldingCards[lastestPlayer].RemoveCard(card);
                }
                //即时更新旁观手牌
                UpdatePlayersCurrentHandState();
                //回合结束
                if (this.CurrentTrickState.AllPlayedShowedCards())
                {
                    this.CurrentTrickState.Winner = TractorRules.GetWinner(this.CurrentTrickState);
                    if (!string.IsNullOrEmpty(this.CurrentTrickState.Winner))
                    {
                        if (
                            !this.CurrentGameState.ArePlayersInSameTeam(CurrentHandState.Starter,
                                                                        this.CurrentTrickState.Winner))
                        {
                            CurrentHandState.Score += currentTrickState.Points;
                            UpdatePlayersCurrentHandState();
                        }


                        //log.Debug("Winner: " + this.CurrentTrickState.Winner);

                    }

                    UpdatePlayerCurrentTrickState();

                    CurrentHandState.LeftCardsCount -= currentTrickState.ShowedCards[lastestPlayer].Count;

                    //开始新的回合
                    if (this.CurrentHandState.LeftCardsCount > 0)
                    {
                        BeginNewTrick(this.CurrentTrickState.Winner);
                    }
                    else //所有牌都出完了
                    {
                        //扣底
                        CalculatePointsFromDiscarded8Cards();
                        Thread.Sleep(2000);
                        this.CurrentHandState.CurrentHandStep = HandStep.Ending;

                        foreach (PlayerEntity p in CurrentGameState.Players)
                        {
                            p.IsReadyToStart = false;
                            p.IsRobot = false;
                        }
                        this.CurrentGameState.nextRestartID = GameState.START_NEXT_HAND;
                        this.CurrentGameState.startNextHandStarter = this.CurrentGameState.NextRank(this.CurrentHandState, this.CurrentTrickState);
                        CurrentHandState.Starter = this.CurrentGameState.startNextHandStarter.PlayerId;

                        //检查是否本轮游戏结束
                        if (this.CurrentGameState.startNextHandStarter.Rank >= 13)
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (PlayerEntity player in CurrentGameState.Players)
                            {
                                if (player == null) continue;
                                player.Rank = 0;
                                if (player.Team == this.CurrentGameState.startNextHandStarter.Team)
                                    sb.Append(string.Format("【{0}】", player.PlayerId));
                            }
                            CurrentHandState.Rank = 0;
                            CurrentHandState.Starter = null;
                            PublishMessage(string.Format("恭喜{0}获胜！点击就绪重新开始游戏", sb.ToString()));

                            this.CurrentGameState.nextRestartID = GameState.RESTART_GAME;
                            this.CurrentGameState.startNextHandStarter = null;
                        }
                        UpdatePlayersCurrentHandState();

                        UpdateGameState();

                        SaveGameStateToFile();
                    }
                }
                else
                    UpdatePlayerCurrentTrickState();

            }
        }

        public ShowingCardsValidationResult ValidateDumpingCards(List<int> selectedCards, string playerId)
        {
            var result = TractorRules.IsLeadingCardsValid(this.CurrentHandState.PlayerHoldingCards, selectedCards,
                                                          playerId);
            result.PlayerId = playerId;

            if (result.ResultType == ShowingCardsValidationResultType.DumpingFail)
            {
                //甩牌失败，扣分：牌的张数x10
                int punishScore = selectedCards.Count * 10;
                if (
                    !this.CurrentGameState.ArePlayersInSameTeam(CurrentHandState.Starter,
                                                                playerId))
                {
                    CurrentHandState.Score -= punishScore;
                }
                else
                {
                    CurrentHandState.Score += punishScore;
                }
                //log.Debug("tried to dump cards and failed, punish score: " + punishScore);
                foreach (var player in PlayersProxy)
                {
                    if (player.Key != playerId)
                    {
                        player.Value.NotifyDumpingValidationResult(result);
                    }
                }
            }
            var cardString = "";
            foreach (var card in selectedCards)
            {
                cardString += card.ToString() + " ";
            }
            //log.Debug(playerId + " tried to dump cards: " + cardString + " Result: " + result.ResultType.ToString());
            return result;
        }

        public void RefreshPlayersCurrentHandState()
        {
            UpdatePlayersCurrentHandState();
        }

        //随机组队
        public void TeamUp()
        {
            bool isValid = true;
            foreach (PlayerEntity player in this.CurrentGameState.Players)
            {
                if (player == null || player.Team == GameTeam.None)
                {
                    isValid = false;
                    break;
                }
            }

            if (!isValid)
            {
                PublishMessage("随机组队失败：玩家人数不够");
                return;
            }

            //create team randomly
            ShuffleCurrentGameStatePlayers();
            CurrentGameState.Players[0].Team = GameTeam.VerticalTeam;
            CurrentGameState.Players[2].Team = GameTeam.VerticalTeam;
            CurrentGameState.Players[1].Team = GameTeam.HorizonTeam;
            CurrentGameState.Players[3].Team = GameTeam.HorizonTeam;
            //log.Debug("restart game");
            foreach (var p in CurrentGameState.Players)
            {
                p.Rank = 0;
                p.IsReadyToStart = false;
                p.IsRobot = false;
            }
            this.CurrentHandState = new CurrentHandState(this.CurrentGameState);
            this.CurrentHandState.Rank = 0;
            this.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(this.CurrentGameState.Players.Count);
            CurrentHandState.IsFirstHand = true;
            UpdateGameState();
            UpdatePlayersCurrentHandState();
            CurrentGameState.nextRestartID = GameState.RESTART_GAME;

            PublishMessage("随机组队成功！请点击就绪开始游戏");
        }

        //和下家互换座位
        public void MoveToNextPosition(string playerId)
        {
            bool isValid = true;
            foreach (PlayerEntity player in this.CurrentGameState.Players)
            {
                if (player == null || player.Team == GameTeam.None)
                {
                    isValid = false;
                    break;
                }
            }

            if (!isValid)
            {
                PublishMessage("和下家互换座位失败：玩家人数不够");
                return;
            }

            int meIndex = -1;
            for (int i = 0; i < 4; i++)
            {
                if (CurrentGameState.Players[i].PlayerId == playerId)
                {
                    meIndex = i;
                    break;
                }
            }

            int nextIndex = (meIndex + 1) % 4;
            string nextPlayerId = CurrentGameState.Players[nextIndex].PlayerId;
            PlayerEntity temp = CurrentGameState.Players[nextIndex];
            CurrentGameState.Players[nextIndex] = CurrentGameState.Players[meIndex];
            CurrentGameState.Players[meIndex] = temp;

            CurrentGameState.Players[0].Team = GameTeam.VerticalTeam;
            CurrentGameState.Players[2].Team = GameTeam.VerticalTeam;
            CurrentGameState.Players[1].Team = GameTeam.HorizonTeam;
            CurrentGameState.Players[3].Team = GameTeam.HorizonTeam;

            //log.Debug("restart game");
            foreach (var p in CurrentGameState.Players)
            {
                if (p == null) continue;
                p.Rank = 0;
                p.IsReadyToStart = false;
                p.IsRobot = false;
            }
            this.CurrentHandState = new CurrentHandState(this.CurrentGameState);
            this.CurrentHandState.Rank = 0;
            this.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(this.CurrentGameState.Players.Count);
            CurrentHandState.IsFirstHand = true;
            UpdateGameState();
            UpdatePlayersCurrentHandState();
            CurrentGameState.nextRestartID = GameState.RESTART_GAME;

            PublishMessage(string.Format("玩家【{0}】和下家【{1}】互换座位成功！请点击就绪开始游戏", playerId, nextPlayerId));
        }

        //旁观：选牌
        public void CardsReady(string playerId, ArrayList myCardIsReady)
        {
            PlayerEntity player = this.CurrentGameState.Players.Single(p => p != null && p.PlayerId == playerId);
            List<string> badObs = new List<string>();
            foreach (string ob in player.Observers)
            {
                try
                {
                    this.ObserversProxy[ob].NotifyCardsReady(myCardIsReady);
                }
                catch (Exception)
                {
                    badObs.Add(ob);
                }
            }
            RemoveObserver(badObs);
        }

        //读取牌局
        public void RestoreGameStateFromFile()
        {
            bool isValid = true;
            foreach (PlayerEntity player in this.CurrentGameState.Players)
            {
                if (player == null || player.Team == GameTeam.None)
                {
                    isValid = false;
                    break;
                }
            }

            if (!isValid)
            {
                PublishMessage("读取牌局失败：玩家人数不够");
                return;
            }

            Stream stream = null;
            Stream stream2 = null;
            try
            {
                DataContractSerializer ser = new DataContractSerializer(typeof(GameState));
                string fileNameGamestate = string.Format("{0}\\backup_gamestate.xml", this.LogsByRoomFolder);
                stream = new FileStream(fileNameGamestate, FileMode.Open, FileAccess.Read, FileShare.Read);
                GameState gs = (GameState)ser.ReadObject(stream);

                for (int i = 0; i < 4; i++)
                {
                    this.CurrentGameState.Players[i].Rank = gs.Players[i].Rank;
                }

                string fileNameHandState = string.Format("{0}\\backup_HandState.xml", this.LogsByRoomFolder);
                DataContractSerializer ser2 = new DataContractSerializer(typeof(CurrentHandState));
                stream2 = new FileStream(fileNameHandState, FileMode.Open, FileAccess.Read, FileShare.Read);
                CurrentHandState hs = (CurrentHandState)ser2.ReadObject(stream2);

                int lastStarterIndex = -1;
                for (int i = 0; i < 4; i++)
                {
                    if (gs.Players[i] != null && gs.Players[i].PlayerId == hs.Starter)
                    {
                        lastStarterIndex = i;
                        break;
                    }
                }

                if (lastStarterIndex < 0)
                {
                    PublishMessage("读取牌局失败：上盘牌局无庄家");
                    return;
                }

                PlayerEntity thisStarter = this.CurrentGameState.Players[lastStarterIndex];

                this.CurrentHandState = new CurrentHandState(this.CurrentGameState);
                this.CurrentHandState.Starter = thisStarter.PlayerId;
                this.CurrentHandState.Rank = thisStarter.Rank;
                this.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(this.CurrentGameState.Players.Count);

                if (stream != null)
                {
                    stream.Close();
                }
                if (stream2 != null)
                {
                    stream2.Close();
                }

                this.CurrentGameState.nextRestartID = GameState.START_NEXT_HAND;
                this.CurrentGameState.startNextHandStarter = thisStarter;

                for (int i = 0; i < 4; i++)
                {
                    this.CurrentGameState.Players[i].IsReadyToStart = false;
                    this.CurrentGameState.Players[i].IsRobot = false;
                }

                UpdateGameState();
                UpdatePlayersCurrentHandState();

                PublishMessage("读取牌局成功！请点击就绪继续上盘游戏");
            }
            catch (Exception ex)
            {
                PublishMessage("读取牌局失败：牌局存档文件读取失败");
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
                if (stream2 != null)
                {
                    stream2.Close();
                }
            }
        }

        //设置从几打起
        public void SetBeginRank(string beginRankString)
        {
            bool isValid = true;
            foreach (PlayerEntity player in this.CurrentGameState.Players)
            {
                if (player == null || player.Team == GameTeam.None)
                {
                    isValid = false;
                    break;
                }
            }

            if (!isValid)
            {
                PublishMessage(string.Format("设置从{0}打起失败：玩家人数不够", beginRankString));
                return;
            }
            int beginRank = 0;
            if (!int.TryParse(beginRankString, out beginRank))
            {
                switch (beginRankString)
                {
                    case "J":
                        beginRank = 9;
                        break;
                    case "Q":
                        beginRank = 10;
                        break;
                    case "K":
                        beginRank = 11;
                        break;
                    case "A":
                        beginRank = 12;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                beginRank -= 2;
            }
            for (int i = 0; i < 4; i++)
            {
                this.CurrentGameState.Players[i].Rank = beginRank;
                this.CurrentGameState.Players[i].IsReadyToStart = false;
                this.CurrentGameState.Players[i].IsRobot = false;
            }
            this.CurrentHandState = new CurrentHandState(this.CurrentGameState);
            this.CurrentHandState.Rank = beginRank;
            this.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(this.CurrentGameState.Players.Count);
            CurrentHandState.IsFirstHand = true;

            UpdateGameState();
            UpdatePlayersCurrentHandState();

            CurrentGameState.nextRestartID = GameState.RESTART_GAME;

            PublishMessage(string.Format("设置从{0}打起成功！请点击就绪开始游戏", beginRankString));
        }

        //旁观玩家 by id
        public void ObservePlayerById(string playerId, string observerId)
        {
            foreach (PlayerEntity player in CurrentGameState.Players)
            {
                if (player != null)
                {
                    if (player.Observers.Contains(observerId))
                    {
                        player.Observers.Remove(observerId);
                    }
                    if (player.PlayerId == playerId)
                    {
                        player.Observers.Add(observerId);
                    }
                }
            }
            //即时更新旁观手牌
            UpdateGameState();
            UpdatePlayersCurrentHandState();
        }
        #endregion

        #region Host Action
        public void RestartGame(int curRank)
        {
            //log.Debug("restart game with current set rank");

            this.CurrentHandState = new CurrentHandState(this.CurrentGameState);
            this.CurrentHandState.Rank = curRank;
            this.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(this.CurrentGameState.Players.Count);
            CurrentHandState.IsFirstHand = true;
            UpdatePlayersCurrentHandState();
            var currentHandId = this.CurrentHandState.Id;
            DistributeCards();
            if (this.CurrentHandState.Id != currentHandId)
                return;

            // reset timer everytime Trump is modified
            var oldTrump = this.CurrentHandState.Trump;
            while (true)
            {
                PublishStartTimer(5);
                //加一秒缓冲时间，让客户端倒计时完成
                Thread.Sleep(5000 + 1000);
                if (this.CurrentHandState.Trump == oldTrump)
                {
                    break;
                }
                oldTrump = this.CurrentHandState.Trump;
            }

            if (this.CurrentHandState.Trump != Suit.None)
                DistributeLast8Cards();
            else if (PlayersProxy.Count == 4)
                RestartGame(curRank);
        }

        public void RestartCurrentHand()
        {
            //log.Debug("restart current hand, starter: " + this.CurrentHandState.Starter + " Rank: " + this.CurrentHandState.Rank.ToString());
            StartNextHand(this.CurrentGameState.Players.Single(p => p != null && p.PlayerId == this.CurrentHandState.Starter));
        }

        public void StartNextHand(PlayerEntity nextStarter)
        {
            UpdateGameState();
            this.CurrentHandState = new CurrentHandState(this.CurrentGameState);
            this.CurrentHandState.Starter = nextStarter.PlayerId;
            this.CurrentHandState.Rank = nextStarter.Rank;
            this.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(this.CurrentGameState.Players.Count);

            //log.Debug("start next hand, starter: " + this.CurrentHandState.Starter + " Rank: " + this.CurrentHandState.Rank.ToString());

            UpdatePlayersCurrentHandState();

            var currentHandId = this.CurrentHandState.Id;

            DistributeCards();
            if (this.CurrentHandState.Id != currentHandId)
                return;

            // reset timer everytime Trump is modified
            var oldTrump = this.CurrentHandState.Trump;
            while (true)
            {
                PublishStartTimer(5);
                Thread.Sleep(5000 + 1000);
                if (this.CurrentHandState.Trump == oldTrump)
                {
                    break;
                }
                oldTrump = this.CurrentHandState.Trump;
            }
            if (this.CurrentHandState.Trump != Suit.None)
                DistributeLast8Cards();

            else if (PlayersProxy.Count == 4)
            {
                //如果庄家TEAM亮不起，则庄家的下家成为新的庄家
                var nextStarter2 = CurrentGameState.GetNextPlayerAfterThePlayer(false, CurrentHandState.Starter);
                this.CurrentHandState.Starter = nextStarter2.PlayerId;
                this.CurrentHandState.Rank = nextStarter2.Rank;
                //log.Debug("starter team fail to make trump, next starter: " + this.CurrentHandState.Starter + " Rank: " + this.CurrentHandState.Rank.ToString());

                UpdatePlayersCurrentHandState();

                //10 seconds to make trump
                // reset timer everytime Trump is modified
                var oldTrump2 = this.CurrentHandState.Trump;
                while (true)
                {
                    PublishStartTimer(5);
                    Thread.Sleep(5000 + 1000);
                    if (this.CurrentHandState.Trump == oldTrump2)
                    {
                        break;
                    }
                    oldTrump2 = this.CurrentHandState.Trump;
                }
                if (this.CurrentHandState.Trump != Suit.None)
                    DistributeLast8Cards();
                else if (PlayersProxy.Count == 4)
                {
                    //如果下家也亮不起，重新发牌
                    StartNextHand(nextStarter);
                }
            }
        }

        //发牌
        public void DistributeCards()
        {
            CurrentHandState.CurrentHandStep = HandStep.DistributingCards;
            UpdatePlayersCurrentHandState();
            string currentHandId = this.CurrentHandState.Id;
            this.CardsShoe.Shuffle();
            int cardNumberofEachPlayer = TractorRules.GetCardNumberofEachPlayer(this.CurrentGameState.Players.Count);
            int j = 0;

            var LogList = new Dictionary<string, StringBuilder>();
            foreach (var player in PlayersProxy)
            {
                LogList[player.Key] = new StringBuilder();
            }

            for (int i = 0; i < cardNumberofEachPlayer; i++)
            {
                foreach (var player in PlayersProxy)
                {
                    var index = j++;
                    if (this.CurrentHandState.Id == currentHandId)
                    {
                        player.Value.GetDistributedCard(CardsShoe.Cards[index]);
                        //旁观：发牌
                        PlayerEntity pe = this.CurrentGameState.Players.Single(p => p != null && p.PlayerId == player.Key);
                        List<string> badObs = new List<string>();
                        foreach (string ob in pe.Observers)
                        {
                            try
                            {
                                ObserversProxy[ob].GetDistributedCard(CardsShoe.Cards[index]);
                            }
                            catch (Exception)
                            {
                                badObs.Add(ob);
                            }
                        }
                        RemoveObserver(badObs);
                        LogList[player.Key].Append(CardsShoe.Cards[index].ToString() + ", ");
                    }
                    else
                        return;

                    this.CurrentHandState.PlayerHoldingCards[player.Key].AddCard(CardsShoe.Cards[index]);
                }
                Thread.Sleep(500);
            }

            //log.Debug("distribute cards to each player: ");
            foreach (var logItem in LogList)
            {
                //log.Debug(logItem.Key + ": " + logItem.Value.ToString());
            }

            foreach (var keyvalue in this.CurrentHandState.PlayerHoldingCards)
            {
                //log.Debug(keyvalue.Key + "'s cards:  " + keyvalue.Value.ToString());
            }

            CurrentHandState.CurrentHandStep = HandStep.DistributingCardsFinished;
            UpdatePlayersCurrentHandState();
        }

        //发底牌
        public void DistributeLast8Cards()
        {
            var last8Cards = new int[8];
            if (CurrentHandState.CurrentHandStep == HandStep.DistributingCardsFinished)
            {
                for (int i = 0; i < 8; i++)
                {
                    last8Cards[i] = CardsShoe.Cards[CardsShoe.Cards.Length - i - 1];
                }
            }
            else if (CurrentHandState.CurrentHandStep == HandStep.Last8CardsRobbed)
            {
                last8Cards = CurrentHandState.DiscardedCards;
            }

            CurrentHandState.CurrentHandStep = HandStep.DistributingLast8Cards;
            UpdatePlayersCurrentHandState();


            IPlayer last8Holder = PlayersProxy.SingleOrDefault(p => p.Key == CurrentHandState.Last8Holder).Value;
            if (last8Holder != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    var card = last8Cards[i];
                    last8Holder.GetDistributedCard(card);
                    //旁观：发牌
                    PlayerEntity pe = this.CurrentGameState.Players.Single(p => p != null && p.PlayerId == CurrentHandState.Last8Holder);
                    List<string> badObs = new List<string>();
                    foreach (string ob in pe.Observers)
                    {
                        try
                        {
                            ObserversProxy[ob].GetDistributedCard(card);
                        }
                        catch (Exception)
                        {
                            badObs.Add(ob);
                        }
                    }
                    RemoveObserver(badObs);
                    this.CurrentHandState.PlayerHoldingCards[CurrentHandState.Last8Holder].AddCard(card);
                }
            }

            var logMsg = "distribute last 8 cards to " + CurrentHandState.Last8Holder + ", cards: ";
            foreach (var card in last8Cards)
            {
                logMsg += card.ToString() + ", ";
            }
            //log.Debug(logMsg);

            CurrentHandState.CurrentHandStep = HandStep.DistributingLast8CardsFinished;
            UpdatePlayersCurrentHandState();

            Thread.Sleep(100);
            CurrentHandState.CurrentHandStep = HandStep.DiscardingLast8Cards;
            UpdatePlayersCurrentHandState();
        }

        //begin new trick
        private void BeginNewTrick(string leader)
        {
            List<String> playerIDList = new List<string>();
            foreach (PlayerEntity p in this.CurrentGameState.Players)
            {
                if (p == null) continue;
                playerIDList.Add(p.PlayerId);
            }
            this.CurrentTrickState = new CurrentTrickState(playerIDList);
            this.CurrentTrickState.Learder = leader;
            this.CurrentTrickState.Trump = CurrentHandState.Trump;
            this.CurrentTrickState.Rank = CurrentHandState.Rank;
            UpdatePlayerCurrentTrickState();
        }

        //计算被扣底牌的分数，然后加到CurrentHandState.Score
        private void CalculatePointsFromDiscarded8Cards()
        {
            if (!this.CurrentTrickState.AllPlayedShowedCards())
                return;
            if (this.CurrentHandState.LeftCardsCount > 0)
                return;

            var cards = this.CurrentTrickState.ShowedCards[this.CurrentTrickState.Winner];
            var cardscp = new CurrentPoker(cards, (int)this.CurrentHandState.Trump,
                                           this.CurrentHandState.Rank);

            //最后一把牌的赢家不跟庄家一伙
            if (!this.CurrentGameState.ArePlayersInSameTeam(this.CurrentHandState.Starter,
                                                    this.CurrentTrickState.Winner))
            {

                var points = 0;
                foreach (var card in this.CurrentHandState.DiscardedCards)
                {
                    if (card % 13 == 3)
                        points += 5;
                    else if (card % 13 == 8)
                        points += 10;
                    else if (card % 13 == 11)
                        points += 10;
                }

                if (cardscp.HasTractors())
                {
                    points *= 8;
                }
                else if (cardscp.GetPairs().Count > 0)
                {
                    points *= 4;
                }
                else
                {
                    points *= 2;
                }

                this.CurrentHandState.Score += points;


            }
        }

        //保存牌局
        private void SaveGameStateToFile()
        {
            if (string.IsNullOrEmpty(this.CurrentHandState.Starter)) return;
            Stream stream = null;
            Stream stream2 = null;
            try
            {
                string fileNameGamestate = string.Format("{0}\\backup_gamestate.xml", this.LogsByRoomFolder);
                stream = new FileStream(fileNameGamestate, FileMode.Create, FileAccess.Write, FileShare.None);
                DataContractSerializer ser = new DataContractSerializer(typeof(GameState));
                ser.WriteObject(stream, this.CurrentGameState);

                string fileNameHandState = string.Format("{0}\\backup_HandState.xml", this.LogsByRoomFolder);
                stream2 = new FileStream(fileNameHandState, FileMode.Create, FileAccess.Write, FileShare.None);
                DataContractSerializer ser2 = new DataContractSerializer(typeof(CurrentHandState));
                ser2.WriteObject(stream2, this.CurrentHandState);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
                if (stream2 != null)
                {
                    stream2.Close();
                }
            }
        }
        #endregion

        #region Update Client State
        public void UpdateGameState()
        {
            foreach (IPlayer player in PlayersProxy.Values)
            {
                player.NotifyGameState(this.CurrentGameState);
            }
            List<string> badObs = new List<string>();
            foreach (var player in ObserversProxy)
            {
                try
                {
                    player.Value.NotifyGameState(this.CurrentGameState);
                }
                catch (Exception)
                {
                    badObs.Add(player.Key);
                }
            }
            RemoveObserver(badObs);
        }

        public void UpdatePlayersCurrentHandState()
        {
            foreach (IPlayer player in PlayersProxy.Values)
            {
                player.NotifyCurrentHandState(this.CurrentHandState);
            }
            List<string> badObs = new List<string>();
            foreach (var player in ObserversProxy)
            {
                try
                {
                    player.Value.NotifyCurrentHandState(this.CurrentHandState);
                }
                catch (Exception)
                {
                    badObs.Add(player.Key);
                }
            }
            RemoveObserver(badObs);
        }

        public void UpdatePlayerCurrentTrickState()
        {
            foreach (IPlayer player in PlayersProxy.Values)
            {
                player.NotifyCurrentTrickState(this.CurrentTrickState);
            }
            List<string> badObs = new List<string>();
            foreach (var player in ObserversProxy)
            {
                try
                {
                    player.Value.NotifyCurrentTrickState(this.CurrentTrickState);
                }
                catch (Exception)
                {
                    badObs.Add(player.Key);
                }
            }
            RemoveObserver(badObs);
        }

        public void PublishMessage(string msg)
        {
            foreach (IPlayer player in PlayersProxy.Values)
            {
                player.NotifyMessage(msg);
            }
            List<string> badObs = new List<string>();
            foreach (var player in ObserversProxy)
            {
                try
                {
                    player.Value.NotifyMessage(msg);
                }
                catch (Exception)
                {
                    badObs.Add(player.Key);
                }
            }
            RemoveObserver(badObs);
        }

        public void PublishStartTimer(int timerLength)
        {
            foreach (IPlayer player in PlayersProxy.Values)
            {
                player.NotifyStartTimer(timerLength);
            }
            List<string> badObs = new List<string>();
            foreach (var player in ObserversProxy)
            {
                try
                {
                    player.Value.NotifyStartTimer(timerLength);
                }
                catch (Exception)
                {
                    badObs.Add(player.Key);
                }
            }
            RemoveObserver(badObs);
        }

        #endregion

        public static void LogClientInfo(string clientIP, string playerID, bool isCheating)
        {
            Stream streamRW = null;
            try
            {
                Dictionary<string, ClientInfo> clientInfoDict = new Dictionary<string, ClientInfo>();
                string fileName = string.Format("{0}\\clientinfo.xml", LogsFolder);
                DataContractSerializer ser = new DataContractSerializer(typeof(Dictionary<string, ClientInfo>));
                if (File.Exists(fileName))
                {
                    streamRW = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                    clientInfoDict = (Dictionary<string, ClientInfo>)ser.ReadObject(streamRW);
                }
                else
                {
                    streamRW = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                }


                if (!clientInfoDict.ContainsKey(clientIP))
                {
                    clientInfoDict[clientIP] = new ClientInfo(clientIP, playerID);
                }
                clientInfoDict[clientIP].logLogin(playerID, isCheating);
                streamRW.SetLength(0);
                ser.WriteObject(streamRW, clientInfoDict);
            }
            catch (Exception ex)
            {
                //log.Debug(string.Format("failed update clientinfo for {0}-{1}-{2}", clientIP, playerID, isCheating));
                //log.Debug(string.Format("exception: {0}", ex.Message));
            }
            finally
            {
                if (streamRW != null)
                {
                    streamRW.Close();
                }
            }
        }

        public void ShuffleCurrentGameStatePlayers()
        {
            List<PlayerEntity> shuffledPlayers = new List<PlayerEntity>();
            int N = CurrentGameState.Players.Count;
            for (int i = N; i >= 1; i--)
            {
                int r = new Random().Next(i);
                PlayerEntity curPlayer = CurrentGameState.Players[r];
                shuffledPlayers.Add(curPlayer);
                CurrentGameState.Players.Remove(curPlayer);
            }
            for (int i = 0; i < N; i++)
            {
                CurrentGameState.Players.Add(shuffledPlayers[i]);
            }
            shuffledPlayers.Clear();
        }

        private void RemoveObserver(List<string> badObs)
        {
            foreach (string playerId in badObs)
            {
                //log.Debug(playerId + " quit as observer.");
                ObserversProxy.Remove(playerId);
                foreach (PlayerEntity p in CurrentGameState.Players)
                {
                    if (p == null) continue;
                    if (p.Observers.Contains(playerId))
                    {
                        p.Observers.Remove(playerId);
                        break;
                    }
                }
            }
        }
    }
}
