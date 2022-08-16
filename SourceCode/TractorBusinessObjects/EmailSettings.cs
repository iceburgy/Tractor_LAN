using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    public class EmailSettings
    {
        [DataMember]
        public string EmailFromAddress = "";
        [DataMember]
        public string EmailFromName = "";
        [DataMember]
        public string EmailFromPass = "";
        [DataMember]
        public string EmailSubject = "";

        public EmailSettings(string fa, string fn, string fp, string sub)
        {
            EmailFromAddress = fa;
            EmailFromName = fn;
            EmailFromPass = fp;
            EmailSubject = sub;
        }
    }
}