using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    public class DaojuInfoByPlayer
    {
        public DaojuInfoByPlayer(int sb, DateTime lqd, List<string> osi, string skiu)
        {
            this.Shengbi = sb;
            this.lastQiandao = lqd;
            this.ownedSkinInfo = osi;
            this.skinInUse = skiu;
        }
        [DataMember]
        public int Shengbi = 0;
        [DataMember]
        public DateTime lastQiandao;
        [DataMember]
        public bool isRenewed
        {
            get
            {
                var now = DateTime.Now;
                return now.Date > this.lastQiandao.Date;
            }
        }
        [DataMember]
        public List<string> ownedSkinInfo;
        [DataMember]
        public string skinInUse = CommonMethods.defaultSkinInUse;
    }
}