using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    public class CardsShoe
    {
        [DataMember]
        public int[] Cards = null;
        private int _deckNumber;
        private int shuffleRiffleFlips = 5;
        private int shuffleOverhandPasses = 5;

        public CardsShoe()
        {
            _deckNumber = 2;
            Cards = new int[54*DeckNumber];
            FillNewCards();
        }

        public bool IsCardsRestored { get; set; }

        public int DeckNumber
        {
            get { return _deckNumber; }
            set
            {
                _deckNumber = value;
                Cards = new int[54*DeckNumber];
                FillNewCards();
            }
        }


        public void FillNewCards()
        {
            for (int i = 0; i < DeckNumber; i++)
            {
                for (int j = 0; j < 54; j++)
                {
                    Cards[i*54 + j] = j;
                }
            }
            this.ShuffleFisherYales();
        }

        public void ShuffleKnuth()
        {
            int N = Cards.Length;
            for (int i = 0; i < N; i++)
            {
                int r = CommonMethods.RandomNext(i + 1);
                Swap(i, r);
            }
        }

        public void ShuffleFisherYales()
        {
            int N = Cards.Length;
            for (int i = N - 1; i >= 0; i--)
            {
                int r = CommonMethods.RandomNext(i + 1);
                Swap(i, r);
            }
        }

        public void ShuffleRiffleAndOverhand()
        {
            ShuffleRiffle();
            ShuffleOverhand();
        }

        public void ShuffleRiffle()
        {
            int N = Cards.Length;
            List<int> newList = new List<int>(Cards);

            for (int i = 0; i < shuffleRiffleFlips; i++)
            {
                //cut the deck at the middle +/- 10%
                int cutPoint = Cards.Length / 2 + (CommonMethods.RandomNext(2) == 0 ? -1 : 1) * CommonMethods.RandomNext((int)(N * 0.1));

                //split the deck
                List<int> left = new List<int>(newList.Take(cutPoint));
                List<int> right = new List<int>(newList.Skip(cutPoint));

                newList.Clear();

                while (left.Count > 0 && right.Count > 0)
                {
                    //allow for imperfect riffling so that more than one card can come form the same side in a row
                    //biased towards the side with more cards
                    //remove the if and else and brackets for perfect riffling
                    if (CommonMethods.random.NextDouble() >= ((double)left.Count / right.Count) / 2)
                    {
                        newList.Add(right.First());
                        right.RemoveAt(0);
                    }
                    else
                    {
                        newList.Add(left.First());
                        left.RemoveAt(0);
                    }
                }

                //if either hand is out of cards then flip all of the other hand to the shuffled deck
                if (left.Count > 0) newList.AddRange(left);
                if (right.Count > 0) newList.AddRange(right);
            }
            Cards = newList.ToArray();
        }

        public void ShuffleOverhand()
        {
            int N = Cards.Length;
            List<int> mainHand = new List<int>(Cards);
            for (int i = 0; i < shuffleOverhandPasses; i++)
            {
                List<int> otherHand = new List<int>();

                while (mainHand.Count > 0)
                {
                    //cut at up to 20% of the way through the deck
                    int cutSize = Math.Min(mainHand.Count, CommonMethods.RandomNext((int)(N * 0.2)) + 1);
                    //int cutPoint = CommonMethods.RandomNext(mainHand.Count - cutSize + 1);
                    int cutPoint = 0;

                    //grab the next cut up to the end of the cards left in the main hand at a random point
                    List<int> temp = mainHand.GetRange(cutPoint, cutSize);
                    mainHand.RemoveRange(cutPoint, cutSize);

                    //add them to the cards in the other hand, sometimes to the front sometimes to the back
                    if (CommonMethods.random.NextDouble() >= 0.1)
                    {
                        //front
                        temp.AddRange(otherHand);
                        otherHand = temp;
                    }
                    else
                    {
                        //end
                        otherHand.AddRange(temp);
                    }
                }

                //move the cards back to the main hand
                mainHand = otherHand;
            }
            Cards = mainHand.ToArray();
        }

        //Better
        public void Shuffle()
        {
            int N = Cards.Length;
            for (int i = 0; i < N; i++)
            {
                int r = CommonMethods.random.Next(i, N);
                Swap(i, r);
            }
        }

        public void TestSetManyExposeTrumps()
        {
            int rankDelta=2;
            Suit wantedSuit = Suit.Heart;
            int wantedRank = 2 - rankDelta;
            var wantedNumber = ((int)wantedSuit - 1) * 13 + wantedRank;
            int playerPos = 0;
            int posDelta = 4;
            Swap(playerPos, wantedNumber);

            playerPos += posDelta;
            wantedNumber = 52;
            Swap(playerPos, wantedNumber);

            playerPos += posDelta;
            wantedNumber = 52 + 54;
            Swap(playerPos, wantedNumber);

            playerPos = 1;
            wantedSuit = Suit.Spade;
            wantedRank = 2 - rankDelta;
            wantedNumber = ((int)wantedSuit - 1) * 13 + wantedRank;
            Swap(playerPos, wantedNumber);

            playerPos += posDelta;
            wantedNumber = ((int)wantedSuit - 1) * 13 + wantedRank + 54;
            Swap(playerPos, wantedNumber);

            playerPos += posDelta;
            wantedNumber = 53;
            Swap(playerPos, wantedNumber);

            playerPos += posDelta;
            wantedNumber = 53 + 54;
            Swap(playerPos, wantedNumber);
        }

        public void TestSet2()
        {
            Cards = new int[] { 23, 26, 37, 27, 44, 23, 8, 35, 12, 52, 30, 45, 4, 42, 19, 11, 51, 22, 7, 48, 27, 7, 43, 5, 24, 9, 47, 18, 1, 34, 16, 17, 25, 44, 51, 22, 9, 31, 53, 17, 46, 25, 4, 38, 41, 50, 14, 19, 5, 49, 39, 40, 34, 36, 18, 1, 36, 11, 13, 37, 52, 12, 13, 14, 41, 3, 15, 49, 43, 20, 46, 10, 53, 26, 29, 21, 33, 0, 29, 8, 6, 32, 16, 32, 10, 35, 2, 28, 31, 30, 40, 48, 15, 2, 24, 28, 21, 39, 38, 42, 20, 50, 47, 33, 45, 3, 6, 0 };
        }

        public void TestSet3TractorDumpNCards()
        {
            Cards = new int[] { 22, 17, 46, 34, 7, 17, 11, 13, 9, 34, 24, 47, 53, 32, 25, 23, 44, 5, 14, 41, 5, 27, 21, 49, 36, 19, 31, 16, 4, 18, 43, 0, 30, 38, 30, 52, 11, 19, 10, 1, 33, 49, 26, 37, 4, 42, 3, 0, 9, 40, 31, 29, 27, 2, 48, 1, 14, 12, 37, 26, 50, 24, 12, 8, 22, 53, 41, 6, 33, 48, 51, 39, 21, 28, 38, 35, 23, 8, 45, 39, 16, 43, 20, 28, 10, 52, 36, 51, 45, 20, 7, 2, 44, 35, 32, 40, 42, 18, 29, 50, 3, 46, 13, 15, 47, 6, 15, 25 };
        }

        public void TestSet4DumpWithPair()
        {
            Cards = new int[] { 5, 51, 52, 7, 9, 21, 18, 2, 12, 23, 22, 29, 53, 8, 26, 16, 9, 46, 12, 49, 25, 35, 48, 36, 39, 32, 47, 11, 53, 3, 37, 44, 0, 7, 37, 33, 48, 35, 3, 16, 10, 46, 8, 19, 27, 14, 44, 11, 43, 51, 36, 50, 47, 50, 21, 28, 4, 20, 43, 40, 26, 24, 31, 4, 6, 10, 40, 17, 15, 13, 5, 25, 19, 14, 31, 32, 17, 39, 2, 45, 29, 38, 1, 6, 1, 0, 24, 41, 15, 27, 22, 42, 41, 20, 30, 42, 45, 13, 30, 18, 38, 52, 34, 34, 23, 33, 49, 28 };
        }

        public void TestSet4NoTrump()
        {
            Cards = new int[] { 10, 4, 33, 7, 9, 48, 7, 0, 39, 36, 11, 19, 51, 31, 48, 25, 15, 1, 11, 21, 28, 0, 34, 41, 37, 43, 42, 4, 32, 46, 49, 3, 44, 35, 22, 27, 26, 28, 5, 5, 33, 47, 9, 50, 13, 12, 52, 18, 8, 53, 6, 50, 45, 44, 23, 12, 20, 8, 1, 51, 31, 53, 25, 16, 10, 18, 24, 49, 16, 47, 39, 3, 36, 40, 52, 22, 41, 34, 14, 32, 15, 24, 2, 29, 20, 17, 38, 2, 38, 45, 23, 14, 30, 40, 19, 46, 26, 30, 21, 29, 13, 17, 43, 37, 35, 27, 6, 42 };
        }

        public void TestSet4_1_NoTrump()
        {
            Cards = new int[] { 53, 4, 33, 52, 53, 48, 7, 52, 39, 36, 11, 19, 51, 31, 48, 25, 15, 1, 11, 21, 28, 0, 34, 41, 37, 43, 42, 4, 32, 46, 49, 3, 44, 35, 22, 27, 26, 28, 5, 5, 33, 47, 9, 50, 13, 12, 7, 18, 8, 10, 6, 50, 45, 44, 23, 12, 20, 8, 1, 51, 31, 9, 25, 16, 10, 18, 24, 49, 16, 47, 39, 3, 36, 40, 52, 0, 41, 34, 14, 32, 15, 24, 2, 29, 20, 17, 38, 2, 38, 45, 23, 14, 30, 40, 19, 46, 26, 30, 21, 29, 13, 17, 43, 37, 35, 27, 6, 42 };
        }

        public void TestSet5MakeTrumpTwiceJoker()
        {
            Cards = new int[] { 11, 0, 39, 52, 36, 0, 20, 13, 35, 1, 2, 16, 23, 46, 35, 4, 49, 50, 38, 48, 34, 30, 49, 6, 10, 24, 44, 44, 19, 31, 47, 18, 13, 25, 42, 45, 21, 45, 15, 22, 8, 26, 33, 52, 31, 40, 50, 51, 29, 12, 10, 7, 7, 5, 34, 37, 32, 24, 36, 4, 3, 53, 5, 29, 30, 37, 12, 41, 28, 11, 28, 40, 9, 53, 32, 6, 2, 46, 17, 9, 15, 14, 20, 8, 51, 47, 25, 48, 43, 38, 41, 1, 16, 23, 22, 42, 26, 33, 19, 27, 17, 27, 3, 14, 18, 21, 39, 43 };
        }

        public void TestSet6With4Joker()
        {
            Cards = new int[] { 53, 23, 2, 33, 30, 48, 16, 25, 27, 8, 40, 17, 41, 26, 13, 17, 52, 25, 48, 38, 19, 9, 15, 20, 41, 33, 39, 0, 35, 36, 38, 12, 52, 31, 3, 40, 18, 13, 49, 46, 4, 12, 31, 8, 53, 50, 21, 30, 21, 34, 42, 9, 37, 36, 16, 5, 43, 32, 24, 29, 32, 28, 23, 28, 46, 10, 1, 49, 1, 24, 44, 14, 43, 5, 19, 47, 35, 3, 11, 2, 22, 14, 22, 37, 44, 51, 50, 0, 34, 6, 20, 47, 29, 27, 45, 39, 10, 26, 51, 18, 6, 11, 42, 7, 15, 4, 45, 7 };
        }

        public void TestSet7DumpfailWithTractor()
        {
            Cards = new int[] { 53, 15, 14, 34, 3, 20, 41, 24, 29, 48, 30, 9, 31, 50, 27, 42, 45, 3, 39, 53, 13, 35, 22, 8, 38, 13, 36, 1, 17, 34, 5, 32, 25, 40, 12, 33, 52, 6, 47, 43, 35, 19, 1, 0, 10, 44, 10, 16, 27, 24, 52, 23, 41, 2, 43, 11, 19, 40, 6, 32, 23, 4, 38, 39, 4, 7, 46, 26, 25, 26, 42, 44, 28, 45, 22, 18, 0, 49, 17, 51, 29, 50, 47, 15, 20, 46, 16, 33, 37, 9, 5, 12, 2, 28, 30, 49, 8, 11, 7, 36, 51, 14, 21, 21, 48, 18, 37, 31 };
        }

        public void TestSet8TwoPairsNoTrump()
        {
            Cards = new int[] { 20, 39, 36, 31, 34, 45, 33, 20, 39, 35, 11, 15, 9, 26, 22, 28, 17, 17, 13, 45, 2, 37, 2, 13, 40, 5, 46, 43, 6, 40, 52, 5, 29, 18, 21, 0, 36, 44, 1, 21, 42, 12, 14, 48, 16, 38, 35, 14, 6, 4, 8, 41, 44, 0, 32, 3, 16, 43, 47, 25, 52, 15, 11, 23, 49, 31, 53, 25, 50, 10, 50, 7, 33, 27, 12, 27, 10, 23, 8, 19, 51, 1, 28, 47, 48, 19, 53, 7, 38, 30, 29, 18, 42, 4, 32, 30, 22, 41, 24, 34, 51, 9, 26, 37, 46, 3, 24, 49 };
        }

        public void TestSet9Last4IsTractor()
        {
            Cards = new int[] { 51, 22, 42, 9, 32, 53, 40, 30, 43, 14, 14, 28, 41, 49, 35, 45, 40, 45, 24, 12, 36, 25, 47, 6, 29, 16, 51, 37, 43, 37, 35, 5, 17, 46, 11, 0, 9, 3, 19, 15, 50, 29, 13, 28, 17, 22, 27, 12, 11, 4, 1, 41, 24, 20, 16, 2, 52, 50, 26, 0, 2, 10, 32, 19, 38, 34, 36, 39, 23, 18, 23, 44, 46, 5, 8, 15, 7, 48, 18, 7, 20, 26, 4, 33, 49, 44, 33, 27, 13, 25, 48, 52, 1, 21, 8, 3, 21, 39, 31, 47, 42, 31, 53, 6, 34, 30, 10, 38 };
        }

        private void Swap(int i, int r)
        {
            int temp = Cards[r];
            Cards[r] = Cards[i];
            Cards[i] = temp;
        }
    }
}