﻿using Duan.Xiugang.Tractor.Objects;
using Newtonsoft.Json;
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
        public static string ReplaysFolder = "replays";
        public static string ClientinfoFileName = "clientinfo.json";
        public string BackupGamestateFileName = "backup_gamestate.json";
        public string BackupHandStateFileName = "backup_HandState.json";
        public string BackupTrickStateFileName = "backup_TrickState.json";
        public string BackupRoomSettingFileName = "backup_RoomSetting.json";
        public string LogsByRoomFullFolder;
        public string ReplaysByRoomFullFolder;

        TractorHost tractorHost;
        public RoomState CurrentRoomState;
        public CardsShoe CardsShoe { get; set; }
        public ReplayEntity replayEntity;

        public Dictionary<string, IPlayer> PlayersProxy { get; set; }
        public Dictionary<string, IPlayer> ObserversProxy { get; set; }
        private ServerLocalCache serverLocalCache;
        private bool isGameOver;

        public GameRoom(int roomID, string roomName, TractorHost host)
        {
            string rootFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            LogsByRoomFullFolder = string.Format("{0}\\{1}\\{2}", rootFolder, LogsFolder, roomID);
            ReplaysByRoomFullFolder = string.Format("{0}\\{1}\\{2}", rootFolder, ReplaysFolder, roomID);

            this.tractorHost = host;
            CurrentRoomState = new RoomState(roomID);
            CardsShoe = new CardsShoe();
            PlayersProxy = new Dictionary<string, IPlayer>();
            ObserversProxy = new Dictionary<string, IPlayer>();
            serverLocalCache = new ServerLocalCache();
            CurrentRoomState.roomSetting = ReadRoomSettingFromFile();
            CurrentRoomState.roomSetting.RoomName = roomName;
            CurrentRoomState.roomSetting.RoomOwner = string.Empty;
            if (TractorHost.gameConfig != null) CurrentRoomState.roomSetting.IsFullDebug = TractorHost.gameConfig.IsFullDebug;

            string fullLogFilePath = string.Format("{0}\\myroom_{1}_logfile.txt", LogsByRoomFullFolder, roomID);
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
                        log.Debug(string.Format("observer {0} attempted double observing.", playerID));
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

                    log.Debug(string.Format("observer {0} joined.", playerID));

                    player.NotifyRoomSetting(this.CurrentRoomState.roomSetting, false);
                    ObserversProxy.Add(playerID, player);
                    ObservePlayerById(CurrentRoomState.CurrentGameState.Players[0].PlayerId, playerID);

                    if (IsGameOnGoing())
                    {
                        IPlayerInvoke(playerID, player, "NotifyCurrentTrickState", new List<object>() { CurrentRoomState.CurrentTrickState }, true);
                        IPlayerInvoke(playerID, player, "NotifyCurrentHandState", new List<object>() { CurrentRoomState.CurrentHandState }, true);
                    }
                    else
                    {
                        player.NotifyRoomSetting(this.CurrentRoomState.roomSetting, true);
                    }

                    Thread.Sleep(2000);
                    IPlayerInvoke(playerID, player, "NotifyGameState", new List<object>() { CurrentRoomState.CurrentGameState }, true);

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

                player.NotifyRoomSetting(this.CurrentRoomState.roomSetting, true);
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

        private bool IsGameOnGoing()
        {
            return HandStep.DiscardingLast8Cards <= CurrentRoomState.CurrentHandState.CurrentHandStep &&
                CurrentRoomState.CurrentHandState.CurrentHandStep <= HandStep.Playing;
        }

        private bool IsRoomFull()
        {
            return this.CurrentRoomState.CurrentGameState.Players.Where(p => p != null).Count() == 4;
        }

        private bool IsAllOnline()
        {
            return this.CurrentRoomState.CurrentGameState.Players.Where(p => p != null && p.IsOffline == false).Count() == 4;
        }

        public bool PlayerReenterRoom(string playerID, string clientIP, IPlayer player, bool allowSameIP)
        {
            if (PlayersProxy.Keys.Contains(playerID) || ObserversProxy.Keys.Contains(playerID))
            {
                player.NotifyMessage(new string[] { "玩家重新加入失败", "玩家重名" });
                return false;
            }
            int posID = -1;
            for (int i = 0; i < 4; i++)
            {
                PlayerEntity p = CurrentRoomState.CurrentGameState.Players[i];
                if (p != null && p.PlayerId == playerID && CurrentRoomState.CurrentGameState.Players[i].IsOffline)
                {
                    posID = i;
                    break;
                }
            }

            if (posID < 0)
            {
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

            PublishMessage(new string[] { string.Format("玩家【{0}】断线重连成功", playerID) });
            PublishStartTimer(0);
            //加一秒缓冲时间，让客户端倒计时完成
            Thread.Sleep(0 + 1000);

            return true;
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

            if (needsRestart && !this.isGameOver) ResetAndRestartGame();

            foreach (string playerID in playerIDs)
            {
                PlayerQuitWorker(playerID);
            }
            UpdateGameState();

            //如果房主退出，则选择位置最靠前的玩家为房主
            if (playerIDs.Contains(CurrentRoomState.roomSetting.RoomOwner))
            {
                PlayerEntity next = this.CurrentRoomState.CurrentGameState.Players.FirstOrDefault(p => p != null);
                if (next != null) CurrentRoomState.roomSetting.RoomOwner = next.PlayerId;
                else CurrentRoomState.roomSetting.RoomOwner = string.Empty;
                UpdatePlayerRoomSettings(false);
            }

            return needsRestart;
        }

        public bool IsActualPlayer(string playerID)
        {
            foreach (PlayerEntity p in this.CurrentRoomState.CurrentGameState.Players)
            {
                if (p != null && p.Observers.Contains(playerID)) return false;
            }
            return true;
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

                log.Debug(playerID + " is not responding.");
                needsRestart = true;
                CurrentRoomState.CurrentGameState.PlayerToIP.Remove(playerID);
                PlayersProxy.Remove(playerID);
                this.tractorHost.PlayerToIP.Remove(playerID);
                this.tractorHost.PlayersProxy.Remove(playerID);
            }

            if (IsGameOnGoing())
            {
                MarkPlayersOffline(playerIDs);
            }

            return needsRestart && !IsGameOnGoing();
        }

        public void MarkPlayersOffline(List<string> playerIDs)
        {
            HashSet<string> offlinePlayerIDs = new HashSet<string>();
            {
                foreach (string playerID in playerIDs)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (CurrentRoomState.CurrentGameState.Players[i] != null && CurrentRoomState.CurrentGameState.Players[i].PlayerId == playerID)
                        {
                            CurrentRoomState.CurrentGameState.Players[i].IsOffline = true;
                            CurrentRoomState.CurrentGameState.Players[i].OfflineSince = DateTime.Now;
                            CurrentRoomState.CurrentGameState.Players[i].IsRobot = false;
                            offlinePlayerIDs.Add(playerID);
                            log.Debug(playerID + " went offline.");

                            break;
                        }
                    }
                }
            }
            if (offlinePlayerIDs.Count > 0)
            {
                var threadUpdateGameState = new Thread(() =>
                {
                    this.UpdateGameState();
                    Thread.Sleep(1000);
                    string[] msgs = new string[offlinePlayerIDs.Count];
                    int i = 0;
                    foreach (string playerID in offlinePlayerIDs)
                    {
                        msgs[i] = string.Format("玩家【{0}】已离线", playerID);
                        i++;
                    }
                    PublishMessage(msgs);
                    PublishStartTimer(CurrentRoomState.roomSetting.secondsToWaitForReenter);
                });
                threadUpdateGameState.Start();
            }
        }

        public void PlayerIsReadyToStart(string playerID)
        {
            lock (CurrentRoomState.CurrentGameState)
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
                            CleanupCaches();
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

        public void SpecialEndGameRequest(string playerID)
        {
            string teammateID = CurrentRoomState.CurrentGameState.GetNextPlayerAfterThePlayer(true, playerID).PlayerId;
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList(), "NotifyMessage", new List<object>() { new string[] { string.Format("玩家【{0}】发起投降提议", playerID) } });
            IPlayerInvoke(teammateID, PlayersProxy[teammateID], "SpecialEndGameShouldAgree", new List<object>() { }, true);
        }

        public void SpecialEndGameDeclined(string playerID)
        {
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList(), "NotifyMessage", new List<object>() { new string[] { string.Format("玩家【{0}】拒绝投降提议", playerID) } });
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
                        string initiaterID = CurrentRoomState.CurrentGameState.GetNextPlayerAfterThePlayer(true, playerID).PlayerId;
                        msgs = new string[] { string.Format("玩家【{0}】提议【投降】", initiaterID), string.Format("玩家【{0}】附议", playerID) };
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
            
            StringBuilder logMsg = new StringBuilder();
            logMsg.Append("player " + CurrentRoomState.CurrentHandState.Last8Holder + " discard 8 cards: ");

            foreach (var cardItem in cards)
            {
                logMsg.Append(string.Format("{0} {1}, ", CommonMethods.GetSuitString(cardItem), CommonMethods.GetNumberString(cardItem)));
            }
            log.Debug(logMsg.ToString());

            logMsg.Clear();
            logMsg.Append(CurrentRoomState.CurrentHandState.Last8Holder + "'s cards after discard 8 cards: ");

            foreach (int cardItem in CurrentRoomState.CurrentHandState.PlayerHoldingCards[CurrentRoomState.CurrentHandState.Last8Holder].GetCardsInList())
            {
                logMsg.Append(string.Format("{0} {1}, ", CommonMethods.GetSuitString(cardItem), CommonMethods.GetNumberString(cardItem)));
            }
            log.Debug(logMsg.ToString());

            UpdatePlayersCurrentHandState();

            //等待5秒，让玩家反底
            var trump = CurrentRoomState.CurrentHandState.Trump;
            Thread.Sleep(2000);
            if (trump == CurrentRoomState.CurrentHandState.Trump) //没有玩家反
            {
                //log trump and rank before a new game starts
                log.Debug(string.Format("starting a new game: starter {0} {1} {2}", CurrentRoomState.CurrentHandState.Starter, CurrentRoomState.CurrentHandState.Trump.ToString(), CommonMethods.GetNumberString(CurrentRoomState.CurrentHandState.Rank)));

                BeginNewTrick(CurrentRoomState.CurrentHandState.Starter);
                CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.Playing;
                UpdatePlayersCurrentHandState();

                BuildReplayEntity();

                SaveGameStateToFile();
            }
        }

        private void BuildReplayEntity()
        {
            this.replayEntity = new ReplayEntity();
            this.replayEntity.ReplayId = string.Format("{0}-{1}-{2}", System.DateTime.Now.ToString(string.Format("yyyy-MM-dd{0}HH-mm-ss", CommonMethods.replaySeparator)), CurrentRoomState.CurrentHandState.Starter.Replace(" ", "-"), CommonMethods.GetNumberString(CurrentRoomState.CurrentHandState.Rank));
            this.replayEntity.CurrentHandState = CommonMethods.DeepClone<CurrentHandState>(CurrentRoomState.CurrentHandState);
            this.replayEntity.CurrentTrickStates = new List<CurrentTrickState>();
            this.replayEntity.Players = new string[4];
            this.replayEntity.PlayerRanks = new int[4];
            for (int i = 0; i < 4; i++)
            {
                this.replayEntity.Players[i] = CurrentRoomState.CurrentGameState.Players[i].PlayerId;
                this.replayEntity.PlayerRanks[i] = CurrentRoomState.CurrentGameState.Players[i].Rank;
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
                    log.Debug(string.Format("player made trump: {0} {1} {2} {3}", trumpMaker, trumpExposingPoker.ToString(), trump.ToString(), CommonMethods.GetNumberString(CurrentRoomState.CurrentHandState.Rank)));

                    TrumpState tempLastTrumState = new TrumpState();
                    tempLastTrumState.TrumpExposingPoker = CurrentRoomState.CurrentHandState.TrumpExposingPoker;
                    tempLastTrumState.TrumpMaker = CurrentRoomState.CurrentHandState.TrumpMaker;
                    tempLastTrumState.Trump = CurrentRoomState.CurrentHandState.Trump;
                    CurrentRoomState.CurrentHandState.LastTrumpStates.Add(tempLastTrumState);

                    if (CurrentRoomState.CurrentHandState.IsFirstHand && CurrentRoomState.CurrentHandState.CurrentHandStep < HandStep.DistributingLast8Cards)
                    {
                        CurrentRoomState.CurrentHandState.Starter = trumpMaker;
                    }
                    UpdatePlayersCurrentHandState();
                }
            }
        }

        public void PlayerShowCards(CurrentTrickState currentTrickState)
        {
            string lastestPlayer = currentTrickState.LatestPlayerShowedCard();
            CurrentRoomState.CurrentTrickState.ShowedCards[lastestPlayer] = currentTrickState.ShowedCards[lastestPlayer];
            StringBuilder logMsg = new StringBuilder();
            logMsg.Append("Player " + lastestPlayer + " showed cards: ");
            foreach (var card in CurrentRoomState.CurrentTrickState.ShowedCards[lastestPlayer])
            {
                logMsg.Append(string.Format("{0} {1} ", CommonMethods.GetSuitString(card), CommonMethods.GetNumberString(card)));
            }
            log.Debug(logMsg.ToString());
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

                if (this.replayEntity != null) this.replayEntity.CurrentTrickStates.Add(CommonMethods.DeepClone<CurrentTrickState>(CurrentRoomState.CurrentTrickState));

                //开始新的回合
                if (CurrentRoomState.CurrentHandState.LeftCardsCount > 0)
                {
                    BeginNewTrick(CurrentRoomState.CurrentTrickState.Winner);
                }
                else //所有牌都出完了
                {
                    //扣底
                    CalculatePointsFromDiscarded8Cards();
                    if (this.replayEntity != null)
                    {
                        this.replayEntity.CurrentHandState.ScoreLast8CardsBase = CurrentRoomState.CurrentHandState.ScoreLast8CardsBase;
                        this.replayEntity.CurrentHandState.ScoreLast8CardsMultiplier = CurrentRoomState.CurrentHandState.ScoreLast8CardsMultiplier;
                        this.replayEntity.CurrentHandState.Score = CurrentRoomState.CurrentHandState.Score;
                        this.replayEntity.CurrentHandState.ScoreCards = CurrentRoomState.CurrentHandState.ScoreCards;
                        this.replayEntity.CurrentHandState.ScorePunishment = CurrentRoomState.CurrentHandState.ScorePunishment;
                    }
                    //log score details
                    int scoreLast8Cards = CurrentRoomState.CurrentHandState.ScoreLast8CardsBase * CurrentRoomState.CurrentHandState.ScoreLast8CardsMultiplier;
                    log.Debug("score from score cards: " + (CurrentRoomState.CurrentHandState.Score - scoreLast8Cards - CurrentRoomState.CurrentHandState.ScorePunishment));
                    string scoreFromLast8Cards="score from last 8 cards: " + scoreLast8Cards;
                    if (CurrentRoomState.CurrentHandState.ScoreLast8CardsBase > 0)
                    {
                        scoreFromLast8Cards += (string.Format("【{0}x{1}】", CurrentRoomState.CurrentHandState.ScoreLast8CardsBase, CurrentRoomState.CurrentHandState.ScoreLast8CardsMultiplier));
                    }
                    log.Debug(scoreFromLast8Cards);
                    log.Debug("score from dump punishment: " + CurrentRoomState.CurrentHandState.ScorePunishment);
                    log.Debug("score total: " + CurrentRoomState.CurrentHandState.Score);
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

                    UpdatePlayersCurrentHandState();

                    UpdateGameState();

                    UpdateReplayState();

                    //保存游戏当前状态，以备继续游戏
                    SaveGameStateToFile();

                    if (sb != null)
                    {
                        PublishMessage(new string[] { sb.ToString(), "获胜！", "点击就绪重新开始游戏" });
                        this.isGameOver = true;
                    }
                    else if (TractorHost.gameConfig.IsFullDebug && IsAllOnline())
                    {
                        CleanupCaches();
                        var threadStartNextHand = new Thread(() =>
                        {
                            Thread.Sleep(3000);
                            StartNextHand(CurrentRoomState.CurrentGameState.startNextHandStarter);
                        });
                        threadStartNextHand.Start();                        
                        return;
                    }
                    CleanupOfflinePlayers();
                }
            }
            else
            {
                if (CurrentRoomState.CurrentTrickState.CountOfPlayerShowedCards() == 1)
                {
                    CurrentRoomState.CurrentTrickState.serverLocalCache = serverLocalCache;

                    //保存游戏当前状态，以备继续游戏
                    SaveGameStateToFile();
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
                if ((DateTime.Now - player.OfflineSince).TotalSeconds <= CurrentRoomState.roomSetting.secondsToWaitForReenter) continue; //玩家断线后有一定时间断线重连，否则自动托管
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

        private void CleanupCaches()
        {
            //清空缓存
            this.serverLocalCache = new ServerLocalCache();
            CurrentRoomState.CurrentTrickState.Learder = string.Empty;
            CurrentRoomState.CurrentTrickState.ShowedCards.Clear();
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
                // 录像回放
                if (this.replayEntity != null)
                {
                    CurrentTrickState dumpTrick = CommonMethods.DeepClone<CurrentTrickState>(CurrentRoomState.CurrentTrickState);
                    dumpTrick.ShowedCards.Clear();
                    dumpTrick.ShowedCards.Add(playerId, selectedCards);
                    this.replayEntity.CurrentTrickStates.Add(dumpTrick);
                }

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
            string[] oldTeams = new string[4];
            string[] msgs = new string[10];
            msgs[0] = "随机组队前";
            for (int i = 0; i < 4; i++)
            {
                msgs[i + 1] = CurrentRoomState.CurrentGameState.Players[i].PlayerId;
                oldTeams[i] = CurrentRoomState.CurrentGameState.Players[i].PlayerId;
            }

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

            msgs[5] = "随机组队后";
            bool teamChanged = false;
            for (int i = 0; i < 4; i++)
            {
                msgs[i + 6] = CurrentRoomState.CurrentGameState.Players[i].PlayerId;
                if (oldTeams[i] != CurrentRoomState.CurrentGameState.Players[i].PlayerId) teamChanged = true;
            }
            if (!teamChanged) msgs[5] += "，组队未变，可再次尝试！";
            PublishMessage(msgs);
        }

        //旁观：选牌
        public void CardsReady(string playerId, ArrayList myCardIsReady)
        {
            PlayerEntity player = CurrentRoomState.CurrentGameState.Players.Single(p => p != null && p.PlayerId == playerId);
            IPlayerInvokeForAll(ObserversProxy, player.Observers.ToList<string>(), "NotifyCardsReady", new List<object>() { myCardIsReady });
        }

        //读取牌局
        public void RestoreGameStateFromFile(GameState gs, CurrentHandState hs, List<string> restoredMsg)
        {
            for (int i = 0; i < 4; i++)
            {
                CurrentRoomState.CurrentGameState.Players[i].Rank = gs.Players[i].Rank;
            }
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
            restoredMsg.Add("请点击就绪继续游戏");
            PublishMessage(restoredMsg.ToArray());
        }

        //继续牌局
        public void ResumeGameFromFile()
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
                PublishMessage(new string[] { "继续牌局失败", "玩家人数不够" });
                return;
            }
            List<string> restoredMsg = new List<string>();
            string fileNameGamestate = string.Format("{0}\\{1}", this.LogsByRoomFullFolder, this.BackupGamestateFileName);

            GameState gs = CommonMethods.ReadObjectFromFile<GameState>(fileNameGamestate);
            if (gs == null)
            {
                restoredMsg.Add("继续牌局【失败】- GameState");
                PublishMessage(restoredMsg.ToArray());
                return;
            }
            gs.Players.RemoveAll(p => p == null);
            if (gs.Players.Count != 4)
            {
                restoredMsg.AddRange(new string[] { "继续牌局【失败】- GameState", "游戏文件中玩家人数不够：" + gs.Players.Count });
                PublishMessage(restoredMsg.ToArray());
                return;
            }
            bool isResumable = true;
            for (int i = 0; i < 4; i++)
            {
                if (CurrentRoomState.CurrentGameState.Players[i].PlayerId != gs.Players[i].PlayerId)
                {
                    restoredMsg.Add("继续牌局【失败】- GameState");
                    restoredMsg.Add("游戏文件中玩家与当前玩家不匹配");
                    restoredMsg.Add(string.Format("游戏文件中【{0}号位】玩家为：", i + 1));
                    restoredMsg.Add(gs.Players[i].PlayerId);
                    restoredMsg.Add(string.Format("当前【{0}号位】玩家为：", i + 1));
                    restoredMsg.Add(CurrentRoomState.CurrentGameState.Players[i].PlayerId);
                    isResumable = false;
                    break;
                }
            }

            string fileNameHandState = string.Format("{0}\\{1}", this.LogsByRoomFullFolder, this.BackupHandStateFileName);
            CurrentHandState hs = CommonMethods.ReadObjectFromFile<CurrentHandState>(fileNameHandState);
            if (hs == null)
            {
                restoredMsg.Add("继续牌局【失败】- HandState");
                PublishMessage(restoredMsg.ToArray());
                return;
            }

            // 到了这里，至少可以还原牌局了，但不一定能还原手牌
            // 因为游戏状态备份文件不包含旁观玩家信息
            // 还原之前必须先把旁观玩家清除，否则会造成游戏状态紊乱
            List<string> obs = new List<string>();
            foreach (PlayerEntity p in CurrentRoomState.CurrentGameState.Players)
            {
                obs.AddRange(p.Observers);
            }
            foreach (string ob in obs)
            {
                this.tractorHost.PlayerExitRoom(ob);
            }

            foreach (var cp in hs.PlayerHoldingCards)
            {
                if (cp.Value.Count == 0)
                {
                    restoredMsg.AddRange(new string[] { "继续牌局【失败】- HandState", "上盘牌局已出完所有牌" }); ;
                    isResumable = false;
                    break;
                }
            }
            if (!isResumable)
            {
                RestoreGameStateFromFile(gs, hs, restoredMsg);
                return;
            }
            CurrentRoomState.CurrentGameState = gs;
            CurrentRoomState.CurrentHandState = hs;

            string fileNameTrickState = string.Format("{0}\\{1}", this.LogsByRoomFullFolder, this.BackupTrickStateFileName);
            CurrentTrickState ts = CommonMethods.ReadObjectFromFile<CurrentTrickState>(fileNameTrickState);
            if (ts == null)
            {
                restoredMsg.Add("继续牌局【失败】- TrickState");
                PublishMessage(restoredMsg.ToArray());
                return;
            }
            CurrentRoomState.CurrentTrickState = ts;

            for (int i = 0; i < 4; i++)
            {
                CurrentRoomState.CurrentGameState.Players[i].IsReadyToStart = true;
                CurrentRoomState.CurrentGameState.Players[i].IsRobot = false;
            }

            PublishMessage(new string[] { CommonMethods.resumeGameSignal });
            Thread.Sleep(2000);

            UpdatePlayerCurrentTrickState();
            UpdatePlayersCurrentHandState();
            Thread.Sleep(2000);
            UpdateGameState();

            restoredMsg.Add("继续牌局【成功】");
            PublishMessage(restoredMsg.ToArray());
        }

        //读取房间游戏设置
        public RoomSetting ReadRoomSettingFromFile()
        {
            RoomSetting roomSetting = new RoomSetting();
            string fileNameRoomSetting = string.Format("{0}\\{1}", this.LogsByRoomFullFolder, this.BackupRoomSettingFileName);
            RoomSetting rs = CommonMethods.ReadObjectFromFile<RoomSetting>(fileNameRoomSetting);
            if (rs != null)
            {
            	roomSetting = rs;
            }
            return roomSetting;
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
            if (HandStep.DistributingCards <= this.CurrentRoomState.CurrentHandState.CurrentHandStep && this.CurrentRoomState.CurrentHandState.CurrentHandStep <= HandStep.Playing)
            {
                UpdatePlayersCurrentHandState();
            }
        }
        #endregion

        #region Host Action
        public void RestartGame(int curRank)
        {
            this.isGameOver = false;
            log.Debug("restart game with current set rank: " + CommonMethods.GetNumberString(curRank));

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
                if (CurrentRoomState.CurrentHandState.Trump == Suit.None)
                {
                    PublishMessage(new string[] { "如果无人亮主", "则重新发牌！" });
                }
                PublishStartTimer(5);
                //加一秒缓冲时间，让客户端倒计时完成
                Thread.Sleep(5000 + 1000);
                if (CurrentRoomState.CurrentHandState.Trump == oldTrump)
                {
                    break;
                }
                oldTrump = CurrentRoomState.CurrentHandState.Trump;
            }

            if (IsAllOnline())
            {
                if (CurrentRoomState.CurrentHandState.Trump != Suit.None) DistributeLast8Cards();
                else RestartGame(curRank);
            }
        }

        public void RestartCurrentHand()
        {
            log.Debug("restart current hand, starter: " + CurrentRoomState.CurrentHandState.Starter + " Rank: " + CommonMethods.GetNumberString(CurrentRoomState.CurrentHandState.Rank));
            StartNextHand(CurrentRoomState.CurrentGameState.Players.Single(p => p != null && p.PlayerId == CurrentRoomState.CurrentHandState.Starter));
        }

        public void StartNextHand(PlayerEntity nextStarter)
        {
            this.isGameOver = false;

            UpdateGameState();
            CurrentRoomState.CurrentHandState = new CurrentHandState(CurrentRoomState.CurrentGameState);
            CurrentRoomState.CurrentHandState.Starter = nextStarter.PlayerId;
            CurrentRoomState.CurrentHandState.Rank = nextStarter.Rank;
            CurrentRoomState.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);

            log.Debug("start next hand, starter: " + CurrentRoomState.CurrentHandState.Starter + " Rank: " + CommonMethods.GetNumberString(CurrentRoomState.CurrentHandState.Rank));

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
                    PublishMessage(new string[] { "如果无人亮主", "则打无主！" });
                }
                PublishStartTimer(5);
                Thread.Sleep(5000 + 1000);
                if (CurrentRoomState.CurrentHandState.Trump == oldTrump)
                {
                    break;
                }
                oldTrump = CurrentRoomState.CurrentHandState.Trump;
            }

            if (IsAllOnline())
            {
                //如果无人亮主，则打无主
                if (CurrentRoomState.CurrentHandState.Trump == Suit.None)
                {
                    log.Debug("no one made trump, default to no-trump");
                    CurrentRoomState.CurrentHandState.TrumpExposingPoker = TrumpExposingPoker.PairRedJoker;
                    CurrentRoomState.CurrentHandState.TrumpMaker = CurrentRoomState.CurrentHandState.Starter;
                    CurrentRoomState.CurrentHandState.Trump = Suit.Joker;
                }
                DistributeLast8Cards();
            }
        }

        //发牌
        // returns: needRestart
        public bool DistributeCards()
        {
            CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.DistributingCards;
            UpdatePlayersCurrentHandState();
            string currentHandId = CurrentRoomState.CurrentHandState.Id;

            for (int i = 0; i < 4; i++)
            {
                if (CurrentRoomState.CurrentGameState.Players[i] == null)
                {
                    //玩家退出，停止发牌
                    return true;
                }
            }

            //保证庄家首先摸牌
            string starterID = CurrentRoomState.CurrentGameState.Players[0].PlayerId;
            string preStarterID = CurrentRoomState.CurrentGameState.Players[3].PlayerId;
            if (!string.IsNullOrEmpty(CurrentRoomState.CurrentHandState.Starter)) starterID = CurrentRoomState.CurrentHandState.Starter;
            string[] playersFromStarter = new string[4];
            playersFromStarter[0] = starterID;
            for (int x = 1; x < 4; x++)
            {
                playersFromStarter[x] = CurrentRoomState.CurrentGameState.GetNextPlayerAfterThePlayer(playersFromStarter[x - 1]).PlayerId;
                if (x == 3) preStarterID = playersFromStarter[x];
            }

            if (this.CardsShoe.IsCardsRestored)
            {
                this.CardsShoe.IsCardsRestored = false;
            }
            else
            {
                ShuffleCardsWithRNGCsp(this.CardsShoe);
                //切牌
                IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList(), "NotifyMessage", new List<object>() { new string[] { string.Format("等待玩家【{0}】切牌", preStarterID) } });
                if (ObserversProxy.Count > 0)
                {
                    IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList(), "NotifyMessage", new List<object>() { new string[] { string.Format("等待玩家【{0}】切牌", preStarterID) } });
                }

                string cutInfoString = PlayersProxy[preStarterID].CutCardShoeCards();
                string[] cutInfos = cutInfoString.Split(',');
                CutCards(this.CardsShoe, Int32.Parse(cutInfos[1]));

                string cutMsg = cutInfos[0];
                if (cutInfos[0] == "随机" || cutInfos[0] == "手动")
                {
                    cutMsg = string.Format("{0}{1}张", cutInfos[0], cutInfos[1]);
                }

                IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList(), "NotifyMessage", new List<object>() { new string[] { string.Format("玩家【{0}】切牌：", preStarterID), cutMsg } });
                if (ObserversProxy.Count > 0)
                {
                    IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList(), "NotifyMessage", new List<object>() { new string[] { string.Format("玩家【{0}】切牌：", preStarterID, cutMsg), cutMsg } });
                }
                PublishStartTimer(2);
                //加一秒缓冲时间，让客户端倒计时完成
                Thread.Sleep(2000 + 1000);
                if (!IsAllOnline()) return true;
            }

            if (string.IsNullOrEmpty(CurrentRoomState.CurrentHandState.Starter))
            {
                //新游戏开始前给出提示信息，开始倒计时，并播放提示音，告诉玩家要抢庄
                PublishMessage(new string[] { "新游戏即将开始", "做好准备抢庄！" });
                PublishStartTimer(5);
                //加一秒缓冲时间，让客户端倒计时完成
                Thread.Sleep(5000 + 1000);
                if (!IsAllOnline()) return true;
            }

            int cardNumberofEachPlayer = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);
            int index = 0;

            var LogList = new Dictionary<string, StringBuilder>();
            foreach (var player in PlayersProxy)
            {
                LogList[player.Key] = new StringBuilder();
            }

            for (int i = 0; i < cardNumberofEachPlayer; i++)
            {
                foreach (var playerID in playersFromStarter)
                {
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
                    index++;
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
                StringBuilder cardsString = new StringBuilder();
                foreach (int cardItem in keyvalue.Value.GetCardsInList())
                {
                    cardsString.Append(string.Format("{0} {1}, ", CommonMethods.GetSuitString(cardItem), CommonMethods.GetNumberString(cardItem)));
                }
                log.Debug(keyvalue.Key + "'s cards:  " + cardsString.ToString());
            }

            CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.DistributingCardsFinished;
            UpdatePlayersCurrentHandState();

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

            StringBuilder logMsg = new StringBuilder();
            logMsg.Append("distribute last 8 cards to " + CurrentRoomState.CurrentHandState.Last8Holder + ", cards: ");
            foreach (var cardItem in last8Cards)
            {
                logMsg.Append(string.Format("{0} {1}, ", CommonMethods.GetSuitString(cardItem), CommonMethods.GetNumberString(cardItem)));
            }
            log.Debug(logMsg.ToString());

            CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.DistributingLast8CardsFinished;
            UpdatePlayersCurrentHandState();

            Thread.Sleep(100);
            CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.DiscardingLast8Cards;
            UpdatePlayersCurrentHandState();

            SaveGameStateToFile();

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

                //底牌分数按照2的指数级翻倍
                int multiplier = 1;
                if (cardscp.HasTractors())
                {
                    double index = cardscp.GetTractor(CurrentRoomState.CurrentTrickState.LeadingSuit).Count * 2;
                    double multiDouble = Math.Pow(2, index);
                    multiplier = Convert.ToInt32(multiDouble);
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
            CommonMethods.WriteObjectToFile(CurrentRoomState.CurrentGameState, this.LogsByRoomFullFolder, this.BackupGamestateFileName);
            CommonMethods.WriteObjectToFile(CurrentRoomState.CurrentHandState, this.LogsByRoomFullFolder, this.BackupHandStateFileName);
            CommonMethods.WriteObjectToFile(CurrentRoomState.CurrentTrickState, this.LogsByRoomFullFolder, this.BackupTrickStateFileName);
        }

        //保存房间游戏设置
        private void SaveRoomSettingToFile()
        {
            CommonMethods.WriteObjectToFile(this.CurrentRoomState.roomSetting, this.LogsByRoomFullFolder, this.BackupRoomSettingFileName);
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

        public void UpdateReplayState()
        {
            if (this.replayEntity == null) return;
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "NotifyReplayState", new List<object>() { this.replayEntity });
            IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList<string>(), "NotifyReplayState", new List<object>() { this.replayEntity });

            string[] fileInfo = CommonMethods.GetReplayEntityFullFilePath(this.replayEntity, ReplaysByRoomFullFolder);
            if (fileInfo != null)
            {
                CommonMethods.WriteObjectToFile(this.replayEntity, fileInfo[0], string.Format("{0}", fileInfo[1]));
            }
            else
            {
                this.log.Debug(string.Format("录像文件名错误: {0}", this.replayEntity.ReplayId));
            }
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

        public void UpdatePlayerRoomSettings(bool showMessage)
        {
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "NotifyRoomSetting", new List<object>() { this.CurrentRoomState.roomSetting, showMessage });
            IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList<string>(), "NotifyRoomSetting", new List<object>() { this.CurrentRoomState.roomSetting, showMessage });
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
            CleanupCaches();
            CurrentRoomState.CurrentHandState = new CurrentHandState(CurrentRoomState.CurrentGameState);
            CurrentRoomState.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);
            CurrentRoomState.CurrentHandState.IsFirstHand = true;
            UpdatePlayersCurrentHandState();
            UpdateGameState();
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "StartGame", new List<object>() { });
        }

        public static void LogClientInfo(string clientIP, string playerID, bool isCheating)
        {
            Dictionary<string, ClientInfo> clientInfoDict = new Dictionary<string, ClientInfo>();
            string fileName = string.Format("{0}\\{1}", LogsFolder, ClientinfoFileName);
            if (File.Exists(fileName))
            {
                clientInfoDict = CommonMethods.ReadObjectFromFile<Dictionary<string, ClientInfo>>(fileName);
            }
            if (!clientInfoDict.ContainsKey(clientIP))
            {
                clientInfoDict[clientIP] = new ClientInfo(clientIP, playerID);
            }
            clientInfoDict[clientIP].logLogin(playerID, isCheating);
            CommonMethods.WriteObjectToFile(clientInfoDict, LogsFolder, ClientinfoFileName);
        }

        public void ShuffleCurrentGameStatePlayers()
        {
            for (int i = 3; i >= 1; i--)
            {
                //randomly choose a player within 0 to i, and put it at position i
                int r = CommonMethods.RandomNext(i + 1);
                if (r != i) SwapPlayers(r, i);
            }
        }

        private void SwapPlayers(int p1, int p2)
        {
            PlayerEntity temp = CurrentRoomState.CurrentGameState.Players[p1];
            CurrentRoomState.CurrentGameState.Players[p1] = CurrentRoomState.CurrentGameState.Players[p2];
            CurrentRoomState.CurrentGameState.Players[p2] = temp;
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

        public void ShuffleCardsWithRNGCsp(CardsShoe cardShoe)
        {
            log.Debug(string.Format("before shuffle: {0}", string.Join(", ", cardShoe.Cards)));
            cardShoe.KnuthShuffleWithRNGCsp();
            log.Debug(string.Format("after shuffle: {0}", string.Join(", ", cardShoe.Cards)));
        }

        //切牌
        public void CutCards(CardsShoe cardShoe, int cutPoint)
        {
            log.Debug(string.Format("cutPoint: {0}", cutPoint));
            if (cutPoint <= 0 || cutPoint >= cardShoe.Cards.Length) return;
            log.Debug(string.Format("before cut: {0}", string.Join(", ", cardShoe.Cards)));

            CommonMethods.RotateArray(cardShoe.Cards, cutPoint);

            log.Debug(string.Format("after cut: {0}", string.Join(", ", cardShoe.Cards)));
        }

        public void Swap(int[] Cards, int i, int r)
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
            UpdatePlayerRoomSettings(true);

            //save setting to file
            SaveRoomSettingToFile();
        }
    }
}
