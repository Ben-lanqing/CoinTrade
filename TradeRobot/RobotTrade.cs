using CoreLibrary;
using TradeLibrary;
using TradeLibrary.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Lq.Log4Net;
using System.Threading;
using CoreLibrary.Model;
using CoreLibrary.DB;

namespace TradeRobot
{
    public class Robot_Trade
    {
        public bool IsRunning { set; get; }
        public Account CurrentAccount { set; get; }

        #region
        string platform;
        List<string> symbolList;
        Dictionary<string, List<Order>> currentOrdersDic;
        Dictionary<string, List<Order>> filledOrdersDic;
        TradeHepler tradeHepler;
        System.Timers.Timer timer;
        Config config;
        #endregion

        public Robot_Trade(List<CoinConfig> coinConfigs, Config config, string platform, string api_key, string secret_key, List<string> symbols = null)
        {
            if (string.IsNullOrEmpty(platform)) throw (new Exception("para is null"));
            this.platform = platform;
            this.config = config;
            symbolList = symbols ?? new List<string>();
            CurrentAccount = new Account(coinConfigs);
            currentOrdersDic = new Dictionary<string, List<Order>>();
            filledOrdersDic = new Dictionary<string, List<Order>>();
            tradeHepler = new TradeHepler(coinConfigs, platform, api_key, secret_key);
            InitTimer();
        }

        public void Run()
        {
            IsRunning = true;
            timer.Start();
        }
        public void Stop()
        {
            IsRunning = false;
            timer.Stop();
        }

        #region Trade
        public Trade Trade(string symbol, string side, decimal price, decimal amount)
        {
            var trade = tradeHepler.PostTrade(symbol, side, price, amount, platform);
            //if (trade != null && trade.result && !string.IsNullOrEmpty(trade.order_id))
            //{
            //    Order order = new Order()
            //    {
            //        order_id = trade.order_id,
            //        amount = amount,
            //        price = price,
            //        symbol = symbol,
            //        type = side,
            //        create_date = Utils.GetDateTimeDec()
            //    };
            //    //AddUpdateSessionOrder(symbol, order);
            //}
            return trade;
        }
        public Trade MarketTrade(string symbol, string side, decimal price, decimal amount)
        {
            return tradeHepler.PostMarketTrade(symbol, side, price, amount, platform);
            //return trade == null ? false : trade.result;
        }
        public Orders GetOrders(string symbol, string order_id)
        {
            var orders = tradeHepler.GetOrderInfo(symbol, order_id, platform);
            return orders;
        }
        public Orders GetCurrentOrders(string symbol)
        {
            try
            {
                switch (platform)
                {
                    case "OK":
                    //return GetTicker_OK(symbol);
                    //case "BA":
                    //    return GetDepth_BA(symbol, size);
                    case "HB":
                    //return GetTicker_OK(symbol);
                    case "FC":
                        return GetCurrentOrders_FC(symbol);
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("GetCurrentOrders", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("GetCurrentOrders", e);
                return null;
            }

        }
        public Orders GetFilledOrders(string symbol)
        {
            try
            {
                switch (platform)
                {
                    case "OK":
                    //return GetTicker_OK(symbol);
                    //case "BA":
                    //    return GetDepth_BA(symbol, size);
                    case "HB":
                    //return GetTicker_OK(symbol);
                    case "FC":
                        return GetFilledOrders_FC(symbol);
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("GetCurrentOrders", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("GetCurrentOrders", e);
                return null;
            }

        }
        public void InitFilledOrders(string symbol)
        {
            try
            {
                switch (platform)
                {
                    case "OK":
                    //return GetTicker_OK(symbol);
                    //case "BA":
                    //    return GetDepth_BA(symbol, size);
                    case "HB":
                    //return GetTicker_OK(symbol);
                    case "FC":
                        InitFilledOrders_FC(symbol);
                        return;
                    default:
                        return;
                }
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("InitFilledOrders", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("InitFilledOrders", e);
            }

        }
        public int ClearCurrentOrders(string symbol)
        {
            try
            {
                switch (platform)
                {
                    case "OK":
                    //return GetTicker_OK(symbol);
                    //case "BA":
                    //    return GetDepth_BA(symbol, size);
                    case "HB":
                    //return GetTicker_OK(symbol);
                    case "FC":
                        return ClearCurrentOrders_FC(symbol);
                    default:
                        return -1;
                }
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("ClearOrders", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("ClearOrders", e);
                return -1;
            }
        }
        public CancelOrder CancelOrder(string symbol, string order_id)
        {
            return tradeHepler.CancelOrder(symbol, order_id, platform);

        }
        #endregion

        #region CurrentOrders
        public void AddUpdateCurrentOrders(string symbol, Orders orders)
        {
            if (!currentOrdersDic.ContainsKey(symbol))
            {
                currentOrdersDic.Add(symbol, orders.orders);
            }
            else
            {
                currentOrdersDic[symbol] = orders.orders;
            }
        }

        public List<Order> CurrentOrders(string symbol)
        {
            if (currentOrdersDic.ContainsKey(symbol))
            {
                return ModelHelper<Order>.CloneList(currentOrdersDic[symbol]);
            }
            return new List<Order>();
        }


        public void AddFilledOrders(string symbol, List<Order> orders)
        {
            List<Order> newOrders = new List<Order>();
            if (!filledOrdersDic.ContainsKey(symbol))
            {
                filledOrdersDic.Add(symbol, orders);
                newOrders.AddRange(orders);
                if (config.UseDataBase == true && newOrders.Count > 0) AddOrders2DB(newOrders, symbol);
            }
            else
            {
                var list = filledOrdersDic[symbol];
                Parallel.ForEach(orders, (item, loop) =>
                {
                    if (!list.Exists(a => a.order_id == item.order_id))
                    {
                        list.Add(item);
                        newOrders.Add(item);
                        if (config.UseDataBase == true && newOrders.Count > 0) AddOrder2DB(item);
                    }
                });

                if (list.Count > 500)
                {
                    list.RemoveRange(0, list.Count - 500);
                }
            }
            //if (config.UseDataBase == true && newOrders.Count > 0) AddOrders2DB(newOrders);

        }
        public void AddOrUpdateFilledOrders(string symbol, List<Order> orders)
        {
            List<Order> newOrders = new List<Order>();
            if (!filledOrdersDic.ContainsKey(symbol))
            {
                filledOrdersDic.Add(symbol, orders);
                newOrders.AddRange(orders);
            }
            else
            {
                var list = filledOrdersDic[symbol];
                Parallel.ForEach(orders, (item, loop) =>
                {
                    if (!list.Exists(a => a.order_id == item.order_id))
                    {
                        list.Add(item);
                        newOrders.Add(item);
                    }
                });

                if (list.Count > 500)
                {
                    list.RemoveRange(0, list.Count - 500);
                }
            }
            if (config.UseDataBase == true && newOrders.Count > 0) AddOrUpdateOrders2DB(newOrders);

        }
        public List<Order> FilledOrders(string symbol)
        {
            if (filledOrdersDic.ContainsKey(symbol))
            {
                return ModelHelper<Order>.CloneList(filledOrdersDic[symbol]);
            }
            return new List<Order>();
        }
        #endregion

        #region private
        private void InitTimer()
        {
            // 初始化系统计时器配置
            timer = new System.Timers.Timer(2000);
            timer.Elapsed += Timer_Elapsed;
        }
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            #region Account
            Account acc = tradeHepler.GetAccount(platform);
            if (acc != null && acc.result)
            {
                CurrentAccount = acc;
            }
            else
            {
            }
            #endregion
            #region CurrentOrders

            Parallel.ForEach(symbolList, symbol =>
            {
                var orders = GetCurrentOrders(symbol);
            });
            #endregion
            #region FilledOrders

            Parallel.ForEach(symbolList, symbol =>
            {
                var orders = GetFilledOrders(symbol);
            });
            #endregion
        }

        #region FC
        private Orders GetOrders_FC(string limit, string states, string symbol, string after = null, string before = null)
        {
            var orders = tradeHepler.GetOrders_FC(limit, states, symbol, after, before);
            return orders;
        }
        private Orders GetCurrentOrders_FC(string symbol)
        {
            var orders = tradeHepler.GetOrders_FC("150", "submitted,partial_filled", symbol, null, null);
            if (orders != null && orders.orders.Count > 0)
            {
                AddUpdateCurrentOrders(symbol, orders);
            }
            return orders;
        }
        private Orders GetFilledOrders_FC(string symbol, string before = null)
        {
            var orders = tradeHepler.GetOrders_FC("150", "filled", symbol, null, before);
            if (orders != null && orders.orders.Count > 0)
            {
                AddFilledOrders(symbol, orders.orders);
            }

            return orders;
        }
        private void InitFilledOrders_FC(string symbol, string before = null)
        {
            var orders = tradeHepler.GetOrders_FC("150", "filled", symbol, null, before);
            if (orders != null && orders.orders.Count > 0)
            {
                AddOrUpdateFilledOrders(symbol, orders.orders);
            }
        }
        private int ClearCurrentOrders_FC(string symbol)
        {
            Orders orders = GetCurrentOrders_FC(symbol);
            if (!orders.result) return -1;

            int ClearedCount = 0;
            Parallel.ForEach(orders.orders, order =>
            {
                decimal cancelPrice = order.price;
                string ordertype = order.type == "buy" ? "+B" : "-S";
                for (int i = 1; i <= 3; i++)
                {
                    CancelOrder cancel = tradeHepler.CancelOrder_FC(order.order_id);
                    if (cancel.result)
                    {
                        ClearedCount++;
                        string msg = $"{ordertype} P:{cancelPrice.ToString("0.00")} ID:{order.order_id}";
                        Log4NetUtility.Info("ClearOrders", msg);
                        break;
                    }
                    else
                    {
                        string msg = $"{i} {ordertype} P:{cancelPrice.ToString("0.00")} ID:{order.order_id} Msg:{cancel.error_code} {cancel.msg}";
                        Log4NetUtility.Info("ClearOrders", msg);
                        Thread.Sleep(250);
                    }
                }

            });
            currentOrdersDic.Remove(symbol);
            return ClearedCount;
        }
        #endregion

        private void AddOrder2DB(Order order)
        {
            try
            {
                order orderdb = new order();
                orderdb.orderid = order.order_id;
                orderdb.amount = Math.Round(order.amount, 4);
                orderdb.createdate = order.create_date;
                orderdb.fees = order.fill_fees;
                orderdb.platform = platform;
                orderdb.price = Math.Round(order.price, 4);
                orderdb.side = order.type;
                orderdb.status = order.status;
                orderdb.symbol = order.symbol;
                orderdb.date = DateTime.Now.ToString("yyyyMMddHHmmss");

                DbHelper.CreateInstance().AddOrder(orderdb);

            }
            catch (Exception e)
            {
                //Log4NetUtility.Error("AddOrder2DB", Utils.Exception2String(e));
                throw (e);
            }
        }
        //private void AddOrder2DB(Order order)
        //{
        //    try
        //    {
        //        order orderdb = new order();
        //        orderdb.amount = Math.Round(order.amount, 4);
        //        orderdb.createdate = order.create_date;
        //        orderdb.fees = order.fill_fees;
        //        orderdb.orderid = order.order_id;
        //        orderdb.platform = platform;
        //        orderdb.price = Math.Round(order.price, 4);
        //        orderdb.side = order.type;
        //        orderdb.status = order.status;
        //        orderdb.symbol = order.symbol;
        //        orderdb.date = DateTime.Now.ToString("yyyyMMddHHmmss");

        //        using (var db = new CoinTradeDBEntities())
        //        {
        //            db.order.Add(orderdb);
        //            db.SaveChanges();
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Log4NetUtility.Error("AddOrder2DB", Utils.Exception2String(e));

        //    }
        //}
        private void AddOrders2DB(List<Order> orders, string symbol)
        {
            try
            {
                List<order> list = new List<order>();
                var filledlist = filledOrdersDic[symbol];
                foreach (var order in orders)
                {
                    if (!filledlist.Exists(a => a.order_id == order.order_id))
                        AddOrder2DB(order);
                }

            }
            catch (Exception e)
            {
                Log4NetUtility.Error("AddOrders2DB", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("AddOrders2DB", e);
            }
        }
        private void AddOrUpdateOrder2DB(Order order)
        {
            try
            {
                order orderdb = new order();
                orderdb.orderid = order.order_id;
                orderdb.amount = Math.Round(order.amount, 4);
                orderdb.createdate = order.create_date;
                orderdb.fees = order.fill_fees;
                orderdb.platform = platform;
                orderdb.price = Math.Round(order.price, 4);
                orderdb.side = order.type;
                orderdb.status = order.status;
                orderdb.symbol = order.symbol;
                orderdb.date = DateTime.Now.ToString("yyyyMMddHHmmss");

                DbHelper.CreateInstance().AddUpdateOrder(orderdb);

            }
            catch (Exception e)
            {
                Log4NetUtility.Error("AddOrUpdateOrder2DB", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("AddOrUpdateOrder2DB", e);
            }
        }
        private void AddOrUpdateOrders2DB(List<Order> orders)
        {
            try
            {
                List<order> list = new List<order>();
                foreach (var order in orders)
                {
                    AddOrUpdateOrder2DB(order);
                }

            }
            catch (Exception e)
            {
                Log4NetUtility.Error("AddOrUpdateOrders2DB", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("AddOrUpdateOrders2DB", e);
            }
        }

        #endregion
    }
}
