﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Duan.Xiugang.Tractor.Objects;
using System.Collections;
using System.Threading;


namespace Duan.Xiugang.Tractor.Player
{
    public delegate void GameHallUpdatedEventHandler(List<RoomState> roomStates, List<string> names);
    public delegate void RoomSettingUpdatedEventHandler(RoomSetting roomSetting, bool isRoomSettingModified);
    public delegate void ShowAllHandCardsEventHandler();
    public delegate void NewPlayerJoinedEventHandler();
    public delegate void NewPlayerReadyToStartEventHandler(bool readyToStart);
    public delegate void PlayerToggleIsRobotEventHandler(bool isRobot);
    public delegate void PlayersTeamMadeEventHandler();
    public delegate void GameStartedEventHandler();
    public delegate void HostIsOnlineEventHandler(bool success);

    public delegate void GetCardEventHandler(int cardNumber);    
    public delegate void TrumpChangedEventHandler(CurrentHandState currentHandState);
    public delegate void DistributingCardsFinishedEventHandler();
    public delegate void ReenterFromOfflineEventHandler();    
    public delegate void StarterFailedForTrumpEventHandler();
    public delegate void StarterChangedEventHandler();
    public delegate void NotifyMessageEventHandler(string[] msg);
    public delegate void NotifyStartTimerEventHandler(int timerLength);
    public delegate void NotifyCardsReadyEventHandler(ArrayList myCardIsReady);
    public delegate void ResortMyCardsEventHandler();

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
        public string MyOwnId { get; set; }
        public bool IsTryingReenter { get; set; }

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
        public event NewPlayerReadyToStartEventHandler NewPlayerReadyToStart;
        public event PlayerToggleIsRobotEventHandler PlayerToggleIsRobot;
        public event PlayersTeamMadeEventHandler PlayersTeamMade;
        public event GameStartedEventHandler GameOnStarted;
        public event HostIsOnlineEventHandler HostIsOnline;
        
        public event GetCardEventHandler PlayerOnGetCard;        
        public event TrumpChangedEventHandler TrumpChanged;
        public event DistributingCardsFinishedEventHandler AllCardsGot;
        public event ReenterFromOfflineEventHandler ReenterFromOfflineEvent;        
        public event StarterFailedForTrumpEventHandler StarterFailedForTrump; //亮不起
        public event StarterChangedEventHandler StarterChangedEvent; //庄家确定
        public event NotifyMessageEventHandler NotifyMessageEvent; //广播消息
        public event NotifyStartTimerEventHandler NotifyStartTimerEvent; //广播倒计时
        public event NotifyCardsReadyEventHandler NotifyCardsReadyEvent; //旁观：选牌
        public event ResortMyCardsEventHandler ResortMyCardsEvent; //旁观：重新画手牌
        
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
            if (PlayerOnGetCard != null)
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

        public void RestoreGameStateFromFile(string playerId, bool restoreCardsShoe)
        {
            _tractorHost.RestoreGameStateFromFile(playerId, restoreCardsShoe);
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

        public void MoveToNextPosition(string playerId)
        {
            _tractorHost.MoveToNextPosition(playerId);
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

        public void ExitRoom(string playerID)
        {
            _tractorHost.PlayerExitRoom(playerID);
        }

        public void SpecialEndGame(string playerID, SpecialEndingType endType)
        {
            _tractorHost.SpecialEndGame(playerID, endType);
        }

        public void Quit()
        {
            _tractorHost.BeginPlayerQuit(this.MyOwnId, PlayerQuitCallback, _tractorHost);
            (_tractorHost as IDisposable).Dispose();
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
            if (NotifyMessageEvent != null)
                NotifyMessageEvent(msg);
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
            if (this.IsTryingReenter)
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

                else if (currentHandState.CurrentHandStep == HandStep.DistributingCardsFinished)
                {
                    if (AllCardsGot != null)
                        AllCardsGot();
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
                ResortMyCardsEvent();
            }
        }

        public void NotifyCurrentTrickState(CurrentTrickState currentTrickState)
        {
            this.CurrentTrickState = currentTrickState;
            if (this.IsTryingReenter) return;

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
            bool teamMade = false;
            bool observerAdded = false;
            foreach (PlayerEntity p in gameState.Players)
            {
                if (p != null && p.Observers.Contains(this.MyOwnId))
                {
                    this.isObserver = true;
                    this.PlayerId = p.PlayerId;
                    break;
                }
            }
            int totalPlayers = 0;
            for (int i = 0; i < 4; i++)
            {
                if (gameState.Players[i] != null && gameState.Players[i].Team != GameTeam.None &&
                    (this.CurrentGameState.Players[i] == null || this.CurrentGameState.Players[i].PlayerId != gameState.Players[i].PlayerId || this.CurrentGameState.Players[i].Team != gameState.Players[i].Team))
                {
                    teamMade = true;
                }
                HashSet<string> oldObs = new HashSet<string>(), newObs = new HashSet<string>();
                if (gameState.Players[i] != null) oldObs = gameState.Players[i].Observers;
                if (this.CurrentGameState.Players[i] != null) newObs = this.CurrentGameState.Players[i].Observers;
                newObs.ExceptWith(oldObs);
                if (newObs.Count>0)
                {
                    observerAdded = true;
                }
                if (gameState.Players[i] != null)
                {
                    totalPlayers++;
                }
            }
            observerAdded = observerAdded && totalPlayers == 4;

            this.CurrentGameState = gameState;

            foreach (PlayerEntity p in gameState.Players)
            {
                if (p == null) continue;
                if (p.PlayerId == this.PlayerId)
                {
                    if (NewPlayerReadyToStart != null)
                    {
                        NewPlayerReadyToStart(p.IsReadyToStart);
                    }
                    if (PlayerToggleIsRobot != null)
                    {
                        PlayerToggleIsRobot(p.IsRobot);
                    }
                    break;
                }
            }

            if (teamMade || observerAdded)
            {
                if (PlayersTeamMade != null)
                {
                    PlayersTeamMade();
                }
            }

            if (NewPlayerJoined != null)
            {
                NewPlayerJoined();
            }

            if (this.IsTryingReenter)
            {
                if (ReenterFromOfflineEvent != null)
                {
                    ReenterFromOfflineEvent();
                }
                this.IsTryingReenter = false;
            }
        }

        public void NotifyGameHall(List<RoomState> roomStates, List<string> names)
        {
            if (GameHallUpdatedEvent != null)
            {
                GameHallUpdatedEvent(roomStates, names);
            }
        }

        public void NotifyRoomSetting(RoomSetting roomSetting, bool isRoomSettingModified)
        {
            if (RoomSettingUpdatedEvent != null)
            {
                RoomSettingUpdatedEvent(roomSetting, isRoomSettingModified);
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

        public ShowingCardsValidationResult ValidateDumpingCards(string playerId, List<int> selectedCards)
        {

            var result = _tractorHost.ValidateDumpingCards(selectedCards, this.PlayerId);
            if (result.ResultType != ShowingCardsValidationResultType.DumpingSuccess)
            {
                _tractorHost.RefreshPlayersCurrentHandState(playerId);
            }
            
            return result;
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
    }
}
