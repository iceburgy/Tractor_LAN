using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    [Serializable]
    public class GameState
    {
        [DataMember]
        public readonly List<PlayerEntity> Players;
        [DataMember]
        public Dictionary<string, string> PlayerToIP;

        public int nextRestartID = 0;
        public const int RESTART_GAME = 1;
        public const int RESTART_CURRENT_HAND = 2;
        public const int START_NEXT_HAND = 3;
        [DataMember]
        public PlayerEntity startNextHandStarter = null;

        public GameState()
        {
            PlayerToIP = new Dictionary<string, string>();
            Players = new List<PlayerEntity>();
            for (int i = 0; i < 4; i++)
            {
                Players.Add(null);
            }
        }

        public List<PlayerEntity> VerticalTeam
        {
            get
            {
                var team = new List<PlayerEntity>();
                foreach (PlayerEntity player in Players)
                {
                    if (player != null && player.Team == GameTeam.VerticalTeam)
                    {
                        team.Add(player);
                    }
                }
                return team;
            }
        }

        public List<PlayerEntity> HorizonTeam
        {
            get
            {
                var team = new List<PlayerEntity>();
                foreach (PlayerEntity player in Players)
                {
                    if (player != null && player.Team == GameTeam.HorizonTeam)
                    {
                        team.Add(player);
                    }
                }
                return team;
            }
        }

        public void MakeTeam(List<PlayerEntity> players)
        {
            foreach (PlayerEntity player in Players)
            {
                if (players.Exists(p => p != null && p.PlayerId == player.PlayerId))
                {
                    player.Team = GameTeam.VerticalTeam;
                }
                else
                {
                    player.Team = GameTeam.HorizonTeam;
                }
            }
        }

        /// <summary>
        ///     calculate the next state of this game
        /// </summary>
        /// <param name="starter">player id of the starter of this ending hand</param>
        /// <param name="score">socre got by the team without starter</param>
        /// <returns>the starter of next hand</returns>
        public PlayerEntity NextRankWorker(RoomState CurrentRoomState)
        {
            string starter = CurrentRoomState.CurrentHandState.Starter;
            int score = CurrentRoomState.CurrentHandState.Score;
            PlayerEntity nextStarter = null;

            if (!Players.Exists(p => p != null && p.PlayerId == starter))
            {
                //log
                return null;
            }

            GameTeam starterTeam = Players.Single(p => p.PlayerId == starter).Team;

            if (score >= 80)
            {
                nextStarter = GetNextPlayerAfterThePlayer(false, starter);
                int rankToAdd = (score - 80) / 40;
                foreach (PlayerEntity player in Players)
                {
                    if (player == null) continue;
                    if (player.Team != starterTeam && !CurrentRoomState.roomSetting.GetManditoryRanks().Contains(player.Rank))
                    {
                        UpdatePlayerRoundWinnerBonusShengbi(rankToAdd, player, 1);
                        UpdatePlayerRank(CurrentRoomState, rankToAdd, player);
                    }
                }
            }
            else
            {
                nextStarter = GetNextPlayerAfterThePlayer(true, starter);
                int rankToAdd = 1;
                if (score <= 0)
                {
                    rankToAdd = Math.Abs(score) / 40 + 3;
                }
                else if (score < 40)
                {
                    rankToAdd = 2;
                }
                else
                {
                    rankToAdd = 1;
                }

                foreach (PlayerEntity player in Players)
                {
                    if (player == null) continue;
                    if (player.Team == starterTeam)
                    {
                        UpdatePlayerRoundWinnerBonusShengbi(rankToAdd, player, 0);
                        UpdatePlayerRank(CurrentRoomState, rankToAdd, player);
                    }
                }
            }

            return nextStarter;
        }

        private void UpdatePlayerRoundWinnerBonusShengbi(int rankToAdd, PlayerEntity player, int affenderAddition)
        {
            int maxRankToAdd = Math.Min(rankToAdd, 13 - player.Rank);
            player.rankToAdd = maxRankToAdd;
            player.roundWinnerBonusShengbi = (maxRankToAdd + affenderAddition) * CommonMethods.roundWinnerBonusShengbi;
        }

        private static void UpdatePlayerRank(RoomState CurrentRoomState, int rankToAdd, PlayerEntity player)
        {
            //检查必打牌，default为5,10,K
            List<int> mandRanks = CurrentRoomState.roomSetting.GetManditoryRanks();
            int index = mandRanks.BinarySearch(player.Rank);
            if (index >= 0)
            {
                index++;
            }
            else
            {
                index = ~index;
            }
            if (index < mandRanks.Count)
            {
                player.Rank = Math.Min(player.Rank + rankToAdd, mandRanks[index]);
            }
            else
            {
                player.Rank = player.Rank + rankToAdd;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="handState"></param>
        /// <param name="lastTrickState">扣抵的牌</param>
        /// <returns></returns>
        public PlayerEntity NextRank(RoomState CurrentRoomState)
        {
            if (!Players.Exists(p => p != null && p.PlayerId == CurrentRoomState.CurrentHandState.Starter))
            {
                //log
                return null;
            }

            //处理J到底
            if (CurrentRoomState.roomSetting.AllowJToBottom && CurrentRoomState.CurrentHandState.Rank == 9)
            {
                var cards = CurrentRoomState.CurrentTrickState.ShowedCards[CurrentRoomState.CurrentTrickState.Winner];
                var cardscp = new CurrentPoker(cards, (int)CurrentRoomState.CurrentHandState.Trump,
                                               CurrentRoomState.CurrentHandState.Rank);

                //最后一把牌的赢家不跟庄家一伙
                if (!CurrentRoomState.CurrentGameState.ArePlayersInSameTeam(CurrentRoomState.CurrentHandState.Starter,
                                                        CurrentRoomState.CurrentTrickState.Winner))
                {

                    //主J勾到底

                    if (cardscp.MasterRank > 0)
                    {
                        foreach (PlayerEntity player in Players)
                        {
                            if (ArePlayersInSameTeam(CurrentRoomState.CurrentHandState.Starter,
                                player.PlayerId))
                            {
                                player.Rank = 0;
                            }
                        }
                    }
                    //副J勾一半
                    else if (cardscp.SubRank > 0)
                    {
                        foreach (PlayerEntity player in Players)
                        {
                            if (ArePlayersInSameTeam(CurrentRoomState.CurrentHandState.Starter,
                                player.PlayerId))
                            {
                                player.Rank = CurrentRoomState.CurrentHandState.Rank / 2;
                            }
                        }
                    }
                }
            }

            return NextRankWorker(CurrentRoomState);
        }

        /// <summary>
        ///     get the next player after the player
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public PlayerEntity GetNextPlayerAfterThePlayer(string playerId)
        {
            int thisPlayerIndex = -1;
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i] != null && Players[i].PlayerId == playerId)
                {
                    thisPlayerIndex = i;
                }
            }
            return Players[(thisPlayerIndex + 1) % 4];
        }

        /// <summary>
        ///     get the next player after the player
        /// </summary>
        /// <param name="inSameTeam">in the starter's team or not</param>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public PlayerEntity GetNextPlayerAfterThePlayer(bool inSameTeam, string playerId)
        {
            PlayerEntity result = null;
            if (!Players.Exists(p => p != null && p.PlayerId == playerId))
            {
                //log
            }
            GameTeam thePlayerTeam = Players.Single(p => p != null && p.PlayerId == playerId).Team;

            bool afterStarter = false;
            foreach (PlayerEntity player in Players)
            {
                if (player == null) continue;
                if (player.PlayerId != playerId && !afterStarter)
                    continue;
                if (player.PlayerId == playerId)
                {
                    afterStarter = true;
                }
                else if (player.PlayerId != playerId && afterStarter)
                {
                    if ((inSameTeam && player.Team == thePlayerTeam) || (!inSameTeam && player.Team != thePlayerTeam))
                    {
                        result = player;
                        break;
                    }
                }
            }

            if (result == null)
            {
                foreach (PlayerEntity player in Players)
                {
                    if (player != null && player.PlayerId != playerId)
                    {
                        if ((inSameTeam && player.Team == thePlayerTeam) ||
                            (!inSameTeam && player.Team != thePlayerTeam))
                        {
                            result = player;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public bool ArePlayersInSameTeam(string playerId1, string playerId2)
        {
            if (VerticalTeam.Exists(p => p.PlayerId == playerId1))
            {
                return VerticalTeam.Exists(p => p.PlayerId == playerId2);
            }
            if (HorizonTeam.Exists(p => p.PlayerId == playerId1))
            {
                return HorizonTeam.Exists(p => p.PlayerId == playerId2);
            }

            return false;
        }
    }


    public enum GameTeam
    {
        None,
        VerticalTeam,
        HorizonTeam
    }
}