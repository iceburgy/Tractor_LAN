using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Duan.Xiugang.Tractor.Player
{
    public class PlayerLocalCache
    {
        public Dictionary<string, List<int>> ShowedCardsInCurrentTrick { get; set; }
        public int WinnerPosition { get; set; }
        public int WinnerResult { get; set; }
        public string WinnderID { get; set; }

        public PlayerLocalCache()
        {
            ShowedCardsInCurrentTrick = new Dictionary<string, List<int>>();
        }
    }
}
