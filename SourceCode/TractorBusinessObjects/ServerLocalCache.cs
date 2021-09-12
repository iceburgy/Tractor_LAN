using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Duan.Xiugang.Tractor.Objects
{
    [Serializable]
    public class ServerLocalCache
    {
        public Dictionary<string, List<int>> lastShowedCards;
        public string lastLeader;
        public bool muteSound;

        public ServerLocalCache()
        {
            lastShowedCards = new Dictionary<string, List<int>>();
        }
    }
}
