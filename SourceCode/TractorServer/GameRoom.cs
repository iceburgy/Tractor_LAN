using Duan.Xiugang.Tractor.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace TractorServer
{
    public class GameRoom
    {
        private log4net.ILog log;
        public static string LogsFolder = "logs";
        public string LogsByRoomFolder;

        TractorHost tractorHost;
        public RoomState CurrentRoomState;
        public CardsShoe CardsShoe { get; set; }

        public Dictionary<string, IPlayer> PlayersProxy { get; set; }
        public Dictionary<string, IPlayer> ObserversProxy { get; set; }
        private ServerLocalCache serverLocalCache;
        private bool isGameOver;

        public GameRoom(int roomID, string roomName, TractorHost host)
        {
            this.tractorHost = host;
            CurrentRoomState = new RoomState(roomID);
            LogsByRoomFolder = string.Format("{0}\\{1}", LogsFolder, roomID);
            CardsShoe = new CardsShoe();
            PlayersProxy = new Dictionary<string, IPlayer>();
            ObserversProxy = new Dictionary<string, IPlayer>();
            serverLocalCache = new ServerLocalCache();
            CurrentRoomState.roomSetting = ReadRoomSettingFromFile();
            CurrentRoomState.roomSetting.RoomName = roomName;
            CurrentRoomState.roomSetting.RoomOwner = string.Empty;

            string fullPath = Assembly.GetExecutingAssembly().Location;
            string fullFolder = Path.GetDirectoryName(fullPath);
            string fullLogFilePath = string.Format("{0}\\{1}\\myroom_{2}_logfile.txt", fullFolder, LogsByRoomFolder, roomID);
            log = LoggerUtil.Setup(string.Format("myroom_{0}_logfile", roomID), fullLogFilePath);
        }

        #region implement interface ITractorHost
        public bool PlayerEnterRoom(string playerID, string clientIP, IPlayer player, bool allowSameIP, int posID)
        {
            if (!PlayersProxy.Keys.Contains(playerID) && !ObserversProxy.Keys.Contains(playerID))
            {
                if (IsRoomFull())
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
                            PublishMessage(new string[] { msg });
                            player.NotifyMessage(new string[] { "已在游戏中", "请勿双开旁观", "", "" });
                            return false;
                        }
                        else
                        {
                            msg += "？？";
                            PublishMessage(new string[] { msg });
                        }
                    }
                    else
                    {
                        LogClientInfo(clientIP, playerID, false);
                    }

                    log.Debug(string.Format("observer {0}-{1} joined.", playerID, clientIP));

                    ObserversProxy.Add(playerID, player);
                    ObservePlayerById(CurrentRoomState.CurrentGameState.Players[0].PlayerId, playerID);

                    player.NotifyRoomSetting(this.CurrentRoomState.roomSetting, false);
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
                if (string.IsNullOrEmpty(CurrentRoomState.roomSetting.RoomOwner))
                {
                    CurrentRoomState.roomSetting.RoomOwner = playerID;
                }
                if (IsAllOnline())
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

                player.NotifyRoomSetting(this.CurrentRoomState.roomSetting, false);
                return true;
            }
            else
            {
                if (PlayersProxy.Keys.Contains(playerID))
                    IPlayerInvoke(playerID, PlayersProxy[playerID], "NotifyMessage", new List<object>() { "已在房间里" }, true);
                else if (ObserversProxy.Keys.Contains(playerID))
                {
                    IPlayerInvoke(playerID, ObserversProxy[playerID], "NotifyMessage", new List<object>() { "已在房间里" }, true);
                    string obeserveeId = CurrentRoomState.CurrentGameState.Players.Single(p => p != null && p.Observers.Contains(playerID)).PlayerId;
                    ObservePlayerById(obeserveeId, playerID);
                }
                return false;
            }
        }

        private bool IsRoomFull()
        {
            return this.CurrentRoomState.CurrentGameState.Players.Where(p => p != null).Count() >= 4;
        }

        private bool IsAllOnline()
        {
            return this.CurrentRoomState.CurrentGameState.Players.Where(p => p != null && p.IsOffline == false).Count() >= 4;
        }

        private bool IsPlayerOffline(string playerID)
        {
            return this.CurrentRoomState.CurrentGameState.Players.Where(p => p != null && p.IsOffline == true).Count() == 1;
        }

        public bool PlayerReenterRoom(string playerID, string clientIP, IPlayer player, bool allowSameIP)
        {
            if (IsPlayerOffline(playerID) && !PlayersProxy.Keys.Contains(playerID) && !ObserversProxy.Keys.Contains(playerID))
            {
                if (PlayersProxy.Count >= 4)
                {
                    player.NotifyMessage(new string[] { "房间已满", "加入失败", "", "" });
                    return false;
                }

                int posID = -1;
                for (int i = 0; i < 4; i++)
                {
                	if (CurrentRoomState.CurrentGameState.Players[i] ==  null) continue;
                    if (CurrentRoomState.CurrentGameState.Players[i].PlayerId==playerID)
                    {
                        posID = i;
                        break;
                    }
                }

                if (posID < 0) {
                    player.NotifyMessage(new string[] { "未能找回断线玩家信息", "加入失败" });
                    return false;
                }

                if (!CurrentRoomState.CurrentGameState.Players[posID].IsOffline)
                {
                    player.NotifyMessage(new string[] { "玩家并未断线", "重连失败" });
                    return false;
                }

                CurrentRoomState.CurrentGameState.Players[posID].IsOffline = false;
                LogClientInfo(clientIP, playerID, false);
                CurrentRoomState.CurrentGameState.PlayerToIP.Add(playerID, clientIP);
                PlayersProxy.Add(playerID, player);
                log.Debug(string.Format("player {0} re-joined room from offline.", playerID));

                player.NotifyRoomSetting(this.CurrentRoomState.roomSetting, false);
                UpdatePlayerCurrentTrickState();
                UpdatePlayersCurrentHandState();
                Thread.Sleep(2000);
                UpdateGameState();

                return true;
            }
            return false;
        }

        //玩家退出
        // returns: needRestart
        public bool PlayerQuit(List<string> playerIDs)
        {
            bool needsRestart = false;
            foreach (string playerID in playerIDs)
            {
                if (IsActualPlayer(playerID))
                {
                    needsRestart = true;
                    break;
                }
            }

            foreach (string playerID in playerIDs)
            {
                PlayerQuitWorker(playerID);
            }

            //如果房主退出，则选择位置最靠前的玩家为房主
            if (playerIDs.Contains(CurrentRoomState.roomSetting.RoomOwner))
            {
                PlayerEntity next = this.CurrentRoomState.CurrentGameState.Players.FirstOrDefault(p => p != null);
                if (next != null) CurrentRoomState.roomSetting.RoomOwner = next.PlayerId;
                else CurrentRoomState.roomSetting.RoomOwner = string.Empty;
                UpdatePlayerRoomSettings();
            }

            if (needsRestart && !this.isGameOver) ResetAndRestartGame();

            return needsRestart;
        }

        public bool IsActualPlayer(string playerID)
        {
            foreach (PlayerEntity p in this.CurrentRoomState.CurrentGameState.Players)
            {
                if (p != null && p.PlayerId == playerID)
                {
                    return true;
                }
            }
            return false;
        }

        // returns: needRestart
        public void PlayerQuitWorker(string playerID)
        {
            if (!IsActualPlayer(playerID))
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
                if (!IsActualPlayer(playerID))
                {
                    List<string> badObs = new List<string>();
                    badObs.Add(playerID);
                    RemoveObserver(badObs);

                    this.tractorHost.SessionIDGameRoom.Remove(playerID);
                    this.tractorHost.PlayerToIP.Remove(playerID);
                    this.tractorHost.PlayersProxy.Remove(playerID);

                    Thread.Sleep(500);
                    Thread thr = new Thread(new ThreadStart(this.tractorHost.UpdateGameHall));
                    thr.Start();
                    continue;
                }

                log.Debug(playerID + " went offline.");
                needsRestart = true;
                CurrentRoomState.CurrentGameState.PlayerToIP.Remove(playerID);
                PlayersProxy.Remove(playerID);
                this.tractorHost.PlayerToIP.Remove(playerID);
                this.tractorHost.PlayersProxy.Remove(playerID);


                for (int i = 0; i < 4; i++)
                {
                    if (CurrentRoomState.CurrentGameState.Players[i] != null && CurrentRoomState.CurrentGameState.Players[i].PlayerId == playerID)
                    {
                        CurrentRoomState.CurrentGameState.Players[i].IsOffline = true;
                        CurrentRoomState.CurrentGameState.Players[i].IsRobot = false;
                        break;
                    }
                }
            }

            var threadUpdateGameState = new Thread(() => this.UpdateGameState());
            threadUpdateGameState.Start();

            return needsRestart &&
                !(CurrentRoomState.CurrentHandState.CurrentHandStep == HandStep.Playing ||
                CurrentRoomState.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8CardsFinished);
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
                        CleanupCaches();

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
            CheckOfflinePlayers();
        }

        public void SpecialEndGame(string playerID, SpecialEndingType endType)
        {
            if (CurrentRoomState.CurrentHandState.CurrentHandStep != HandStep.DiscardingLast8Cards) return;

            //开始下一盘
            CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.SpecialEnding;

            foreach (PlayerEntity p in CurrentRoomState.CurrentGameState.Players)
            {
                if (p == null) continue;
                p.IsReadyToStart = false;
                p.IsRobot = false;
            }

            StringBuilder sb = null;
            string[] msgs = null;
            switch (endType)
            {
                case SpecialEndingType.Surrender:
                    if (CurrentRoomState.CurrentGameState.ArePlayersInSameTeam(CurrentRoomState.CurrentHandState.Starter, playerID))
                    {
                        CurrentRoomState.CurrentHandState.Score = 80;
                    }
                    else
                    {
                        CurrentRoomState.CurrentHandState.Score = 40;
                    }
                    CurrentRoomState.CurrentGameState.nextRestartID = GameState.START_NEXT_HAND;
                    CurrentRoomState.CurrentGameState.startNextHandStarter = CurrentRoomState.CurrentGameState.NextRank(CurrentRoomState);
                    CurrentRoomState.CurrentHandState.Starter = CurrentRoomState.CurrentGameState.startNextHandStarter.PlayerId;
                    //检查是否本轮游戏结束
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
                    else
                    {
                        msgs = new string[] { string.Format("玩家【{0}】", playerID), "发动了技能【投降】" };
                    }
                    break;
                case SpecialEndingType.RiotByScore:
                    CurrentRoomState.CurrentGameState.nextRestartID = GameState.RESTART_CURRENT_HAND;
                    msgs = new string[] { string.Format("玩家【{0}】", playerID), "发动了技能【无分革命】" };
                    break;
                case SpecialEndingType.RiotByTrump:
                    CurrentRoomState.CurrentGameState.nextRestartID = GameState.RESTART_CURRENT_HAND;
                    msgs = new string[] { string.Format("玩家【{0}】", playerID), "发动了技能【无主革命】" };
                    break;
                default:
                    return;
            }

            SaveGameStateToFile();

            UpdatePlayersCurrentHandState();

            UpdateGameState();

            //所有人展示手牌
            ShowAllHandCards();

            if (sb != null)
            {
                PublishMessage(new string[] { sb.ToString(), "获胜！", "点击就绪重新开始游戏" });
            }
            else if (msgs!=null)
            {
                PublishMessage(msgs);
            }
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
                //log trump and rank before a new game starts
                log.Debug(string.Format("starting a new game: starter {0} {1} {2}", CurrentRoomState.CurrentHandState.Starter, CurrentRoomState.CurrentHandState.Trump.ToString(), (CurrentRoomState.CurrentHandState.Rank + 2).ToString()));

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

                    //log who made what trump
                    log.Debug(string.Format("player made trump: {0} {1} {2} {3}", trumpMaker, trumpExposingPoker.ToString(), trump.ToString(), (CurrentRoomState.CurrentHandState.Rank + 2).ToString()));

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
            CurrentRoomState.CurrentTrickState.ShowedCards[lastestPlayer] = currentTrickState.ShowedCards[lastestPlayer];
            string cardsString = "";
            foreach (var card in CurrentRoomState.CurrentTrickState.ShowedCards[lastestPlayer])
            {
                cardsString += string.Format("{0} {1} ", CommonMethods.GetSuitString(card), CommonMethods.GetNumberString(card));
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
                serverLocalCache = new ServerLocalCache();
                serverLocalCache.lastShowedCards = CurrentRoomState.CurrentTrickState.ShowedCards.ToDictionary(entry => entry.Key, entry => entry.Value.ToList());
                serverLocalCache.lastLeader = CurrentRoomState.CurrentTrickState.Learder;

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
                    PublishStartTimer(2);
                    Thread.Sleep(2000 + 1000);

                    //本局结束画面，更新最后一轮出的牌
                    CurrentRoomState.CurrentTrickState.serverLocalCache = serverLocalCache;
                    CurrentRoomState.CurrentTrickState.serverLocalCache.muteSound = true;
                    UpdatePlayerCurrentTrickState();

                    CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.Ending;

                    if (!TractorHost.gameConfig.IsFullDebug)
                    {
                        foreach (PlayerEntity p in CurrentRoomState.CurrentGameState.Players)
                        {
                        	if (p == null) continue;
                            p.IsReadyToStart = false;
                            p.IsRobot = false;
                        }
                    }
                    CurrentRoomState.CurrentGameState.nextRestartID = GameState.START_NEXT_HAND;
                    CurrentRoomState.CurrentGameState.startNextHandStarter = CurrentRoomState.CurrentGameState.NextRank(CurrentRoomState);
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

                    SaveGameStateToFile();

                    UpdatePlayersCurrentHandState();

                    UpdateGameState();

                    if (sb != null)
                    {
                        PublishMessage(new string[] { sb.ToString(), "获胜！", "点击就绪重新开始游戏" });
                        this.isGameOver = true;
                    }
                    else if (TractorHost.gameConfig.IsFullDebug && AllOnline())
                    {
                        Thread.Sleep(3000);
                        CleanupCaches();

                        StartNextHand(CurrentRoomState.CurrentGameState.startNextHandStarter);
                    }
                    CleanupOfflinePlayers();
                }
            }
            else
            {
                if (CurrentRoomState.CurrentTrickState.CountOfPlayerShowedCards() == 1)
                {
                    CurrentRoomState.CurrentTrickState.serverLocalCache = serverLocalCache;
                }
                UpdatePlayerCurrentTrickState();
            }
            CheckOfflinePlayers();
        }

        private bool CleanupOfflinePlayers()
        {
            //检查是否有离线玩家，if so，将其移出游戏并重开
            List<string> badPlayers = new List<string>();
            foreach (var player in this.CurrentRoomState.CurrentGameState.Players)
            {
            	if (player ==  null) continue;
                if (player.IsOffline)
                {
                    badPlayers.Add(player.PlayerId);
                }
            }
            if (badPlayers.Count > 0)
            {
                foreach (string bp in badPlayers)
                {
                    this.tractorHost.BeginPlayerQuit(bp, PlayerQuitCallback, this.tractorHost);
                }
                return true;
            }
            return false;
        }

        private void CheckOfflinePlayers()
        {
            List<string> badPlayers = new List<string>();
            List<PlayerEntity> offlinePlayers = new List<PlayerEntity>();
            foreach (var player in this.CurrentRoomState.CurrentGameState.Players)
            {
                if (player == null || !player.IsOffline) continue;
                if (CurrentRoomState.CurrentHandState.CurrentHandStep == HandStep.Playing ||
                    CurrentRoomState.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8CardsFinished)
                {
                    if (CurrentRoomState.CurrentTrickState.NextPlayer() != player.PlayerId) continue;
                    if (CurrentRoomState.CurrentTrickState.ShowedCards[player.PlayerId].Count > 0) continue;
                    offlinePlayers.Add(player);
                }
                else
                {
                    badPlayers.Add(player.PlayerId);
                }
            }

            foreach (var player in offlinePlayers)
            {
                List<int> SelectedCards = new List<int>();
                CurrentPoker currentPoker = CurrentRoomState.CurrentHandState.PlayerHoldingCards[player.PlayerId];
                if (CurrentRoomState.CurrentTrickState.IsStarted())
                {
                    //托管代打 - 跟出
                    Algorithm.MustSelectedCards(SelectedCards, CurrentRoomState.CurrentTrickState, currentPoker);
                }
                else
                {
                    //托管代打 - 先手
                    Algorithm.ShouldSelectedCards(SelectedCards, CurrentRoomState.CurrentTrickState, currentPoker);
                }
                if (!TryToRobotPlayOffline(player.PlayerId, SelectedCards, currentPoker))
                {
                    badPlayers.Add(player.PlayerId);
                    break;
                }
            }

            if (badPlayers.Count > 0)
            {
                foreach (string bp in badPlayers)
                {
                    this.tractorHost.BeginPlayerQuit(bp, PlayerQuitCallback, this.tractorHost);
                }
            }
        }

        // return true if succeedful, else false
        private bool TryToRobotPlayOffline(string playerId, List<int> SelectedCards, CurrentPoker currentPoker)
        {
            ShowingCardsValidationResult showingCardsValidationResult =
                TractorRules.IsValid(CurrentRoomState.CurrentTrickState, SelectedCards, currentPoker);
            if (showingCardsValidationResult.ResultType == ShowingCardsValidationResultType.Valid)
            {
                CurrentRoomState.CurrentTrickState.ShowedCards[playerId] = SelectedCards;
                PlayerShowCards(CurrentRoomState.CurrentTrickState);
            }
            else
            {
                PublishMessage(new string[] { string.Format("玩家【{0}】已离线", playerId), "尝试托管代打失败", "游戏即将重开..." });
                PublishStartTimer(5);
                //加一秒缓冲时间，让客户端倒计时完成
                Thread.Sleep(5000 + 1000);

                //将离线玩家移出游戏
                this.tractorHost.BeginPlayerQuit(playerId, PlayerQuitCallback, this.tractorHost);

                //重开游戏
                ResetAndRestartGame();
                return false;
            }
            return true;
        }

        public void PlayerQuitCallback(IAsyncResult ar)
        {
        }

        private bool AllOnline()
        {
            bool allOnline = true;
            foreach (PlayerEntity player in this.CurrentRoomState.CurrentGameState.Players)
            {
                if (player == null) continue;
                if (player.IsOffline)
                {
                    allOnline = false;
                    break;
                }
            }
            return allOnline;
        }

        private void CleanupCaches()
        {
            //清空缓存
            this.serverLocalCache = new ServerLocalCache();
            CurrentRoomState.CurrentTrickState.serverLocalCache = new ServerLocalCache();
            UpdatePlayerCurrentTrickState();
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
                    CurrentRoomState.CurrentHandState.ScorePunishment -= punishScore;
                }
                else
                {
                    CurrentRoomState.CurrentHandState.Score += punishScore;
                    CurrentRoomState.CurrentHandState.ScorePunishment += punishScore;
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
                if (playersIDToCall.Count > 0)
                {
                    IPlayerInvokeForAll(PlayersProxy, playersIDToCall, "NotifyDumpingValidationResult", new List<object>() { result });
                    IPlayerInvokeForAll(PlayersProxy, playersIDToCall, "NotifyMessage", new List<object>() { new string[] { string.Format("玩家【{0}】", playerId), string.Format("甩牌{0}张失败", selectedCards.Count), string.Format("罚分：{0}", punishScore) } });
                    if (ObserversProxy.Count > 0)
                    {
                        IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList(), "NotifyDumpingValidationResult", new List<object>() { result });
                        IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList(), "NotifyMessage", new List<object>() { new string[] { string.Format("玩家【{0}】", playerId), string.Format("甩牌{0}张失败", selectedCards.Count), string.Format("罚分：{0}", punishScore) } });
                    }
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
                PublishMessage(new string[] { "随机组队失败", "玩家人数不够" });
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
            ResetAndRestartGame();
            CurrentRoomState.CurrentGameState.nextRestartID = GameState.RESTART_GAME;

            PublishMessage(new string[] { "随机组队成功", "请点击就绪开始游戏" });
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
                PublishMessage(new string[] { "和下家互换座位失败", "玩家人数不够" });
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
            ResetAndRestartGame();
            CurrentRoomState.CurrentGameState.nextRestartID = GameState.RESTART_GAME;

            PublishMessage(new string[] { string.Format("玩家【{0}】", playerId), string.Format("和下家【{0}】", nextPlayerId), "互换座位成功", "请点击就绪开始游戏" });
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
                PublishMessage(new string[] { "读取牌局失败", "玩家人数不够" });
                return;
            }

            Stream stream = null;
            Stream stream2 = null;
            List<string> restoredMsg = new List<string>();
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
                    PublishMessage(new string[] { "读取牌局失败", "上盘牌局无庄家" });
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

                restoredMsg.Add("读取牌局【成功】");

            }
            catch (Exception ex)
            {
                log.Debug(string.Format("读取牌局 error: {0}", ex));
                restoredMsg.Add("读取牌局【失败】");
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
                if (restoreCardsShoe)
                {
                    Stream stream3 = null;
                    try
                    {
                        string fileNameCardsShoe = string.Format("{0}\\backup_CardsShoe.xml", this.LogsByRoomFolder);
                        DataContractSerializer ser3 = new DataContractSerializer(typeof(CardsShoe));
                        stream3 = new FileStream(fileNameCardsShoe, FileMode.Open, FileAccess.Read, FileShare.Read);
                        CardsShoe cs = (CardsShoe)ser3.ReadObject(stream3);
                        this.CardsShoe.IsCardsRestored = true;
                        this.CardsShoe.Cards = cs.Cards;
                        restoredMsg.Add("还原手牌【成功】");
                    }
                    catch (Exception ex)
                    {
                        log.Debug(string.Format("还原手牌 error: {0}", ex));
                        restoredMsg.Add("还原手牌【失败】");
                    }
                    finally
                    {
                        if (stream3 != null)
                        {
                            stream3.Close();
                        }
                    }
                }
                restoredMsg.Add("请点击就绪继续游戏");
                PublishMessage(restoredMsg.ToArray());
            }
        }

        //读取房间游戏设置
        public RoomSetting ReadRoomSettingFromFile()
        {
            Stream stream = null;
            RoomSetting roomSetting = new RoomSetting();
            try
            {
                string fileNameRoomSetting = string.Format("{0}\\backup_RoomSetting.xml", this.LogsByRoomFolder);
                if (File.Exists(fileNameRoomSetting))
                {
                    DataContractSerializer ser = new DataContractSerializer(typeof(RoomSetting));
                    stream = new FileStream(fileNameRoomSetting, FileMode.Open, FileAccess.Read, FileShare.Read);
                    roomSetting= (RoomSetting)ser.ReadObject(stream);
                    roomSetting.SortManditoryRanks();
                }
                return roomSetting;
            }
            catch (Exception ex)
            {
                return roomSetting;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
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
                PublishMessage(new string[] { string.Format("设置从{0}打起失败", beginRankString), "玩家人数不够" });
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

            PublishMessage(new string[] { string.Format("设置从{0}打起成功", beginRankString), "请点击就绪开始游戏" });
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
            if (new HandStep[] { HandStep.DiscardingLast8Cards, HandStep.Playing }.Contains(this.CurrentRoomState.CurrentHandState.CurrentHandStep))
            {
                UpdatePlayersCurrentHandState();
            }
        }
        #endregion

        #region Host Action
        public void RestartGame(int curRank)
        {
            this.isGameOver = false;
            log.Debug("restart game with current set rank");
            CurrentRoomState.CurrentTrickState.serverLocalCache = new ServerLocalCache();

            CurrentRoomState.CurrentHandState = new CurrentHandState(CurrentRoomState.CurrentGameState);
            CurrentRoomState.CurrentHandState.Rank = curRank;
            CurrentRoomState.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);
            CurrentRoomState.CurrentHandState.IsFirstHand = true;
            UpdatePlayersCurrentHandState();
            var currentHandId = CurrentRoomState.CurrentHandState.Id;

            //新游戏开始前给出提示信息，开始倒计时，并播放提示音，告诉玩家要抢庄
            PublishMessage(new string[] { "新游戏即将开始", "做好准备抢庄！" });
            PublishStartTimer(5);
            //加一秒缓冲时间，让客户端倒计时完成
            Thread.Sleep(5000 + 1000);

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
            else if (IsAllOnline())
                RestartGame(curRank);
        }

        public void RestartCurrentHand()
        {
            log.Debug("restart current hand, starter: " + CurrentRoomState.CurrentHandState.Starter + " Rank: " + CurrentRoomState.CurrentHandState.Rank.ToString());
            StartNextHand(CurrentRoomState.CurrentGameState.Players.Single(p => p != null && p.PlayerId == CurrentRoomState.CurrentHandState.Starter));
        }

        public void StartNextHand(PlayerEntity nextStarter)
        {
            this.isGameOver = false;
            CurrentRoomState.CurrentTrickState.serverLocalCache = new ServerLocalCache();

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
                if (CurrentRoomState.CurrentHandState.Trump == Suit.None)
                {
                    PublishMessage(new string[] { "无人亮主", "庄家将会自动下台！" });
                }
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
            else if (IsAllOnline() && !string.IsNullOrEmpty(CurrentRoomState.CurrentHandState.Starter))
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
                    if (CurrentRoomState.CurrentHandState.Trump == Suit.None)
                    {
                        PublishMessage(new string[] { "再次无人亮主", "庄家将会自动下台！", "并且重新摸牌！" });
                    }
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
                else if (IsAllOnline())
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
                if (CurrentRoomState.CurrentGameState.Players[i] == null)
                {
                    //玩家退出，停止发牌
                    return true;
                }
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
            CurrentRoomState.CurrentTrickState = new CurrentTrickState(playerIDList, CurrentRoomState.CurrentTrickState.serverLocalCache);
            CurrentRoomState.CurrentTrickState.Learder = leader;
            CurrentRoomState.CurrentTrickState.Trump = CurrentRoomState.CurrentHandState.Trump;
            CurrentRoomState.CurrentTrickState.Rank = CurrentRoomState.CurrentHandState.Rank;
            UpdatePlayerCurrentTrickState();
        }

        //计算被扣底牌的分数，然后加到CurrentHandState.Score
        public void CalculatePointsFromDiscarded8Cards()
        {
            if (!CurrentRoomState.CurrentTrickState.AllPlayedShowedCards())
                return;
            if (CurrentRoomState.CurrentHandState.LeftCardsCount > 0)
                return;

            var cards = CurrentRoomState.CurrentTrickState.ShowedCards[CurrentRoomState.CurrentTrickState.Learder];
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

                int multiplier = 1;
                if (cardscp.HasTractors())
                {
                    multiplier *= (2 * cardscp.GetTractor(CurrentRoomState.CurrentTrickState.LeadingSuit).Count * 2);
                }
                else if (cardscp.GetPairs().Count > 0)
                {
                    multiplier *= 4;
                }
                else
                {
                    multiplier *= 2;
                }

                CurrentRoomState.CurrentHandState.Score += (points * multiplier);
                CurrentRoomState.CurrentHandState.ScoreLast8CardsBase += points;
                CurrentRoomState.CurrentHandState.ScoreLast8CardsMultiplier += multiplier;

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

        //保存房间游戏设置
        private void SaveRoomSettingToFile()
        {
            Stream stream = null;
            try
            {
                string fileNameRoomSetting = string.Format("{0}\\backup_RoomSetting.xml", this.LogsByRoomFolder);
                stream = new FileStream(fileNameRoomSetting, FileMode.Create, FileAccess.Write, FileShare.None);
                DataContractSerializer ser = new DataContractSerializer(typeof(RoomSetting));
                ser.WriteObject(stream, this.CurrentRoomState.roomSetting);
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

        public void PublishMessage(string[] msg)
        {
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "NotifyMessage", new List<object>() { msg });
            IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList<string>(), "NotifyMessage", new List<object>() { msg });
        }

        public void PublishStartTimer(int timerLength)
        {
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "NotifyStartTimer", new List<object>() { timerLength });
            IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList<string>(), "NotifyStartTimer", new List<object>() { timerLength });
        }

        public void ShowAllHandCards()
        {
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "NotifyShowAllHandCards", new List<object>() { });
            IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList<string>(), "NotifyShowAllHandCards", new List<object>() { });
        }

        public void UpdatePlayerRoomSettings()
        {
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "NotifyRoomSetting", new List<object>() { this.CurrentRoomState.roomSetting, true });
            IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList<string>(), "NotifyRoomSetting", new List<object>() { this.CurrentRoomState.roomSetting, true });
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
            bool needsRestart = PlayerQuitFromCleanup(allBadPlayerIDs);
            if (needsRestart)
            {
                foreach (string bp in allBadPlayerIDs)
                {
                    this.tractorHost.BeginPlayerQuit(bp, PlayerQuitCallback, this.tractorHost);
                }
            }
            return needsRestart;
        }

        private void ResetAndRestartGame()
        {
            CurrentRoomState.CurrentHandState = new CurrentHandState(CurrentRoomState.CurrentGameState);
            CurrentRoomState.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);
            CurrentRoomState.CurrentHandState.IsFirstHand = true;
            UpdatePlayersCurrentHandState();
            UpdateGameState();
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "StartGame", new List<object>() { });
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
                CurrentRoomState.CurrentGameState.PlayerToIP.Remove(playerId);
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
            int temp = Cards[r];
            Cards[r] = Cards[i];
            Cards[i] = temp;
        }

        internal void SetRoomSetting(RoomSetting newSetting)
        {
            if (this.CurrentRoomState.roomSetting.Equals(newSetting)) return;

            newSetting.SortManditoryRanks();
            this.CurrentRoomState.roomSetting = newSetting;
            UpdatePlayerRoomSettings();

            //save setting to file
            SaveRoomSettingToFile();
        }
    }
}
