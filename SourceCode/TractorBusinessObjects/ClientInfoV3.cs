using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    public class ClientInfoV3
    {
        [DataMember]
        public string PlayerID;
        [DataMember]
        public string PlayerEmail = "";
        [DataMember]
        public string overridePass;
        [DataMember]
        public HashSet<string> playerIPList;
        [DataMember]
        public List<string> cheatHistory;
        [DataMember]
        public List<string> loginHistory;

        private const string datePatt = @"yyyy/MM/dd-HH:mm:ss";

        public ClientInfoV3(string ip, string id, string orp, string pe)
        {
            PlayerID = id;
            PlayerEmail= pe;
            overridePass = orp;
            playerIPList = new HashSet<string>();
            playerIPList.Add(ip);
            cheatHistory = new List<string>();
            loginHistory = new List<string>();
        }

        public void logLogin(string ip, bool isCheating)
        {
            playerIPList.Add(ip);
            DateTime saveNow = DateTime.Now.ToLocalTime();
            string lastLogin = saveNow.ToString(datePatt);
            loginHistory.Add(string.Format("{0} at {1}", ip, lastLogin));
            if (isCheating)
            {
                cheatHistory.Add(string.Format("player {0} attempted double observing with IP {1} at {2}", PlayerID, ip, lastLogin));
            }
        }
    }
}