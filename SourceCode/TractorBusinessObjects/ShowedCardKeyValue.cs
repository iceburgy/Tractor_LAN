using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    [Serializable]
    public class ShowedCardKeyValue
    {
        [DataMember]
        public string PlayerID;

        [DataMember]
        public List<int> Cards;

        public ShowedCardKeyValue()
        {
            PlayerID = string.Empty;
            Cards = new List<int>();
        }
    }
}
