using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    //一个回合牌的状态
    [DataContract]
    [Serializable]
    public class CurrentTrickState
    {
        //第一个出牌的玩家
        public CurrentTrickState()
        {
            ShowedCards = new List<ShowedCardKeyValue>();
            serverLocalCache = new ServerLocalCache();
        }

        public CurrentTrickState(List<string> playeIds, ServerLocalCache slc)
        {
            serverLocalCache = slc;
            ShowedCards = new List<ShowedCardKeyValue>();
            foreach (string playeId in playeIds)
            {
                ShowedCardKeyValue newEntry = new ShowedCardKeyValue();
                newEntry.PlayerID = playeId;
                newEntry.Cards = new List<int>();
                ShowedCards.Add(newEntry);
            }
        }

        [DataMember]
        public string Learder { get; set; }

        [DataMember]
        public string Winner { get; set; }

        //player ID and cards
        [DataMember]
        public List<ShowedCardKeyValue> ShowedCards { get; set; }

        [DataMember]
        public ServerLocalCache serverLocalCache { get; set; }

        [DataMember]
        public Suit Trump { get; set; }

        [DataMember]
        public int Rank { get; set; }

        public List<int> LeadingCards
        {
            get
            {
                if (IsStarted())
                {
                    return CommonMethods.GetShowedCardsByPlayerID(ShowedCards, Learder);
                }
                return new List<int>();
            }
        }

        public Suit LeadingSuit
        {
            get
            {
                if (IsStarted())
                {
                    if (PokerHelper.IsTrump(LeadingCards[0], Trump, Rank)) return Trump;
                    else return PokerHelper.GetSuit(LeadingCards[0]);
                }
                return Suit.None;
            }
        }

        public int Points
        {
            get
            {
                int points = 0;
                for (int i = 0; i < ShowedCards.Count; i++)
                {
                    ShowedCardKeyValue keyValue = ShowedCards[i];
                    foreach (int card in keyValue.Cards)
                    {
                        if (card % 13 == 3)
                            points += 5;
                        else if (card % 13 == 8)
                            points += 10;
                        else if (card % 13 == 11)
                            points += 10;
                    }
                }
                return points;
            }
        }

        public List<int> ScoreCards
        {
            get
            {
                List<int> scorecards = new List<int>();

                for (int i = 0; i < ShowedCards.Count; i++)
                {
                    ShowedCardKeyValue keyValue = ShowedCards[i];
                    foreach (int card in keyValue.Cards)
                    {
                        if (card % 13 == 3 || card % 13 == 8 || card % 13 == 11)
                            scorecards.Add(card);
                    }
                }
                return scorecards;
            }
        }

        /// <summary>
        ///     get the next player who should show card
        /// </summary>
        /// <returns>return "" if all player has showed cards</returns>
        public string NextPlayer()
        {
            string playerId = "";
            if (CommonMethods.GetShowedCardsByPlayerID(ShowedCards, Learder).Count == 0)
                playerId = Learder;

            else
            {
                bool afterLeader = false;
                //find next player to show card after learder
                for (int i = 0; i < ShowedCards.Count; i++)
                {
                    ShowedCardKeyValue keyValue = ShowedCards[i];
                    if (keyValue.PlayerID != Learder && afterLeader == false)
                        continue;
                    if (keyValue.PlayerID == Learder) // search from learder
                    {
                        afterLeader = true;
                    }
                    if (afterLeader)
                    {
                        if (keyValue.Cards.Count == 0)
                        {
                            playerId = keyValue.PlayerID;
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(playerId))
                {
                    for (int i = 0; i < ShowedCards.Count; i++)
                    {
                        ShowedCardKeyValue keyValue = ShowedCards[i];
                        if (keyValue.PlayerID != Learder)
                        {
                            if (keyValue.Cards.Count == 0)
                            {
                                playerId = keyValue.PlayerID;
                                break;
                            }
                        }
                        else //search end before leader;
                            break;
                    }
                }
            }

            return playerId;
        }


        /// <summary>
        ///     get the next player after a player
        /// </summary>
        /// <returns>return "" if all player has showed cards</returns>
        public string NextPlayer(string playerId)
        {
            string nextPlayer = "";
            if (CommonMethods.GetShowedCardsByPlayerID(ShowedCards, playerId).Count == 0)
                return "";


            bool afterLeader = false;
            //find next player to show card after learder
            for (int i = 0; i < ShowedCards.Count; i++)
            {
                ShowedCardKeyValue keyValue = ShowedCards[i];
                if (keyValue.PlayerID != playerId && afterLeader == false)
                    continue;
                else if (keyValue.PlayerID == playerId) // search from learder
                {
                    afterLeader = true;
                }
                else if (afterLeader)
                {
                    nextPlayer = keyValue.PlayerID;
                    break;
                }
            }


            if (string.IsNullOrEmpty(nextPlayer))
            {
                for (int i = 0; i < ShowedCards.Count; i++)
                {
                    ShowedCardKeyValue keyValue = ShowedCards[i];
                    if (keyValue.PlayerID != playerId)
                    {
                        nextPlayer = keyValue.PlayerID;
                    }
                    break;
                }
            }


            return nextPlayer;
        }

        /// <summary>
        ///     get the latest player who just showed cards
        /// </summary>
        /// <returns>return "" if no player has showed cards</returns>
        public string LatestPlayerShowedCard()
        {
            string playerId = "";
            if (string.IsNullOrEmpty(Learder) || CommonMethods.GetShowedCardsByPlayerID(ShowedCards, Learder).Count == 0)
                return playerId;

            bool afterLeader = false;
            //find next player to show card after learder
            for (int i = 0; i < ShowedCards.Count; i++)
            {
                ShowedCardKeyValue keyValue = ShowedCards[i];
                if (keyValue.PlayerID != Learder && afterLeader == false)
                    continue;
                else if (keyValue.PlayerID == Learder) //search from leader;
                {
                    playerId = Learder;
                    afterLeader = true;
                }
                else if (afterLeader)
                {
                    if (keyValue.Cards.Count == 0)
                        return playerId;
                    playerId = keyValue.PlayerID;
                }
            }


            for (int i = 0; i < ShowedCards.Count; i++)
            {
                ShowedCardKeyValue keyValue = ShowedCards[i];
                if (keyValue.PlayerID != Learder)
                {
                    if (keyValue.Cards.Count == 0)
                        return playerId;
                    playerId = keyValue.PlayerID;
                }
                else //search end before leader
                    break;
            }

            return playerId;
        }

        public bool AllPlayedShowedCards()
        {
            for (int i = 0; i < ShowedCards.Count; i++)
            {
                ShowedCardKeyValue keyValue = ShowedCards[i];
                if (keyValue.Cards.Count == 0)
                    return false;
            }
            return true;
        }

        public bool IsStarted()
        {
            if (string.IsNullOrEmpty(Learder))
                return false;
            if (ShowedCards.Count == 0)
                return false;
            return CommonMethods.GetShowedCardsByPlayerID(ShowedCards, Learder).Count > 0;
        }

        public int CountOfPlayerShowedCards()
        {
            int result = 0;
            for (int i = 0; i < ShowedCards.Count; i++)
            {
                ShowedCardKeyValue keyValue = ShowedCards[i];
                if (keyValue.Cards.Count > 0)
                    result++;
            }
            return result;
        }
    }
}