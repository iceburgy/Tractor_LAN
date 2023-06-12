using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    public class HostStatus
    {
        public HostStatus()
        {
            this.PreviousDate = DateTime.Now.Date;
        }
        [DataMember]
        public DateTime PreviousDate;
    }
}