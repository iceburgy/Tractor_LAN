using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Duan.Xiugang.Tractor.Objects
{
    [Serializable]
    public class ServerLocalCache
    {
        public List<ShowedCardKeyValue> lastShowedCards;
        public string lastLeader;
        public bool muteSound;

        public ServerLocalCache()
        {
            lastShowedCards = new List<ShowedCardKeyValue>();
        }
    }
}
