using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
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
        public PlayerEntity NextRank(string starter, int score)
        {
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
                foreach (PlayerEntity player in Players)
                {
                    int scoreCopy = score;
                    while (scoreCopy >= 120)
                    {
                        //5,10,K必打
                        if (player.Team != starterTeam && player.Rank != 3 && player.Rank != 8 && player.Rank != 11)
                        {
                            player.Rank = player.Rank + 1;
                            scoreCopy -= 40;
                        }
                        else
                            break;
                    }
                }
            }
            else
            {
                nextStarter = GetNextPlayerAfterThePlayer(true, starter);
                if (score == 0)
                {
                    foreach (PlayerEntity player in Players)
                    {
                        if (player == null) continue;
                        if (player.Team == starterTeam)
                        {
                            //5,10,K必打
                            if (player.Rank < 3 && player.Rank + 3 > 3)
                                player.Rank = 3;
                            else if (player.Rank < 8 && player.Rank + 3 > 8)
                                player.Rank = 8;
                            else if (player.Rank < 11 && player.Rank + 3 > 11)
                                player.Rank = 11;
                            else
                                player.Rank = player.Rank + 3;
                        }
                    }
                }
                else if (score < 40)
                {
                    foreach (PlayerEntity player in Players)
                    {
                        if (player == null) continue;
                        if (player.Team == starterTeam)
                        {
                            //5,10,K必打
                            if (player.Rank < 3 && player.Rank + 2 > 3)
                                player.Rank = 3;
                            else if (player.Rank < 8 && player.Rank + 2 > 8)
                                player.Rank = 8;
                            else if (player.Rank < 11 && player.Rank + 2 > 11)
                                player.Rank = 11;
                            else
                                player.Rank = player.Rank + 2;
                        }
                    }
                }
                else
                {
                    foreach (PlayerEntity player in Players)
                    {
                        if (player == null) continue;
                        if (player.Team == starterTeam)
                            player.Rank = player.Rank + 1;
                    }
                }
            }

            return nextStarter;
        }

        /// <summary>
        /// </summary>
        /// <param name="handState"></param>
        /// <param name="lastTrickState">扣抵的牌</param>
        /// <returns></returns>
        public PlayerEntity NextRank(CurrentHandState handState, CurrentTrickState lastTrickState)
        {
            if (!Players.Exists(p => p != null && p.PlayerId == handState.Starter))
            {
                //log
                return null;
            }

            return NextRank(handState.Starter, handState.Score);
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