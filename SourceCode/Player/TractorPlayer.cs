using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Duan.Xiugang.Tractor.Objects;
using System.Collections;
using System.Threading;


namespace Duan.Xiugang.Tractor.Player
{
    public delegate void GameHallUpdatedEventHandler(List<RoomState> roomStates, List<string> names);
    public delegate void RoomSettingUpdatedEventHandler(RoomSetting roomSetting, bool showMessage);
    public delegate void ShowAllHandCardsEventHandler();
    public delegate void NewPlayerJoinedEventHandler(bool meJoined);
    public delegate void ReplayStateReceivedEventHandler(ReplayEntity replayState);
    public delegate void NewPlayerReadyToStartEventHandler(bool readyToStart, bool anyBecomesReady);
    public delegate void PlayerToggleIsRobotEventHandler(bool isRobot);
    public delegate void PlayersTeamMadeEventHandler();
    public delegate void GameStartedEventHandler();
    public delegate void HostIsOnlineEventHandler(bool success);

    public delegate void GetCardEventHandler(int cardNumber);    
    public delegate void TrumpChangedEventHandler(CurrentHandState currentHandState);
    public delegate void DistributingCardsFinishedEventHandler();
    public delegate void ReenterOrResumeEventHandler();    
    public delegate void StarterFailedForTrumpEventHandler();
    public delegate void StarterChangedEventHandler();
    public delegate void NotifyMessageEventHandler(string[] msg);
    public delegate void NotifyStartTimerEventHandler(int timerLength);
    public delegate void NotifyCardsReadyEventHandler(ArrayList myCardIsReady);
    public delegate void ObservePlayerByIDEventHandler();
    public delegate void CutCardShoeCardsEventHandler();
    public delegate void SpecialEndGameShouldAgreeEventHandler();
    public delegate void NotifyEmojiEventHandler(string playerID, int emojiType, int emojiIndex, bool isCenter, string msgString);
    public delegate void NotifyTryToDumpResultEventHandler(ShowingCardsValidationResult result);

    public delegate void DistributingLast8CardsEventHandler();
    public delegate void DiscardingLast8EventHandler();
    public delegate void Last8DiscardedEventHandler();    
    
    public delegate void ShowingCardBeganEventHandler();
    public delegate void CurrentTrickStateUpdateEventHandler();
    public delegate void DumpingFailEventHandler(ShowingCardsValidationResult result);
    public delegate void TrickFinishedEventHandler();
    public delegate void TrickStartedEventHandler();

    public delegate void HandEndingEventHandler();
    public delegate void SpecialEndingEventHandler();
    
    
    
    

    public class TractorPlayer : IPlayer
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public int Rank { get; set; }
        public bool isObserver { get; set; }
        public bool isObserverChanged { get; set; }
        public bool isReplay { get; set; }
        public ReplayEntity replayEntity { get; set; }
        public Stack<CurrentTrickState> replayedTricks { get; set; }
        public int replayAngle { get; set; }
        public string MyOwnId { get; set; }
        public bool IsTryingReenter { get; set; }
        public bool IsTryingResumeGame { get; set; }

        public GameState CurrentGameState;
        public CurrentPoker CurrentPoker;
        public CurrentHandState CurrentHandState { get; set; }
        public CurrentTrickState CurrentTrickState { get; set; }
        public PlayerLocalCache playerLocalCache { get; set; }
        public bool ShowLastTrickCards;
        public RoomSetting CurrentRoomSetting;

        public event GameHallUpdatedEventHandler GameHallUpdatedEvent;
        public event RoomSettingUpdatedEventHandler RoomSettingUpdatedEvent;
        public event ShowAllHandCardsEventHandler ShowAllHandCardsEvent;        
        public event NewPlayerJoinedEventHandler NewPlayerJoined;
        public event ReplayStateReceivedEventHandler ReplayStateReceived;        
        public event NewPlayerReadyToStartEventHandler NewPlayerReadyToStart;
        public event PlayerToggleIsRobotEventHandler PlayerToggleIsRobot;
        public event PlayersTeamMadeEventHandler PlayersTeamMade;
        public event GameStartedEventHandler GameOnStarted;
        public event HostIsOnlineEventHandler HostIsOnline;
        
        public event GetCardEventHandler PlayerOnGetCard;        
        public event TrumpChangedEventHandler TrumpChanged;
        public event DistributingCardsFinishedEventHandler AllCardsGot;
        public event ReenterOrResumeEventHandler ReenterOrResumeEvent;        
        public event StarterFailedForTrumpEventHandler StarterFailedForTrump; //亮不起
        public event StarterChangedEventHandler StarterChangedEvent; //庄家确定
        public event NotifyMessageEventHandler NotifyMessageEvent; //广播消息
        public event NotifyStartTimerEventHandler NotifyStartTimerEvent; //广播倒计时
        public event NotifyCardsReadyEventHandler NotifyCardsReadyEvent; //旁观：选牌
        public event ObservePlayerByIDEventHandler ObservePlayerByIDEvent; //旁观：重新画手牌
        public event CutCardShoeCardsEventHandler CutCardShoeCardsEvent; //旁观：选牌
        public event SpecialEndGameShouldAgreeEventHandler SpecialEndGameShouldAgreeEvent; //旁观：选牌
        public event NotifyEmojiEventHandler NotifyEmojiEvent; //表情包
        public event NotifyTryToDumpResultEventHandler NotifyTryToDumpResultEvent;

        public event DistributingLast8CardsEventHandler DistributingLast8Cards;
        public event DiscardingLast8EventHandler DiscardingLast8;
        public event Last8DiscardedEventHandler Last8Discarded;

        public event ShowingCardBeganEventHandler ShowingCardBegan;
        public event CurrentTrickStateUpdateEventHandler PlayerShowedCards;
        public event DumpingFailEventHandler DumpingFail; //甩牌失败
        public event TrickFinishedEventHandler TrickFinished;
        public event TrickStartedEventHandler TrickStarted;
        
        public event HandEndingEventHandler HandEnding;
        public event SpecialEndingEventHandler SpecialEndingEvent;

        private readonly ITractorHost _tractorHost;

        public TractorPlayer()
        {
            CurrentPoker = new CurrentPoker();
            PlayerId = Guid.NewGuid().ToString();
            CurrentGameState = new GameState();
            CurrentHandState = new CurrentHandState(CurrentGameState);
            CurrentTrickState = new CurrentTrickState();
            playerLocalCache = new PlayerLocalCache();

            var instanceContext = new InstanceContext(this);
            var channelFactory = new DuplexChannelFactory<ITractorHost>(instanceContext, "NetTcpBinding_ITractorHost");
            _tractorHost = channelFactory.CreateChannel();



        }

        //player get card 
        public void GetDistributedCard(int number)
        {
            this.CurrentPoker.AddCard(number);
            if (CurrentHandState.CurrentHandStep != HandStep.DistributingLast8Cards && PlayerOnGetCard != null)
                PlayerOnGetCard(number);

            if (this.CurrentPoker.Count == TractorRules.GetCardNumberofEachPlayer(this.CurrentGameState.Players.Count) && this.PlayerId != this.CurrentHandState.Last8Holder)
            {
                if (AllCardsGot != null)
                    AllCardsGot();
            }
            else if (this.CurrentPoker.Count == TractorRules.GetCardNumberofEachPlayer(this.CurrentGameState.Players.Count) + 8)
            {
                if (AllCardsGot != null)
                    AllCardsGot();
            }

        }

        public void ClearAllCards()
        {
            this.CurrentPoker.Clear();
        }

        public void ResumeGameFromFile(string playerId)
        {
            _tractorHost.ResumeGameFromFile(playerId);
        }

        public void SetBeginRank(string playerId, string beginRankString)
        {
            _tractorHost.SetBeginRank(playerId, beginRankString);
        }

        public void SaveRoomSetting(string playerId, RoomSetting roomSetting)
        {
            _tractorHost.SaveRoomSetting(playerId, roomSetting);
        }

        public void TeamUp(string playerId)
        {
            _tractorHost.TeamUp(playerId);
        }

        public void SwapSeat(string playerId, int offset)
        {
            _tractorHost.SwapSeat(playerId, offset);
        }

        public void CardsReady(string playerId, ArrayList myCardIsReady)
        {
            _tractorHost.CardsReady(playerId, myCardIsReady);
        }

        public void ObservePlayerById(string playerId, string observerId)
        {
            _tractorHost.ObservePlayerById(playerId, observerId);
        }

        public void PlayerEnterHall(string playerID)
        {
            _tractorHost.PlayerEnterHall(playerID);
        }

        public void PlayerEnterRoom(string playerID, int roomID, int posID)
        {
            _tractorHost.PlayerEnterRoom(playerID, roomID, posID);
        }

        public void ReadyToStart()
        {
            _tractorHost.PlayerIsReadyToStart(this.PlayerId);
        }

        public void ToggleIsRobot()
        {
            _tractorHost.PlayerToggleIsRobot(this.PlayerId);
        }

        public void SendEmoji(int emojiType, int emojiIndex, bool isCenter)
        {
            _tractorHost.PlayerSendEmoji(this.PlayerId, emojiType, emojiIndex, isCenter);
        }

        public void ExitRoom(string playerID)
        {
            _tractorHost.PlayerExitRoom(playerID);
        }

        public void MarkPlayerOffline(string playerID)
        {
            _tractorHost.MarkPlayerOffline(playerID);
        }

        public void SpecialEndGameRequest(string playerID)
        {
            _tractorHost.SpecialEndGameRequest(playerID);
        }

        public void SpecialEndGame(string playerID, SpecialEndingType endType)
        {
            _tractorHost.SpecialEndGame(playerID, endType);
        }

        public void SpecialEndGameDeclined(string playerID)
        {
            _tractorHost.SpecialEndGameDeclined(playerID);
        }

        public void Quit()
        {
            _tractorHost.BeginPlayerQuit(this.MyOwnId, PlayerQuitCallback, _tractorHost);
        }

        public void PlayerQuitCallback(IAsyncResult ar)
        {
            //string result = _tractorHost.EndPlayerQuit(ar);
            //Console.WriteLine("Result: {0}", result);
        }

        public void PingHost()
        {
            _tractorHost.BeginPingHost(this.MyOwnId, PingHostCallback, _tractorHost);
        }

        public void PingHostCallback(IAsyncResult ar)
        {
            bool success = false;
            try
            {
                string result = _tractorHost.EndPingHost(ar);
                success = !string.IsNullOrEmpty(result);
                Thread.Sleep(1000);
            }
            catch (Exception) { }
            if (HostIsOnline != null)
                HostIsOnline(success);
        }

        public void ShowCards(string playerId, List<int> cards)
        {
            if (this.CurrentTrickState.NextPlayer() == PlayerId)
            {
                this.CurrentTrickState.ShowedCards[PlayerId] = cards;

                _tractorHost.PlayerShowCards(playerId, this.CurrentTrickState);
                
            }
        }

        public void StartGame()
        {
            ClearAllCards();
            this.CurrentPoker.Rank = CurrentHandState.Rank;

            if (GameOnStarted != null)
                GameOnStarted();
        }

        public void NotifyMessage(string[] msg)
        {
            if (msg != null && msg.Length > 0 && NotifyMessageEvent != null)
                NotifyMessageEvent(msg);
        }

        public void NotifyEmoji(string playerID, int emojiType, int emojiIndex, bool isCenter, string msgString, bool noSpeaker)
        {
            if (NotifyEmojiEvent != null)
                NotifyEmojiEvent(playerID, emojiType, emojiIndex, isCenter, msgString);
        }

        public void NotifyStartTimer(int timerLength)
        {
            if (NotifyStartTimerEvent != null)
                NotifyStartTimerEvent(timerLength);
        }

        public void ExposeTrump(TrumpExposingPoker trumpExposingPoker, Suit trump)
        {
            _tractorHost.PlayerMakeTrump(trumpExposingPoker, trump, this.PlayerId);
            
        }

        public void NotifyCurrentHandState(CurrentHandState currentHandState)
        {
            bool trumpChanged = false;
            bool newHandStep = false;
            bool starterChanged = false;
            trumpChanged = this.CurrentHandState.Trump != currentHandState.Trump || (this.CurrentHandState.Trump == currentHandState.Trump && this.CurrentHandState.TrumpExposingPoker < currentHandState.TrumpExposingPoker);
            newHandStep = this.CurrentHandState.CurrentHandStep != currentHandState.CurrentHandStep;
            starterChanged = this.CurrentHandState.Starter  != currentHandState.Starter;

            this.CurrentHandState = currentHandState;

            //断线重连后重画手牌
            if (this.IsTryingReenter || this.IsTryingResumeGame)
            {
                this.CurrentPoker = (CurrentPoker)this.CurrentHandState.PlayerHoldingCards[this.MyOwnId].Clone();
                this.CurrentPoker.Rank = this.CurrentHandState.Rank;
                this.CurrentPoker.Trump = this.CurrentHandState.Trump;
                if (AllCardsGot != null)
                    AllCardsGot();

                return;
            }

            this.CurrentPoker.Trump = this.CurrentHandState.Trump;

            if (trumpChanged)
            {
                if (TrumpChanged != null)
                    TrumpChanged(currentHandState);

                //resort cards
                if (currentHandState.CurrentHandStep > HandStep.DistributingCards)
                {
                    if (AllCardsGot != null)
                        AllCardsGot();
                }
            }

            if (currentHandState.CurrentHandStep == HandStep.BeforeDistributingCards)
            {
                StartGame();
            }
            else if (newHandStep)
            {
                if (currentHandState.CurrentHandStep == HandStep.DistributingCardsFinished)
                {
                    if (AllCardsGot != null)
                        AllCardsGot();
                }

                else if (currentHandState.CurrentHandStep == HandStep.DistributingLast8Cards)
                {
                    if (DistributingLast8Cards != null)
                        DistributingLast8Cards();
                }

                else if (currentHandState.CurrentHandStep == HandStep.DiscardingLast8Cards)
                {
                    if (DiscardingLast8 != null)
                        DiscardingLast8();
                }
                else if (currentHandState.CurrentHandStep == HandStep.DiscardingLast8CardsFinished)
                {
                    if (Last8Discarded != null)
                        Last8Discarded();
                }
                //player begin to showing card
                //开始出牌
                else if (currentHandState.CurrentHandStep == HandStep.Playing)
                {
                    if (AllCardsGot != null)
                        AllCardsGot();

                    if (ShowingCardBegan != null)
                        ShowingCardBegan();
                }

                else if (currentHandState.CurrentHandStep == HandStep.Ending)
                {
                    if (HandEnding != null)
                        HandEnding();
                }
                else if (currentHandState.CurrentHandStep == HandStep.SpecialEnding)
                {
                    if (SpecialEndingEvent != null)
                        SpecialEndingEvent();
                }
            }

            //显示庄家
            if (starterChanged || this.CurrentHandState.CurrentHandStep == HandStep.Ending || currentHandState.CurrentHandStep == HandStep.SpecialEnding)
            {
                if (StarterChangedEvent != null)
                    StarterChangedEvent();
            }

            //摸完牌，庄家亮不起主，所以换庄家
            if (currentHandState.CurrentHandStep == HandStep.DistributingCardsFinished && starterChanged)
            {
                this.CurrentPoker.Rank = this.CurrentHandState.Rank;
                if (StarterFailedForTrump != null)
                {
                    StarterFailedForTrump();
                }
            }

            if (this.isObserver &&
                this.CurrentHandState.PlayerHoldingCards != null &&
                this.CurrentHandState.PlayerHoldingCards.ContainsKey(this.PlayerId) &&
                this.CurrentHandState.PlayerHoldingCards[this.PlayerId] != null)
            {
                //即时更新旁观手牌
                this.CurrentPoker = this.CurrentHandState.PlayerHoldingCards[this.PlayerId];
                ObservePlayerByIDEvent();
            }
        }

        public void NotifyCurrentTrickState(CurrentTrickState currentTrickState)
        {
            this.CurrentTrickState = currentTrickState;
            if (this.IsTryingReenter || this.IsTryingResumeGame) return;

            if (this.CurrentHandState.CurrentHandStep == HandStep.Ending || this.CurrentHandState.CurrentHandStep == HandStep.SpecialEnding)
            {
                return;
            }

            if (this.CurrentTrickState.LatestPlayerShowedCard() != "")
            {
                if (PlayerShowedCards != null)
                    PlayerShowedCards();
            }

            if (!string.IsNullOrEmpty(this.CurrentTrickState.Winner))
            {
                if (TrickFinished != null)
                    TrickFinished();
            }

            if (!this.CurrentTrickState.IsStarted())
            {
                if (TrickStarted != null)
                    TrickStarted();
            }

        }

        public void NotifyGameState(GameState gameState)
        {
            //bug修复：如果所有人都就绪了，然后来自服务器的新消息就绪人数既不是0又不是4（由于网络延迟导致有一人未就绪的来自服务器的消息滞后到达），那么不处理这条消息
            bool isCurrentAllReady = this.CurrentGameState.Players.Where(p => p != null && p.IsReadyToStart).Count() >= 4;
            int newReadyCount = gameState.Players.Where(p => p != null && p.IsReadyToStart).Count();
            if (isCurrentAllReady && 0 < newReadyCount && newReadyCount < 4)
            {
                return;
            }

            bool teamMade = false;
            bool playerChanged = false;
            bool ObserverChanged = false;
            foreach (PlayerEntity p in gameState.Players)
            {
                if (p != null && p.Observers.Contains(this.MyOwnId))
                {
                    if (this.PlayerId != p.PlayerId)
                    {
                        this.isObserver = true;
                        this.isObserverChanged = true;
                        this.PlayerId = p.PlayerId;
                    }
                    break;
                }
            }
            int totalPlayers = 0;
            for (int i = 0; i < 4; i++)
            {
                playerChanged = playerChanged || !(this.CurrentGameState.Players[i] != null && gameState.Players[i] != null && this.CurrentGameState.Players[i].PlayerId == gameState.Players[i].PlayerId);
                ObserverChanged = ObserverChanged || !(this.CurrentGameState.Players[i] != null && gameState.Players[i] != null && this.CurrentGameState.Players[i].Observers.SetEquals(gameState.Players[i].Observers));

                if (gameState.Players[i] != null && gameState.Players[i].Team != GameTeam.None &&
                    (this.CurrentGameState.Players[i] == null || this.CurrentGameState.Players[i].PlayerId != gameState.Players[i].PlayerId || this.CurrentGameState.Players[i].Team != gameState.Players[i].Team))
                {
                    teamMade = true;
                }
                if (gameState.Players[i] != null)
                {
                    totalPlayers++;
                }
            }
            bool meJoined = !this.CurrentGameState.Players.Exists(p => p != null && p.PlayerId == this.MyOwnId) && gameState.Players.Exists(p => p != null && p.PlayerId == this.MyOwnId);
            var anyBecomesReady = CommonMethods.SomeoneBecomesReady(this.CurrentGameState.Players, gameState.Players);

            this.CurrentGameState = gameState;

            foreach (PlayerEntity p in gameState.Players)
            {
                if (p == null) continue;
                if (p.PlayerId == this.PlayerId)
                {
                    if (NewPlayerReadyToStart != null)
                    {
                        NewPlayerReadyToStart(p.IsReadyToStart, anyBecomesReady);
                    }
                    if (PlayerToggleIsRobot != null)
                    {
                        PlayerToggleIsRobot(p.IsRobot);
                    }
                    break;
                }
            }

            if (teamMade || ObserverChanged && totalPlayers == 4)
            {
                if (PlayersTeamMade != null)
                {
                    PlayersTeamMade();
                }
            }

            if ((playerChanged || ObserverChanged) && NewPlayerJoined != null)
            {
                NewPlayerJoined(meJoined);
            }

            if (this.IsTryingReenter || this.IsTryingResumeGame)
            {
                ReenterOrResumeEvent();
                this.IsTryingReenter = false;
                this.IsTryingResumeGame = false;
            }
        }

        public void NotifyReplayState(ReplayEntity replayState)
        {

            if (ReplayStateReceived != null)
            {
                ReplayStateReceived(replayState);
            }
        }

        public void NotifyGameHall(List<RoomState> roomStates, List<string> names)
        {
            if (GameHallUpdatedEvent != null)
            {
                GameHallUpdatedEvent(roomStates, names);
            }
        }

        public void NotifyRoomSetting(RoomSetting roomSetting, bool showMessage)
        {
            if (RoomSettingUpdatedEvent != null)
            {
                RoomSettingUpdatedEvent(roomSetting, showMessage);
            }
        }

        public void NotifyShowAllHandCards()
        {
            if (ShowAllHandCardsEvent != null)
            {
                ShowAllHandCardsEvent();
            }
        }

        public void NotifyCardsReady(ArrayList myCardIsReady)
        {
            if (NotifyCardsReadyEvent != null)
            {
                NotifyCardsReadyEvent(myCardIsReady);
            }
        }

        public void CutCardShoeCards()
        {
            if (CutCardShoeCardsEvent != null)
            {
                 CutCardShoeCardsEvent();
            }
        }

        public void PlayerHasCutCards(string playerID, string cutInfo)
        {
            _tractorHost.PlayerHasCutCards(playerID, cutInfo);
        }

        public void SpecialEndGameShouldAgree()
        {
            if (SpecialEndGameShouldAgreeEvent != null)
            {
                SpecialEndGameShouldAgreeEvent();
            }
        }

        public void ValidateDumpingCards(string playerId, List<int> selectedCards)
        {
            _tractorHost.ValidateDumpingCards(selectedCards, this.PlayerId);
        }

        public void NotifyTryToDumpResult(ShowingCardsValidationResult result)
        {
            if (result.ResultType != ShowingCardsValidationResultType.DumpingSuccess)
            {
                _tractorHost.RefreshPlayersCurrentHandState(this.MyOwnId);
            }
            if (NotifyTryToDumpResultEvent != null)
            {
                NotifyTryToDumpResultEvent(result);
            }
        }


        public void NotifyDumpingValidationResult(ShowingCardsValidationResult result)
        {
            if (result.ResultType == ShowingCardsValidationResultType.DumpingFail)
            {
                if (DumpingFail != null)
                    DumpingFail(result);
            }
        }

        public void DiscardCards(string playerId, int[] cards)
        {
            if (this.CurrentHandState.Last8Holder != PlayerId)
                return;

            _tractorHost.StoreDiscardedCards(playerId, cards);
            
        }

        //我是否可以亮主
        public List<Suit> AvailableTrumps()
        {
            List<Suit> availableTrumps = new List<Suit>();
            int rank = this.CurrentHandState.Rank;

            if (this.CurrentHandState.CurrentHandStep >= HandStep.DistributingLast8Cards)
            {
                availableTrumps.Clear();
                return availableTrumps;
            }

            //当前是自己亮的单张主，只能加固
            if (this.CurrentHandState.TrumpMaker == this.PlayerId)
            {
                if (this.CurrentHandState.TrumpExposingPoker == TrumpExposingPoker.SingleRank)
                {
                    if (rank != 53)
                    {
                        if (this.CurrentPoker.Clubs[rank] > 1)
                        {
                            if (this.CurrentHandState.Trump == Suit.Club)
                                availableTrumps.Add(Suit.Club);
                        }
                        if (this.CurrentPoker.Diamonds[rank] > 1)
                        {
                            if (this.CurrentHandState.Trump == Suit.Diamond)
                                availableTrumps.Add(Suit.Diamond);
                        }
                        if (this.CurrentPoker.Spades[rank] > 1)
                        {
                            if (this.CurrentHandState.Trump == Suit.Spade)
                                availableTrumps.Add(Suit.Spade);
                        }
                        if (this.CurrentPoker.Hearts[rank] > 1)
                        {
                            if (this.CurrentHandState.Trump == Suit.Heart)
                                availableTrumps.Add(Suit.Heart);
                        }
                    }
                }
                return availableTrumps;
            }

            //如果目前无人亮主
            if (this.CurrentHandState.TrumpExposingPoker == TrumpExposingPoker.None)
            {
                if (rank != 53)
                {
                    if (this.CurrentPoker.Clubs[rank] > 0)
                    {
                        availableTrumps.Add(Suit.Club);
                    }
                    if (this.CurrentPoker.Diamonds[rank] > 0)
                    {
                        availableTrumps.Add(Suit.Diamond);
                    }
                    if (this.CurrentPoker.Spades[rank] > 0)
                    {
                        availableTrumps.Add(Suit.Spade);
                    }
                    if (this.CurrentPoker.Hearts[rank] > 0)
                    {
                        availableTrumps.Add(Suit.Heart);
                    }
                }
            }
            //亮了单张
            else if (this.CurrentHandState.TrumpExposingPoker == TrumpExposingPoker.SingleRank)
            {

                if (rank != 53)
                {
                    if (this.CurrentPoker.Clubs[rank] > 1)
                    {
                        availableTrumps.Add(Suit.Club);
                    }
                    if (this.CurrentPoker.Diamonds[rank] > 1)
                    {
                        availableTrumps.Add(Suit.Diamond);
                    }
                    if (this.CurrentPoker.Spades[rank] > 1)
                    {
                        availableTrumps.Add(Suit.Spade);
                    }
                    if (this.CurrentPoker.Hearts[rank] > 1)
                    {
                        availableTrumps.Add(Suit.Heart);
                    }
                }
            }

            if (this.CurrentHandState.TrumpExposingPoker != TrumpExposingPoker.PairRedJoker)
            {
                if (rank != 53)
                {
                    if (this.CurrentPoker.BlackJoker == 2)
                    {
                        availableTrumps.Add(Suit.Joker);
                    }
                }
            }


            if (rank != 53)
            {
                if (this.CurrentPoker.RedJoker == 2)
                {
                    availableTrumps.Add(Suit.Joker);
                }
            }
            return availableTrumps;
        }

        public void NotifySgcsPlayerUpdated(string content) { }
        public void NotifyCreateCollectStar(WebSocketObjects.SGCSState state) { }
        public void NotifyEndCollectStar(WebSocketObjects.SGCSState state) { }
        public void NotifyGrabStar(int playerIndex, int starIndex) { }
        public void NotifyOnlinePlayerList(string playerID, bool isJoining) { }
        public void NotifyGameRoomPlayerList(string playerID, bool isJoining, string roomName) { }
        public void Close() { }
    }
}
