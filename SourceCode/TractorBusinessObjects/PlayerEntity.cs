using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    [Serializable]
    public class PlayerEntity
    {
        // place holder for tracking rank increment, and bonus shengbi
        public int rankToAdd;
        public int roundWinnerBonusShengbi;

        [DataMember]
        public DateTime OfflineSince { get; set; }

        [DataMember]
        public string PlayerId { get; set; }

        [DataMember]
        public int Rank { get; set; }

        [DataMember]
        public GameTeam Team { get; set; }

        [DataMember]
        public bool IsReadyToStart { get; set; }

        [DataMember]
        public bool IsRobot { get; set; }

        [DataMember]
        public bool IsOffline { get; set; }

        [DataMember]
        public HashSet<string> Observers { get; set; }

        [DataMember]
        public string PlayingSG = string.Empty;
    }
}