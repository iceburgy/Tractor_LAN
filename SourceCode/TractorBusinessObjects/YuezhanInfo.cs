using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    public class YuezhanInfo
    {
        public YuezhanInfo(string o, string dd, List<string> p)
        {
            this.owner = o;
            this.dueDate = dd;
            this.participants = p;
        }

        [DataMember]
        public string owner;
        [DataMember]
        public string dueDate;
        [DataMember]
        public List<string> participants;
    }
}