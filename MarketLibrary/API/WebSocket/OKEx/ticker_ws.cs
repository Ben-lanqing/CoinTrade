using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketLibrary.API.WebSocket.OKEx
{
    public class ticker_ws
    {
        public ticker data { set; get; }
        public string channel { set; get; }
    }
    public class ticker
    {
        public string  buy { set; get; }
        public string high { set; get; }
        public string last { set; get; }
        public string low { set; get; }
        public string sell { set; get; }
        public string vol { set; get; }
        public decimal timestamp { set; get; }
    }
}
