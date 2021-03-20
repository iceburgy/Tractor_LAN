using System;

namespace Duan.Xiugang.Tractor.Objects
{
    public class CardsShoe
    {
        public int[] Cards = null;
        private int _deckNumber;

        public CardsShoe()
        {
            _deckNumber = 2;
            Cards = new int[54*DeckNumber];
            FillNewCards();
        }

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

        public void TestSet()
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

        private void Swap(int i, int r)
        {
            int temp = Cards[r];
            Cards[r] = Cards[i];
            Cards[i] = temp;
        }
    }
}