using CoreLibrary.Model;
using Lq.Log4Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeLibrary.Model
{
    public class Account
    {
        public bool result { set; get; }
        public decimal date { set; get; }
        public decimal error_code { set; get; }
        public string msg { set; get; }
        public List<Balance> balances { set; get; }

        List<CoinConfig> coinConfigs;
        public Account(List<CoinConfig> coinConfigs)
        {
            result = false;
            balances = new List<Balance>();
            this.coinConfigs = coinConfigs;
        }
        public Account(List<CoinConfig> coinConfigs,API.Rest.FCoin.account acc)
        {
            try
            {
                balances = new List<Balance>();
                if (acc == null)
                {
                    result = false;
                    msg = "null";
                    error_code = -1;
                    return;
                }
                result = acc.status == 0;
                msg = acc.msg;
                error_code = acc.status;

                if (acc.status == 0 && acc.data.Count() > 0)
                {
                    foreach (var item in acc.data)
                    {
                        Balance balance = new Balance();
                        balance.currency = item.currency;
                        balance.available = decimal.Parse(item.available);
                        balance.frozen = decimal.Parse(item.frozen);
                        balance.balance = decimal.Parse(item.balance);
                        balances.Add(balance);
                    }
                }
               this. coinConfigs = coinConfigs ;
            }
            catch (Exception e)
            {

                throw (e);
            }
        }

        public decimal GetFreeFund(string currency = "usdt")
        {
            if (balances == null) return 0;
            var item = balances.FirstOrDefault(a => a.currency == currency);
            if (item != null)
            {
                return item.available;
            }
            return 0;
        }
        public decimal GetFreezedFund(string currency = "usdt")
        {
            if (balances == null) return 0;
            var item = balances.FirstOrDefault(a => a.currency == currency);
            if (item != null)
            {
                return item.frozen;
            }
            return 0;
        }

        public decimal GetFreeCoin(string symbol)
        {
            var coin = coinConfigs.FirstOrDefault(a => a.Symbol == symbol);
            return GetFreeFund(coin.CoinA);
        }
        public decimal GetFreezedCoin(string symbol)
        {
            var coin = coinConfigs.FirstOrDefault(a => a.Symbol == symbol);
            return GetFreezedFund(coin.CoinA);

        }
        public decimal GetNet(string symbol, decimal price)
        {
            decimal net = GetFreeFund() + GetFreezedFund() + (GetFreeCoin(symbol) + GetFreezedCoin(symbol)) * price;
            return net;
        }

    }

}
