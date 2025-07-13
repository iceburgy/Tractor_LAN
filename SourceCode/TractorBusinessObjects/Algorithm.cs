using Duan.Xiugang.Tractor.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Duan.Xiugang.Tractor.Objects
{
    public class Algorithm
    {
        private const int exposeTrumpThreshold = 5;
        private const int exposeTrumpJokerThreshold = 4;
        //跟出
        public static void MustSelectedCards(List<int> selectedCards, CurrentTrickState currentTrickState, CurrentPoker currentPoker)
        {
            var currentCards = (CurrentPoker)currentPoker.Clone();
            var leadingCardsCp = new CurrentPoker();
            leadingCardsCp.TrumpInt = (int)currentTrickState.Trump;
            leadingCardsCp.Rank = currentTrickState.Rank;
            foreach (int card in currentTrickState.LeadingCards)
            {
                leadingCardsCp.AddCard(card);
            }

            Suit leadingSuit = currentTrickState.LeadingSuit;
            bool isTrump = PokerHelper.IsTrump(currentTrickState.LeadingCards[0], currentCards.Trump, currentCards.Rank);
            if (isTrump) leadingSuit = currentCards.Trump;


            selectedCards.Clear();
            var allSuitCardsCp = (CurrentPoker)currentPoker.Clone();
            var allSuitCards = allSuitCardsCp.Cards;

            List<int> leadingTractors = leadingCardsCp.GetTractor(leadingSuit);
            var leadingPairs = leadingCardsCp.GetPairs((int)leadingSuit);

            //如果别人出拖拉机，我如果有，也应该出拖拉机
            List<int> currentTractors = currentCards.GetTractor(leadingSuit);
            for (int i = 0; i < leadingTractors.Count && i < currentTractors.Count && selectedCards.Count < leadingCardsCp.Count; i++)
            {
                selectedCards.Add(currentTractors[i]);
                selectedCards.Add(currentTractors[i]);
                allSuitCards[currentTractors[i]] -= 2;
                leadingPairs.Remove(currentTractors[i]);
                leadingTractors.Remove(currentTractors[i]);
            }
            //对子：先跳过常主
            var currentPairs = currentCards.GetPairs((int)leadingSuit);
            for (int i = 0; i < leadingPairs.Count && i < currentPairs.Count && selectedCards.Count < leadingCardsCp.Count; i++)
            {
                if (allSuitCards[(int)currentPairs[i]] <= 0 || i % 13 == currentCards.Rank || i >= 52) continue;
                selectedCards.Add((int)currentPairs[i]);
                selectedCards.Add((int)currentPairs[i]);
                allSuitCards[(int)currentPairs[i]] -= 2;
            }
            //对子
            for (int i = 0; i < leadingPairs.Count && i < currentPairs.Count && selectedCards.Count < leadingCardsCp.Count; i++)
            {
                if (allSuitCards[(int)currentPairs[i]] <= 0) continue;
                selectedCards.Add((int)currentPairs[i]);
                selectedCards.Add((int)currentPairs[i]);
                allSuitCards[(int)currentPairs[i]] -= 2;
            }
            //单张先跳过对子、常主
            var currentSuitCards = currentCards.GetSuitCardsWithJokerAndRank((int)leadingSuit);
            for (int i = 0; i < currentSuitCards.Length && selectedCards.Count < leadingCardsCp.Count; i++)
            {
                if (allSuitCards[(int)currentSuitCards[i]] <= 0 || i % 13 == currentCards.Rank || i >= 52) continue;
                selectedCards.Add(currentSuitCards[i]);
                allSuitCards[(int)currentSuitCards[i]]--;
            }
            //单张先跳过对子
            for (int i = 0; i < currentSuitCards.Length && selectedCards.Count < leadingCardsCp.Count; i++)
            {
                if (allSuitCards[(int)currentSuitCards[i]] <= 0) continue;
                selectedCards.Add(currentSuitCards[i]);
                allSuitCards[(int)currentSuitCards[i]]--;
            }
            //单张
            for (int i = 0; i < currentSuitCards.Length && selectedCards.Count < leadingCardsCp.Count; i++)
            {
                if (allSuitCards[currentSuitCards[i]] <= 0) continue;
                selectedCards.Add(currentSuitCards[i]);
                allSuitCards[(int)currentSuitCards[i]]--;
            }
            //其他花色的牌先跳过所有主牌，和副牌对子，即副牌单张
            for (int i = 0; i < allSuitCards.Length && selectedCards.Count < leadingCardsCp.Count; i++)
            {
                bool isITrump = PokerHelper.IsTrump(i, currentCards.Trump, currentCards.Rank);
                if (isITrump || allSuitCards[i] <= 0 || allSuitCards[i] == 2) continue;
                selectedCards.Add(i);
                allSuitCards[i]--;
            }
            //其他花色的牌跳过所有主牌
            for (int i = 0; i < allSuitCards.Length && selectedCards.Count < leadingCardsCp.Count; i++)
            {
                bool isITrump = PokerHelper.IsTrump(i, currentCards.Trump, currentCards.Rank);
                if (isITrump || allSuitCards[i] <= 0) continue;
                while (allSuitCards[i] > 0 && selectedCards.Count < leadingCardsCp.Count)
                {
                    selectedCards.Add(i);
                    allSuitCards[i]--;
                }
            }
            //被迫选主牌：先跳过对子，和常主
            for (int i = 0; i < allSuitCards.Length && selectedCards.Count < leadingCardsCp.Count; i++)
            {
                if (allSuitCards[i] <= 0 || allSuitCards[i] == 2 || i % 13 == currentCards.Rank || i >= 52) continue;
                selectedCards.Add(i);
                allSuitCards[i]--;
            }
            //被迫选主牌跳过对子
            for (int i = 0; i < allSuitCards.Length && selectedCards.Count < leadingCardsCp.Count; i++)
            {
                if (allSuitCards[i] <= 0 || allSuitCards[i] == 2) continue;
                selectedCards.Add(i);
                allSuitCards[i]--;
            }
            //被迫选主牌对子：先跳过常主
            for (int i = 0; i < allSuitCards.Length && selectedCards.Count < leadingCardsCp.Count; i++)
            {
                if (i % 13 == currentCards.Rank || i >= 52) continue;
                while (allSuitCards[i] > 0 && selectedCards.Count < leadingCardsCp.Count)
                {
                    selectedCards.Add(i);
                    allSuitCards[i]--;
                }
            }
            //被迫选主牌对子
            for (int i = 0; i < allSuitCards.Length && selectedCards.Count < leadingCardsCp.Count; i++)
            {
                while (allSuitCards[i] > 0 && selectedCards.Count < leadingCardsCp.Count)
                {
                    selectedCards.Add(i);
                    allSuitCards[i]--;
                }
            }
        }

        //跟选：在有必选牌的情况下自动选择必选牌，方便玩家快捷出牌
        public static void MustSelectedCardsNoShow(List<int> selectedCards, CurrentTrickState currentTrickState, CurrentPoker currentPoker)
        {
            var currentCards = (CurrentPoker)currentPoker.Clone();
            var leadingCardsCp = new CurrentPoker();
            leadingCardsCp.TrumpInt = (int)currentTrickState.Trump;
            leadingCardsCp.Rank = currentTrickState.Rank;
            foreach (int card in currentTrickState.LeadingCards)
            {
                leadingCardsCp.AddCard(card);
            }

            Suit leadingSuit = currentTrickState.LeadingSuit;
            bool isTrump = PokerHelper.IsTrump(currentTrickState.LeadingCards[0], currentCards.Trump, currentCards.Rank);
            if (isTrump) leadingSuit = currentCards.Trump;

            selectedCards.Clear();
            List<int> currentSuitCards = currentCards.GetSuitCardsWithJokerAndRank((int)leadingSuit).ToList<int>();

            List<int> leadingTractors = leadingCardsCp.GetTractor(leadingSuit);
            var leadingPairs = leadingCardsCp.GetPairs((int)leadingSuit);

            //如果别人出拖拉机，则选择我手中相同花色的拖拉机
            List<int> currentTractors = currentCards.GetTractor(leadingSuit);
            if (currentTractors.Count <= leadingTractors.Count)
            {
                for (int i = 0; i < leadingTractors.Count && i < currentTractors.Count && selectedCards.Count < leadingCardsCp.Count; i++)
                {
                    selectedCards.Add(currentTractors[i]);
                    selectedCards.Add(currentTractors[i]);
                    currentSuitCards.Remove(currentTractors[i]);
                    currentSuitCards.Remove(currentTractors[i]);
                    leadingPairs.Remove(currentTractors[i]);
                    leadingTractors.Remove(currentTractors[i]);
                }
            }
            //如果别人出对子，则选择我手中相同花色的对子
            var currentPairs = currentCards.GetPairs((int)leadingSuit);
            if (currentPairs.Count <= leadingPairs.Count)
            {
                for (int i = 0; i < leadingPairs.Count && i < currentPairs.Count && selectedCards.Count < leadingCardsCp.Count; i++)
                {
                    if (selectedCards.Contains((int)currentPairs[i])) continue;
                    selectedCards.Add((int)currentPairs[i]);
                    selectedCards.Add((int)currentPairs[i]);
                    currentSuitCards.Remove((int)currentPairs[i]);
                    currentSuitCards.Remove((int)currentPairs[i]);
                }
            }
            //如果别人出单张，则选择我手中相同花色的单张
            if (currentSuitCards.Count() <= leadingCardsCp.Count - selectedCards.Count)
            {
                selectedCards.AddRange(currentSuitCards);
            }
        }

        //先手
        public static void ShouldSelectedCards(List<int> selectedCards, CurrentTrickState currentTrickState, CurrentPoker currentPoker)
        {
            var currentCards = (CurrentPoker)currentPoker.Clone();
            var allSuitCardsCp = (CurrentPoker)currentPoker.Clone();
            var allSuitCards = allSuitCardsCp.Cards;
            var maxValue = currentPoker.Rank == 12 ? 11 : 12;

            //先出A
            foreach (Suit st in Enum.GetValues(typeof(Suit)))
            {
                if (st == Suit.None || st == Suit.Joker || st == currentCards.Trump) continue;
                int maxCards = currentCards.GetMaxCards((int)st);
                if (maxCards % 13 == maxValue && allSuitCards[maxCards] == 1)
                {
                    selectedCards.Add(maxCards);
                    return;
                }
                //dumping causing concurrency issue, TODO: use timer tick
                //if (maxCards % 13 == maxValue)
                //{
                //    while (maxCards % 13 > 0 && allSuitCards[maxCards] == 2 || maxCards == currentTrickState.Rank)
                //    {
                //    if(maxCards != currentTrickState.Rank){
                //        selectedCards.Add(maxCards);
                //        selectedCards.Add(maxCards);
                //    }
                //        maxCards--;
                //    }
                //    selectedCards.Add(maxCards);
                //    if (allSuitCards[maxCards] == 2) selectedCards.Add(maxCards);
                //    return;
                //}
            }
            //再出对子
            foreach (Suit st in Enum.GetValues(typeof(Suit)))
            {
                if (st == Suit.None || st == Suit.Joker || st == currentCards.Trump) continue;
                List<int> currentTractors = currentCards.GetTractor(st);
                if (currentTractors.Count > 1)
                {
                    foreach (int tr in currentTractors)
                    {
                        selectedCards.Add(tr);
                        selectedCards.Add(tr);
                    }
                    return;
                }
                var currentPairs = currentCards.GetPairs((int)st);
                if (currentPairs.Count > 0)
                {
                    selectedCards.Add((int)currentPairs[currentPairs.Count - 1]);
                    selectedCards.Add((int)currentPairs[currentPairs.Count - 1]);
                    return;
                }

            }


            List<int> masterTractors = currentCards.GetTractor(currentTrickState.Trump);
            if (masterTractors.Count > 1)
            {
                foreach (int tr in masterTractors)
                {
                    selectedCards.Add(tr);
                    selectedCards.Add(tr);
                }
                return;
            }

            var masterPair = currentCards.GetPairs((int)currentTrickState.Trump);
            if (masterPair.Count > 0)
            {
                selectedCards.Add((int)masterPair[masterPair.Count - 1]);
                selectedCards.Add((int)masterPair[masterPair.Count - 1]);
                return;
            }

            int minMaster = currentCards.GetMinMasterCards((int)currentTrickState.Trump);
            if (minMaster >= 0)
            {
                selectedCards.Add(minMaster);
                return;
            }

            //其他花色的牌
            for (int i = 0; i < allSuitCards.Length; i++)
            {
                if (allSuitCards[i] > 0)
                {
                    selectedCards.Add(i);
                    return;
                }
            }
        }

        //埋底
        public static void ShouldSelectedLast8Cards(List<int> selectedCards, CurrentPoker currentPoker)
        {
            List<int> goodCards = new List<int>();
            var currentCards = (CurrentPoker)currentPoker.Clone();
            var badCardsCp = (CurrentPoker)currentPoker.Clone();
            var allSuitCardsCp = (CurrentPoker)currentPoker.Clone();
            var allSuitCards = allSuitCardsCp.Cards;
            var maxValue = currentPoker.Rank == 12 ? 11 : 12;

            //副牌里，先挑出好牌来，最好不埋
            //挑出A及以下的牌
            foreach (Suit st in Enum.GetValues(typeof(Suit)))
            {
                if (st == Suit.None || st == Suit.Joker || st == currentCards.Trump) continue;
                int maxCards = currentCards.GetMaxCards((int)st);
                if (maxCards % 13 == maxValue)
                {
                    while (maxCards % 13 > 0 && allSuitCards[maxCards] == 2 || maxCards == currentCards.Rank) //保证打几的牌没有对子，也不会打断继续往下找大牌对子
                    {
                        if (maxCards != currentCards.Rank)
                        {
                            goodCards.Add(maxCards);
                            goodCards.Add(maxCards);
                            badCardsCp.RemoveCard(maxCards);
                            badCardsCp.RemoveCard(maxCards);
                        }
                        maxCards--;
                    }
                    goodCards.Add(maxCards);
                    badCardsCp.RemoveCard(maxCards);
                    if (allSuitCards[maxCards] == 2)
                    {
                        goodCards.Add(maxCards);
                        badCardsCp.RemoveCard(maxCards);
                    }
                }
            }
            //再挑出对子
            foreach (Suit st in Enum.GetValues(typeof(Suit)))
            {
                if (st == Suit.None || st == Suit.Joker || st == currentCards.Trump) continue;
                var currentPairs = currentCards.GetPairs((int)st);
                foreach (int pair in currentPairs)
                {
                    goodCards.Add(pair);
                    goodCards.Add(pair);
                    badCardsCp.RemoveCard(pair);
                    badCardsCp.RemoveCard(pair);
                }
            }

            //将剩余的差牌按花色排序，少的靠前
            List<int[]> badCardsBySuit = new List<int[]>();
            foreach (Suit st in Enum.GetValues(typeof(Suit)))
            {
                if (st == Suit.None || st == Suit.Joker || st == currentCards.Trump) continue;
                badCardsBySuit.Add(badCardsCp.GetSuitCardsWithJokerAndRank((int)st));
            }
            badCardsBySuit.Sort((a, b) => (a.Length - b.Length));
            var masterCards = badCardsCp.GetSuitCardsWithJokerAndRank((int)currentCards.Trump);
            badCardsBySuit.Add(masterCards);

            //从差到好选出8张牌
            foreach (int[] badCards in badCardsBySuit)
            {
                foreach (int bc in badCards)
                {
                    selectedCards.Add(bc);
                    if (selectedCards.Count == 8) return;
                }
            }

            foreach (int goodCard in goodCards)
            {
                selectedCards.Add(goodCard);
                if (selectedCards.Count == 8) return;
            }

            //如果副牌总共不到8张，那就埋主
            while (selectedCards.Count < 8)
            {
                int minMaster = currentCards.GetMinMasterCards((int)currentCards.Trump);
                if (minMaster >= 0)
                {
                    selectedCards.Add(minMaster);
                }
            }
        }

        public static Suit TryExposingTrump(List<Suit> availableTrump, CurrentPoker currentPoker, bool fullDebug)
        {
            int nonJokerMaxCount = 0;
            Suit nonJoker = Suit.None;
            Suit mayJoker = Suit.None;
            var currentCards = (CurrentPoker)currentPoker.Clone();

            foreach (Suit st in availableTrump)
            {
                switch (st)
                {
                    case Suit.Heart:
                        int heartCount = currentCards.HeartsNoRankTotal;
                        if (heartCount >= exposeTrumpThreshold && heartCount > nonJokerMaxCount)
                        {
                            nonJokerMaxCount = heartCount;
                            nonJoker = st;
                        }
                        break;
                    case Suit.Spade:
                        int spadeCount = currentCards.SpadesNoRankCount;
                        if (spadeCount >= exposeTrumpThreshold && spadeCount > nonJokerMaxCount)
                        {
                            nonJokerMaxCount = spadeCount;
                            nonJoker = st;
                        }
                        break;
                    case Suit.Diamond:
                        int diamondCount = currentCards.DiamondsNoRankTotal;
                        if (diamondCount >= exposeTrumpThreshold && diamondCount > nonJokerMaxCount)
                        {
                            nonJokerMaxCount = diamondCount;
                            nonJoker = st;
                        }
                        break;
                    case Suit.Club:
                        int clubCount = currentCards.ClubsNoRankTotal;
                        if (clubCount >= exposeTrumpThreshold && clubCount > nonJokerMaxCount)
                        {
                            nonJokerMaxCount = clubCount;
                            nonJoker = st;
                        }
                        break;
                    case Suit.Joker:
                        int jokerAndRankCount = currentCards.GetSuitCardsWithJokerAndRank((int)Suit.Joker).Length;
                        if (fullDebug && jokerAndRankCount >= exposeTrumpJokerThreshold) mayJoker = Suit.Joker;
                        break;
                    default:
                        break;
                }
            }
            if (nonJokerMaxCount > 0) return nonJoker;
            else return mayJoker;
        }
    }
}
