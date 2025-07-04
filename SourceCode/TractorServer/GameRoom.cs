﻿﻿using Duan.Xiugang.Tractor.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace TractorServer
{
    public class GameRoom
    {
        private log4net.ILog log;
        public static string LogsFolder = "logs";
        public static string ReplaysFolder = "replays";
        public static string ClientinfoFileName = "clientinfo_v2.json";
        public static string ClientinfoV3FileName = "clientinfo_v3.json";
        public static string EmailSettingsFileName = "emailsettings.json";
        public static string RegCodesFileName = "regcodes.json";
        public static string SkinInfoFileName = "skininfo.json";
        public static string ForbidSayingsFileName = "forbidSayings.json";
        public static string IllegalOperationLogName = "illegalOperationHistory";
        public static string TransactionLogName = "transactionHistory";
        public static string HostStatusFileName = "hostStatus.json";
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
        public WebSocketObjects.SGCSState sgcsState;
        public WebSocketObjects.SGGBState sggbState;

        public Dictionary<string, IPlayer> PlayersProxy { get; set; }
        public Dictionary<string, IPlayer> ObserversProxy { get; set; }
        private ServerLocalCache serverLocalCache;
        private bool isGameRestarted;
        private bool shouldKeepCardsShoeFromRestore = false;
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
        public bool PlayerEnterRoom(string playerID, string clientIP, IPlayer player, bool allowSameIP, int posID, bool skipUpdateGameState)
        {
            string otherID = string.Empty;
            if (CurrentRoomState.CurrentGameState.PlayerToIP.ContainsValue(clientIP))
            {
                foreach (KeyValuePair<string, string> entry in CurrentRoomState.CurrentGameState.PlayerToIP)
                {
                    if (string.Equals(entry.Value, clientIP))
                    {
                        otherID = entry.Key;
                    }
                }
            }
            if (!string.IsNullOrEmpty(otherID) && !(clientIP.StartsWith("127.0") || clientIP.StartsWith("192.168")))
            {
                string cheating = string.Format("player {0} (with other ID {1}) attempted to double play with IP {2}", playerID, otherID, clientIP);
                tractorHost.LogClientInfo(clientIP, playerID, cheating, false, "");
                this.tractorHost.illegalOperationLogger.Debug(string.Format("observer {0} attempted to double play.", playerID));
            }

            if (!PlayersProxy.Keys.Contains(playerID) && !ObserversProxy.Keys.Contains(playerID))
            {
                //加入旁观
                if (posID < 0)
                {
                    int firstActualPlayerIndex = CommonMethods.GetFirstActualPlayerIndex(CurrentRoomState.CurrentGameState.Players);
                    if (firstActualPlayerIndex < 0)
                    {
                        new Thread(new ThreadStart(() =>
                        {
                            Thread.Sleep(700);
                            player.NotifyMessage(new string[] { "房间为空", "不能加入旁观模式", "请点击座位加入玩家模式" });
                        })).Start();
                        return false;
                    }
                    //防止双开旁观
                    string msg = string.Format("玩家【{0}】加入旁观", playerID);
                    if (!string.IsNullOrEmpty(otherID))
                    {
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

                    log.Debug(string.Format("observer {0} joined.", playerID));

                    player.NotifyRoomSetting(this.CurrentRoomState.roomSetting, false);
                    ObserversProxy.Add(playerID, player);
                    ObservePlayerById(CurrentRoomState.CurrentGameState.Players[firstActualPlayerIndex].PlayerId, playerID, skipUpdateGameState);

                    if (IsGameOnGoing())
                    {
                        IPlayerInvoke(playerID, player, "NotifyCurrentTrickState", new List<object>() { CurrentRoomState.CurrentTrickState }, true);
                        IPlayerInvoke(playerID, player, "NotifyCurrentHandState", new List<object>() { CurrentRoomState.CurrentHandState }, true);
                    }
                    else
                    {
                        player.NotifyRoomSetting(this.CurrentRoomState.roomSetting, true);
                    }

                    //Thread.Sleep(2000);
                    if (!skipUpdateGameState) IPlayerInvoke(playerID, player, "NotifyGameState", new List<object>() { CurrentRoomState.CurrentGameState }, true);

                    // gobang state publication if it is on
                    // need delay to create, to allow for some time for client to destroy old UI when exit and observe
                    new Thread(new ThreadStart(() =>
                    {
                        Thread.Sleep(500);
                        if (this.sggbState != null && (this.sggbState.GameStage != "over" || !string.IsNullOrEmpty(this.sggbState.PlayerIdWinner)))
                        {
                            IPlayerInvokeForAll(ObserversProxy, new List<string>() { playerID }, "NotifyUpdateGobang", new List<object>() { this.sggbState });
                        }
                    })).Start();

                    return true;
                }

                if (!string.IsNullOrEmpty(otherID))
                {
                    string msg = string.Format("玩家【{0}】加入房间", playerID);
                    if (!allowSameIP)
                    {
                        msg += "？？失败";
                        PublishMessage(new string[] { msg });
                        player.NotifyMessage(new string[] { "已在游戏中", "请勿双开", "", "" });
                        return false;
                    }
                    else
                    {
                        msg += "？？";
                        PublishMessage(new string[] { msg });
                    }
                }

                if (CurrentRoomState.CurrentGameState.Players[posID] == null)
                {
                    CurrentRoomState.CurrentGameState.Players[posID] = new PlayerEntity { PlayerId = playerID, Rank = 0, Team = GameTeam.None, Observers = new HashSet<string>() };
                }
                else
                {
                    player.NotifyMessage(new string[] { "该座位已被占用", "请尝试选择另一个座位" });
                    return false;
                }
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
                if (!skipUpdateGameState) UpdateGameState();

                player.NotifyRoomSetting(this.CurrentRoomState.roomSetting, true);
                // gobang state publication if it is on
                if (this.sggbState != null && (this.sggbState.GameStage != "over" || !string.IsNullOrEmpty(this.sggbState.PlayerIdWinner)))
                {
                    IPlayerInvokeForAll(PlayersProxy, new List<string>() { playerID }, "NotifyUpdateGobang", new List<object>() { this.sggbState });
                }
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
                    ObservePlayerById(obeserveeId, playerID, skipUpdateGameState);
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
            CurrentRoomState.CurrentGameState.PlayerToIP.Add(playerID, clientIP);
            PlayersProxy.Add(playerID, player);
            log.Debug(string.Format("player {0} re-joined room from offline.", playerID));

            player.NotifyRoomSetting(this.CurrentRoomState.roomSetting, false);
            UpdatePlayersCurrentHandState();

            // if it is a new trickstate, allow reentered player to show all 4 players showed cards first
            List<String> playerIDList = new List<string>();
            foreach (PlayerEntity p in CurrentRoomState.CurrentGameState.Players)
            {
                if (p == null) continue;
                playerIDList.Add(p.PlayerId);
            }
            if (!CurrentRoomState.CurrentTrickState.IsStarted() && serverLocalCache.lastShowedCards.Count > 0)
            {
                CurrentTrickState cts = CommonMethods.DeepClone<CurrentTrickState>(CurrentRoomState.CurrentTrickState);
                cts.ShowedCards = CommonMethods.DeepClone<List<ShowedCardKeyValue>>(serverLocalCache.lastShowedCards);
                cts.Learder = serverLocalCache.lastLeader;
                //先发第一次，让短线重连的玩家收到上一轮的CurrentTrickState，以重画已出的牌
                player.NotifyCurrentTrickState(cts);
            }
            else
            {
                player.NotifyCurrentTrickState(CurrentRoomState.CurrentTrickState);
            }

            Thread.Sleep(2000);
            UpdateGameState();

            //再发一次，让短线重连的玩家收到最新的CurrentTrickState
            if (!CurrentRoomState.CurrentTrickState.IsStarted())
            {
                player.NotifyCurrentTrickState(CurrentRoomState.CurrentTrickState);
            }

            PublishMessage(new string[] { string.Format("玩家【{0}】断线重连成功", playerID) });

            if (CurrentRoomState.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8Cards)
            {
                //玩家埋底倒计时
                if (CurrentRoomState.roomSetting.secondsToDiscardCards > 0)
                {
                    string nextPlayerID = CurrentRoomState.CurrentHandState.Last8Holder;
                    PlayerEntity nextPlayer = CommonMethods.GetPlayerByID(this.CurrentRoomState.CurrentGameState.Players, nextPlayerID);
                    if (string.Equals(nextPlayerID, playerID, StringComparison.OrdinalIgnoreCase) && !nextPlayer.IsOffline)
                    {
                        PublishStartTimerByPlayer(CurrentRoomState.roomSetting.secondsToDiscardCards, playerID);
                    }
                }
            }
            else
            {
                //玩家出牌倒计时
                if (CurrentRoomState.roomSetting.secondsToShowCards > 0)
                {
                    string nextPlayerID = this.CurrentRoomState.CurrentTrickState.NextPlayer();
                    PlayerEntity nextPlayer = CommonMethods.GetPlayerByID(this.CurrentRoomState.CurrentGameState.Players, nextPlayerID);
                    if (string.Equals(nextPlayerID, playerID, StringComparison.OrdinalIgnoreCase) && !nextPlayer.IsOffline)
                    {
                        PublishStartTimerByPlayer(CurrentRoomState.roomSetting.secondsToShowCards, playerID);
                    }
                }
            }

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

            if (needsRestart) ResetAndRestartGame();

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
                UpdatePlayerRoomSettings(false);
            }

            return needsRestart;
        }

        //玩家已在房间里，直接从旁观加入游戏
        public bool PlayerExitAndEnterRoom(string playerID, string clientIP, IPlayer player, bool allowSameIP, int posID)
        {
            if (!IsActualPlayer(playerID))
            {
                RemoveObserver(new List<string>() { playerID });
                if (this.PlayerEnterRoom(playerID, clientIP, player, allowSameIP, posID, false))
                {
                    return true;
                }
                UpdateGameState();
                return false;
            }
            else
            {
                SwapPlayers(CommonMethods.GetPlayerIndexByID(this.CurrentRoomState.CurrentGameState.Players, playerID), posID);
                player.NotifyRoomSetting(this.CurrentRoomState.roomSetting, true);
                ResetAndRestartGame();
                CurrentRoomState.CurrentGameState.nextRestartID = GameState.RESTART_GAME;
                UpdateGameState();
                return true;
            }
        }

        public bool IsActualPlayer(string playerID)
        {
            foreach (PlayerEntity p in this.CurrentRoomState.CurrentGameState.Players)
            {
                if (p != null && p.PlayerId == playerID) return true;
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
                return;
            }

            CurrentRoomState.CurrentGameState.PlayerToIP.Remove(playerID);
            log.Debug(playerID + " quit.");
            if (this.sgcsState != null && !this.sgcsState.IsGameOver)
            {
                this.NotifyEndCollectStar(CommonMethods.GetPlayerIndexByID(this.CurrentRoomState.CurrentGameState.Players, playerID));
            }
            if (this.sggbState != null && !string.IsNullOrEmpty(this.sggbState.GameStage))
            {
                this.sggbState.GameAction = "quit";
                this.UpdateGobang(playerID, this.sggbState);
            }
            PlayersProxy.Remove(playerID);
            if (PlayersProxy.Count == 0 && this.sgcsState != null) this.sgcsState.IsGameOver = true;
            for (int i = 0; i < 4; i++)
            {
                if (CurrentRoomState.CurrentGameState.Players[i] != null)
                {
                    CurrentRoomState.CurrentGameState.Players[i].Rank = 0;
                    CurrentRoomState.CurrentGameState.Players[i].IsReadyToStart = false;
                    CurrentRoomState.CurrentGameState.Players[i].IsRobot = false;
                    CurrentRoomState.CurrentGameState.Players[i].IsQiangliang = false;
                    CurrentRoomState.CurrentGameState.Players[i].Team = GameTeam.None;
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
                    this.tractorHost.CleanupIPlayer(playerID);
                    continue;
                }

                log.Debug(playerID + " is not responding.");
                needsRestart = true;
                CurrentRoomState.CurrentGameState.PlayerToIP.Remove(playerID);

                // perform clean up
                if (this.sgcsState != null && !this.sgcsState.IsGameOver)
                {
                    this.NotifyEndCollectStar(CommonMethods.GetPlayerIndexByID(this.CurrentRoomState.CurrentGameState.Players, playerID));
                }
                if (this.sggbState != null && !string.IsNullOrEmpty(this.sggbState.GameStage))
                {
                    this.sggbState.GameAction = "quit";
                    this.UpdateGobang(playerID, this.sggbState);
                }
                PlayersProxy.Remove(playerID);
                if (PlayersProxy.Count == 0 && this.sgcsState != null) this.sgcsState.IsGameOver = true;
                this.tractorHost.CleanupIPlayer(playerID);
            }

            new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(500);
                this.tractorHost.UpdateGameHall();
            })).Start();

            if (IsGameOnGoing())
            {
                MarkPlayersOffline(playerIDs);
            }

            return needsRestart && !IsGameOnGoing();
        }

        // returns: needs to restart
        public bool handleWSPlayerDisconnect(string playerID)
        {
            if (this.IsAllOnline() && this.IsActualPlayer(playerID) && (HandStep.DiscardingLast8Cards <= this.CurrentRoomState.CurrentHandState.CurrentHandStep && this.CurrentRoomState.CurrentHandState.CurrentHandStep <= HandStep.Playing))
            {
                return this.PlayerQuitFromCleanup(new List<string>() { playerID });
            }
            return true;
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
                            CurrentRoomState.CurrentGameState.Players[i].IsQiangliang = false;
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
                        p.IsReadyToStart = !p.IsReadyToStart;
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
                    this.isGameRestarted = false;
                    switch (CurrentRoomState.CurrentGameState.nextRestartID)
                    {
                        case GameState.RESTART_GAME:
                            CleanupCaches();
                            CheckRoomSettingRandomTeamUp();
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

        private void CheckRoomSettingRandomTeamUp()
        {
            if (this.CurrentRoomState.roomSetting.RandomTeamUp)
            {
                this.TeamUp(false);
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

        public void PlayerToggleIsQiangliang(string playerID)
        {
            foreach (PlayerEntity p in CurrentRoomState.CurrentGameState.Players)
            {
                if (p != null && p.PlayerId == playerID)
                {
                    p.IsQiangliang = !p.IsQiangliang;
                    break;
                }
            }
            UpdateGameState();
        }

        public void NotifySgcsPlayerUpdated(string content)
        {
            if (this.sgcsState.IsGameOver) return;
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList(), "NotifySgcsPlayerUpdated", new List<object>() { content });
            IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList(), "NotifySgcsPlayerUpdated", new List<object>() { content });
        }

        // returns: true - created successfully, false - someone else is already playing
        public bool NotifyCreateCollectStar(WebSocketObjects.SGCSState state)
        {
            bool needToUpdateGameState = false;
            if (state.Stage < 0)
            {
                if (IsSmallGameOngoing()) return false;
                this.sgcsState = state;
                log.Debug(string.Format("player {0} started game: {1}", sgcsState.PlayerId, WebSocketObjects.SmallGameName_CollectStar));
                this.sgcsState.Stage = 1;
                for (int i = 0; i < this.CurrentRoomState.CurrentGameState.Players.Count; i++)
                {
                    WebSocketObjects.SGCSDude dude = new WebSocketObjects.SGCSDude();
                    this.sgcsState.Dudes.Add(dude);
                    dude.Tint = CommonMethods.dudeTints[i];
                    PlayerEntity p = this.CurrentRoomState.CurrentGameState.Players[i];
                    if (p != null)
                    {
                        dude.PlayerId = this.CurrentRoomState.CurrentGameState.Players[i].PlayerId;
                        dude.Enabled = true;
                        p.PlayingSG = WebSocketObjects.SmallGameName_CollectStar;
                        needToUpdateGameState = true;
                        dude.X = CommonMethods.random.Next(400);
                        dude.Y = 450;
                    }
                }

                int starX = 12;
                for (int i = 0; i < 12; i++)
                {
                    WebSocketObjects.SGCSStar newStar = new WebSocketObjects.SGCSStar();
                    newStar.Enabled = true;
                    newStar.X = starX + i * 70;
                    newStar.Bounce = (4 + CommonMethods.random.Next(4)) / 10.0;
                    this.sgcsState.Stars.Add(newStar);
                }
                WebSocketObjects.SGCSBomb newBomb = new WebSocketObjects.SGCSBomb();
                newBomb.X = CommonMethods.random.Next(800);
                newBomb.VelX = (50 + CommonMethods.random.Next(100)) * (CommonMethods.random.Next(2) == 0 ? -1 : 1);
                this.sgcsState.Bombs.Add(newBomb);
            }
            else
            {
                this.sgcsState.Stage++;
                for (int i = 0; i < 12; i++)
                {
                    this.sgcsState.Stars[i].Enabled = true;
                    this.sgcsState.Stars[i].X = 5 + CommonMethods.random.Next(790);
                    this.sgcsState.Stars[i].Bounce = (4 + CommonMethods.random.Next(4)) / 10.0;
                }
                WebSocketObjects.SGCSBomb nextBomb = new WebSocketObjects.SGCSBomb();
                nextBomb.X = CommonMethods.random.Next(800);
                nextBomb.VelX = (50 + CommonMethods.random.Next(100)) * (CommonMethods.random.Next(2) == 0 ? -1 : 1);
                this.sgcsState.Bombs.Add(nextBomb);
            }

            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList(), "NotifyCreateCollectStar", new List<object>() { this.sgcsState });
            IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList(), "NotifyCreateCollectStar", new List<object>() { this.sgcsState });
            if (needToUpdateGameState)
            {
                UpdateGameState();
            }
            return true;
        }

        private bool IsSmallGameOngoing()
        {
            return (this.sgcsState != null && !this.sgcsState.IsGameOver) ||
                (this.sggbState != null && !string.IsNullOrEmpty(this.sggbState.PlayerId1));
        }

        public void NotifyGrabStar(int playerIndex, int starIndex)
        {
            if (this.sgcsState.IsGameOver) return;
            lock (this.sgcsState)
            {
                if (this.sgcsState.Stars[starIndex].Enabled)
                {
                    this.sgcsState.Dudes[playerIndex].Score += 10;
                    this.sgcsState.Stars[starIndex].Enabled = false;
                    IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList(), "NotifyGrabStar", new List<object>() { playerIndex, starIndex });
                    IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList(), "NotifyGrabStar", new List<object>() { playerIndex, starIndex });
                    if (!this.sgcsState.Stars.Exists(s => s.Enabled))
                    {
                        new Thread(() =>
                        {
                            this.NotifyCreateCollectStar(this.sgcsState);
                        }).Start();
                    }
                }
            }
        }

        public void NotifyEndCollectStar(int playerIndex)
        {
            this.sgcsState.IsGameOver = true;
            this.sgcsState.Dudes[playerIndex].Enabled = false;
            this.CurrentRoomState.CurrentGameState.Players[playerIndex].PlayingSG = string.Empty;
            log.Debug(string.Format("player {0} ended game: {1}", this.CurrentRoomState.CurrentGameState.Players[playerIndex].PlayerId, WebSocketObjects.SmallGameName_CollectStar));
            foreach (var dude in this.sgcsState.Dudes)
            {
                if (dude.Enabled == true)
                {
                    this.sgcsState.IsGameOver = false;
                    break;
                }
            }
            if (this.sgcsState.IsGameOver)
            {
                log.Debug(string.Format("game: {0} fully ended", WebSocketObjects.SmallGameName_CollectStar));
            }

            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList(), "NotifyEndCollectStar", new List<object>() { this.sgcsState });
            IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList(), "NotifyEndCollectStar", new List<object>() { this.sgcsState });
            UpdateGameState();
        }

        // returns: true - updated successfully, false - someone else is already playing
        public bool UpdateGobang(string playerID, WebSocketObjects.SGGBState state)
        {
            lock (this)
            {
                bool isOngoing = this.IsSmallGameOngoing();
                if (isOngoing && state.GameAction == "create")
                {
                    return false;
                }

                this.sggbState = state;
                switch (state.GameAction)
                {
                    case "create":
                        this.sggbState.GameStage = "created";
                        this.sggbState.ChessBoard = new int[CommonMethods.gobangBoardSize, CommonMethods.gobangBoardSize];
                        TractorHost.log.Debug(string.Format("player {0} attempted to start game: {1}", playerID, WebSocketObjects.SmallGameName_Gobang));
                        break;
                    case "restart":
                        this.sggbState.GameStage = "restarted";
                        this.sggbState.ChessBoard = new int[CommonMethods.gobangBoardSize, CommonMethods.gobangBoardSize];
                        TractorHost.log.Debug(string.Format("player {0} attempted to restart game: {1}", playerID, WebSocketObjects.SmallGameName_Gobang));
                        break;
                    case "join":
                        this.sggbState.GameStage = "joined";
                        this.sggbState.PlayerIdMoving = this.sggbState.PlayerId1;
                        break;
                    case "move":
                        this.sggbState.GameStage = "moved";
                        int moveColor = this.sggbState.PlayerIdMoved == this.sggbState.PlayerId1 ? 1 : 2;
                        this.sggbState.ChessBoard[this.sggbState.CurMove[0], this.sggbState.CurMove[1]] = moveColor;
                        log.Debug(string.Format("small game {0} player {1} made a move: [{2},{3}] with color {4}", WebSocketObjects.SmallGameName_Gobang, playerID, this.sggbState.CurMove[0], this.sggbState.CurMove[1], moveColor));
                        this.CheckGBWinner(moveColor);
                        if (this.sggbState.GameStage == "over" && !string.IsNullOrEmpty(this.sggbState.PlayerIdWinner))
                        {
                            string theOther = this.sggbState.PlayerIdWinner == this.sggbState.PlayerId1 ? this.sggbState.PlayerId2 : this.sggbState.PlayerId1;
                            TractorHost.log.Debug(string.Format("small game {0} completed, player {1} with color {2} win and {3} lose", WebSocketObjects.SmallGameName_Gobang, this.sggbState.PlayerIdWinner, moveColor, theOther));
                        }
                        break;
                    case "quit":
                        // no broadcast
                        // there is at least one real player and no change to them
                        if ((!string.IsNullOrEmpty(this.sggbState.PlayerId1) || !string.IsNullOrEmpty(this.sggbState.PlayerId2)) &&
                            (playerID != this.sggbState.PlayerId1 && playerID != this.sggbState.PlayerId2))
                        {
                            var tempState = new WebSocketObjects.SGGBState();
                            tempState.GameStage = "over";
                            IPlayerInvokeForAll(PlayersProxy, new List<string>() { playerID }, "NotifyUpdateGobang", new List<object>() { tempState });
                            return true;
                        }
                        // yes broadcast
                        // real players decreased from two to one, restart the game
                        if ((!string.IsNullOrEmpty(this.sggbState.PlayerId1) && !string.IsNullOrEmpty(this.sggbState.PlayerId2)) &&
                            (playerID == this.sggbState.PlayerId1 || playerID == this.sggbState.PlayerId2))
                        {
                            // one of the real players quit and the other one is still remaining, promote the other as the player1
                            string remainingPlayer = playerID == this.sggbState.PlayerId1 ? this.sggbState.PlayerId2 : this.sggbState.PlayerId1;
                            this.sggbState = new WebSocketObjects.SGGBState();
                            this.sggbState.GameStage = "restarted";
                            this.sggbState.PlayerId1 = remainingPlayer;
                            this.sggbState.PlayerId2 = "";
                        }
                        else
                        {
                            // no real players (a game just finished and all real players will be set to empty)
                            // or real players decreased from one to zero
                            // end the game altogether
                            this.sggbState = new WebSocketObjects.SGGBState();
                            this.sggbState.GameStage = "over";
                        }
                        break;
                    default:
                        break;
                }

                IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList(), "NotifyUpdateGobang", new List<object>() { this.sggbState });
                IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList(), "NotifyUpdateGobang", new List<object>() { this.sggbState });
                return true;
            }
        }

        private void CheckGBWinner(int moveColor)
        {
            // horizontal
            // left
            int count = 1;
            int tempX = this.sggbState.CurMove[0];
            int tempY = this.sggbState.CurMove[1];
            while (tempY - 1 >= 0 && this.sggbState.ChessBoard[tempX, tempY - 1] == moveColor)
            {
                count++;
                tempY--;
            }
            // right
            tempX = this.sggbState.CurMove[0];
            tempY = this.sggbState.CurMove[1];
            while (tempY + 1 < CommonMethods.gobangBoardSize && this.sggbState.ChessBoard[tempX, tempY + 1] == moveColor)
            {
                count++;
                tempY++;
            }
            if (count >= CommonMethods.gobangWinCount)
            {
                this.sggbState.GameStage = "over";
                this.sggbState.PlayerIdWinner = this.sggbState.PlayerIdMoved;
                return;
            }

            // vertical
            // up
            count = 1;
            tempX = this.sggbState.CurMove[0];
            tempY = this.sggbState.CurMove[1];
            while (tempX - 1 >= 0 && this.sggbState.ChessBoard[tempX - 1, tempY] == moveColor)
            {
                count++;
                tempX--;
            }
            // down
            tempX = this.sggbState.CurMove[0];
            tempY = this.sggbState.CurMove[1];
            while (tempX + 1 < CommonMethods.gobangBoardSize && this.sggbState.ChessBoard[tempX + 1, tempY] == moveColor)
            {
                count++;
                tempX++;
            }
            if (count >= CommonMethods.gobangWinCount)
            {
                this.sggbState.GameStage = "over";
                this.sggbState.PlayerIdWinner = this.sggbState.PlayerIdMoved;
                return;
            }

            // diagnal - forward slash
            // up-right
            count = 1;
            tempX = this.sggbState.CurMove[0];
            tempY = this.sggbState.CurMove[1];
            while (tempX - 1 >= 0 && tempY + 1 < CommonMethods.gobangBoardSize && this.sggbState.ChessBoard[tempX - 1, tempY + 1] == moveColor)
            {
                count++;
                tempX--;
                tempY++;
            }
            // down-left
            tempX = this.sggbState.CurMove[0];
            tempY = this.sggbState.CurMove[1];
            while (tempX + 1 < CommonMethods.gobangBoardSize && tempY - 1 >= 0 && this.sggbState.ChessBoard[tempX + 1, tempY - 1] == moveColor)
            {
                count++;
                tempX++;
                tempY--;
            }
            if (count >= CommonMethods.gobangWinCount)
            {
                this.sggbState.GameStage = "over";
                this.sggbState.PlayerIdWinner = this.sggbState.PlayerIdMoved;
                return;
            }

            // diagnal - backward slash
            // up-left
            count = 1;
            tempX = this.sggbState.CurMove[0];
            tempY = this.sggbState.CurMove[1];
            while (tempX - 1 >= 0 && tempY - 1 >= 0 && this.sggbState.ChessBoard[tempX - 1, tempY - 1] == moveColor)
            {
                count++;
                tempX--;
                tempY--;
            }
            // down-right
            tempX = this.sggbState.CurMove[0];
            tempY = this.sggbState.CurMove[1];
            while (tempX + 1 < CommonMethods.gobangBoardSize && tempY + 1 < CommonMethods.gobangBoardSize && this.sggbState.ChessBoard[tempX + 1, tempY + 1] == moveColor)
            {
                count++;
                tempX++;
                tempY++;
            }
            if (count >= CommonMethods.gobangWinCount)
            {
                this.sggbState.GameStage = "over";
                this.sggbState.PlayerIdWinner = this.sggbState.PlayerIdMoved;
                return;
            }
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
                p.IsQiangliang = false;
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

            UpdatePlayersCurrentHandState();

            UpdateGameState();

            //所有人展示手牌
            ShowAllHandCards();

            //清除手牌信息
            foreach (var entry in CurrentRoomState.CurrentHandState.PlayerHoldingCards)
            {
                entry.Value.Clear();
            }
            CurrentRoomState.CurrentHandState.LeftCardsCount = 0;
            SaveGameStateToFile();


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
            //玩家埋底倒计时-完成
            PublishStartTimerByPlayer(0, CurrentRoomState.CurrentHandState.Last8Holder);

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
            if (trump == CurrentRoomState.CurrentHandState.Trump) //没有玩家反
            {
                //log trump and rank before a new game starts
                log.Debug(string.Format("starting a new game: starter {0} {1} {2}", CurrentRoomState.CurrentHandState.Starter, CurrentRoomState.CurrentHandState.Trump.ToString(), CommonMethods.GetNumberString(CurrentRoomState.CurrentHandState.Rank)));

                CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.Playing;
                UpdatePlayersCurrentHandState();
                BeginNewTrick(CurrentRoomState.CurrentHandState.Starter);

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
        public void PlayerMakeTrump(Duan.Xiugang.Tractor.Objects.TrumpExposingPoker trumpExposingPoker, Duan.Xiugang.Tractor.Objects.Suit trump, string trumpMaker, int usedShengbi)
        {
            lock (CurrentRoomState.CurrentHandState)
            {
                //too late
                if (CurrentRoomState.CurrentHandState.CurrentHandStep >= HandStep.DistributingLast8Cards)
                {
                    log.Debug(string.Format("player attempted to make trump but too late: {0} {1} {2} {3}", trumpMaker, trumpExposingPoker.ToString(), trump.ToString(), CommonMethods.GetNumberString(CurrentRoomState.CurrentHandState.Rank)));
                    return;
                }
                //invalid user;
                if (PlayersProxy[trumpMaker] == null)
                    return;
                if (trumpExposingPoker > CurrentRoomState.CurrentHandState.TrumpExposingPoker)
                {
                    string trumpChangeType = "亮主";
                    if (trumpMaker == CurrentRoomState.CurrentHandState.TrumpMaker)
                    {
                        trumpChangeType = "加固亮主";
                    }
                    else if (!string.IsNullOrEmpty(CurrentRoomState.CurrentHandState.TrumpMaker))
                    {
                        trumpChangeType = "反主";
                    }
                    CurrentRoomState.CurrentHandState.TrumpExposingPoker = trumpExposingPoker;
                    CurrentRoomState.CurrentHandState.TrumpMaker = trumpMaker;
                    CurrentRoomState.CurrentHandState.Trump = trump;
                    if (usedShengbi == 1) tractorHost.UsedShengbi(trumpMaker, CommonMethods.usedShengbiType_Qiangliangka);

                    //log who made what trump
                    log.Debug(string.Format("player made trump: {0} {1} {2} {3}", trumpMaker, trumpExposingPoker.ToString(), trump.ToString(), CommonMethods.GetNumberString(CurrentRoomState.CurrentHandState.Rank)));
                    string msg = string.Format("玩家【{0}】【{1}】：【{2}】", trumpMaker, trumpChangeType, CommonMethods.GetSuitNameByTrumpExposingPokerInChinese(trump, trumpExposingPoker));
                    this.tractorHost.PlayerSendEmojiWorker(trumpMaker, -1, -1, false, msg, true, false);

                    TrumpState tempLastTrumState = new TrumpState();
                    tempLastTrumState.TrumpExposingPoker = CurrentRoomState.CurrentHandState.TrumpExposingPoker;
                    tempLastTrumState.TrumpMaker = CurrentRoomState.CurrentHandState.TrumpMaker;
                    tempLastTrumState.Trump = CurrentRoomState.CurrentHandState.Trump;
                    tempLastTrumState.IsNoTrumpMaker = CurrentRoomState.CurrentHandState.IsNoTrumpMaker;
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

            //玩家出牌倒计时-完成
            PublishStartTimerByPlayer(0, lastestPlayer);

            CurrentRoomState.CurrentTrickState.ShowedCards = CommonMethods.SetShowedCardsByPlayerID(CurrentRoomState.CurrentTrickState.ShowedCards, lastestPlayer, CommonMethods.GetShowedCardsByPlayerID(currentTrickState.ShowedCards, lastestPlayer));
            StringBuilder logMsg = new StringBuilder();
            logMsg.Append("Player " + lastestPlayer + " showed cards: ");
            foreach (var card in CommonMethods.GetShowedCardsByPlayerID(CurrentRoomState.CurrentTrickState.ShowedCards,lastestPlayer))
            {
                logMsg.Append(string.Format("{0} {1} ", CommonMethods.GetSuitString(card), CommonMethods.GetNumberString(card)));
            }
            log.Debug(logMsg.ToString());
            //更新每个用户手中的牌在SERVER
            foreach (int card in CommonMethods.GetShowedCardsByPlayerID(CurrentRoomState.CurrentTrickState.ShowedCards,lastestPlayer))
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
                serverLocalCache.lastShowedCards = CommonMethods.DeepClone<List<ShowedCardKeyValue>>(CurrentRoomState.CurrentTrickState.ShowedCards);
                serverLocalCache.lastLeader = CurrentRoomState.CurrentTrickState.Learder;

                UpdatePlayerCurrentTrickState();

                CurrentRoomState.CurrentHandState.LeftCardsCount -= CommonMethods.GetShowedCardsByPlayerID(currentTrickState.ShowedCards, lastestPlayer).Count;

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
                    int scoreLast8Cards = CurrentRoomState.CurrentHandState.ScoreLast8CardsBase * CurrentRoomState.CurrentHandState.ScoreLast8CardsMultiplier;
                    PublishStartTimer(2);
                    Thread.Sleep(2000 + 1000);

                    CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.Ending;
                    UpdatePlayersCurrentHandState();

                    if (!TractorHost.gameConfig.IsFullDebug)
                    {
                        foreach (PlayerEntity p in CurrentRoomState.CurrentGameState.Players)
                        {
                            if (p == null) continue;
                            p.IsReadyToStart = false;
                            p.IsRobot = false;
                            p.IsQiangliang = false;
                        }
                    }
                    CurrentRoomState.CurrentGameState.nextRestartID = GameState.START_NEXT_HAND;
                    CurrentRoomState.CurrentGameState.startNextHandStarter = CurrentRoomState.CurrentGameState.NextRank(CurrentRoomState);

                    // gather score information and log score details
                    int winPoints = CurrentRoomState.CurrentHandState.Score - scoreLast8Cards - CurrentRoomState.CurrentHandState.ScorePunishment;
                    string last8ScoreDetails = "";
                    if (CurrentRoomState.CurrentHandState.ScoreLast8CardsBase > 0)
                    {
                        last8ScoreDetails = string.Format("【{0}x{1}】",
                            CurrentRoomState.CurrentHandState.ScoreLast8CardsBase,
                        CurrentRoomState.CurrentHandState.ScoreLast8CardsMultiplier);
                    }
                    string scoreMsg = string.Format("上分：{0}，底分：{1}{2}，罚分：{3}，总分：{4}",
                        winPoints,
                        scoreLast8Cards,
                        last8ScoreDetails,
                        CurrentRoomState.CurrentHandState.ScorePunishment,
                        CurrentRoomState.CurrentHandState.Score);
                    log.Debug(scoreMsg);

                    bool isGameOver = CurrentRoomState.CurrentGameState.startNextHandStarter.Rank >= 13;
                    IssueRoundoverBonus(scoreMsg, isGameOver);

                    //检查是否本轮游戏结束
                    StringBuilder sb = null;
                    List<string> winners = new List<string>();
                    List<string> losers = new List<string>();
                    if (isGameOver)
                    {
                        sb = new StringBuilder();
                        foreach (PlayerEntity player in CurrentRoomState.CurrentGameState.Players)
                        {
                            string pid = player.PlayerId;
                            if (player == null) continue;
                            if (player.Team == CurrentRoomState.CurrentGameState.startNextHandStarter.Team)
                            {
                                sb.Append(string.Format("【{0}】", pid));
                                winners.Add(pid);
                            }
                            else
                            {
                                losers.Add(pid);
                            }
                            player.Rank = 0;
                        }
                        IssueGameoverBonus(winners, losers);
                        CurrentRoomState.CurrentHandState.Rank = 0;
                        CurrentRoomState.CurrentHandState.Starter = null;

                        CurrentRoomState.CurrentGameState.nextRestartID = GameState.RESTART_GAME;
                        CurrentRoomState.CurrentGameState.startNextHandStarter = null;
                    }
                    else
                    {
                        //推迟下盘庄家的更新，以保证结束画面仍显示当前盘的庄家，而不是下盘的庄家
                        CurrentRoomState.CurrentHandState.Starter = CurrentRoomState.CurrentGameState.startNextHandStarter.PlayerId;
                    }

                    //本局结束画面，更新最后一轮出的牌
                    CurrentRoomState.CurrentTrickState.serverLocalCache = serverLocalCache;
                    CurrentRoomState.CurrentTrickState.serverLocalCache.muteSound = true;
                    UpdatePlayerCurrentTrickState();

                    UpdateGameState();

                    UpdateReplayState();

                    //保存游戏当前状态，以备继续游戏
                    if (isGameOver)
                    {
                        CleanUpGameStateFile();
                    }
                    else
                    {
                        SaveGameStateToFile();
                    }

                    if (isGameOver)
                    {
                        PublishMessage(new string[] { sb.ToString(), "获胜！", "点击就绪重新开始游戏" });
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
                    this.shouldKeepCardsShoeFromRestore = false;
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
                //玩家出牌倒计时
                if (CurrentRoomState.roomSetting.secondsToShowCards > 0)
                {
                    string nextPlayerID = this.CurrentRoomState.CurrentTrickState.NextPlayer();
                    PlayerEntity nextPlayer = CommonMethods.GetPlayerByID(this.CurrentRoomState.CurrentGameState.Players, nextPlayerID);
                    if (!nextPlayer.IsOffline)
                    {
                        PublishStartTimerByPlayer(CurrentRoomState.roomSetting.secondsToShowCards, nextPlayerID);
                    }
                }
            }
        }

        private void IssueRoundoverBonus(string scoreMsg, bool isGameOver)
        {
            lock (this)
            {
                bool isDefenderWin = CurrentRoomState.CurrentGameState.ArePlayersInSameTeam(this.CurrentRoomState.CurrentHandState.Starter, CurrentRoomState.CurrentGameState.startNextHandStarter.PlayerId);
                string bonusType = "";
                if (isDefenderWin)
                {
                    bonusType = "守庄成功";
                }
                else
                {
                    bonusType = "攻庄成功";
                }

                int winBonus = 0;
                StringBuilder sb = new StringBuilder();
                sb.Append(scoreMsg);

                sb.Append("，玩家");
                Dictionary<string, ClientInfoV3> clientInfoV3Dict = this.tractorHost.LoadClientInfoV3();
                int rankToAdd = -1;
                string rankToAddStr = "无人升级";
                int nextRank = -1;
                foreach (PlayerEntity player in this.CurrentRoomState.CurrentGameState.Players)
                {
                    if (player.roundWinnerBonusShengbi > 0)
                    {
                        if (rankToAdd < 0)
                        {
                            rankToAdd = player.rankToAdd;
                            nextRank = player.Rank;
                        }
                        winBonus = player.roundWinnerBonusShengbi;
                        player.roundWinnerBonusShengbi = 0;
                        clientInfoV3Dict[player.PlayerId].transactShengbi(winBonus, TractorHost.transactionLogger, player.PlayerId, bonusType);
                        sb.Append(string.Format("【{0}】", player.PlayerId));
                    }
                }
                sb.Append(bonusType);

                if (rankToAdd > 0)
                {
                    rankToAddStr = string.Format("升{0}级", rankToAdd);
                }
                sb.Append(string.Format("，获得福利：升币+{0}，{1}", winBonus, rankToAddStr));

                if (!isGameOver)
                {
                    string nextStarterStr = string.Format("下盘玩家【{0}】打{1}", CurrentRoomState.CurrentGameState.startNextHandStarter.PlayerId, CommonMethods.GetNumberString(nextRank));
                    sb.Append(string.Format("，{0}", nextStarterStr));
                }
                else
                {
                    sb.Append("，获胜");
                }

                CommonMethods.WriteObjectToFile(clientInfoV3Dict, GameRoom.LogsFolder, GameRoom.ClientinfoV3FileName);

                DaojuInfo daojuInfo = this.tractorHost.buildPlayerToShengbi(clientInfoV3Dict);
                this.tractorHost.PublishDaojuInfo(daojuInfo);
                this.tractorHost.UpdateGameHall();
                string fullMsg = sb.ToString();
                this.tractorHost.PlayerSendEmojiWorker(this.CurrentRoomState.CurrentHandState.Starter, -1, -1, false, fullMsg, true, false);
                PlayerEntity p0 = this.CurrentRoomState.CurrentGameState.Players[0];
                PlayerEntity p1 = this.CurrentRoomState.CurrentGameState.Players[1];
                if (p0 != null)
                {
                    TractorHost.log.Debug(string.Format("玩家【{0}】打【{1}】", p0.PlayerId, CommonMethods.GetNumberString(p0.Rank)));
                }
                if (p1 != null)
                {
                    TractorHost.log.Debug(string.Format("玩家【{0}】打【{1}】", p1.PlayerId, CommonMethods.GetNumberString(p1.Rank)));
                }
            }
        }

        private void IssueGameoverBonus(List<string> winners, List<string> losers)
        {
            lock (this)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("玩家");
                Dictionary<string, ClientInfoV3> clientInfoV3Dict = this.tractorHost.LoadClientInfoV3();
                foreach (string w in winners)
                {
                    clientInfoV3Dict[w].transactShengbi(CommonMethods.winnerBonusShengbi, TractorHost.transactionLogger, w, "获胜");
                    sb.Append(string.Format("【{0}】", w));
                }
                sb.Append(string.Format("获胜，获得福利：升币+{0}，", CommonMethods.winnerBonusShengbi));

                sb.Append("玩家");
                foreach (string l in losers)
                {
                    clientInfoV3Dict[l].transactShengbi(CommonMethods.loserBonusShengbi, TractorHost.transactionLogger, l, "惜败");
                    sb.Append(string.Format("【{0}】", l));
                }
                sb.Append(string.Format("惜败，获得福利：升币+{0}", CommonMethods.loserBonusShengbi));

                CommonMethods.WriteObjectToFile(clientInfoV3Dict, GameRoom.LogsFolder, GameRoom.ClientinfoV3FileName);

                DaojuInfo daojuInfo = this.tractorHost.buildPlayerToShengbi(clientInfoV3Dict);
                this.tractorHost.PublishDaojuInfo(daojuInfo);
                this.tractorHost.UpdateGameHall();
                string fullMsg = sb.ToString();
                this.tractorHost.PlayerSendEmojiWorker("", -1, -1, false, fullMsg, true, true);

                // 播放烟花
                int emojiIndex = CommonMethods.RandomNext(CommonMethods.winEmojiLength);
                this.tractorHost.PlayerSendEmojiWorker(winners[0], 5, emojiIndex, true, "", true, false);
            }
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
                    this.tractorHost.handlePlayerDisconnect(bp);
                }
                return true;
            }
            return false;
        }

        // return true if succeedful, else false
        private bool TryToRobotPlayOffline(string playerId, List<int> SelectedCards, CurrentPoker currentPoker)
        {
            ShowingCardsValidationResult showingCardsValidationResult =
                TractorRules.IsValid(CurrentRoomState.CurrentTrickState, SelectedCards, currentPoker);
            if (showingCardsValidationResult.ResultType == ShowingCardsValidationResultType.Valid)
            {
                CurrentRoomState.CurrentTrickState.ShowedCards = CommonMethods.SetShowedCardsByPlayerID(CurrentRoomState.CurrentTrickState.ShowedCards, playerId, SelectedCards);
                PlayerShowCards(CurrentRoomState.CurrentTrickState);
            }
            else
            {
                PublishMessage(new string[] { string.Format("玩家【{0}】已离线", playerId), "尝试托管代打失败", "游戏即将重开..." });
                PublishStartTimer(5);
                //加一秒缓冲时间，让客户端倒计时完成
                Thread.Sleep(5000 + 1000);

                //将离线玩家移出游戏
                this.tractorHost.handlePlayerDisconnect(playerId);
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
            CurrentRoomState.CurrentTrickState.Winner = string.Empty;
            CurrentRoomState.CurrentTrickState.ShowedCards.Clear();
            CurrentRoomState.CurrentTrickState.serverLocalCache = new ServerLocalCache();
            UpdatePlayerCurrentTrickState();
        }

        public void ValidateDumpingCards(List<int> selectedCards, string playerId)
        {
            lock (CurrentRoomState.CurrentGameState)
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
                        ShowedCardKeyValue newEntry = new ShowedCardKeyValue();
                        newEntry.PlayerID = playerId;
                        newEntry.Cards = selectedCards;
                        dumpTrick.ShowedCards.Add(newEntry);
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
                IPlayerInvokeForAll(PlayersProxy, new List<string>() { playerId }, "NotifyTryToDumpResult", new List<object>() { result });
            }
        }

        public void RefreshPlayersCurrentHandState()
        {
            UpdatePlayersCurrentHandState();
        }

        //随机组队
        public void TeamUp(bool shouldRestart)
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
            StringBuilder chatsb = new StringBuilder();
            msgs[0] = "随机组队前";
            chatsb.Append("随机组队前");
            for (int i = 0; i < 4; i++)
            {
                msgs[i + 1] = CurrentRoomState.CurrentGameState.Players[i].PlayerId;
                chatsb.Append(string.Format("【{0}】", CurrentRoomState.CurrentGameState.Players[i].PlayerId));
                oldTeams[i] = CurrentRoomState.CurrentGameState.Players[i].PlayerId;
            }

            ShuffleCurrentGameStatePlayers();
            CurrentRoomState.CurrentGameState.Players[0].Team = GameTeam.VerticalTeam;
            CurrentRoomState.CurrentGameState.Players[2].Team = GameTeam.VerticalTeam;
            CurrentRoomState.CurrentGameState.Players[1].Team = GameTeam.HorizonTeam;
            CurrentRoomState.CurrentGameState.Players[3].Team = GameTeam.HorizonTeam;
            if (shouldRestart)
            {
                ResetAndRestartGame();
                CurrentRoomState.CurrentGameState.nextRestartID = GameState.RESTART_GAME;
            }
            UpdateGameState();

            msgs[5] = "随机组队后";
            chatsb.Append("随机组队后");
            for (int i = 0; i < 4; i++)
            {
                msgs[i + 6] = CurrentRoomState.CurrentGameState.Players[i].PlayerId;
                chatsb.Append(string.Format("【{0}】", CurrentRoomState.CurrentGameState.Players[i].PlayerId));
            }
            PublishMessage(msgs);
            this.tractorHost.PlayerSendEmojiWorker(this.CurrentRoomState.roomSetting.RoomOwner, -1, -1, false, chatsb.ToString(), true, false);
            new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(500);
                this.tractorHost.UpdateGameHall();
            })).Start();
        }

        //换座
        public void SwapSeat(string playerID, int offset)
        {
            string[] playerPositions = new string[] { "", "下家", "对家", "上家" };
            List<string> msgs = new List<string>();
            msgs.Add(string.Format("玩家【{0}】尝试与【{1}】换座", playerID, playerPositions[offset]));
            bool isValid = true;
            int curSeat = -1;
            for (int i = 0; i < CurrentRoomState.CurrentGameState.Players.Count; i++)
            {
                PlayerEntity player = CurrentRoomState.CurrentGameState.Players[i];
                if (player == null || player.Team == GameTeam.None)
                {
                    isValid = false;
                    break;
                }
                if (player.PlayerId == playerID)
                {
                    curSeat = i;
                }
            }


            if (!isValid)
            {
                msgs.Add("换座失败，玩家人数不够");
                PublishMessage(msgs.ToArray());
                return;
            }

            if (curSeat < 0)
            {
                msgs.Add("换座失败，未找到当前玩家");
                PublishMessage(msgs.ToArray());
                return;
            }

            int nextSeat = (curSeat + offset) % 4;
            msgs.Add("换座前");
            this.tractorHost.PlayerSendEmojiWorker(playerID, -1, -1, false, string.Format("房主发动了技能【换座】，成功与玩家【{0}】互换了座位", CurrentRoomState.CurrentGameState.Players[nextSeat].PlayerId), true, false);
            for (int i = 0; i < 4; i++)
            {
                msgs.Add(CurrentRoomState.CurrentGameState.Players[i].PlayerId);
            }

            SwapPlayers(curSeat, nextSeat);

            CurrentRoomState.CurrentGameState.Players[0].Team = GameTeam.VerticalTeam;
            CurrentRoomState.CurrentGameState.Players[2].Team = GameTeam.VerticalTeam;
            CurrentRoomState.CurrentGameState.Players[1].Team = GameTeam.HorizonTeam;
            CurrentRoomState.CurrentGameState.Players[3].Team = GameTeam.HorizonTeam;
            ResetAndRestartGame();
            CurrentRoomState.CurrentGameState.nextRestartID = GameState.RESTART_GAME;
            UpdateGameState();

            msgs.Add("换座后");
            for (int i = 0; i < 4; i++)
            {
                msgs.Add(CurrentRoomState.CurrentGameState.Players[i].PlayerId);
            }
            PublishMessage(msgs.ToArray());
        }

        //旁观：选牌
        public void CardsReady(string playerId, ArrayList myCardIsReady)
        {
            PlayerEntity player = CurrentRoomState.CurrentGameState.Players.Single(p => p != null && p.PlayerId == playerId);
            IPlayerInvokeForAll(ObserversProxy, player.Observers.ToList<string>(), "NotifyCardsReady", new List<object>() { myCardIsReady });
        }

        //读取牌局
        public void RestoreGameStateFromFile(GameState gs, CurrentHandState hs, List<string> restoredMsg, bool arePlayersMatching)
        {
            if (arePlayersMatching && HandStep.DistributingCards <= hs.CurrentHandStep && hs.CurrentHandStep < HandStep.DiscardingLast8Cards)
            {
                this.shouldKeepCardsShoeFromRestore = true;
            }

            for (int i = 0; i < 4; i++)
            {
                CurrentRoomState.CurrentGameState.Players[i].Rank = gs.Players[i].Rank;
            }
            int lastStarterIndex = -1;
            if (hs.Starter != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (gs.Players[i] != null && gs.startNextHandStarter != null && gs.Players[i].PlayerId == gs.startNextHandStarter.PlayerId)
                    {
                        lastStarterIndex = i;
                        break;
                    }
                }
            }

            CurrentRoomState.CurrentHandState = new CurrentHandState(CurrentRoomState.CurrentGameState);
            CurrentRoomState.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);
            PlayerEntity thisStarter = null;
            if (lastStarterIndex < 0)
            {
                CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.BeforeDistributingCards;
                CurrentRoomState.CurrentGameState.nextRestartID = GameState.RESTART_GAME;
            }
            else
            {
                thisStarter = CurrentRoomState.CurrentGameState.Players[lastStarterIndex];
                CurrentRoomState.CurrentHandState.Starter = thisStarter.PlayerId;
                CurrentRoomState.CurrentHandState.Rank = thisStarter.Rank;
                CurrentRoomState.CurrentGameState.nextRestartID = GameState.START_NEXT_HAND;
                CurrentRoomState.CurrentGameState.startNextHandStarter = thisStarter;
            }

            for (int i = 0; i < 4; i++)
            {
                CurrentRoomState.CurrentGameState.Players[i].IsReadyToStart = false;
                CurrentRoomState.CurrentGameState.Players[i].IsRobot = false;
                CurrentRoomState.CurrentGameState.Players[i].IsQiangliang = false;
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
            PublishStartTimer(0);
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
            bool arePlayersMatching = false;
            for (int i = 0; i < 4; i++)
            {
                if (CurrentRoomState.CurrentGameState.Players[i].PlayerId != gs.Players[i].PlayerId)
                {
                    restoredMsg.Add("上盘牌局玩家与当前玩家不匹配");
                    restoredMsg.Add(string.Format("上盘【{0}号位】玩家为：", i + 1));
                    restoredMsg.Add(gs.Players[i].PlayerId);
                    restoredMsg.Add(string.Format("当前【{0}号位】玩家为：", i + 1));
                    restoredMsg.Add(CurrentRoomState.CurrentGameState.Players[i].PlayerId);
                    isResumable = false;
                    break;
                }
            }

            // 重置每个玩家的状态
            for (int i = 0; i < 4; i++)
            {
                PlayerEntity p = gs.Players[i];
                p.IsOffline = false;
                p.IsReadyToStart = true;
                p.IsRobot = false;
                p.IsQiangliang = false;
                p.Observers.Clear();
                HashSet<string> obs = CurrentRoomState.CurrentGameState.Players[i].Observers;
                if (obs.Count > 0)
                {
                    foreach (string ob in obs)
                    {
                        p.Observers.Add(ob);
                    }
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

            foreach (var cp in hs.PlayerHoldingCards)
            {
                if (cp.Value.Count == 0)
                {
                    restoredMsg.Add("上盘牌局已出完所有牌");
                    restoredMsg.Add("游戏将顺延至下盘牌局并继续");
                    isResumable = false;
                    arePlayersMatching = true; // 如果玩家匹配，则要考虑是否保持原有手牌
                    break;
                }
            }
            if (!isResumable)
            {
                RestoreGameStateFromFile(gs, hs, restoredMsg, arePlayersMatching);
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
                CurrentRoomState.CurrentGameState.Players[i].IsQiangliang = false;
            }

            PublishMessage(new string[] { CommonMethods.resumeGameSignal });
            Thread.Sleep(2000);

            UpdatePlayersCurrentHandState();
            UpdatePlayerCurrentTrickState();
            Thread.Sleep(2000);
            UpdateGameState();

            restoredMsg.Add("继续牌局【成功】");
            PublishMessage(restoredMsg.ToArray());

            if (CurrentRoomState.CurrentHandState.CurrentHandStep == HandStep.DiscardingLast8Cards)
            {
                //玩家埋底倒计时
                if (CurrentRoomState.roomSetting.secondsToDiscardCards > 0)
                {
                    string nextPlayerID = CurrentRoomState.CurrentHandState.Last8Holder;
                    PlayerEntity nextPlayer = CommonMethods.GetPlayerByID(this.CurrentRoomState.CurrentGameState.Players, nextPlayerID);
                    if (!nextPlayer.IsOffline)
                    {
                        PublishStartTimerByPlayer(CurrentRoomState.roomSetting.secondsToDiscardCards, nextPlayerID);
                    }
                }
            }
            else
            {
                //玩家出牌倒计时
                if (CurrentRoomState.roomSetting.secondsToShowCards > 0)
                {
                    string nextPlayerID = this.CurrentRoomState.CurrentTrickState.NextPlayer();
                    PlayerEntity nextPlayer = CommonMethods.GetPlayerByID(this.CurrentRoomState.CurrentGameState.Players, nextPlayerID);
                    if (!nextPlayer.IsOffline)
                    {
                        PublishStartTimerByPlayer(CurrentRoomState.roomSetting.secondsToShowCards, nextPlayerID);
                    }
                }
            }
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
                CurrentRoomState.CurrentGameState.Players[i].IsQiangliang = false;
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
        public void ObservePlayerById(string playerId, string observerId, bool skipUpdateGameState)
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
            if (!skipUpdateGameState) UpdateGameState();
            if (HandStep.DistributingCards <= this.CurrentRoomState.CurrentHandState.CurrentHandStep &&
                this.CurrentRoomState.CurrentHandState.CurrentHandStep <= HandStep.Playing &&
                !CurrentRoomState.CurrentTrickState.IsStarted() &&
                serverLocalCache.lastShowedCards.Count > 0)
            {
                CurrentTrickState cts = CommonMethods.DeepClone<CurrentTrickState>(CurrentRoomState.CurrentTrickState);
                cts.ShowedCards = CommonMethods.DeepClone<List<ShowedCardKeyValue>>(serverLocalCache.lastShowedCards);
                cts.Learder = serverLocalCache.lastLeader;
                IPlayerInvokeForAll(ObserversProxy, new List<string>() { observerId }, "NotifyCurrentTrickStateByType", new List<object>() { cts, CommonMethods.NotifyStateType_ObservePlayerById });
            }
            IPlayerInvokeForAll(ObserversProxy, new List<string>() { observerId }, "NotifyCurrentHandStateByType", new List<object>() { CurrentRoomState.CurrentHandState, CommonMethods.NotifyStateType_ObservePlayerById });
        }
        #endregion

        #region Host Action
        int curRankDC;
        public void RestartGame(int curRank)
        {
            curRankDC = curRank;
            log.Debug("restart game with current set rank: " + CommonMethods.GetNumberString(curRank));

            CurrentRoomState.CurrentHandState = new CurrentHandState(CurrentRoomState.CurrentGameState);
            CurrentRoomState.CurrentHandState.Rank = curRank;
            CurrentRoomState.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);
            CurrentRoomState.CurrentHandState.IsFirstHand = true;
            UpdatePlayersCurrentHandState();

            if (DistributeCards()) return;
        }
        public void RestartGamePart2()
        {
            int curRank = curRankDC;
            var currentHandId = CurrentRoomState.CurrentHandState.Id;
            if (CurrentRoomState.CurrentHandState.Id != currentHandId)
                return;

            // reset timer everytime Trump is modified
            var oldTrump = CurrentRoomState.CurrentHandState.Trump;
            var oldTrumpExposingPoker = CurrentRoomState.CurrentHandState.TrumpExposingPoker;
            while (true)
            {
                if (CurrentRoomState.CurrentHandState.Trump == Suit.None)
                {
                    PublishMessage(new string[] { "如果无人亮主", "则重新发牌！" });
                }
                PublishStartTimer(5);
                //加一秒缓冲时间，让客户端倒计时完成
                Thread.Sleep(5000 + 1000);
                if (!IsAllOnline())
                {
                    return;
                }
                if (CurrentRoomState.CurrentHandState.Trump == oldTrump && CurrentRoomState.CurrentHandState.TrumpExposingPoker == oldTrumpExposingPoker)
                {
                    break;
                }
                oldTrump = CurrentRoomState.CurrentHandState.Trump;
                oldTrumpExposingPoker = CurrentRoomState.CurrentHandState.TrumpExposingPoker;
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
            UpdateGameState();
            CurrentRoomState.CurrentHandState = new CurrentHandState(CurrentRoomState.CurrentGameState);
            CurrentRoomState.CurrentHandState.Starter = nextStarter.PlayerId;
            CurrentRoomState.CurrentHandState.Rank = nextStarter.Rank;
            CurrentRoomState.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);

            log.Debug("start next hand, starter: " + CurrentRoomState.CurrentHandState.Starter + " Rank: " + CommonMethods.GetNumberString(CurrentRoomState.CurrentHandState.Rank));

            UpdatePlayersCurrentHandState();

            if (DistributeCards()) return;
        }

        public void StartNextHandPart2()
        {
            var currentHandId = CurrentRoomState.CurrentHandState.Id;
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
                if (!IsAllOnline())
                {
                    return;
                }
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
                    CurrentRoomState.CurrentHandState.IsNoTrumpMaker = true;
                }
                DistributeLast8Cards();
            }
        }

        //发牌
        Dictionary<string, StringBuilder> LogList;
        string starterID;
        int cardDistributedCount;
        string[] playersFromStarter;
        int cardNumberofEachPlayer;
        // returns: needRestart
        public bool DistributeCards()
        {
            CurrentRoomState.CurrentHandState.CurrentHandStep = HandStep.DistributingCards;
            SaveGameStateToFile();
            UpdatePlayersCurrentHandState();

            for (int i = 0; i < 4; i++)
            {
                if (CurrentRoomState.CurrentGameState.Players[i] == null)
                {
                    //玩家退出，停止发牌
                    return true;
                }
            }

            //保证庄家首先摸牌
            starterID = CurrentRoomState.CurrentGameState.Players[0].PlayerId;
            if (!string.IsNullOrEmpty(CurrentRoomState.CurrentHandState.Starter)) starterID = CurrentRoomState.CurrentHandState.Starter;
            playersFromStarter = new string[4];
            playersFromStarter[0] = starterID;
            for (int x = 1; x < 4; x++)
            {
                playersFromStarter[x] = CurrentRoomState.CurrentGameState.GetNextPlayerAfterThePlayer(playersFromStarter[x - 1]).PlayerId;
            }

            if (this.shouldKeepCardsShoeFromRestore)
            {
                this.shouldKeepCardsShoeFromRestore = false;
                StartDistributingCards();
            }
            else
            {
                ShuffleCardsWithRNGCsp(this.CardsShoe);
                //this.CardsShoe.TestSet2();

                //切牌
                IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList(), "NotifyMessage", new List<object>() { new string[] { "等待玩家切牌：", playersFromStarter[3] } });
                if (ObserversProxy.Count > 0)
                {
                    IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList(), "NotifyMessage", new List<object>() { new string[] { "等待玩家切牌：", playersFromStarter[3] } });
                }

                PlayersProxy[playersFromStarter[3]].CutCardShoeCards();
                PublishStartTimerByPlayer(5, playersFromStarter[3]);
            }
            return false;
        }

        public void PlayerHasCutCards(string cutInfo)
        {
            PublishStartTimerByPlayer(0, playersFromStarter[3]);
            string[] cutInfos;
            if (string.IsNullOrEmpty(cutInfo))
            {
                cutInfos = new string[] { "取消", "0" };
            }
            else
            {
                cutInfos = cutInfo.Split(',');
            }
            CutCards(this.CardsShoe, Int32.Parse(cutInfos[1]));

            string cutMsg = cutInfos[0];
            if (cutInfos[0] == "随机" || cutInfos[0] == "手动")
            {
                cutMsg = string.Format("{0}{1}张", cutInfos[0], cutInfos[1]);
            }

            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList(), "NotifyMessage", new List<object>() { new string[] { string.Format("玩家切牌：{0}", cutMsg), playersFromStarter[3] } });
            if (ObserversProxy.Count > 0)
            {
                IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList(), "NotifyMessage", new List<object>() { new string[] { string.Format("玩家切牌：{0}", cutMsg), playersFromStarter[3] } });
            }
            PublishStartTimer(2);
            //加一秒缓冲时间，让客户端倒计时完成
            Thread.Sleep(2000 + 1000);
            if (!IsAllOnline() || this.isGameRestarted) return;

            if (string.IsNullOrEmpty(CurrentRoomState.CurrentHandState.Starter))
            {
                TractorHost.log.Debug(string.Format("新游戏即将开始，玩家【{0}】【{1}】对阵【{2}】【{3}】",
                    this.CurrentRoomState.CurrentGameState.Players[0].PlayerId,
                    this.CurrentRoomState.CurrentGameState.Players[2].PlayerId,
                    this.CurrentRoomState.CurrentGameState.Players[1].PlayerId,
                    this.CurrentRoomState.CurrentGameState.Players[3].PlayerId));
                //新游戏开始前给出提示信息，开始倒计时，并播放提示音，告诉玩家要抢庄
                PublishMessage(new string[] { "新游戏即将开始", "做好准备抢庄！" });
                PublishStartTimer(5);
                //加一秒缓冲时间，让客户端倒计时完成
                Thread.Sleep(5000 + 1000);
                if (!IsAllOnline()) return;
            }

            StartDistributingCards();
        }

        private void StartDistributingCards()
        {
            // Setting up Timer for distributing cards
            cardNumberofEachPlayer = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);
            cardDistributedCount = 0;
            LogList = new Dictionary<string, StringBuilder>();
            foreach (var player in PlayersProxy)
            {
                LogList[player.Key] = new StringBuilder();
            }

            this.DistributeNextCard();
        }

        private void DistributeNextCard()
        {
            new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(500);
                this.DistributeNextCardWorker();
            })).Start();
        }

        private void DistributeNextCardWorker()
        {
            int totalToDis = cardNumberofEachPlayer * 4;
            if (cardDistributedCount >= totalToDis || !IsAllOnline())
            {
                if (cardDistributedCount == totalToDis && IsAllOnline()) this.DistributeCardsPart2();
                return;
            }

            for (int i = 0; i < playersFromStarter.Length; i++)
            {
                string playerID = playersFromStarter[i];
                if (!PlayersProxy.ContainsKey(playerID))
                {
                    return;
                }
                if (!string.IsNullOrEmpty(IPlayerInvoke(playerID, PlayersProxy[playerID], "GetDistributedCard", new List<object>() { CardsShoe.Cards[cardDistributedCount] }, true)))
                {
                    return;
                }
                //旁观：发牌
                PlayerEntity pe = CurrentRoomState.CurrentGameState.Players.Single(p => p != null && p.PlayerId == playerID);
                if (IPlayerInvokeForAll(ObserversProxy, pe.Observers.ToList<string>(), "GetDistributedCard", new List<object>() { CardsShoe.Cards[cardDistributedCount] }))
                {
                    return;
                }
                LogList[playerID].Append(CardsShoe.Cards[cardDistributedCount].ToString() + ", ");

                CurrentRoomState.CurrentHandState.PlayerHoldingCards[playerID].AddCard(CardsShoe.Cards[cardDistributedCount]);
                cardDistributedCount++;
            }
            this.DistributeNextCard();
        }

        public void DistributeCardsPart2()
        {
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
            if (CurrentRoomState.CurrentGameState.nextRestartID == GameState.RESTART_GAME) this.RestartGamePart2();
            else this.StartNextHandPart2();
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
                    if (!string.IsNullOrEmpty(IPlayerInvoke(CurrentRoomState.CurrentHandState.Last8Holder, last8Holder, "GetDistributedCard", new List<object>() { card }, true))) return true;
                    //旁观：发牌
                    PlayerEntity pe = CurrentRoomState.CurrentGameState.Players.Single(p => p != null && p.PlayerId == CurrentRoomState.CurrentHandState.Last8Holder);
                    if (IPlayerInvokeForAll(ObserversProxy, pe.Observers.ToList<string>(), "GetDistributedCard", new List<object>() { card })) return true;
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

            //玩家埋底倒计时
            if (CurrentRoomState.roomSetting.secondsToDiscardCards > 0)
            {
                string nextPlayerID = CurrentRoomState.CurrentHandState.Last8Holder;
                PlayerEntity nextPlayer = CommonMethods.GetPlayerByID(this.CurrentRoomState.CurrentGameState.Players, nextPlayerID);

                PublishStartTimerByPlayer(CurrentRoomState.roomSetting.secondsToDiscardCards, nextPlayerID);
            }

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
            //玩家出牌倒计时
            if (CurrentRoomState.roomSetting.secondsToShowCards > 0)
            {
                string nextPlayerID = this.CurrentRoomState.CurrentTrickState.NextPlayer();
                PlayerEntity nextPlayer = CommonMethods.GetPlayerByID(this.CurrentRoomState.CurrentGameState.Players, nextPlayerID);
                if (!nextPlayer.IsOffline)
                {
                    PublishStartTimerByPlayer(CurrentRoomState.roomSetting.secondsToShowCards, nextPlayerID);
                }
            }
        }

        //计算被扣底牌的分数，然后加到CurrentHandState.Score
        public void CalculatePointsFromDiscarded8Cards()
        {
            if (!CurrentRoomState.CurrentTrickState.AllPlayedShowedCards())
                return;
            if (CurrentRoomState.CurrentHandState.LeftCardsCount > 0)
                return;

            var cards = CommonMethods.GetShowedCardsByPlayerID(CurrentRoomState.CurrentTrickState.ShowedCards, CurrentRoomState.CurrentTrickState.Learder);
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

        //本轮游戏结束，清理牌局
        private void CleanUpGameStateFile()
        {
            List<string> filesToDelete = new List<string>();
            filesToDelete.Add(string.Format("{0}\\{1}", this.LogsByRoomFullFolder, this.BackupGamestateFileName));
            filesToDelete.Add(string.Format("{0}\\{1}", this.LogsByRoomFullFolder, this.BackupHandStateFileName));
            filesToDelete.Add(string.Format("{0}\\{1}", this.LogsByRoomFullFolder, this.BackupTrickStateFileName));
            foreach (string fileName in filesToDelete)
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
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

        public void PublishEmoji(string playerID, int emojiType, int emojiIndex, bool isCenter, string msgString, bool noSpeaker)
        {
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "NotifyEmoji", new List<object>() { playerID, emojiType, emojiIndex, isCenter, msgString, noSpeaker });
            IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList<string>(), "NotifyEmoji", new List<object>() { playerID, emojiType, emojiIndex, isCenter, msgString, noSpeaker });
        }

        public void PublishStartTimer(int timerLength)
        {
            this.PublishStartTimerByPlayer(timerLength, string.Empty);
        }

        public void PublishStartTimerByPlayer(int timerLength, string playerID)
        {
            IPlayerInvokeForAll(PlayersProxy, PlayersProxy.Keys.ToList<string>(), "NotifyStartTimer", new List<object>() { timerLength, playerID });
            IPlayerInvokeForAll(ObserversProxy, ObserversProxy.Keys.ToList<string>(), "NotifyStartTimer", new List<object>() { timerLength, playerID });
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
                if (!PlayersProxyToCall.ContainsKey(playerID)) continue;
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
            catch (Exception ex)
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
                    this.tractorHost.handlePlayerDisconnect(bp);
                }
            }
            return needsRestart;
        }

        private void ResetAndRestartGame()
        {
            log.Debug("restart game");
            this.isGameRestarted = true;
            CleanupCaches();
            CurrentRoomState.CurrentHandState = new CurrentHandState(CurrentRoomState.CurrentGameState);
            CurrentRoomState.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(CurrentRoomState.CurrentGameState.Players.Count);
            CurrentRoomState.CurrentHandState.IsFirstHand = true;
            foreach (var p in CurrentRoomState.CurrentGameState.Players)
            {
                if (p == null) continue;
                p.Rank = 0;
                p.IsReadyToStart = false;
                p.IsRobot = false;
                p.IsQiangliang = false;
            }
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
            cardShoe.ShuffleKnuth();
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

            this.tractorHost.PlayerSendEmojiWorker(this.CurrentRoomState.roomSetting.RoomOwner, -1, -1, false, "房主修改了房间设置", true, false);
        }
    }
}
