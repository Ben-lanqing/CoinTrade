using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketLibrary.API.WebSocket.FCoin
{
    public class depth_ws
    {
        public List<decimal> bids { set; get; }
        public List<decimal> asks { set; get; }
        public decimal seq { set; get; }
        public decimal ts { set; get; }
        public string type { set; get; }

    }
}
