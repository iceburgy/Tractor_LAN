using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Duan.Xiugang.Tractor.Objects
{
    public class ServerLocalCache
    {
        public Dictionary<string, List<int>> lastShowedCards;
        public string lastLeader;

        public ServerLocalCache()
        {
            lastShowedCards = new Dictionary<string, List<int>>();
        }
    }
}
