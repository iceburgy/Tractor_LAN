using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    public class RoomSetting
    {
        [DataMember]
        public string RoomName;
        [DataMember]
        public string RoomOwner;
        [DataMember]
        public List<int> ManditoryRanks;
        [DataMember]
        public int AllowRiotWithTooFewScoreCards;
        [DataMember]
        public int AllowRiotWithTooFewTrumpCards;
        [DataMember]
        public bool AllowJToBottom;
        [DataMember]
        public bool AllowSurrender;

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (this.GetType() != obj.GetType())
            {
                return false;
            }

            RoomSetting rs = (RoomSetting)obj;

            if (RoomName != rs.RoomName) return false;
            if (RoomOwner != rs.RoomOwner) return false;
            if (AllowRiotWithTooFewScoreCards != rs.AllowRiotWithTooFewScoreCards) return false;
            if (AllowRiotWithTooFewTrumpCards != rs.AllowRiotWithTooFewTrumpCards) return false;
            if (AllowJToBottom != rs.AllowJToBottom) return false;
            if (AllowSurrender != rs.AllowSurrender) return false;
            if (!ManditoryRanks.SequenceEqual(rs.ManditoryRanks)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public List<int> GetManditoryRanks()
        {
            return this.ManditoryRanks;
        }

        public void SetManditoryRanks(List<int> ranks)
        {
            this.ManditoryRanks = new List<int>(ranks);
            this.ManditoryRanks.Sort();
        }

        public void SortManditoryRanks()
        {
            this.ManditoryRanks.Sort();
        }

        public RoomSetting()
        {
            this.ManditoryRanks = new List<int>() { 3, 8, 11 };
        }
    }
}
