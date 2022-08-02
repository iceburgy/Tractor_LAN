using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    public class ClientInfo
    {
        [DataMember]
        public string IP;
        [DataMember]
        public HashSet<string> playerIdList;
        [DataMember]
        public HashSet<string> playerIdListAttempted;
        [DataMember]
        public List<string> cheatHistory;
        [DataMember]
        public string lastLogin;
        [DataMember]
        public int maxIDsAllowed = 1;

        private const string datePatt = @"yyyy/MM/dd-HH:mm:ss";

        public ClientInfo(string clientIP)
        {
            IP = clientIP;
            playerIdList = new HashSet<string>();
            playerIdListAttempted = new HashSet<string>();
            cheatHistory = new List<string>();
        }

        public void logLogin(string playerId, bool isCheating)
        {
            playerIdList.Add(playerId);
            DateTime saveNow = DateTime.Now.ToLocalTime();
            lastLogin = saveNow.ToString(datePatt);
            if (isCheating)
            {
                cheatHistory.Add(string.Format("player {0} attempted double observing at {1}", playerId, lastLogin));
            }
        }

        public void logLoginAttempted(string playerId)
        {
            playerIdListAttempted.Add(playerId);
        }
    }
}