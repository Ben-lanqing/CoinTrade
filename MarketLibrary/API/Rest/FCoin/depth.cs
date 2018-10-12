using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketLibrary.API.Rest.FCoin
{
    public class depth
    {
        public string msg { set; get; }
        public string type { set; get; }
        public int status { set; get; }
        public depthData data { set; get; }
    }
    public class depthData
    {
        public List<decimal> bids { set; get; }
        public List<decimal> asks { set; get; }
        public decimal seq { set; get; }
        public decimal ts { set; get; }
        public string type { set; get; }
        public string version { set; get; }

    }
}
