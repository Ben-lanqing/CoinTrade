using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketLibrary.Model
{
    public class Depth
    {
        public bool result { set; get; }
        public string type { get; set; }
        /// <summary>
        /// 买方深度
        /// </summary>
        public List<decimal[]> bids { get; set; }
        /// <summary>
        /// 卖方深度
        /// </summary>
        public List<decimal[]> asks { get; set; }


        public Depth(API.WebSocket.FCoin.depth_ws depth)
        {
            type = depth.type;
            bids = new List<decimal[]>();
            asks = new List<decimal[]>();
            if (depth == null || depth.bids == null) return;
            int bidsPairCount = depth.bids.Count / 2;
            int asksPairCount = depth.asks.Count / 2;
            for (int i = 0; i < bidsPairCount; i++)
            {
                bids?.Add(new decimal[] { depth.bids[i * 2], depth.bids[i * 2 + 1] });
            }
            for (int i = 0; i < asksPairCount; i++)
            {
                asks?.Add(new decimal[] { depth.asks[i * 2], depth.asks[i * 2 + 1] });
            }
        }
    }

}
