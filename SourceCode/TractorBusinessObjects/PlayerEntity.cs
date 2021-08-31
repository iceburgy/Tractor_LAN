using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    public class PlayerEntity
    {
        public DateTime OfflineSince { get; set; }

        [DataMember]
        public string PlayerId { get; set; }

        [DataMember]
        public string PlayerName { get; set; }

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
    }
}