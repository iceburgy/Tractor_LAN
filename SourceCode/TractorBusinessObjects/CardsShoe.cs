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

        internal void TestSet()
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

        private void Swap(int i, int r)
        {
            int temp = Cards[r];
            Cards[r] = Cards[i];
            Cards[i] = temp;
        }
    }
}