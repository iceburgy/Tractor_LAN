using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    public class OnlineBonusInfo
    {
        public DateTime OnlineSince;
        public DateTime OfflineSince;
        public OnlineBonusInfo()
        {
            this.OnlineSince = DateTime.Now;
            this.OfflineSince = DateTime.Now.AddYears(1);
        }
    }
}