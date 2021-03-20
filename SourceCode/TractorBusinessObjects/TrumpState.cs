using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    //亮过的牌的信息
    [DataContract]
    public class TrumpState
    {
        [DataMember]
        public Suit Trump { get; set; }

        //亮主的牌
        [DataMember]
        public TrumpExposingPoker TrumpExposingPoker { get; set; }

        //亮主的人
        [DataMember]
        public string TrumpMaker { get; set; }
    }
}