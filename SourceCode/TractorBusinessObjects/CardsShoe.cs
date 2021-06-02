using System;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    public class CardsShoe
    {
        [DataMember]
        public int[] Cards = null;
        private int _deckNumber;

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
        }

        //Knuth shuffle
        public void KnuthShuffle()
        {
            Random rand = new Random();
            int N = Cards.Length;
            for (int i = 0; i < N; i++)
            {
                int r = rand.Next(i + 1);
                Swap(i, r);
            }
        }

        //Better
        public void Shuffle()
        {
            Random rand = new Random();
            int N = Cards.Length;
            for (int i = 0; i < N; i++)
            {
                int r = rand.Next(i, N);
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

        private void Swap(int i, int r)
        {
            int temp = Cards[r];
            Cards[r] = Cards[i];
            Cards[i] = temp;
        }
    }
}