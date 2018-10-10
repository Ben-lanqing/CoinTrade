using CoreLibrary;
using Lq.Log4Net;
using TradeLibrary.API.Rest;
using TradeLibrary.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreLibrary.Model;
using System.Threading;
using CoreLibrary.DB;

namespace TradeLibrary
{
    public class TradeHepler
    {
        public string Platform { set; get; }

        private static string url_prex_FC = "https://api.fcoin.com";

        RestApi_FC restApi_FC;
        List<CoinConfig> CoinConfigs;

        public TradeHepler(List<CoinConfig> coinConfigs, string platform, string api_key, string secret_key)
        {
            Platform = platform;
            CoinConfigs = coinConfigs;

            if (!string.IsNullOrEmpty(platform))
            {
                switch (platform)
                {
                    case "FC":
                        restApi_FC = new RestApi_FC(url_prex_FC, api_key, secret_key);


                        break;
                    case "OK":
                        break;
                    case "BA":
                        break;
                    case "HB":
                        break;
                }
            }
        }

        #region 账户信息

        public Account GetAccount(string platform)
        {
            try
            {
                switch (platform)
                {
                    case "HB":

                    case "FC":
                        return GetAccount_FC();
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("GetAccount", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("GetAccount", e);
                return null;

            }
        }
        public Account GetAccount_FC()
        {
            string JsonStr = restApi_FC.userinfo();
            var data = ModelHelper<API.Rest.FCoin.account>.Json2Model(JsonStr);
            return new Account(CoinConfigs, data);

        }
        #endregion

        #region 下单
        public Trade PostTrade(string symbol, string side, decimal price, decimal amount, string platform, string type = "limit")
        {
            switch (platform)
            {
                case "OK":
                case "HB":
                case "FC":
                    return PostTrade_FC(symbol, side, price, amount, type);
                default:
                    return null;
            }
        }
        public Trade PostTrade_FC(string symbol, string side, decimal price, decimal amount, string type)
        {
            var coinInfo = CoinConfigs.FirstOrDefault(a => a.Symbol == symbol);
            if (coinInfo == null || coinInfo.AmountLimit == 0 || coinInfo.PriceLimit == 0) throw (new Exception());
            string priceStr = coinInfo.FormatPrice2S(price);
            string amountStr = coinInfo.FormatAmount2S(amount);
            string JsonStr = restApi_FC.trade(symbol, side, type, priceStr, amountStr);
            //Log4NetUtility.Debug("Trade", JsonStr);
            var data = ModelHelper<API.Rest.FCoin.trade>.Json2Model(JsonStr);
            return new Trade(data);
        }
        public Trade PostMarketTrade(string symbol, string side, decimal price, decimal amount, string platform)
        {
            switch (platform)
            {

                case "FC":
                    return PostMarketTrade_FC(symbol, side, price, amount);
                default:
                    return null;
            }
        }
        public Trade PostMarketTrade_FC(string symbol, string side, decimal price, decimal amount)
        {
            var coinInfo = CoinConfigs.FirstOrDefault(a => a.Symbol == symbol);
            if (coinInfo == null || coinInfo.AmountLimit == 0 || coinInfo.PriceLimit == 0) throw (new Exception());
            decimal rate = 1;
            if (side == "buy") rate = 1.08m; else rate = 0.92m;
            string priceStr = coinInfo.FormatPrice2S(price * rate);
            string amountStr = coinInfo.FormatAmount2S(amount);
            Log4NetUtility.Debug("Trade", $"MarketTrade. price:{price} priceStr:{priceStr} amount:{amount} amountStr:{amountStr}");
            string JsonStr = restApi_FC.trade(symbol, side, "limit", priceStr, amountStr);
            Log4NetUtility.Debug("Trade", JsonStr);
            var data = ModelHelper<API.Rest.FCoin.trade>.Json2Model(JsonStr);
            return new Trade(data);
        }

        #endregion

        #region 获取用户的订单信息
        public Orders GetOrderInfo(string symbol, string order_id, string platform)
        {
            switch (platform)
            {
                case "OK":

                //case "BA":
                //    return GetDepth_BA(symbol, size);
                case "HB":
                case "FC":
                    return GetOrders_FC(order_id);
                default:
                    return null;
            }
        }

        public Orders GetOrders_FC(string limit, string states, string symbol, string after = null, string before = null)
        {
            try
            {
                string JsonStr = restApi_FC.orders_info(limit, states, symbol, after, before);
                var data = ModelHelper<API.Rest.FCoin.ordersInfo>.Json2Model(JsonStr);
                Orders order = new Orders(data);
                return order;
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("GetOrderInfo_FC", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("GetOrderInfo_FC", e);
                return new Orders();
            }
        }
        public Orders GetOrders_FC(string order_id)
        {
            try
            {
                string JsonStr = restApi_FC.order_info(order_id);
                var data = ModelHelper<API.Rest.FCoin.orderInfo>.Json2Model(JsonStr);
                Orders order = new Orders(data);
                return order;
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("GetOrderInfo_FC", Utils.Exception2String(e));
                return new Orders();
            }
        }

        #endregion

        #region 撤销订单
        public CancelOrder CancelOrder(string symbol, string order_id, string platform)

        {
            switch (platform)
            {
                case "OK":
                //return CancelOrder_OK(symbol, order_id);

                //case "BA":
                //    return GetDepth_BA(symbol, size);
                case "FC":
                    return CancelOrder_FC(order_id);
                default:
                    return null;
            }
        }
        public CancelOrder CancelOrder_FC(string order_id)
        {
            string JsonStr = restApi_FC.cancel_order(order_id);
            var data = ModelHelper<API.Rest.FCoin.cancelorder>.Json2Model(JsonStr);
            return new CancelOrder(data);
        }

        #endregion
    }
}
