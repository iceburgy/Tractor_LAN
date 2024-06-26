﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    public class ReplayEntity
    {
        [DataMember]
        public string ReplayId { get; set; }
        [DataMember]
        public CurrentHandState CurrentHandState { get; set; }
        [DataMember]
        public List<CurrentTrickState> CurrentTrickStates { get; set; }
        [DataMember]
        public string[] Players { get; set; }
        [DataMember]
        public int[] PlayerRanks { get; set; }
    }
}
