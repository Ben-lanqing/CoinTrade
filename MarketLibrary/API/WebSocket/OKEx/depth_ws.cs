using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketLibrary.API.WebSocket.OKEx
{
    public class depth_ws
    {
        public depth data { set; get; }
        public decimal timestamp { set; get; }
        public string channel { set; get; }
    }
    public class depth
    {
        public decimal[] bids { set; get; }
        public decimal[] asks { set; get; }

    }
}
