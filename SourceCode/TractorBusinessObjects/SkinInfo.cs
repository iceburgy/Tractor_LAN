using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    public class SkinInfo
    {
        public SkinInfo(string sn, string ss, string sd, int st, int sc, DateTime exp)
        {
            this.skinName = sn;
            this.skinSex = ss;
            this.skinDesc = sd;
            this.skinType = st;
            this.skinCost = sc;
            this.skinExpiration = exp;
        }
        [DataMember]
        public string skinName;
        [DataMember]
        public string skinSex;
        [DataMember]
        public string skinDesc;
        [DataMember]
        public int skinType; //0-static;1-dynamic
        [DataMember]
        public int skinCost;
        [DataMember]
        public DateTime skinExpiration;

        [DataMember]
        public bool isSkinExpired
        {
            get
            {
                var now = DateTime.Now;
                return now >= this.skinExpiration;
            }
        }
    }
}