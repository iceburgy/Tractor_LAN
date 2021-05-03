using Duan.Xiugang.Tractor.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace TractorServer
{
    public class GameRoom
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static string LogsFolder = "logs";
        public string LogsByRoomFolder;

        public RoomState CurrentRoomState;
        public CardsShoe CardsShoe { get; set; }

        public Dictionary<string, IPlayer> PlayersProxy { get; set; }
        public Dictionary<string, IPlayer> ObserversProxy { get; set; }
        private Dictionary<string, List<int>> lastShowedCards;

        public GameRoom(int roomID, string roomName)
        {
            CurrentRoomState = new RoomState(roomID, roomName);
            LogsByRoomFolder = string.Format("{0}\\{1}", LogsFolder, roomID);
            CardsShoe = new CardsShoe();
            PlayersProxy = new Dictionary<string, IPlayer>();
            ObserversProxy = new Dictionary<string, IPlayer>();
            lastShowedCards = new Dictionary<string, List<int>>();
        }

        #region implement interface ITractorHost
        public bool PlayerEnterRoom(string playerID, string clientIP, IPlayer player, bool allowSameIP, int posID)
        {
            if (!PlayersProxy.Keys.Contains(playerID) && !ObserversProxy.Keys.Contains(playerID))
            {
                if (PlayersProxy.Count >= 4)
                {
                    //防止双开旁观
                    string msg = string.Format("玩家【{0}】加入旁观", playerID);
                    if (CurrentRoomState.CurrentGameState.PlayerToIP.ContainsValue(clientIP))
                    {
                        LogClientInfo(clientIP, playerID, true);
                        log.Debug(string.Format("observer {0}-{1} attempted double observing.", playerID, clientIP));
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
                    log.Debug(string.Format("observer {0}-{1} joined.", playerID, clientIP));

                    ObserversProxy.Add(playerID, player);
                    ObservePlayerById(CurrentRoomState.CurrentGameState.Players[0].PlayerId, playerID);
                    return true;
                }

                if (posID>=0 && CurrentRoomState.CurrentGameState.Players[posID] == null)
                {
                    CurrentRoomState.CurrentGameState.Players[posID] = new PlayerEntity { PlayerId = playerID, Rank = 0, Team = GameTeam.None, Observers = new HashSet<string>() };
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (CurrentRoomState.CurrentGameState.Players[i] == null)
                        {
                            CurrentRoomState.CurrentGameState.Players[i] = new PlayerEntity { PlayerId = playerID, Rank = 0, Team = GameTeam.None, Observers = new HashSet<string>() };
                            break;
                        }
                    }
                }
                LogClientInfo(clientIP, playerID, false);
                CurrentRoomState.CurrentGameState.PlayerToIP.Add(playerID, clientIP);
                PlayersProxy.Add(playerID, player);
                log.Debug(string.Format("player {0} joined.", playerID));
                if (PlayersProxy.Count == 4)
                {
                    //create team
                    CurrentRoomState.CurrentGameState.Players[0].Team = GameTeam.VerticalTeam;
                    CurrentRoomState.CurrentGameState.Players[2].Team = GameTeam.VerticalTeam;
                    CurrentRoomState.CurrentGameState.Players[1].Team = GameTeam.HorizonTeam;
                    CurrentRoomState.CurrentGameState.Players[3].Team = GameTeam.HorizonTeam;
                    log.Debug("restart game");
                    foreach (var p in CurrentRoomState.CurrentGameState.Players)
                    {
                        p.Rank = 0;
                    }
                    CurrentRoomState.CurrentHandState.Rank = 0;
                    CurrentRoomState.CurrentGameState.nextRestartID = GameState.RESTART_GAME;
                }
                UpdateGameState();
                return true;
            }
            else
            {
                if (PlayersProxy.Keys.Contains(playerID))
                    IPlayerInvoke(playerID, PlayersProxy[playerID], "NotifyMessage", new List<object>() { "已在房间里" }, true);
                else if (ObserversProxy.Keys.Contains(playerID))
                {
                    List<string> badObs = new List<string>();
                    try
                    {
                        IPlayerInvoke(playerID, ObserversProxy[playerID], "NotifyMessage", new List<object>() { "已在房间里" }, true);
                        string obeserveeId = CurrentRoomState.CurrentGameState.Players.Single(p => p != null && p.Observers.Contains(playerID)).PlayerId;
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

        //玩家退出
        // returns: needRestart
        public bool PlayerQuit(List<string> playerIDs)
        {
            bool needsRestart = false;
            foreach (string playerID in playerIDs)
            {
                if (PlayersProxy.ContainsKey(playerID))
                {
                    needsRestart = true;
                    break;
                }
            }

            if (needsRestart)
            {
                CurrentRoomState.CurrentHandState = new CurrentHandState(CurrentRoomState.CurrentGameState);
                CurrentRoomState.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);
                CurrentRoomState.CurrentHandState.IsFirstHand = true;
                UpdatePlayersCurrentHandState();
            }

            foreach (string playerID in playerIDs)
            {
                PlayerQuitWorker(playerID);
            }

            if (needsRestart)
            {
                UpdateGameState();
                IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "StartGame", new List<object>() { });
            }

            return needsRestart;
        }

        // returns: needRestart
        public void PlayerQuitWorker(string playerID)
        {
            if (!PlayersProxy.ContainsKey(playerID))
            {
                List<string> badObs = new List<string>();
                badObs.Add(playerID);
                RemoveObserver(badObs);
                UpdateGameState();
                return;
            }

            CurrentRoomState.CurrentGameState.PlayerToIP.Remove(playerID);
            log.Debug(playerID + " quit.");
            PlayersProxy.Remove(playerID);
            for (int i = 0; i < 4; i++)
            {
                if (CurrentRoomState.CurrentGameState.Players[i] != null)
                {
                    CurrentRoomState.CurrentGameState.Players[i].Rank = 0;
                    CurrentRoomState.CurrentGameState.Players[i].IsReadyToStart = false;
                    CurrentRoomState.CurrentGameState.Players[i].IsRobot = false;
                    CurrentRoomState.CurrentGameState.Players[i].Team = GameTeam.None;
                    foreach (string ob in CurrentRoomState.CurrentGameState.Players[i].Observers)
                    {
                        ObserversProxy.Remove(ob);
                        // notify exit to hall
                    }
                    if (CurrentRoomState.CurrentGameState.Players[i].PlayerId == playerID)
                    {
                        CurrentRoomState.CurrentGameState.Players[i] = null;
                    }
                }
            }
        }

        //清理断线玩家
        // returns: needRestart
        public bool PlayerQuitFromCleanup(List<string> playerIDs)
        {
            bool needsRestart = false;
            foreach (string playerID in playerIDs)
            {
                if (!PlayersProxy.ContainsKey(playerID))
                {
                    List<string> badObs = new List<string>();
                    badObs.Add(playerID);
                    RemoveObserver(badObs);
                    continue;
                }

                needsRestart = true;
                CurrentRoomState.CurrentGameState.PlayerToIP.Remove(playerID);
                log.Debug(playerID + " quit.");
                PlayersProxy.Remove(playerID);
                for (int i = 0; i < 4; i++)
                {
                    if (CurrentRoomState.CurrentGameState.Players[i] != null)
                    {
                        CurrentRoomState.CurrentGameState.Players[i].Rank = 0;
                        CurrentRoomState.CurrentGameState.Players[i].IsReadyToStart = false;
                        CurrentRoomState.CurrentGameState.Players[i].IsRobot = false;
                        CurrentRoomState.CurrentGameState.Players[i].Team = GameTeam.None;
                        foreach (string ob in CurrentRoomState.CurrentGameState.Players[i].Observers)
                        {
                            ObserversProxy.Remove(ob);
                            // notify exit to hall
                        }
                        if (CurrentRoomState.CurrentGameState.Players[i].PlayerId == playerID)
                        {
                            CurrentRoomState.CurrentGameState.Players[i] = null;
                        }
                    }
                }
            }
            return needsRestart;
        }

        public void PlayerIsReadyToStart(string playerID)
        {
            foreach (PlayerEntity p in CurrentRoomState.CurrentGameState.Players)
            {
                if (p != null && p.PlayerId == playerID)
                {
                    p.IsReadyToStart = true;
                    break;
                }
            }
            UpdateGameState();
            int isReadyToStart = 0;
            foreach (PlayerEntity p in CurrentRoomState.CurrentGameState.Players)
            {
                if (p != null && p.IsReadyToStart)
                {
                    isReadyToStart++;
                }
            }

            if (isReadyToStart == 4)
            {
                switch (CurrentRoomState.CurrentGameState.nextRestartID)
                {
                    case GameState.RESTART_GAME:
                        RestartGame(CurrentRoomState.CurrentHandState.Rank);
                        break;
                    case GameState.RESTART_CURRENT_HAND:
                        RestartCurrentHand();
                        break;
                    case GameState.START_NEXT_HAND:
                        //清空缓存
                        this.lastShowedCards.Clear();
                        CurrentRoomState.CurrentTrickState.ShowedCardsInLastTrick.Clear();

                        StartNextHand(CurrentRoomState.CurrentGameState.startNextHandStarter);
                        break;
                    default:
                        break;
                }
            }
        }

        public void PlayerToggleIsRobot(string playerID)
        {
            foreach (PlayerEntity p in CurrentRoomState.CurrentGameState.Players)
            {
                if (p != null && p.PlayerId == playerID)
                {
                    p.IsRobot = !p.IsRobot;
                    break;
                }
            }
            UpdateGameState();
        }

        //player discard last 8 cards
        public void StoreDiscardedCards(int[] cards)
        {
            CurrentRoomState.CurrentHandState.DiscardedCards = cards;
            CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.DiscardingLast8CardsFinished;
            foreach (var card in cards)
            {
                CurrentRoomState.CurrentHandState.PlayerHoldingCards[CurrentRoomState.CurrentHandState.Last8Holder].RemoveCard(card);
            }
            var logMsg = "player " + CurrentRoomState.CurrentHandState.Last8Holder + " discard 8 cards: ";
            foreach (var card in cards)
            {
                logMsg += card.ToString() + ", ";
            }
            log.Debug(logMsg);

            log.Debug(CurrentRoomState.CurrentHandState.Last8Holder + "'s cards after discard 8 cards: " + CurrentRoomState.CurrentHandState.PlayerHoldingCards[CurrentRoomState.CurrentHandState.Last8Holder].ToString());

            UpdatePlayersCurrentHandState();

            //等待5秒，让玩家反底
            var trump = CurrentRoomState.CurrentHandState.Trump;
            Thread.Sleep(2000);
            if (trump == CurrentRoomState.CurrentHandState.Trump) //没有玩家反
            {
                BeginNewTrick(CurrentRoomState.CurrentHandState.Starter);
                CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.Playing;
                UpdatePlayersCurrentHandState();
            }
        }

        //亮主
        public void PlayerMakeTrump(Duan.Xiugang.Tractor.Objects.TrumpExposingPoker trumpExposingPoker, Duan.Xiugang.Tractor.Objects.Suit trump, string trumpMaker)
        {
            lock (CurrentRoomState.CurrentHandState)
            {
                //invalid user;
                if (PlayersProxy[trumpMaker] == null)
                    return;
                if (trumpExposingPoker > CurrentRoomState.CurrentHandState.TrumpExposingPoker)
                {
                    CurrentRoomState.CurrentHandState.TrumpExposingPoker = trumpExposingPoker;
                    CurrentRoomState.CurrentHandState.TrumpMaker = trumpMaker;
                    CurrentRoomState.CurrentHandState.Trump = trump;

                    TrumpState tempLastTrumState = new TrumpState();
                    tempLastTrumState.TrumpExposingPoker = CurrentRoomState.CurrentHandState.TrumpExposingPoker;
                    tempLastTrumState.TrumpMaker = CurrentRoomState.CurrentHandState.TrumpMaker;
                    tempLastTrumState.Trump = CurrentRoomState.CurrentHandState.Trump;
                    CurrentRoomState.CurrentHandState.LastTrumpStates.Add(tempLastTrumState);

                    if (CurrentRoomState.CurrentHandState.IsFirstHand && CurrentRoomState.CurrentHandState.CurrentHandStep < HandStep.DistributingLast8Cards)
                        CurrentRoomState.CurrentHandState.Starter = trumpMaker;
                    //反底
                    if (CurrentRoomState.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8CardsFinished)
                    {
                        CurrentRoomState.CurrentHandState.Last8Holder = trumpMaker;
                        CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.Last8CardsRobbed;
                        if (DistributeLast8Cards()) return;
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
                CurrentRoomState.CurrentTrickState.ShowedCards[lastestPlayer] = currentTrickState.ShowedCards[lastestPlayer];
                string cardsString = "";
                foreach (var card in CurrentRoomState.CurrentTrickState.ShowedCards[lastestPlayer])
                {
                    cardsString += card.ToString() + " ";
                }
                log.Debug("Player " + lastestPlayer + " showed cards: " + cardsString);
                //更新每个用户手中的牌在SERVER
                foreach (int card in CurrentRoomState.CurrentTrickState.ShowedCards[lastestPlayer])
                {
                    CurrentRoomState.CurrentHandState.PlayerHoldingCards[lastestPlayer].RemoveCard(card);
                }
                //即时更新旁观手牌
                UpdatePlayersCurrentHandState();
                //回合结束
                if (CurrentRoomState.CurrentTrickState.AllPlayedShowedCards())
                {
                    CurrentRoomState.CurrentTrickState.Winner = TractorRules.GetWinner(CurrentRoomState.CurrentTrickState);
                    if (!string.IsNullOrEmpty(CurrentRoomState.CurrentTrickState.Winner))
                    {
                        if (
                            !CurrentRoomState.CurrentGameState.ArePlayersInSameTeam(CurrentRoomState.CurrentHandState.Starter,
                                                                        CurrentRoomState.CurrentTrickState.Winner))
                        {
                            CurrentRoomState.CurrentHandState.Score += currentTrickState.Points;
                            //收集得分牌
                            CurrentRoomState.CurrentHandState.ScoreCards.AddRange(currentTrickState.ScoreCards);
                            UpdatePlayersCurrentHandState();
                        }


                        log.Debug("Winner: " + CurrentRoomState.CurrentTrickState.Winner);

                    }
                    lastShowedCards = CurrentRoomState.CurrentTrickState.ShowedCards.ToDictionary(entry => entry.Key, entry => entry.Value.ToList());

                    UpdatePlayerCurrentTrickState();

                    CurrentRoomState.CurrentHandState.LeftCardsCount -= currentTrickState.ShowedCards[lastestPlayer].Count;

                    //开始新的回合
                    if (CurrentRoomState.CurrentHandState.LeftCardsCount > 0)
                    {
                        BeginNewTrick(CurrentRoomState.CurrentTrickState.Winner);
                    }
                    else //所有牌都出完了
                    {
                        //扣底
                        CalculatePointsFromDiscarded8Cards();
                        PublishStartTimer(5);
                        Thread.Sleep(5000 + 1000);

                        //如果有人退出，则停止游戏
                        if (this.PlayersProxy.Count < 4)
                        {
                            return;
                        }

                        //本局结束画面，更新最后一轮出的牌
                        CurrentRoomState.CurrentTrickState.ShowedCardsInLastTrick = lastShowedCards;
                        UpdatePlayerCurrentTrickState();

                        CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.Ending;

                        if (!TractorHost.gameConfig.IsFullDebug)
                        {
                            foreach (PlayerEntity p in CurrentRoomState.CurrentGameState.Players)
                            {
                                p.IsReadyToStart = false;
                                p.IsRobot = false;
                            }
                        }
                        CurrentRoomState.CurrentGameState.nextRestartID = GameState.START_NEXT_HAND;
                        CurrentRoomState.CurrentGameState.startNextHandStarter = CurrentRoomState.CurrentGameState.NextRank(CurrentRoomState.CurrentHandState, CurrentRoomState.CurrentTrickState);
                        CurrentRoomState.CurrentHandState.Starter = CurrentRoomState.CurrentGameState.startNextHandStarter.PlayerId;

                        //检查是否本轮游戏结束
                        StringBuilder sb = null;
                        if (CurrentRoomState.CurrentGameState.startNextHandStarter.Rank >= 13)
                        {
                            sb = new StringBuilder();
                            foreach (PlayerEntity player in CurrentRoomState.CurrentGameState.Players)
                            {
                                if (player == null) continue;
                                player.Rank = 0;
                                if (player.Team == CurrentRoomState.CurrentGameState.startNextHandStarter.Team)
                                    sb.Append(string.Format("【{0}】", player.PlayerId));
                            }
                            CurrentRoomState.CurrentHandState.Rank = 0;
                            CurrentRoomState.CurrentHandState.Starter = null;

                            CurrentRoomState.CurrentGameState.nextRestartID = GameState.RESTART_GAME;
                            CurrentRoomState.CurrentGameState.startNextHandStarter = null;
                        }
                        UpdatePlayersCurrentHandState();

                        UpdateGameState();

                        SaveGameStateToFile();
                        if (sb != null)
                        {
                            PublishMessage(string.Format("恭喜{0}获胜！点击就绪重新开始游戏", sb.ToString()));
                        }
                        else if (TractorHost.gameConfig.IsFullDebug)
                        {
                            Thread.Sleep(3000);
                            StartNextHand(CurrentRoomState.CurrentGameState.startNextHandStarter);
                        }
                    }
                }
                else
                {
                    if (CurrentRoomState.CurrentTrickState.CountOfPlayerShowedCards() == 1)
                    {
                        CurrentRoomState.CurrentTrickState.ShowedCardsInLastTrick = lastShowedCards;
                    }
                    UpdatePlayerCurrentTrickState();
                }
            }
        }

        public ShowingCardsValidationResult ValidateDumpingCards(List<int> selectedCards, string playerId)
        {
            var result = TractorRules.IsLeadingCardsValid(CurrentRoomState.CurrentHandState.PlayerHoldingCards, selectedCards,
                                                          playerId);
            result.PlayerId = playerId;

            if (result.ResultType == ShowingCardsValidationResultType.DumpingFail)
            {
                //甩牌失败，扣分：牌的张数x10
                int punishScore = selectedCards.Count * 10;
                if (
                    !CurrentRoomState.CurrentGameState.ArePlayersInSameTeam(CurrentRoomState.CurrentHandState.Starter,
                                                                playerId))
                {
                    CurrentRoomState.CurrentHandState.Score -= punishScore;
                    CurrentRoomState.CurrentHandState.ScoreAdjustment -= punishScore;
                }
                else
                {
                    CurrentRoomState.CurrentHandState.Score += punishScore;
                    CurrentRoomState.CurrentHandState.ScoreAdjustment += punishScore;
                }
                log.Debug("tried to dump cards and failed, punish score: " + punishScore);
                List<string> playersIDToCall = new List<string>();
                foreach (var player in PlayersProxy)
                {
                    if (player.Key != playerId)
                    {
                        playersIDToCall.Add(player.Key);
                    }
                }
                if (playersIDToCall.Count>0)
                {
                    IPlayerInvokeForAll(PlayersProxy, playersIDToCall, "NotifyDumpingValidationResult", new List<object>() { result });
                }
            }
            var cardString = "";
            foreach (var card in selectedCards)
            {
                cardString += card.ToString() + " ";
            }
            log.Debug(playerId + " tried to dump cards: " + cardString + " Result: " + result.ResultType.ToString());
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
            foreach (PlayerEntity player in CurrentRoomState.CurrentGameState.Players)
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
            CurrentRoomState.CurrentGameState.Players[0].Team = GameTeam.VerticalTeam;
            CurrentRoomState.CurrentGameState.Players[2].Team = GameTeam.VerticalTeam;
            CurrentRoomState.CurrentGameState.Players[1].Team = GameTeam.HorizonTeam;
            CurrentRoomState.CurrentGameState.Players[3].Team = GameTeam.HorizonTeam;
            log.Debug("restart game");
            foreach (var p in CurrentRoomState.CurrentGameState.Players)
            {
                p.Rank = 0;
                p.IsReadyToStart = false;
                p.IsRobot = false;
            }
            CurrentRoomState.CurrentHandState = new CurrentHandState(CurrentRoomState.CurrentGameState);
            CurrentRoomState.CurrentHandState.Rank = 0;
            CurrentRoomState.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);
            CurrentRoomState.CurrentHandState.IsFirstHand = true;
            UpdateGameState();
            UpdatePlayersCurrentHandState();
            CurrentRoomState.CurrentGameState.nextRestartID = GameState.RESTART_GAME;

            PublishMessage("随机组队成功！请点击就绪开始游戏");
        }

        //和下家互换座位
        public void MoveToNextPosition(string playerId)
        {
            bool isValid = true;
            foreach (PlayerEntity player in CurrentRoomState.CurrentGameState.Players)
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
                if (CurrentRoomState.CurrentGameState.Players[i].PlayerId == playerId)
                {
                    meIndex = i;
                    break;
                }
            }

            int nextIndex = (meIndex + 1) % 4;
            string nextPlayerId = CurrentRoomState.CurrentGameState.Players[nextIndex].PlayerId;
            PlayerEntity temp = CurrentRoomState.CurrentGameState.Players[nextIndex];
            CurrentRoomState.CurrentGameState.Players[nextIndex] = CurrentRoomState.CurrentGameState.Players[meIndex];
            CurrentRoomState.CurrentGameState.Players[meIndex] = temp;

            CurrentRoomState.CurrentGameState.Players[0].Team = GameTeam.VerticalTeam;
            CurrentRoomState.CurrentGameState.Players[2].Team = GameTeam.VerticalTeam;
            CurrentRoomState.CurrentGameState.Players[1].Team = GameTeam.HorizonTeam;
            CurrentRoomState.CurrentGameState.Players[3].Team = GameTeam.HorizonTeam;

            log.Debug("restart game");
            foreach (var p in CurrentRoomState.CurrentGameState.Players)
            {
                if (p == null) continue;
                p.Rank = 0;
                p.IsReadyToStart = false;
                p.IsRobot = false;
            }
            CurrentRoomState.CurrentHandState = new CurrentHandState(CurrentRoomState.CurrentGameState);
            CurrentRoomState.CurrentHandState.Rank = 0;
            CurrentRoomState.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);
            CurrentRoomState.CurrentHandState.IsFirstHand = true;
            UpdateGameState();
            UpdatePlayersCurrentHandState();
            CurrentRoomState.CurrentGameState.nextRestartID = GameState.RESTART_GAME;

            PublishMessage(string.Format("玩家【{0}】和下家【{1}】互换座位成功！请点击就绪开始游戏", playerId, nextPlayerId));
        }

        //旁观：选牌
        public void CardsReady(string playerId, ArrayList myCardIsReady)
        {
            PlayerEntity player = CurrentRoomState.CurrentGameState.Players.Single(p => p != null && p.PlayerId == playerId);
            IPlayerInvokeForAll(ObserversProxy, player.Observers.ToList<string>(), "NotifyCardsReady", new List<object>() { myCardIsReady });
        }

        //读取牌局
        public void RestoreGameStateFromFile(bool restoreCardsShoe)
        {
            bool isValid = true;
            foreach (PlayerEntity player in CurrentRoomState.CurrentGameState.Players)
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
            Stream stream3 = null;
            try
            {
                DataContractSerializer ser = new DataContractSerializer(typeof(GameState));
                string fileNameGamestate = string.Format("{0}\\backup_gamestate.xml", this.LogsByRoomFolder);
                stream = new FileStream(fileNameGamestate, FileMode.Open, FileAccess.Read, FileShare.Read);
                GameState gs = (GameState)ser.ReadObject(stream);

                for (int i = 0; i < 4; i++)
                {
                    CurrentRoomState.CurrentGameState.Players[i].Rank = gs.Players[i].Rank;
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

                PlayerEntity thisStarter = CurrentRoomState.CurrentGameState.Players[lastStarterIndex];

                CurrentRoomState.CurrentHandState = new CurrentHandState(CurrentRoomState.CurrentGameState);
                CurrentRoomState.CurrentHandState.Starter = thisStarter.PlayerId;
                CurrentRoomState.CurrentHandState.Rank = thisStarter.Rank;
                CurrentRoomState.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);

                CurrentRoomState.CurrentGameState.nextRestartID = GameState.START_NEXT_HAND;
                CurrentRoomState.CurrentGameState.startNextHandStarter = thisStarter;

                for (int i = 0; i < 4; i++)
                {
                    CurrentRoomState.CurrentGameState.Players[i].IsReadyToStart = false;
                    CurrentRoomState.CurrentGameState.Players[i].IsRobot = false;
                }

                UpdateGameState();
                UpdatePlayersCurrentHandState();

                if (restoreCardsShoe)
                {
                    string fileNameCardsShoe = string.Format("{0}\\backup_CardsShoe.xml", this.LogsByRoomFolder);
                    if (File.Exists(fileNameCardsShoe))
                    {
                        DataContractSerializer ser3 = new DataContractSerializer(typeof(CardsShoe));
                        stream3 = new FileStream(fileNameCardsShoe, FileMode.Open, FileAccess.Read, FileShare.Read);
                        CardsShoe cs = (CardsShoe)ser3.ReadObject(stream3);
                        this.CardsShoe.IsCardsRestored = true;
                        this.CardsShoe.Cards = cs.Cards;
                    }
                }

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
                if (stream3 != null)
                {
                    stream3.Close();
                }
            }
        }

        //设置从几打起
        public void SetBeginRank(string beginRankString)
        {
            bool isValid = true;
            foreach (PlayerEntity player in CurrentRoomState.CurrentGameState.Players)
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
                CurrentRoomState.CurrentGameState.Players[i].Rank = beginRank;
                CurrentRoomState.CurrentGameState.Players[i].IsReadyToStart = false;
                CurrentRoomState.CurrentGameState.Players[i].IsRobot = false;
            }
            CurrentRoomState.CurrentHandState = new CurrentHandState(CurrentRoomState.CurrentGameState);
            CurrentRoomState.CurrentHandState.Rank = beginRank;
            CurrentRoomState.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);
            CurrentRoomState.CurrentHandState.IsFirstHand = true;

            UpdateGameState();
            UpdatePlayersCurrentHandState();

            CurrentRoomState.CurrentGameState.nextRestartID = GameState.RESTART_GAME;

            PublishMessage(string.Format("设置从{0}打起成功！请点击就绪开始游戏", beginRankString));
        }

        //旁观玩家 by id
        public void ObservePlayerById(string playerId, string observerId)
        {
            foreach (PlayerEntity player in CurrentRoomState.CurrentGameState.Players)
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
            log.Debug("restart game with current set rank");

            CurrentRoomState.CurrentHandState = new CurrentHandState(CurrentRoomState.CurrentGameState);
            CurrentRoomState.CurrentHandState.Rank = curRank;
            CurrentRoomState.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);
            CurrentRoomState.CurrentHandState.IsFirstHand = true;
            UpdatePlayersCurrentHandState();
            var currentHandId = CurrentRoomState.CurrentHandState.Id;
            if (DistributeCards()) return;
            if (CurrentRoomState.CurrentHandState.Id != currentHandId)
                return;

            // reset timer everytime Trump is modified
            var oldTrump = CurrentRoomState.CurrentHandState.Trump;
            while (true)
            {
                PublishStartTimer(5);
                //加一秒缓冲时间，让客户端倒计时完成
                Thread.Sleep(5000 + 1000);
                if (CurrentRoomState.CurrentHandState.Trump == oldTrump)
                {
                    break;
                }
                oldTrump = CurrentRoomState.CurrentHandState.Trump;
            }

            if (CurrentRoomState.CurrentHandState.Trump != Suit.None)
            {
                if (DistributeLast8Cards()) return;
            }
            else if (PlayersProxy.Count == 4)
                RestartGame(curRank);
        }

        public void RestartCurrentHand()
        {
            log.Debug("restart current hand, starter: " + CurrentRoomState.CurrentHandState.Starter + " Rank: " + CurrentRoomState.CurrentHandState.Rank.ToString());
            StartNextHand(CurrentRoomState.CurrentGameState.Players.Single(p => p != null && p.PlayerId == CurrentRoomState.CurrentHandState.Starter));
        }

        public void StartNextHand(PlayerEntity nextStarter)
        {
            UpdateGameState();
            CurrentRoomState.CurrentHandState = new CurrentHandState(CurrentRoomState.CurrentGameState);
            CurrentRoomState.CurrentHandState.Starter = nextStarter.PlayerId;
            CurrentRoomState.CurrentHandState.Rank = nextStarter.Rank;
            CurrentRoomState.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);

            log.Debug("start next hand, starter: " + CurrentRoomState.CurrentHandState.Starter + " Rank: " + CurrentRoomState.CurrentHandState.Rank.ToString());

            UpdatePlayersCurrentHandState();

            var currentHandId = CurrentRoomState.CurrentHandState.Id;

            if (DistributeCards()) return;
            if (CurrentRoomState.CurrentHandState.Id != currentHandId)
                return;

            // reset timer everytime Trump is modified
            var oldTrump = CurrentRoomState.CurrentHandState.Trump;
            while (true)
            {
                PublishStartTimer(5);
                Thread.Sleep(5000 + 1000);
                if (CurrentRoomState.CurrentHandState.Trump == oldTrump)
                {
                    break;
                }
                oldTrump = CurrentRoomState.CurrentHandState.Trump;
            }
            if (CurrentRoomState.CurrentHandState.Trump != Suit.None)
            {
                if (DistributeLast8Cards()) return;
            }
            else if (PlayersProxy.Count == 4)
            {
                //如果庄家TEAM亮不起，则庄家的下家成为新的庄家
                var nextStarter2 = CurrentRoomState.CurrentGameState.GetNextPlayerAfterThePlayer(false, CurrentRoomState.CurrentHandState.Starter);
                CurrentRoomState.CurrentHandState.Starter = nextStarter2.PlayerId;
                CurrentRoomState.CurrentHandState.Rank = nextStarter2.Rank;
                log.Debug("starter team fail to make trump, next starter: " + CurrentRoomState.CurrentHandState.Starter + " Rank: " + CurrentRoomState.CurrentHandState.Rank.ToString());

                UpdatePlayersCurrentHandState();

                //10 seconds to make trump
                // reset timer everytime Trump is modified
                var oldTrump2 = CurrentRoomState.CurrentHandState.Trump;
                while (true)
                {
                    PublishStartTimer(5);
                    Thread.Sleep(5000 + 1000);
                    if (CurrentRoomState.CurrentHandState.Trump == oldTrump2)
                    {
                        break;
                    }
                    oldTrump2 = CurrentRoomState.CurrentHandState.Trump;
                }
                if (CurrentRoomState.CurrentHandState.Trump != Suit.None)
                {
                    if (DistributeLast8Cards()) return;
                }
                else if (PlayersProxy.Count == 4)
                {
                    //如果下家也亮不起，重新发牌
                    StartNextHand(nextStarter);
                }
            }
        }

        //发牌
        // returns: needRestart
        public bool DistributeCards()
        {
            CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.DistributingCards;
            UpdatePlayersCurrentHandState();
            string currentHandId = CurrentRoomState.CurrentHandState.Id;

            if (this.CardsShoe.IsCardsRestored)
            {
                this.CardsShoe.IsCardsRestored = false;
            }
            else
            {
                ShuffleCards(this.CardsShoe);
                //this.CardsShoe.Shuffle();
            }
            int cardNumberofEachPlayer = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);
            int j = 0;

            var LogList = new Dictionary<string, StringBuilder>();
            foreach (var player in PlayersProxy)
            {
                LogList[player.Key] = new StringBuilder();
            }

            List<string> playerIDs = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                playerIDs.Add(CurrentRoomState.CurrentGameState.Players[i].PlayerId);
            }
            for (int i = 0; i < cardNumberofEachPlayer; i++)
            {
                foreach (var playerID in playerIDs)
                {
                    var index = j++;
                    if (CurrentRoomState.CurrentHandState.Id == currentHandId)
                    {
                        if (!string.IsNullOrEmpty(IPlayerInvoke(playerID, PlayersProxy[playerID], "GetDistributedCard", new List<object>() { CardsShoe.Cards[index] }, true))) return true;
                        //旁观：发牌
                        PlayerEntity pe = CurrentRoomState.CurrentGameState.Players.Single(p => p != null && p.PlayerId == playerID);
                        if (IPlayerInvokeForAll(ObserversProxy, pe.Observers.ToList<string>(), "GetDistributedCard", new List<object>() { CardsShoe.Cards[index] })) return true;
                        LogList[playerID].Append(CardsShoe.Cards[index].ToString() + ", ");
                    }
                    else
                        return true;

                    CurrentRoomState.CurrentHandState.PlayerHoldingCards[playerID].AddCard(CardsShoe.Cards[index]);
                }
                Thread.Sleep(500);
            }

            log.Debug("distribute cards to each player: ");
            foreach (var logItem in LogList)
            {
                log.Debug(logItem.Key + ": " + logItem.Value.ToString());
            }

            foreach (var keyvalue in CurrentRoomState.CurrentHandState.PlayerHoldingCards)
            {
                log.Debug(keyvalue.Key + "'s cards:  " + keyvalue.Value.ToString());
            }

            CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.DistributingCardsFinished;
            UpdatePlayersCurrentHandState();

            SaveGameStateToFile();
            SaveCardsShoeToFile();

            return false;
        }

        //发底牌
        // returns: needRestart
        public bool DistributeLast8Cards()
        {
            var last8Cards = new int[8];
            if (CurrentRoomState.CurrentHandState.CurrentHandStep == HandStep.DistributingCardsFinished)
            {
                for (int i = 0; i < 8; i++)
                {
                    last8Cards[i] = CardsShoe.Cards[CardsShoe.Cards.Length - i - 1];
                }
            }
            else if (CurrentRoomState.CurrentHandState.CurrentHandStep == HandStep.Last8CardsRobbed)
            {
                last8Cards = CurrentRoomState.CurrentHandState.DiscardedCards;
            }

            CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.DistributingLast8Cards;
            UpdatePlayersCurrentHandState();


            IPlayer last8Holder = PlayersProxy.SingleOrDefault(p => p.Key == CurrentRoomState.CurrentHandState.Last8Holder).Value;
            if (last8Holder != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    var card = last8Cards[i];
                    if(!string.IsNullOrEmpty(IPlayerInvoke(CurrentRoomState.CurrentHandState.Last8Holder, last8Holder, "GetDistributedCard", new List<object>() { card }, true))) return true;
                    //旁观：发牌
                    PlayerEntity pe = CurrentRoomState.CurrentGameState.Players.Single(p => p != null && p.PlayerId == CurrentRoomState.CurrentHandState.Last8Holder);
                    if(IPlayerInvokeForAll(ObserversProxy, pe.Observers.ToList<string>(), "GetDistributedCard", new List<object>() { card })) return true;
                    CurrentRoomState.CurrentHandState.PlayerHoldingCards[CurrentRoomState.CurrentHandState.Last8Holder].AddCard(card);
                }
            }

            var logMsg = "distribute last 8 cards to " + CurrentRoomState.CurrentHandState.Last8Holder + ", cards: ";
            foreach (var card in last8Cards)
            {
                logMsg += card.ToString() + ", ";
            }
            log.Debug(logMsg);

            CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.DistributingLast8CardsFinished;
            UpdatePlayersCurrentHandState();

            Thread.Sleep(100);
            CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.DiscardingLast8Cards;
            UpdatePlayersCurrentHandState();

            return false;
        }

        //begin new trick
        private void BeginNewTrick(string leader)
        {
            List<String> playerIDList = new List<string>();
            foreach (PlayerEntity p in CurrentRoomState.CurrentGameState.Players)
            {
                if (p == null) continue;
                playerIDList.Add(p.PlayerId);
            }
            CurrentRoomState.CurrentTrickState = new CurrentTrickState(playerIDList, CurrentRoomState.CurrentTrickState.ShowedCardsInLastTrick);
            CurrentRoomState.CurrentTrickState.Learder = leader;
            CurrentRoomState.CurrentTrickState.Trump = CurrentRoomState.CurrentHandState.Trump;
            CurrentRoomState.CurrentTrickState.Rank = CurrentRoomState.CurrentHandState.Rank;
            UpdatePlayerCurrentTrickState();
        }

        //计算被扣底牌的分数，然后加到CurrentHandState.Score
        private void CalculatePointsFromDiscarded8Cards()
        {
            if (!CurrentRoomState.CurrentTrickState.AllPlayedShowedCards())
                return;
            if (CurrentRoomState.CurrentHandState.LeftCardsCount > 0)
                return;

            var cards = CurrentRoomState.CurrentTrickState.ShowedCards[CurrentRoomState.CurrentTrickState.Winner];
            var cardscp = new CurrentPoker(cards, (int)CurrentRoomState.CurrentHandState.Trump,
                                           CurrentRoomState.CurrentHandState.Rank);

            //最后一把牌的赢家不跟庄家一伙
            if (!CurrentRoomState.CurrentGameState.ArePlayersInSameTeam(CurrentRoomState.CurrentHandState.Starter,
                                                    CurrentRoomState.CurrentTrickState.Winner))
            {

                var points = 0;
                foreach (var card in CurrentRoomState.CurrentHandState.DiscardedCards)
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

                CurrentRoomState.CurrentHandState.Score += points;
                CurrentRoomState.CurrentHandState.ScoreAdjustment += points;

            }
        }

        //保存牌局
        private void SaveGameStateToFile()
        {
            if (string.IsNullOrEmpty(CurrentRoomState.CurrentHandState.Starter)) return;
            Stream stream = null;
            Stream stream2 = null;
            try
            {
                string fileNameGamestate = string.Format("{0}\\backup_gamestate.xml", this.LogsByRoomFolder);
                stream = new FileStream(fileNameGamestate, FileMode.Create, FileAccess.Write, FileShare.None);
                DataContractSerializer ser = new DataContractSerializer(typeof(GameState));
                ser.WriteObject(stream, CurrentRoomState.CurrentGameState);

                string fileNameHandState = string.Format("{0}\\backup_HandState.xml", this.LogsByRoomFolder);
                stream2 = new FileStream(fileNameHandState, FileMode.Create, FileAccess.Write, FileShare.None);
                DataContractSerializer ser2 = new DataContractSerializer(typeof(CurrentHandState));
                ser2.WriteObject(stream2, CurrentRoomState.CurrentHandState);

                string fileNameCardsShoe = string.Format("{0}\\backup_CardsShoe.xml", this.LogsByRoomFolder);
                if (File.Exists(fileNameCardsShoe))
                {
                    File.Delete(fileNameCardsShoe);
                }
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

        //保存手牌
        private void SaveCardsShoeToFile()
        {
            Stream stream = null;
            try
            {
                string fileNameCardsShoe = string.Format("{0}\\backup_CardsShoe.xml", this.LogsByRoomFolder);
                stream = new FileStream(fileNameCardsShoe, FileMode.Create, FileAccess.Write, FileShare.None);
                DataContractSerializer ser = new DataContractSerializer(typeof(CardsShoe));
                ser.WriteObject(stream, this.CardsShoe);
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
            }
        }
        #endregion

        #region Update Client State
        public void UpdateGameState()
        {
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "NotifyGameState", new List<object>() { CurrentRoomState.CurrentGameState });
            IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList<string>(), "NotifyGameState", new List<object>() { CurrentRoomState.CurrentGameState });
        }

        public void UpdatePlayersCurrentHandState()
        {
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "NotifyCurrentHandState", new List<object>() { CurrentRoomState.CurrentHandState });
            IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList<string>(), "NotifyCurrentHandState", new List<object>() { CurrentRoomState.CurrentHandState });
        }

        public void UpdatePlayerCurrentTrickState()
        {
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "NotifyCurrentTrickState", new List<object>() { CurrentRoomState.CurrentTrickState });
            IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList<string>(), "NotifyCurrentTrickState", new List<object>() { CurrentRoomState.CurrentTrickState });
        }

        public void PublishMessage(string msg)
        {
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "NotifyMessage", new List<object>() { msg });
            IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList<string>(), "NotifyMessage", new List<object>() { msg });
        }

        public void PublishStartTimer(int timerLength)
        {
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "NotifyStartTimer", new List<object>() { timerLength });
            IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList<string>(), "NotifyStartTimer", new List<object>() { timerLength });
        }

        #endregion

        // IPlayer method invoker
        // returns: needRestart
        public bool IPlayerInvokeForAll(Dictionary<string, IPlayer> PlayersProxyToCall, List<string> playerIDs, string methodName, List<object> args)
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
                return PerformProxyCleanupRestart(badPlayerIDs);
            }
            return false;
        }

        // returns: needRestart if string is not empty
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
                PerformProxyCleanupRestart(new List<string> { badPlayerID });
            }

            return badPlayerID;
        }

        // returns: needRestart
        private bool PerformProxyCleanupRestart(List<string> allBadPlayerIDs)
        {
            StringBuilder sb = new StringBuilder();
            List<string> badPlayers = new List<string>();
            foreach (string badID in allBadPlayerIDs)
            {
                if (PlayersProxy.ContainsKey(badID))
                {
                    badPlayers.Add(badID);
                    sb.Append(string.Format("【{0}】", badID));
                }
            }

            if (badPlayers.Count > 0)
            {
                string msg = string.Format("玩家{0}断线，该玩家需重启游戏后重新开始", sb.ToString());
                foreach (var entry in PlayersProxy)
                {
                    if (!allBadPlayerIDs.Contains(entry.Key))
                    {
                        IPlayerInvoke(entry.Key, entry.Value, "NotifyMessage", new List<object>() { msg }, false);
                    }
                }
            }
            bool needsRestart = PlayerQuitFromCleanup(allBadPlayerIDs);
            if (needsRestart)
            {
                CurrentRoomState.CurrentHandState = new CurrentHandState(CurrentRoomState.CurrentGameState);
                CurrentRoomState.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);
                CurrentRoomState.CurrentHandState.IsFirstHand = true;
                UpdatePlayersCurrentHandState();

                UpdateGameState();
                IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "StartGame", new List<object>() { });
            }
            return needsRestart;
        }

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
                log.Debug(string.Format("failed update clientinfo for {0}-{1}-{2}", clientIP, playerID, isCheating));
                log.Debug(string.Format("exception: {0}", ex.Message));
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
            int N = CurrentRoomState.CurrentGameState.Players.Count;
            for (int i = N; i >= 1; i--)
            {
                int r = new Random().Next(i);
                PlayerEntity curPlayer = CurrentRoomState.CurrentGameState.Players[r];
                shuffledPlayers.Add(curPlayer);
                CurrentRoomState.CurrentGameState.Players.Remove(curPlayer);
            }
            for (int i = 0; i < N; i++)
            {
                CurrentRoomState.CurrentGameState.Players.Add(shuffledPlayers[i]);
            }
            shuffledPlayers.Clear();
        }

        private void RemoveObserver(List<string> badObs)
        {
            foreach (string playerId in badObs)
            {
                log.Debug(playerId + " quit as observer.");
                ObserversProxy.Remove(playerId);
                foreach (PlayerEntity p in CurrentRoomState.CurrentGameState.Players)
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

        private void ShuffleCards(CardsShoe cardShoe)
        {
            Random rand = new Random();
            int N = cardShoe.Cards.Length;
            log.Debug(string.Format("before shuffle: {0}", string.Join(", ", cardShoe.Cards)));
            for (int i = 0; i < N; i++)
            {
                int r = rand.Next(i, N);
                Swap(cardShoe.Cards, i, r);
            }
            log.Debug(string.Format("after shuffle: {0}", string.Join(", ", cardShoe.Cards)));
        }

        private void Swap(int[] Cards, int i, int r)
        {
            log.Debug(string.Format("swapping: {0} and {1}", i, r));
            int temp = Cards[r];
            Cards[r] = Cards[i];
            Cards[i] = temp;
        }
    }
}
