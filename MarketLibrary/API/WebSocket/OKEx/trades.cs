using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketLibrary.API.WebSocket.OKEx
{
    public class trades_ws
    {
        public List<trade> data { set; get; }
        public string id { set; get; }
        public decimal ts { set; get; }
    }
    public class trade
    {
        public decimal amount { set; get; }
        public decimal ts { set; get; }
        public decimal id { set; get; }
        public string side { set; get; }
        public decimal price { set; get; }
    }
}
