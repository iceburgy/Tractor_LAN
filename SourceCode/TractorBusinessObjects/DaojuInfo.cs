using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    public class DaojuInfo
    {
        public DaojuInfo(Dictionary<string, SkinInfo> fsi, Dictionary<string, DaojuInfoByPlayer> djibp)
        {
            this.fullSkinInfo = fsi;
            this.daojuInfoByPlayer = djibp;
        }

        [DataMember]
        public Dictionary<string, SkinInfo> fullSkinInfo;
        [DataMember]
        public Dictionary<string, DaojuInfoByPlayer> daojuInfoByPlayer;
    }
}