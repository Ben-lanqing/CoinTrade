using CoreLibrary;
using Lq.Log4Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeLibrary.Model;
using TradeRobot;

namespace SessionRobot
{
    public class Robot_Session
    {

        public List<Order> SessionOrders { set; get; }

        bool Running = false;
        long CheckSessionDoTime = -1;
        object objectLock = new object();
        Robot_Trade robotTrade;
        string symbol;
        HFTInfo mainData;

        public event EventHandler<SessionEventArgs> SessionEvent;

        public Robot_Session(Robot_Trade robotTrade, string symbol)
        {
            SessionOrders = new List<Order>();
            this.robotTrade = robotTrade;
            this.symbol = symbol;
        }


        public void AddUpdateSessionOrder(Order order)
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
        public void RemoveSessionOrder(Order order)
        {
            SessionOrders.Remove(order);
        }
        public void ClearSessionAllOrders()
        {
            SessionOrders.Clear();

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


        private void CheckSessionOrders(long dotime)
        {
            #region  
            if (SessionOrders.Count == 0 && Running) return;
            if (CheckSessionDoTime == -1)
            {
                lock (objectLock)
                {
                    if (CheckSessionDoTime == -1)
                    {
                        CheckSessionDoTime = dotime;
                    }
                }
            }
            #endregion
            if (CheckSessionDoTime == dotime)
            {
                try
                {
                    //判断Order与缓存验证互斥操作
                    lock (SessionOrders)
                    {
                        if (SessionOrders.Count == 0 && Running) return;
                        Running = true;

                        #region 撤销重复
                        var doubleorder = SessionOrders.FirstOrDefault(b => SessionOrders.Exists(a => a.price == b.price && a.type == b.type && a.order_id != b.order_id));
                        if (doubleorder != null)
                        {
                            Log4NetUtility.Info("", "存在重复缓存单 Price:" + doubleorder.price);
                            //string msg = "";
                            #region CancelOrder
                            CancelOrder cancel = robotTrade.CancelOrder(symbol, doubleorder.order_id);
                            if (cancel.result)
                            {
                                SessionOrders.Remove(doubleorder);
                                doubleorder.status = "3";
                                //DbHelper.DBSaveChange(doubleorder, "UpDate");
                                Log4NetUtility.Info("缓存单验证", "已撤销重复Order。id:" + Utils.ShortID(doubleorder.order_id) + " Price:" + doubleorder.price);
                                CheckSessionDoTime = -1;
                                return;
                            }
                            else
                            {
                                string errmsg = "";
                                if (cancel.error_code.Contains("3008"))
                                {
                                    SessionOrders.Remove(doubleorder);
                                    //DbHelper.CreateInstance().RemoveOrder(doubleorder);
                                    errmsg = "已清除脏数据Order。id:" + Utils.ShortID(doubleorder.order_id) + " Price:" + doubleorder.price;
                                }
                                else
                                {
                                    errmsg = "撤销重复Order失败。id:" + Utils.ShortID(doubleorder.order_id) + " msg:" + cancel.error_code + cancel.msg;
                                }
                                Log4NetUtility.Info("缓存单验证", errmsg);
                                //DbHelper.CreateInstance().AddErrInfo("缓存单验证", errmsg);

                            }
                            #endregion

                        }
                        #endregion

                        #region 撤销异常价格
                        List<Order> errPriceOrders = SessionOrders.Where(a => a.price < 50m).ToList();
                        if (errPriceOrders.Count > 0)
                        {
                            foreach (var order in errPriceOrders)
                            {
                                Log4NetUtility.Info("", "存在异常价格存单 Price:" + order.price);
                                #region CancelOrder
                                CancelOrder cancel = robotTrade.CancelOrder(symbol, order.order_id);
                                if (cancel.result)
                                {
                                    SessionOrders.Remove(order);
                                    order.status = "3";
                                    //DbHelper.DBSaveChange(doubleorder, "UpDate");
                                    Log4NetUtility.Info("缓存单验证", "已撤销重复Order。id:" + Utils.ShortID(order.order_id) + " Price:" + order.price);
                                }
                                else
                                {
                                    string errmsg = "";
                                    if (cancel.error_code.Contains("3008"))
                                    {
                                        SessionOrders.Remove(order);
                                        //DbHelper.CreateInstance().RemoveOrder(doubleorder);
                                        errmsg = "已清除脏数据Order。id:" + Utils.ShortID(order.order_id) + " Price:" + order.price;
                                    }
                                    else
                                    {
                                        errmsg = "撤销重复Order失败。id:" + Utils.ShortID(doubleorder.order_id) + " msg:" + cancel.error_code + cancel.msg;
                                    }
                                    Log4NetUtility.Info("缓存单验证", errmsg);
                                    //DbHelper.CreateInstance().AddErrInfo("缓存单验证", errmsg);

                                }
                                #endregion
                            }
                            CheckSessionDoTime = -1;
                            return;
                        }
                        #endregion

                        #region 调整订单数量 if return
                        int buycount = SessionOrders.Count(a => a.type == "buy");
                        int sellcount = SessionOrders.Count(a => a.type == "sell");
                        int deff = buycount + sellcount - mainData.buyOrderCount - mainData.sellOrderCount;
                        #region 多单 return
                        if (deff > 0)
                        {
                            Log4NetUtility.Info("MoreOrder", string.Format("Order数大于初始值，现值:{0}   初始:{1}", buycount + sellcount, mainData.buyOrderCount + mainData.sellOrderCount));
                            Order cancelOrder;
                            if (buycount > sellcount)
                            {
                                cancelOrder = SessionOrders.OrderBy(a => a.price).First();
                            }
                            else
                            {
                                cancelOrder = SessionOrders.OrderBy(a => a.price).Last();
                            }
                            //string msg = "";
                            CancelOrder cancel = robotTrade.CancelOrder(symbol, cancelOrder.order_id);
                            if (cancel.result)
                            {
                                SessionOrders.Remove(cancelOrder);
                                cancelOrder.status = "3";
                                //DbHelper.DBSaveChange(cancelOrder, "UpDate");
                                string errmsg = string.Format("已撤 {0} id:{1}, Price:{2}", cancelOrder.type == "buy" ? "+B" : "-S", Utils.ShortID(cancelOrder.order_id), cancelOrder.price.ToString("0.00"));
                                //DbHelper.CreateInstance().AddErrInfo("缓存单验证", errmsg);
                                Log4NetUtility.Info("MoreOrder", errmsg);
                            }
                            CheckSessionDoTime = -1;
                            return;
                        }
                        #endregion
                        #region  少单 return
                        if (deff < 0)
                        {
                            Log4NetUtility.Info("LessOrder", string.Format("Order数小于初始值，现值{0}   初始{1}", buycount + sellcount, mainData.buyOrderCount + mainData.sellOrderCount));
                            account = helper.GetUserInfo(Platform);
                            decimal price = 0;
                            //string msg = "";
                            string errmsg = "";
                            string type = "";
                            if (account.GetFreeCoin(Symbol) * ticker.last > account.GetFreeFund())
                            {
                                price = SessionOrders.OrderBy(a => a.price).Last().price + mainData.SpanPrice;
                                type = "sell";
                            }
                            else
                            {
                                price = SessionOrders.OrderBy(a => a.price).First().price - mainData.SpanPrice;
                                type = "buy";
                            }
                            Trade trade = helper.PostTrade(Symbol, type, price, mainData.TradeQty, Platform);

                            //Trade trade = helper.PostTrade_FC(Symbol, type, "limit", price.ToString("0.00"), mainData.TradeQty.ToString("0.000"));

                            if (trade.result && !string.IsNullOrEmpty(trade.order_id))
                            {
                                var addorder = new var(trade.order_id, price, mainData.TradeQty, type);
                                addorder.order_time = Utils.GetDateTimeDec();
                                SessionOrders.Add(addorder);
                                //DbHelper.DBSaveChange(addorder, "Add");
                                errmsg = string.Format("已补充不足Order。 《{0}》Price:{1}", type == "buy" ? "+B" : "-S", price);
                            }
                            else
                            {
                                errmsg = string.Format("补充不足Order失败。《{0}》Price:{1}", type == "buy" ? "+B" : "-S", price);

                            }
                            Log4NetUtility.Info("LessOrder", errmsg);
                            //DbHelper.CreateInstance().AddErrInfo("缓存单验证", errmsg);

                            CheckSessionDoTime = -1;
                            return;
                        }
                        #endregion
                        #endregion

                        #region  判断补充丢单 if return
                        var lessorder = GetLossOrder();
                        if (lessorder != null)
                        {
                            string errmsg = "";
                            //string msg = "";

                            //bool deal = false;
                            //if (lessorder.type == "buy")
                            //{
                            //    deal = TradeHelper.CreateDelegateBuy(lessorder.order_amount, lessorder.price, ref msg);
                            //}
                            //if (lessorder.type == "sell")
                            //{
                            //    deal = TradeHelper.CreateDelegateSell(lessorder.order_amount, lessorder.price, ref msg);
                            //}
                            Trade trade = helper.PostTrade(Symbol, lessorder.type, lessorder.price, lessorder.order_amount, Platform);

                            //Trade trade = helper.PostTrade_FC(Symbol, lessorder.type, "limit", lessorder.price.ToString("0.00"), lessorder.order_amount.ToString("0.000"));

                            if (trade.result && !string.IsNullOrEmpty(trade.order_id))
                            {
                                lessorder.order_id = trade.order_id;
                                //lessorder.order_time = DateTime.Now.Ticks;
                                SessionOrders.Add(lessorder);
                                //DbHelper.DBSaveChange(lessorder, "Add");
                                errmsg = string.Format("AddOrder {0},ID:{1}，Price:{2}", lessorder.type == "buy" ? "《+B》" : "《-S》", Utils.ShortID(lessorder.order_id), lessorder.price.ToString("0.00"));
                            }
                            else
                            {
                                errmsg = $"AddOrder {lessorder.type} Fail! {lessorder.type } Price:{lessorder.price}，ErrMsg: {trade.error_code} {trade.msg}";
                                //errmsg = string.Format("AddOrder{0} Fail,ID:{1}，Price:{2}，ErrMsg: {3} {4}", lessorder.type == "buy" ? "《+B》" : "《-S》", lessorder.order_id.Remove(5, trade.order_id.Count() - 6), lessorder.price, trade.error_code, trade.msg);
                            }
                            Log4NetUtility.Info("LossOrder", errmsg);
                            //DbHelper.CreateInstance().AddErrInfo("判断补充丢单", errmsg);

                            CheckSessionDoTime = -1;
                            return;
                        }

                        #endregion

                        #region 判断单边、平仓
                        //if (filledSessionOrders.Count == 0)
                        //    return;
                        var lastFilledOrderTime = filledSessionOrders.Max(a => a.order_time);
                        //if (buycount == 0 || sellcount == 0 || Utils.GetDateTimeDec(-1000) < lastFilledOrderTime )
                        if (buycount == 0 || sellcount == 0)
                        {
                            if (Utils.GetDateTimeDec(-180) < lastFilledOrderTime)
                                return;
                            //平仓统计
                            //mainData.floatPrice = order.price;
                            mainData.resetCount += 1;
                            string timeStr = DateTime.Now.ToString("yyyyMMddHHmmss");
                            ResetTimes.Add(double.Parse(timeStr));

                            try
                            {

                                ReSetOrder(buycount == 0 ? "sell" : "buy");
                            }
                            catch (Exception e)
                            {
                                ////DbHelper.CreateInstance().AddErrInfo("全包平仓异常", e);

                                Log4NetUtility.Error("全包平仓异常", e.Message);
                                Log4NetUtility.Error("全包平仓异常", e.StackTrace);
                            }
                        }
                        #endregion

                    }
                }
                catch (Exception e)
                {
                    //DbHelper.CreateInstance().AddErrInfo("缓存验证", e);

                    Log4NetUtility.Error("缓存验证", e.Source);
                    Log4NetUtility.Error("缓存验证", e.Message);
                    Log4NetUtility.Error("缓存验证", e.StackTrace);
                }
                finally
                {
                    CheckSessionDoTime = -1;
                    Running = false;
                }
            }
        }
    }
}
