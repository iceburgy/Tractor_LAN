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
            }
            //对子
            var currentPairs = currentCards.GetPairs((int)leadingSuit);
            for (int i = 0; i < leadingPairs.Count && i < currentPairs.Count && selectedCards.Count < leadingCardsCp.Count; i++)
            {
                if (selectedCards.Contains((int)currentPairs[i])) continue;
                selectedCards.Add((int)currentPairs[i]);
                selectedCards.Add((int)currentPairs[i]);
                allSuitCards[(int)currentPairs[i]] -= 2;
            }
            //单张先跳过对子
            var currentSuitCards = currentCards.GetSuitCardsWithJokerAndRank((int)leadingSuit);
            for (int i = 0; i < currentSuitCards.Length && selectedCards.Count < leadingCardsCp.Count; i++)
            {
                if (currentPairs.Contains(currentSuitCards[i]) || allSuitCards[(int)currentSuitCards[i]] <= 0) continue;
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
            //其他花色的牌先跳过对子
            for (int i = 0; i < allSuitCards.Length && selectedCards.Count < leadingCardsCp.Count; i++)
            {
                if (allSuitCards[i] <= 0 || allSuitCards[i] == 2) continue;
                selectedCards.Add(i);
                allSuitCards[i]--;
            }
            //其他花色的牌
            for (int i = 0; i < allSuitCards.Length && selectedCards.Count < leadingCardsCp.Count; i++)
            {
                while (allSuitCards[i] > 0 && selectedCards.Count < leadingCardsCp.Count)
                {
                    selectedCards.Add(i);
                    allSuitCards[i]--;
                }
            }
        }

        //先手
        public static void ShouldSelectedCards(List<int> selectedCards, CurrentTrickState currentTrickState, CurrentPoker currentPoker)
        {
            var currentCards = (CurrentPoker)currentPoker.Clone();
            var allSuitCardsCp = (CurrentPoker)currentPoker.Clone();
            var allSuitCards = allSuitCardsCp.Cards;

            //先出A
            foreach (Suit st in Enum.GetValues(typeof(Suit)))
            {
                if (st == Suit.None || st == Suit.Joker || st == currentCards.Trump) continue;
                int maxCards = currentCards.GetMaxCards((int)st);
                if (maxCards % 13 == 12 && allSuitCards[maxCards] == 1) 
                {
                    selectedCards.Add(maxCards);
                    return;
                }
                //dumping causing concurrency issue, TODO: use timer tick
                //if (maxCards % 13 == 12)
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
            if (minMaster>=0)
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

            //副牌里，先挑出好牌来，最好不埋
            //挑出A及以下的牌
            foreach (Suit st in Enum.GetValues(typeof(Suit)))
            {
                if (st == Suit.None || st == Suit.Joker || st == currentCards.Trump) continue;
                int maxCards = currentCards.GetMaxCards((int)st);
                if (maxCards % 13 == 12)
                {
                    while (maxCards % 13 > 0 && allSuitCards[maxCards] == 2 || maxCards == currentCards.Rank)
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
            var masterCards=badCardsCp.GetSuitCardsWithJokerAndRank((int)currentCards.Trump);
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
        }

        public static Suit TryExposingTrump(List<Suit> availableTrump, CurrentPoker currentPoker)
        {
            var currentCards = (CurrentPoker)currentPoker.Clone();
            foreach (Suit st in availableTrump)
            {
                switch (st)
                {
                    case Suit.Heart:
                        if (currentCards.HeartsNoRankTotal >= exposeTrumpThreshold) return st;
                        break;
                    case Suit.Spade:
                        if (currentCards.SpadesNoRankCount >= exposeTrumpThreshold) return st;
                        break;
                    case Suit.Diamond:
                        if (currentCards.DiamondsNoRankTotal >= exposeTrumpThreshold) return st;
                        break;
                    case Suit.Club:
                        if (currentCards.ClubsNoRankTotal >= exposeTrumpThreshold) return st;
                        break;
                    default:
                        break;
                }
            }
            return Suit.None;
        }
    }
}
