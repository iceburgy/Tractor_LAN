using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    public class EnterHallInfo
    {
        public EnterHallInfo()
        {
        }

        [DataMember]
        public string password;
        [DataMember]
        public string email;
        [DataMember]
        public string clientType;
    }
}