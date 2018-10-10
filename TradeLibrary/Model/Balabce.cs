using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeLibrary.Model
{
    public class Balance
    {
        public string currency { set; get; }
        public decimal available { set; get; }
        public decimal frozen { set; get; }
        public decimal balance { set; get; }
    }
}
