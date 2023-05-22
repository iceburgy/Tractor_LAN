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
        [DataMember]
        public int Shengbi = 0;
        [DataMember]
        public int ShengbiTotal = 0; // the historical shengbi total that will only increase
        [DataMember]
        public DateTime lastQiandao;
        [DataMember]
        public List<string> ownedSkinInfo;
        [DataMember]
        public string skinInUse;
        [DataMember]
        public string clientType;
        [DataMember]
        public DateTime noDongtuUntil;

        private const string datePatt = @"yyyy/MM/dd-HH:mm:ss";
        private const int maxLoginHistory = 10;

        public ClientInfoV3(string ip, string id, string orp, string pe, string ct)
        {
            PlayerID = id;
            PlayerEmail = pe;
            overridePass = orp;
            playerIPList = new HashSet<string>();
            playerIPList.Add(ip);
            cheatHistory = new List<string>();
            loginHistory = new List<string>();
            lastQiandao = DateTime.MinValue;
            ownedSkinInfo = new List<string>() { CommonMethods.defaultSkinInUse };
            skinInUse = CommonMethods.defaultSkinInUse;
            clientType = ct;

            DateTime dtNow = DateTime.Now; ;
            noDongtuUntil = dtNow.AddTicks(-(dtNow.Ticks % 10000000));
        }

        public void logLogin(string ip, string cheating)
        {
            playerIPList.Add(ip);
            DateTime saveNow = DateTime.Now.ToLocalTime();
            string lastLogin = saveNow.ToString(datePatt);
            purgeLoginHistory();
            loginHistory.Add(string.Format("{0} at {1}", ip, lastLogin));
            if (!string.IsNullOrEmpty(cheating))
            {
                cheatHistory.Add(string.Format("{0} at {1}", cheating, lastLogin));
            }
        }

        private void purgeLoginHistory()
        {
            int purgeCount = this.loginHistory.Count - maxLoginHistory + 1;
            if (purgeCount > 0) this.loginHistory.RemoveRange(0, purgeCount);
        }

        public bool performQiandao(log4net.ILog log, string playerID)
        {
            var now = DateTime.Now;
            if (now.Date > this.lastQiandao.Date)
            {
                this.lastQiandao = now;
                this.transactShengbi(CommonMethods.qiandaoBonusShengbi, log, playerID, "签到");
                return true;
            }
            return false;
        }

        public void transactShengbi(int amount, log4net.ILog log, string playerID, string reason)
        {
            int oldShengbi = this.Shengbi;
            this.Shengbi += amount;
            log.Debug(string.Format("升币交易记录：玩家：【{0}】，类型：【{1}】，金额：【{2}】，之前【{3}】，之后【{4}】", playerID, reason, amount, oldShengbi, this.Shengbi));
            if (amount > 0)
            {
                this.ShengbiTotal += amount;
            }
        }
    }
}