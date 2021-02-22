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


[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace TractorServer
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class TractorHost : ITractorHost
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        internal GameState CurrentGameState;
        internal CurrentHandState CurrentHandState;
        internal CurrentTrickState CurrentTrickState;
        public CardsShoe CardsShoe { get; set; }

        public Dictionary<string, IPlayer> PlayersProxy { get; set; }

        public TractorHost()
        {
            CurrentGameState = new GameState();
            CurrentHandState = new CurrentHandState(this.CurrentGameState);
            CardsShoe = new CardsShoe();
            PlayersProxy = new Dictionary<string, IPlayer>();
        }

        #region implement interface ITractorHost

        public void PlayerIsReady(string playerID)
        {
            if (!PlayersProxy.Keys.Contains(playerID))
            {
                IPlayer player = OperationContext.Current.GetCallbackChannel<IPlayer>();
                if (PlayersProxy.Count >= 4)
                {
                    player.NotifyMessage("房间已满");
                    return;
                }

                for (int i = 0; i < 4; i++)
                {
                    if (CurrentGameState.Players[i] == null)
                    {
                        CurrentGameState.Players[i] = new PlayerEntity { PlayerId = playerID, Rank = 0, Team = GameTeam.None };
                        break;
                    }
                }
                PlayersProxy.Add(playerID, player);
                log.Debug(string.Format("player {0} joined.", playerID));
                if (PlayersProxy.Count < 4)
                    UpdateGameState();
                if (PlayersProxy.Count == 4)
                {
                    //create team
                    CurrentGameState.Players[0].Team = GameTeam.VerticalTeam;
                    CurrentGameState.Players[2].Team = GameTeam.VerticalTeam;
                    CurrentGameState.Players[1].Team = GameTeam.HorizonTeam;
                    CurrentGameState.Players[3].Team = GameTeam.HorizonTeam;
                    log.Debug("restart game");
                    foreach (var p in CurrentGameState.Players)
                    {
                        p.Rank = 0;
                    }
                    CurrentHandState.Rank = 0;
                    UpdateGameState();
                    CurrentGameState.nextRestartID = GameState.RESTART_GAME;
                }
            }
            else
            {
                PlayersProxy[playerID].NotifyMessage("已在房间里");
                return;
            }
        }

        public void PlayerIsReadyToStart(string playerID)
        {
			foreach (PlayerEntity p in CurrentGameState.Players) {
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
				switch (CurrentGameState.nextRestartID) {
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

        //玩家推出
        public void PlayerQuit(string playerId)
        {
            log.Debug(playerId + " quit.");
            PlayersProxy.Remove(playerId);
            for (int i = 0; i < 4; i++)
            {
                if (CurrentGameState.Players[i] != null)
                {
                    CurrentGameState.Players[i].Rank = 0;
                    CurrentGameState.Players[i].IsReadyToStart = false;
                    CurrentGameState.Players[i].Team = GameTeam.None;
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
            var logMsg =  "player " + this.CurrentHandState.Last8Holder+ " discard 8 cards: ";
            foreach (var card in cards)
            {
                logMsg += card.ToString() + ", ";
            }
            log.Debug(logMsg);

            log.Debug(this.CurrentHandState.Last8Holder + "'s cards after discard 8 cards: " + this.CurrentHandState.PlayerHoldingCards[this.CurrentHandState.Last8Holder].ToString());

            UpdatePlayersCurrentHandState();

            //等待5秒，让玩家反底
            var trump = CurrentHandState.Trump;
            Thread.Sleep(5000);
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
                foreach(var card in this.CurrentTrickState.ShowedCards[lastestPlayer])
                {
                    cardsString += card.ToString() + " ";
                }
                log.Debug("Player " + lastestPlayer + " showed cards: " + cardsString);
                //更新每个用户手中的牌在SERVER
                foreach (int card in this.CurrentTrickState.ShowedCards[lastestPlayer])
                {
                    this.CurrentHandState.PlayerHoldingCards[lastestPlayer].RemoveCard(card);
                }
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


                       log.Debug("Winner: " + this.CurrentTrickState.Winner);

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
                        UpdatePlayersCurrentHandState();

                        foreach (PlayerEntity p in CurrentGameState.Players)
                        {
                            p.IsReadyToStart = false;
                        }
                        this.CurrentGameState.nextRestartID = GameState.START_NEXT_HAND;
                        this.CurrentGameState.startNextHandStarter = this.CurrentGameState.NextRank(this.CurrentHandState, this.CurrentTrickState);

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
                log.Debug("tried to dump cards and failed, punish score: " + punishScore);
                foreach (var player in PlayersProxy)
                {
                    if (player.Key != playerId)
                    {
                        player.Value.NotifyDumpingValidationResult(result);
                    }
                }
            }
            var cardString = "";
            foreach(var card in selectedCards)
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
        #endregion

        #region Host Action
        public void RestartGame(int curRank)
        {
            log.Debug("restart game with current set rank");

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
                Thread.Sleep(5000);
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
            log.Debug("restart current hand, starter: " + this.CurrentHandState.Starter + " Rank: " + this.CurrentHandState.Rank.ToString());
            StartNextHand(this.CurrentGameState.Players.Single(p => p != null && p.PlayerId == this.CurrentHandState.Starter));
        }

        public void StartNextHand(PlayerEntity nextStarter)
        {
            UpdateGameState();
            this.CurrentHandState = new CurrentHandState(this.CurrentGameState);
            this.CurrentHandState.Starter = nextStarter.PlayerId;
            this.CurrentHandState.Rank = nextStarter.Rank;
            this.CurrentHandState.LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(this.CurrentGameState.Players.Count);

            log.Debug("start next hand, starter: " + this.CurrentHandState.Starter + " Rank: " + this.CurrentHandState.Rank.ToString());

            UpdatePlayersCurrentHandState();

            var currentHandId = this.CurrentHandState.Id;

            DistributeCards();
            if (this.CurrentHandState.Id != currentHandId)
                return;

            // reset timer everytime Trump is modified
            var oldTrump = this.CurrentHandState.Trump;
            while (true)
            {
                Thread.Sleep(5000);
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
                log.Debug("starter team fail to make trump, next starter: " + this.CurrentHandState.Starter + " Rank: " + this.CurrentHandState.Rank.ToString());

                UpdatePlayersCurrentHandState();

                //10 seconds to make trump
                // reset timer everytime Trump is modified
                var oldTrump2 = this.CurrentHandState.Trump;
                while (true)
                {
                    Thread.Sleep(5000);
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
                        LogList[player.Key].Append(CardsShoe.Cards[index].ToString() + ", ");
                    }
                    else
                        return;

                    this.CurrentHandState.PlayerHoldingCards[player.Key].AddCard(CardsShoe.Cards[index]);
                }
                Thread.Sleep(500);
            }

            log.Debug("distribute cards to each player: ");
            foreach (var logItem in LogList)
            {                
                log.Debug(logItem.Key + ": " + logItem.Value.ToString());
            }

            foreach (var keyvalue in this.CurrentHandState.PlayerHoldingCards)
            {
                log.Debug(keyvalue.Key + "'s cards:  " + keyvalue.Value.ToString());
            }

            CurrentHandState.CurrentHandStep = HandStep.DistributingCardsFinished;
            UpdatePlayersCurrentHandState();
        }

        //保存牌局
        private void SaveGameStateToFile()
        {
            if (string.IsNullOrEmpty(this.CurrentHandState.Starter)) return;
            Stream stream = null;
            Stream stream2 = null;
            try
            {
                stream = new FileStream("backup_gamestate", FileMode.Create, FileAccess.Write, FileShare.None);
                DataContractSerializer ser = new DataContractSerializer(typeof(GameState));
                ser.WriteObject(stream, this.CurrentGameState);

                stream2 = new FileStream("backup_HandState", FileMode.Create, FileAccess.Write, FileShare.None);
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
                stream = new FileStream("backup_gamestate", FileMode.Open, FileAccess.Read, FileShare.Read);
                GameState gs = (GameState)ser.ReadObject(stream);

                for (int i = 0; i < 4; i++)
                {
                    this.CurrentGameState.Players[i].Rank = gs.Players[i].Rank;
                }

                DataContractSerializer ser2 = new DataContractSerializer(typeof(CurrentHandState));
                stream2 = new FileStream("backup_HandState", FileMode.Open, FileAccess.Read, FileShare.Read);
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
                    this.CurrentHandState.PlayerHoldingCards[CurrentHandState.Last8Holder].AddCard(card);
                }
            }

            var logMsg = "distribute last 8 cards to " + CurrentHandState.Last8Holder + ", cards: ";
            foreach (var card in last8Cards)
            {
                logMsg += card.ToString() + ", ";
            }
            log.Debug(logMsg);

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
        #endregion

        #region Update Client State
        public void UpdateGameState()
        {
            foreach (IPlayer player in PlayersProxy.Values)
            {
                player.NotifyGameState(this.CurrentGameState);
            }
        }

        public void UpdatePlayersCurrentHandState()
        {
            foreach (IPlayer player in PlayersProxy.Values)
            {
                player.NotifyCurrentHandState(this.CurrentHandState);
            }
        }

        public void UpdatePlayerCurrentTrickState()
        {
            foreach (IPlayer player in PlayersProxy.Values)
            {
                player.NotifyCurrentTrickState(this.CurrentTrickState);
            }
        }

        #endregion

        public void PublishMessage(string msg)
        {
            foreach (IPlayer player in PlayersProxy.Values)
            {
                player.NotifyMessage(msg);
            }
        }
                        
    }
}