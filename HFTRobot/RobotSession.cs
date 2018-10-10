using CoreLibrary;
using CoreLibrary.DB;
using Lq.Log4Net;
using MarketRobot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TradeLibrary.Model;
using TradeRobot;

namespace HFTRobot
{
    public class Robot_Session
    {
        #region 
        Robot_Trade robotTrade;
        Robot_Market robotMarket;
        string symbol;
        HFTInfo info;
        System.Timers.Timer timer;

        bool checkRunning = false;
        long checkSessionDoTime = -1;
        object objectLock = new object();
        object sessionLock = new object();
        #endregion

        public List<Order> SessionOrders
        {
            set;
            get;
        }
        public bool IsRunning { set; get; }
        public event EventHandler<SessionEventArgs> SessionEvent;

        public Robot_Session(Robot_Trade robotTrade, Robot_Market robotMarket, HFTInfo info)
        {
            this.robotTrade = robotTrade;
            this.robotMarket = robotMarket;
            SessionOrders = new List<Order>();
            this.info = info;
            symbol = info.Symbol;
            timer = new System.Timers.Timer(3000);
            timer.Elapsed += Timer_Elapsed;
            IsRunning = false;
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

        public string LogSring()
        {
            int buynum = SessionOrders.Count(a => a.type == "buy");
            int sellnum = SessionOrders.Count(a => a.type == "sell");
            var list = SessionOrders.OrderByDescending(a => a.price);
            StringBuilder sb_orders = new StringBuilder();
            sb_orders.AppendLine();
            foreach (var item in list)
            {
                sb_orders.AppendLine(string.Format("{0}  Price:{1}，Amount:{2}，ID:{3}", item.type == "buy" ? "+B" : "-S", item.price, item.amount, item.order_id));
            }
            return $"Buy Count:{buynum}，Sell Count:{sellnum}. {sb_orders.ToString()}";
        }

        public string LogOrders()
        {
            StringBuilder sb = new StringBuilder();
            var listS = SessionOrders.Where(a => a.type == "sell").OrderByDescending(a => a.price).ToList();
            var listB = SessionOrders.Where(a => a.type == "buy").OrderByDescending(a => a.price).ToList();
            int sCount = listS.Count();
            int bCount = listB.Count();
            int maxCount = new List<int> { sCount, bCount }.Max();
            sb.AppendLine($"                    SellCount:{sCount}  BuyCount:{bCount}                    ");

            for (int i = 0; i < maxCount; i++)
            {
                if (sCount > i)
                {
                    string side = listS[i].type == "buy" ? "+B" : "-S";
                    string ordertimeStr = Utils.GetDateTimeByStr(listS[i].create_date).ToString("HH:mm:ss");
                    sb.Append($"{ordertimeStr} {side} P:{listS[i].price.ToString("0.00")} A:{listS[i].amount.ToString("0.000")} ID:{Utils.ShortID(listS[i].order_id)}");
                }
                else
                {
                    sb.Append($"                                       ");
                }
                if (bCount > i)
                {
                    string side = listB[i].type == "buy" ? "+B" : "-S";
                    string ordertimeStr = Utils.GetDateTimeByStr(listB[i].create_date).ToString("HH:mm:ss");
                    sb.AppendLine($" {ordertimeStr} {side} P:{listB[i].price.ToString("0.00")} A:{listB[i].amount.ToString("0.000")} ID:{Utils.ShortID(listB[i].order_id)}");
                }
                else
                {
                    sb.AppendLine();
                }

            }
            return sb.ToString();
        }
        public void AddUpdateSessionOrder(Order order)
        {
            lock (sessionLock)
            {
                if (SessionOrders.Count > 0)
                {
                    var old = SessionOrders.FirstOrDefault(a => a.order_id == order.order_id);
                    if (old != null)
                    {
                        SessionOrders.Remove(old);
                    }
                    SessionOrders.Add(order);
                }
                else
                {
                    SessionOrders.Add(order);
                }
            }
        }
        public void RemoveSessionOrder(Order order)
        {
            lock (sessionLock)
            {
                if (SessionOrders.Count > 0)
                {
                    var old = SessionOrders.FirstOrDefault(a => a.order_id == order.order_id);
                    if (old != null)
                    {
                        SessionOrders.Remove(old);
                    }
                }
            }
        }
        public void ClearSessionAllOrders()
        {
            lock (sessionLock)
            {
                SessionOrders.Clear();
            }
        }
        public decimal GetEagePrice(bool first)
        {
            try
            {
                if (first)
                    return SessionOrders.OrderBy(a => a.price).First().price;
                else
                    return SessionOrders.OrderBy(a => a.price).Last().price;
            }
            catch (Exception e)
            {
                throw (e);
            }
        }
        public int CountOrder(bool buy)
        {
            try
            {
                if (buy)
                    return SessionOrders.Count(a => a.type == "buy");
                else
                    return SessionOrders.Count(a => a.type == "sell");
            }
            catch (Exception e)
            {
                throw (e);
            }
        }
        public void CheckLostOrders(List<Order> currentOrder)
        {
            var lostOrders = currentOrder.Where(a => !SessionOrders.Exists(b => b.order_id == a.order_id)).ToList();
            if (lostOrders.Count > 0)
            {
                foreach (var lost in lostOrders)
                {
                    AddUpdateSessionOrder(lost);
                    Log4NetUtility.Debug($"CurrentEvent,Add lostOrders Price:{lost.price.ToString("#0.00")} Id:{Utils.ShortID(lost.order_id)}");
                }
            }
        }

        public void FilledSessionOrders(List<Order> filledSessionOrders, List<Order> currentOrder)
        {

            Parallel.ForEach(SessionOrders, (sessionOrder, loop) =>
            {
                try
                {
                    #region  缓存订单在成交订单中
                    var filledOrder = filledSessionOrders.FirstOrDefault(a => a != null && a.order_id == sessionOrder.order_id);
                    if (filledOrder != null)
                    {
                        string type = filledOrder.type == "buy" ? "+B" : "-S";
                        Log4NetUtility.Debug($"OrderFilled {type} [{filledOrder.price.ToString("#0.00")}] ID: {Utils.ShortID(filledOrder.order_id)} ");

                        decimal new_price = 0;//补单Price
                        string new_type = filledOrder.type == "buy" ? "sell" : "buy";
                        if (filledOrder.type == "sell")//卖单成交
                        {
                            //补单价格为成交价的少一单位价格
                            new_price = filledOrder.price - info.SpanPrice;
                        }
                        else
                        {
                            //补单价格为成交价的多一单位价格
                            new_price = filledOrder.price + info.SpanPrice;
                        }
                        Order new_order = new Order() { price = new_price, amount = info.TradeQty, type = new_type, create_date = Utils.GetUtcTimeDec() };
                        CurrentOrderFilled(filledOrder, new_order);
                        //list.Add(new_order);
                    }
                    #endregion
                    #region  缓存订单不在成交\当前订单中
                    else//缓存订单不在成交订单中
                    {
                        //缓存订单也不在当前订单中
                        if (!currentOrder.Exists(a => a.order_id == sessionOrder.order_id))
                        {
                            decimal time = Utils.GetUtcTimeDec(-60);
                            //若订单时间较短可能平台延迟，忽略。否则缓存脏数据
                            if (sessionOrder.create_date < time)
                            {
                                var order = robotTrade.GetOrders(symbol, sessionOrder.order_id);
                                if (order.result && order.orders.Count != 0)
                                {
                                    Order o = order.orders.FirstOrDefault();
                                    if (o.status == "filled")
                                    {
                                        if (sessionOrder.flag == "dirty")
                                        {
                                            RemoveSessionOrder(sessionOrder);
                                            Log4NetUtility.Debug($"Remove dirtyOrders {sessionOrder.type} Price:{sessionOrder.price.ToString("#0.00")} Id:{Utils.ShortID(sessionOrder.order_id)} status:{o.status}");
                                        }
                                        else
                                        {
                                            sessionOrder.flag = "dirty";
                                        }
                                    }
                                }

                            }
                        }
                    }
                    #endregion
                }
                catch (Exception e)
                {
                    throw (e);
                }
            });

        }
        private void CurrentOrderFilled(Order filledOrder, Order newOrder)
        {
            var trade = TradeOrder(newOrder);
            if (trade.result && !string.IsNullOrEmpty(trade.order_id))
            {
                #region 反向挂单成功，处理缓存
                newOrder.order_id = trade.order_id;
                newOrder.create_date = Utils.GetUtcTimeDec();
                AddUpdateSessionOrder(newOrder);
                filledOrder.status = "2";
                RemoveSessionOrder(filledOrder);
                #endregion
                #region 统计震荡
                if (filledOrder.type == "buy")//买成交，行情下降
                {
                    info.dealDCount += 1;
                    //info.DownCount_tmp += 1;
                    info.floatPrice -= info.SpanPrice;
                }
                else//卖成交，行情上升
                {
                    info.dealUCount += 1;
                    //info.UpCount_tmp += 1;
                    info.floatPrice += info.SpanPrice;
                }
                long shockCount = Math.Min(info.dealDCount, info.dealUCount);
                if (info.dealCount != shockCount)
                {
                    //string timeStr = DateTime.Now.ToString("yyyyMMddHHmmss");
                    info.ShockTimes.Add(Utils.GetUtcTimeDec());
                    info.dealCount = shockCount;
                }
                //int shockCount_tmp = Math.Min(info.DownCount_tmp, info.UpCount_tmp);

                //if (info.ShockCount_tmp != shockCount_tmp)
                //{
                //    info.ShockCount_tmp += 1;
                //    //info.EarnAmt = info.EarnAmt + info.SpanPrice * info.TradeQty;
                //}
                #endregion
                #region log
                int buynum = CountOrder(true);
                int sellnum = CountOrder(false);
                string type = filledOrder.type == "sell" ? "+B" : "-S";
                Log4NetUtility.Debug($"OrderFilled {type} [{newOrder.price.ToString("#0.00")}] ID: {Utils.ShortID(trade.order_id)} S:[{sellnum}] B:[{buynum}] ");
                #endregion
            }
        }
        private Trade TradeOrder(Order order)
        {
            if (order == null) return null;
            Trade trade = robotTrade.Trade(symbol, order.type, order.price, order.amount);
            for (int i = 0; i < 3; i++)
            {
                if (trade.result && !string.IsNullOrEmpty(trade.order_id))
                {
                    order.order_id = trade.order_id;
                    order.create_date = Utils.GetUtcTimeDec();
                    AddUpdateSessionOrder(order);
                    Console.WriteLine($"《{order.type}》 Price:{order.price.ToString("#0.0000")} id:{Utils.ShortID(order.order_id)}");
                    break;
                }
                else
                {
                    string type = order.type == "buy" ? "-S" : "+B";
                    Log4NetUtility.Info("RobotHFT", $"《{type}》委托失败，P:《{order.price.ToString("#0.00")}》A:《{order.amount.ToString("#0.0000")}》,第{i + 1}次返回:{trade.error_code} {trade.msg}");
                    Thread.Sleep(1000);
                    if (trade.error_code.Contains("1002"))
                    {
                        Thread.Sleep(2000 * i);
                        //i -= 1;
                    }
                    if (trade.error_code.Contains("1016"))
                    {
                        var side = order.type == "buy" ? "sell" : "buy";
                        var ticker = robotMarket.GetTicker(symbol);
                        decimal price = side == "buy" ? ticker.sell : ticker.buy;

                        Trade tradeM = robotTrade.MarketTrade(symbol, side, price, order.amount);
                        if (tradeM.result)
                        {
                            Log4NetUtility.Info("RobotHFT", $"《{side}》市价调整，P:《{price.ToString("#0.00")}》A:《{order.amount.ToString("#0.0000")}》 ");
                        }
                        else
                        {
                            Log4NetUtility.Info("RobotHFT", $"《{side}》市价失败，P:《{price.ToString("#0.00")}》A:《{order.amount.ToString("#0.0000")}》 {tradeM.error_code} {tradeM.msg}");
                        }
                        Thread.Sleep(2000 * i);
                    }
                }
            }
            return trade;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            long dotime = DateTime.Now.Ticks;
            CheckSessionOrders(dotime);
        }
        private void CheckSessionOrders(long dotime)
        {
            #region  objectLock
            if (SessionOrders.Count == 0 && checkRunning) return;
            if (checkSessionDoTime == -1)
            {
                lock (objectLock)
                {
                    if (checkSessionDoTime == -1)
                    {
                        checkSessionDoTime = dotime;
                    }
                }
            }
            #endregion
            if (checkSessionDoTime == dotime)
            {
                try
                {
                    //判断Order与缓存验证互斥操作
                    lock (sessionLock)
                    {
                        if (SessionOrders.Count == 0 || checkRunning) return;
                        checkRunning = true;

                        #region 判断单边、平仓
                        if (SessionOrders.Count == 0)
                            return;
                        var lastFilledOrderTime = SessionOrders.Max(a => a.create_date);
                        int buycount = SessionOrders.Count(a => a.type == "buy");
                        int sellcount = SessionOrders.Count(a => a.type == "sell");
                        //if (buycount == 0 || sellcount == 0 || Utils.GetDateTimeDec(-1000) < lastFilledOrderTime )
                        bool Reset = false;
                        if (buycount == 0 || sellcount == 0)
                        {
                            if (Utils.GetUtcTimeDec(-180) > lastFilledOrderTime)
                            {
                                Reset = true;
                                Log4NetUtility.Info("Reset", $"单边3分钟");

                            }
                        }
                        if (Utils.GetUtcTimeDec(-600) > lastFilledOrderTime)
                        {
                            Reset = true;
                            Log4NetUtility.Info("Reset", $"缓存静止10分钟");
                        }
                        if (Reset)
                        {
                            SessionEvent?.Invoke(null, new SessionEventArgs(SessionEventType.Reset));
                            return;
                        }
                        #endregion

                        #region 撤销重复
                        bool flag = false;
                        var doubleorder = SessionOrders.FirstOrDefault(b => SessionOrders.Exists(a => a.price == b.price && a.type == b.type && a.order_id != b.order_id));
                        if (doubleorder != null)
                        {
                            SessionEvent?.Invoke(null, new SessionEventArgs(SessionEventType.DoubleOrder, new List<Order>() { doubleorder }));
                            //Log4NetUtility.Info("", "存在重复缓存单 Price:" + doubleorder.price);
                            //string msg = "";
                            #region //CancelOrder
                            //CancelOrder cancel = robotTrade.CancelOrder(symbol, doubleorder.order_id);
                            //if (cancel.result)
                            //{
                            //    sessionOrders.Remove(doubleorder);
                            //    doubleorder.status = "3";
                            //    //DbHelper.DBSaveChange(doubleorder, "UpDate");
                            //    Log4NetUtility.Info("缓存单验证", "已撤销重复Order。id:" + Utils.ShortID(doubleorder.order_id) + " Price:" + doubleorder.price);
                            //    CheckSessionDoTime = -1;
                            //    return;
                            //}
                            //else
                            //{
                            //    string errmsg = "";
                            //    if (cancel.error_code.Contains("3008"))
                            //    {
                            //        sessionOrders.Remove(doubleorder);
                            //        //DbHelper.CreateInstance().RemoveOrder(doubleorder);
                            //        errmsg = "已清除脏数据Order。id:" + Utils.ShortID(doubleorder.order_id) + " Price:" + doubleorder.price;
                            //    }
                            //    else
                            //    {
                            //        errmsg = "撤销重复Order失败。id:" + Utils.ShortID(doubleorder.order_id) + " msg:" + cancel.error_code + cancel.msg;
                            //    }
                            //    Log4NetUtility.Info("缓存单验证", errmsg);
                            //    //DbHelper.CreateInstance().AddErrInfo("缓存单验证", errmsg);

                            //}
                            #endregion
                            flag = true;
                        }
                        #endregion

                        #region 撤销异常价格
                        List<Order> errPriceOrders = SessionOrders.Where(a => a.price < 50m).ToList();
                        if (errPriceOrders.Count > 0)
                        {
                            SessionEvent?.Invoke(null, new SessionEventArgs(SessionEventType.ErrPrice, errPriceOrders));

                            foreach (var order in errPriceOrders)
                            {
                                //Log4NetUtility.Info("", "存在异常价格存单 Price:" + order.price);
                                #region CancelOrder
                                //CancelOrder cancel = robotTrade.CancelOrder(symbol, order.order_id);
                                //if (cancel.result)
                                //{
                                //    sessionOrders.Remove(order);
                                //    order.status = "3";
                                //    //DbHelper.DBSaveChange(doubleorder, "UpDate");
                                //    Log4NetUtility.Info("缓存单验证", "已撤销重复Order。id:" + Utils.ShortID(order.order_id) + " Price:" + order.price);
                                //}
                                //else
                                //{
                                //    string errmsg = "";
                                //    if (cancel.error_code.Contains("3008"))
                                //    {
                                //        sessionOrders.Remove(order);
                                //        //DbHelper.CreateInstance().RemoveOrder(doubleorder);
                                //        errmsg = "已清除脏数据Order。id:" + Utils.ShortID(order.order_id) + " Price:" + order.price;
                                //    }
                                //    else
                                //    {
                                //        errmsg = "撤销重复Order失败。id:" + Utils.ShortID(doubleorder.order_id) + " msg:" + cancel.error_code + cancel.msg;
                                //    }
                                //    Log4NetUtility.Info("缓存单验证", errmsg);
                                //    //DbHelper.CreateInstance().AddErrInfo("缓存单验证", errmsg);

                                //}
                                #endregion
                            }
                            //CheckSessionDoTime = -1;
                            flag = true;
                        }
                        if (flag) return;
                        #endregion

                        #region 调整订单数量 if return
                        int deff = buycount + sellcount - info.buyOrderCount - info.sellOrderCount;
                        #region 多单 return
                        if (deff > 1)
                        {
                            Log4NetUtility.Info("MoreOrder", string.Format("Order数大于初始值，现值:{0}   初始:{1}", buycount + sellcount, info.buyOrderCount + info.sellOrderCount));
                            Order cancelOrder;
                            if (buycount > sellcount)
                            {
                                cancelOrder = SessionOrders.OrderBy(a => a.price).First();
                            }
                            else
                            {
                                cancelOrder = SessionOrders.OrderBy(a => a.price).Last();
                            }
                            SessionEvent?.Invoke(null, new SessionEventArgs(SessionEventType.MoreOrder, new List<Order>() { cancelOrder }));
                            return;
                        }
                        #endregion
                        #region  少单 return
                        if (deff < -1)
                        {
                            Log4NetUtility.Info("LessOrder", string.Format("Order数小于初始值，现值{0}   初始{1}", buycount + sellcount, info.buyOrderCount + info.sellOrderCount));
                            SessionEvent?.Invoke(null, new SessionEventArgs(SessionEventType.LessOrder));
                            return;
                        }
                        #endregion
                        #endregion

                        #region  判断补充丢单 if return
                        var lostorder = GetLostOrder(SessionOrders);
                        if (lostorder != null)
                        {
                            SessionEvent?.Invoke(null, new SessionEventArgs(SessionEventType.LostOrder, new List<Order>() { lostorder }));
                            return;
                        }

                        #endregion

                    }
                }
                #region catch\finally
                catch (Exception e)
                {
                    //DbHelper.CreateInstance().AddErrInfo("缓存验证", e);

                    Log4NetUtility.Error("缓存验证", Utils.Exception2String(e));
                    DbHelper.CreateInstance().AddError("缓存验证", e);

                }
                finally
                {
                    checkSessionDoTime = -1;
                    checkRunning = false;
                }
                #endregion
            }
        }
        /// <summary>
        /// 获取丢单
        /// </summary>
        /// <returns></returns>
        private Order GetLostOrder(List<Order> sessionOrders)
        {
            Order lostorder = null;
            int buycount = sessionOrders.Count(a => a.type == "buy");
            int sellcount = sessionOrders.Count(a => a.type == "sell");
            if (buycount + sellcount > info.OrderQty * 2 + 2) return null;
            var tempList = sessionOrders.OrderBy(a => a.price).ThenByDescending(a => a.type).ToList();

            for (int i = 0; i < (buycount + sellcount - 1); i++)
            {
                //同类订单
                if (tempList[i].type == tempList[i + 1].type)
                {
                    //价格差为一个单位
                    decimal rightprice = tempList[i].price + info.SpanPrice;
                    if (tempList[i + 1].price != rightprice)
                    {
                        lostorder = new Order() { price = rightprice, amount = info.TradeQty, type = tempList[i].type };

                        //lostorder = new Order("", rightprice, info.TradeQty, tempList[i].type);
                        break;
                    }
                }
                else
                {
                    //价格差为两个单位(有时存在同价买卖)
                    decimal rightprice = tempList[i].price + info.SpanPrice * 2;
                    if (tempList[i + 1].price != rightprice)
                    {
                        if (tempList[i + 1].price == tempList[i].price)
                            continue;
                        lostorder = new Order() { price = rightprice, amount = info.TradeQty, type = tempList[i].type == "buy" ? "sell" : "buy" };

                        //lostorder = new Order("", rightprice, info.TradeQty, tempList[i].type == "buy" ? "sell" : "buy");
                        break;
                    }
                }
            }
            return lostorder;
        }

    }
}
