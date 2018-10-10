using CoreLibrary;
using CoreLibrary.DB;
using CoreLibrary.Model;
using Lq.Log4Net;
using MarketLibrary.Model;
using MarketRobot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TradeLibrary.Model;
using TradeRobot;

namespace HFTRobot
{
    public class RobotHFT
    {
        #region
        Config config;
        List<CoinConfig> coinConfigs;
        HFTInfo info;

        string Symbol;
        List<string> Symbols;
        string Platform;
        bool resetflag = false;

        Robot_Market robotMarket;
        Robot_Trade robotTrade;
        Robot_Session robotSession;
        Robot_Current robotCurrent;
        Robot_Report robotReport;
        #endregion

        public RobotHFT()
        {
            Init();
        }

        public void Run()
        {
            robotMarket.Run(new List<string>() { Symbol });
            Log4NetUtility.Info("Robot_Market", "已开始运行");
            robotTrade.Run();
            Log4NetUtility.Info("Robot_Trade", "已开始运行");
            //RobotHFT Start
            Start(true);
            robotSession.Run();
            Log4NetUtility.Info("Robot_Session", "已开始运行");
            robotCurrent.Run();
            Log4NetUtility.Info("Robot_Current", "已开始运行");
            robotReport.Run();
            Log4NetUtility.Info("Robot_Report", "已开始运行");

            Log4NetUtility.Info("RobotHFT", "已开始运行");

        }

        #region Init Start
        private void Init()
        {
            config = Config.LoadConfig();
            coinConfigs = CoinConfig.Load();
            info = new HFTInfo(config);
            Symbols = new List<string>();
            Symbol = config.Symbol;
            Platform = config.Platform;
            robotMarket = new Robot_Market(Platform);
            robotTrade = new Robot_Trade(coinConfigs, config, Platform, config.ApiKey, config.SecretKey, new List<string>() { Symbol });
            robotSession = new Robot_Session(robotTrade, robotMarket, info);
            robotSession.SessionEvent += RobotHFT_SessionEvent;
            robotCurrent = new Robot_Current(robotTrade, robotSession, info);
            robotCurrent.CurrentEvent += RobotHFT_CurrentEvent; ;
            robotReport = new Robot_Report(robotTrade, robotSession, robotMarket, info);
        }


        private void Start(bool firststart = false)
        {

            //撤消当前挂单
            string[] cpStrs = config.CoinPairs.Split(',');
            foreach (string str in cpStrs)
            {
                string[] pair = str.Split(':');
                Symbols.Add(pair[0]);
                ClearAllCurrentOrders(pair[0]);
            }
            Thread.Sleep(2000);
            //市价平衡资源
            ResetAccount(firststart);
            Thread.Sleep(2000);
            //初始化已成交订单缓存记录
            robotTrade.InitFilledOrders(Symbol);
            Thread.Sleep(1000);
            //全量挂单
            HangOrders();

        }
        /// <summary>
        /// 撤消当前挂单
        /// </summary>
        /// <returns></returns>
        private int ClearAllCurrentOrders(string symbol)
        {
            Stopwatch stopwatch_cancel = new Stopwatch();
            stopwatch_cancel.Start();
            int count = robotTrade.ClearCurrentOrders(symbol);

            while (count == -1)
            {
                count = robotTrade.ClearCurrentOrders(symbol);
                Thread.Sleep(1000);
            }
            stopwatch_cancel.Stop();
            if (count > 0) Thread.Sleep(2000);
            robotSession.ClearSessionAllOrders();
            Log4NetUtility.Info("ClearAllCurrentOrders", $"撤单当前委托 {count}个， 共用时:{ stopwatch_cancel.ElapsedMilliseconds}");
            return count;
        }
        /// <summary>
        /// 市价平衡资源
        /// </summary>
        /// <param name="firststart"></param>
        private void ResetAccount(bool firststart = false)
        {
            string side = "";
            decimal reset_amt = 0;
            var coininfo = coinConfigs.FirstOrDefault(a => a.Platform == Platform && a.Symbol == Symbol);
            var ticker = GetTicker();
            var account = GetAccount();
            if (firststart && config.TradeQty != null && config.TradeQty != 0)
            {
                info.TradeQty = (decimal)config.TradeQty;
            }
            else
            {
                decimal price_now = ticker.last;
                decimal net = account.GetNet(this.info.Symbol, price_now);
                int packetNum = (this.info.OrderQty + 3) * 2;
                decimal newTradeQTY = coininfo.FormatAmount2D(net / price_now / packetNum);
                this.info.TradeQty = newTradeQTY;
            }

            decimal coin = account.GetFreeCoin(Symbol);
            decimal coinUSDT = account.GetFreeFund();
            var needcoin = this.info.TradeQty * (this.info.OrderQty + 2);
            var needfund = needcoin * ticker.sell;
            coinUSDT = coininfo.FormatAmount2D(coinUSDT);
            coin = coininfo.FormatAmount2D(coin);
            Log4NetUtility.Info("初始资源", $"FreeFund {coininfo.CoinB}:{coinUSDT}  FreeCoin {coininfo.CoinA}:{coin} ");

            decimal buy_qty = needcoin - coin;
            if (buy_qty > 1 / coininfo.AmountLimit)
            {
                reset_amt = buy_qty;
                side = "buy";
            }
            decimal sell_qty = (needfund - coinUSDT) / ticker.sell;
            if (sell_qty > 1 / coininfo.AmountLimit)
            {
                reset_amt = sell_qty;
                side = "sell";
            }
            Log4NetUtility.Info("初始资源", $"Needfund {coininfo.CoinB}:{coininfo.FormatAmount2D(needfund)}  Needcoin {coininfo.CoinA}:{coininfo.FormatAmount2D(needcoin)}  buy_qty:{coininfo.FormatAmount2D(buy_qty)}  sell_qty:{coininfo.FormatAmount2D(sell_qty)} reset_amt:{coininfo.FormatAmount2D(reset_amt)}");
            if (side != "" && reset_amt != 0)
            {
                resetAccount(side, reset_amt);
            }
            if (firststart)
            {
                ticker = GetTicker();
                account = GetAccount();
                decimal Open_Price = ticker.last;
                decimal net = account.GetNet(coininfo.Symbol, Open_Price);
                info.Open_Price = info.Open_Price == 0 ? Open_Price : info.Open_Price;
                info.Open_Fund = info.Open_Fund == 0 ? net : info.Open_Fund;
                info.Open_Coin = account.GetFreeCoin(Symbol) + account.GetFreezedCoin(Symbol);
                info.lastReStartTime = DateTime.Now;
                info.resetCount = 0;
                Log4NetUtility.Info("初始资源", $"账户信息获取完成。 可用资金 :{account.GetFreeFund().ToString("#0.0000")}  可用币:{account.GetFreeCoin(Symbol).ToString("#0.0000")},净资金:{account.GetNet(Symbol, ticker.last).ToString("#0.0000")}");
            }
        }
        //private void SetTradeQty(bool firststart = false)
        //{
        //    if (firststart && config.TradeQty != null && config.TradeQty != 0)
        //    {
        //        info.TradeQty = (decimal)config.TradeQty;
        //    }
        //    else
        //    {
        //        var info = coinConfigs.FirstOrDefault(a => a.Platform == Platform && a.Symbol == Symbol);
        //        var ticker = GetTicker();
        //        var account = GetAccount();

        //        decimal price_now = ticker.last;
        //        decimal net = account.GetNet(this.info.Symbol, price_now);
        //        int packetNum = (this.info.OrderQty + 3) * 2;
        //        decimal newTradeQTY = info.FormatAmount2D(net / price_now / packetNum);
        //        this.info.TradeQty = newTradeQTY;
        //    }
        //}
        private void resetAccount(string side, decimal resetAmt)
        {
            Stopwatch stopwatch_market = new Stopwatch();
            stopwatch_market.Start();
            bool result_market = false;
            for (int i = 0; i < 3 && !result_market; i++)
            {
                var ticker = GetTicker();

                decimal price = side == "buy" ? ticker.sell : ticker.buy;
                //Trade trade = robotTrade.Trade(Symbol, side, price, resetAmt);
                Trade trade = robotTrade.MarketTrade(Symbol, side, price, resetAmt);
                result_market = trade.result;
                if (result_market)
                {
                    stopwatch_market.Stop();
                    //UpdateMarketFee(trade.order_id);

                    Log4NetUtility.Info("初始资源", $"《市价{side}》，当前Price:{ticker.last.ToString("#0.00")}，Deal数量:{resetAmt.ToString("#0.0000")}，市价用时:{stopwatch_market.ElapsedMilliseconds}");
                    Thread.Sleep(3000);
                    break;
                }
                else
                {
                    Thread.Sleep(1000);
                    if (trade.error_code.Contains("1002"))
                    {
                        Thread.Sleep(2000 * i);
                        //i -= 1;

                    }
                }
            }

        }
        /// <summary>
        /// 全仓挂单
        /// </summary>
        private void HangOrders()
        {
            try
            {
                #region 新缓存列表 newSessionOrders
                List<Order> newSessionOrders = new List<Order>();
                decimal newFloatPrice = GetStandardPrice(GetTicker().last);
                info.floatPrice = newFloatPrice;
                info.buyOrderCount = info.sellOrderCount = info.OrderQty;
                //info.ShockCount_tmp = info.UpCount_tmp = info.DownCount_tmp = 0;
                for (int i = 1; i <= info.buyOrderCount; i++)
                {
                    decimal orderprice = newFloatPrice - i * info.SpanPrice;
                    Order order = new Order() { price = orderprice, amount = info.TradeQty, type = "buy" };
                    newSessionOrders.Add(order);
                }
                for (int i = 1; i <= info.sellOrderCount; i++)
                {
                    decimal orderprice = newFloatPrice + i * info.SpanPrice;
                    Order order = new Order() { price = orderprice, amount = info.TradeQty, type = "sell" };
                    newSessionOrders.Add(order);
                }

                #endregion
                #region 挂单、缓存
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                robotSession.ClearSessionAllOrders();
                foreach (var addOrder in newSessionOrders)
                {
                    TradeOrder(addOrder);
                    Thread.Sleep(250);
                }
                //Parallel.ForEach(newSessionOrders, addOrder =>
                //{
                //    TradeOrder(addOrder);
                //});
                stopwatch.Stop();
                Log4NetUtility.Info("HangOrders", robotSession.LogSring());
                Log4NetUtility.Info("HangOrders", $"Order中间价:{newFloatPrice}。交易总用时(毫秒):{stopwatch.ElapsedMilliseconds}");
                #endregion
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("HangOrders", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("HangOrders", e);
            }
        }
        private decimal GetStandardPrice(decimal currentPrice)
        {
            decimal temp = currentPrice;
            if (info.SpanPrice <= 0.5m)
            {
                temp = Math.Round(temp * 2) / 2;
            }
            else
            {
                temp = Math.Round(temp);
            }
            decimal priceDiff = info.SpanPrice * config.PriceRate ?? 0;
            return temp - priceDiff;
        }

        #endregion

        #region Event process 

        private void RobotHFT_SessionEvent(object sender, SessionEventArgs e)
        {
            try
            {
                Log4NetUtility.Debug($"SessionEvent:{e.EventType} {e.OrderList?.FirstOrDefault()?.type} {e.OrderList?.FirstOrDefault()?.price.ToString("0.0000")}");
                switch (e.EventType)
                {
                    case SessionEventType.Normal:
                        break;
                    case SessionEventType.DoubleOrder:
                        CancelOrders(e.OrderList);
                        break;
                    case SessionEventType.ErrPrice:
                        CancelOrders(e.OrderList);
                        break;
                    case SessionEventType.MoreOrder:
                        CancelOrders(e.OrderList);
                        break;
                    case SessionEventType.LessOrder:
                        TradeOrder(CreateLessOrder());
                        break;
                    case SessionEventType.LostOrder:
                        TradeOrder(e.OrderList?.FirstOrDefault());
                        break;
                    case SessionEventType.Reset:
                        ReSetOrder();
                        break;

                }
            }
            catch (Exception ex)
            {
                Log4NetUtility.Error("SessionEvent", Utils.Exception2String(ex));

            }
        }
        private void RobotHFT_CurrentEvent(object sender, CurrentEventArgs e)
        {
            try
            {
                //Log4NetUtility.Debug($"CurrentEvent:{e.EventType} {e.OrderList?.FirstOrDefault()?.type} {e.OrderList?.FirstOrDefault()?.price.ToString("0.0000")}");
                switch (e.EventType)
                {
                    case CurrentEventType.OrderFilled:
                        var filledorder = (Order)sender;
                        var neworder = e.OrderList.FirstOrDefault();
                        if (filledorder != null && neworder != null)
                            CurrentOrderFilled(filledorder, neworder);
                        break;
                    case CurrentEventType.LostOrders:
                        var lostOrders = e.OrderList;
                        if (lostOrders != null)
                            CurrentLostOrders(lostOrders);
                        break;
                    case CurrentEventType.DirtyOrders:
                        var dirtyOrders = e.OrderList;
                        if (dirtyOrders != null)
                            CurrentDirtyOrders(dirtyOrders);
                        break;
                    case CurrentEventType.LittleTrade:
                        ReSetOrder();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log4NetUtility.Error("CurrentEvent", Utils.Exception2String(ex));

            }

        }

        #region CurrentEvent （Filled、Lost、Dirty）
        /// <summary>
        /// 成交挂单
        /// </summary>
        /// <param name="filledOrder"></param>
        /// <param name="newOrder"></param>
        private void CurrentOrderFilled(Order filledOrder, Order newOrder)
        {
            var trade = TradeOrder(newOrder);
            if (trade.result && !string.IsNullOrEmpty(trade.order_id))
            {
                #region 反向挂单成功，处理缓存
                newOrder.order_id = trade.order_id;
                newOrder.create_date = Utils.GetUtcTimeDec();
                robotSession.AddUpdateSessionOrder(newOrder);
                filledOrder.status = "2";
                robotSession.RemoveSessionOrder(filledOrder);
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
                int buynum = robotSession.CountOrder(true);
                int sellnum = robotSession.CountOrder(false);
                string type = filledOrder.type == "sell" ? "+B" : "-S";
                Log4NetUtility.Debug($"OrderFilled {type} [{newOrder.price.ToString("#0.00")}] ID: {Utils.ShortID(trade.order_id)} S:[{sellnum}] B:[{buynum}] ");
                #endregion
            }
        }
        private void CurrentLostOrders(List<Order> lostOrders)
        {
            foreach (var lost in lostOrders)
            {
                robotSession.AddUpdateSessionOrder(lost);
                Log4NetUtility.Debug($"CurrentEvent,Add lostOrders Price:{lost.price.ToString("#0.00")} Id:{Utils.ShortID(lost.order_id)}");
            }
        }
        private void CurrentDirtyOrders(List<Order> dirtyOrders)
        {
            foreach (var dirtyOrder in dirtyOrders)
            {
                var order = robotTrade.GetOrders(Symbol, dirtyOrder.order_id);
                if (order.result && order.orders.Count != 0)
                {
                    Order o = order.orders.FirstOrDefault();
                    if (o.status == "filled")
                    {
                        robotSession.RemoveSessionOrder(dirtyOrder);
                        Log4NetUtility.Debug($"CurrentEvent,Remove dirtyOrders {dirtyOrder.type} Price:{dirtyOrder.price.ToString("#0.00")} Id:{Utils.ShortID(dirtyOrder.order_id)} status:{o.status}");
                    }
                }
            }
        }
        #endregion

        #region SessionEvent （ReSetOrder）
        /// <summary>
        /// 全量平仓
        /// </summary>
        private void ReSetOrder()
        {
            try
            {
                if (resetflag) return;
                resetflag = true;
                robotSession.Stop();
                robotCurrent.Stop();

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Log4NetUtility.Info("SessionEvent", "************************* 全量平仓<开始> ************************");
                info.resetCount++;
                info.lastReStartTime = DateTime.Now;
                info.ResetTimes.Add(Utils.GetUtcTimeDec());
                //CheckTroppoReset(info.ResetTimes, info.ShockTimes, info.lastReStartTime);


                var coin = coinConfigs.FirstOrDefault(a => a.Symbol == Symbol);
                info.SpanPrice = GetSpanPrice(coin.BasePriceWidth);
                Start();
                stopwatch.Stop();


                #region 清除计数缓存
                decimal timeDoub = Utils.GetUtcTimeDec(-60 * 30);
                info.ShockTimes.RemoveAll(a => a <= timeDoub);
                info.ResetTimes.RemoveAll(a => a <= timeDoub);

                int scount = info.ShockTimes.Count - 800;
                int rcount = info.ResetTimes.Count - 100;
                if (scount > 0)
                    info.ShockTimes.RemoveRange(0, scount);
                if (rcount > 0)
                    info.ResetTimes.RemoveRange(0, rcount);
                #endregion
                //info.ShockCount_tmp = info.UpCount_tmp = info.DownCount_tmp = 0;
                Log4NetUtility.Info("SessionEvent", $"*************************全量平仓<结束>耗时:{stopwatch.ElapsedMilliseconds}******************");

            }
            catch (Exception e)
            {
                Log4NetUtility.Error("SessionEvent", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("SessionEvent", e);

            }
            finally
            {
                resetflag = false;
                robotSession.Run();
                robotCurrent.Run();

            }
        }
        private Order CreateLessOrder()
        {
            var account = GetAccount();
            var ticker = GetTicker();
            decimal price = 0;

            string type = "";
            if (account.GetFreeCoin(Symbol) * ticker.last > account.GetFreeFund())
            {
                price = robotSession.GetEagePrice(false) + info.SpanPrice;
                type = "sell";
            }
            else
            {
                price = robotSession.GetEagePrice(false) - info.SpanPrice;
                type = "buy";
            }
            Order order = new Order() { price = price, amount = info.TradeQty, type = type };

            return order;

        }
        #endregion

        #endregion

        #region trade helper
        private Trade TradeOrder(Order order)
        {
            if (order == null) return null;
            Trade trade = robotTrade.Trade(Symbol, order.type, order.price, order.amount);
            for (int i = 0; i < 3; i++)
            {
                if (trade.result && !string.IsNullOrEmpty(trade.order_id))
                {
                    order.order_id = trade.order_id;
                    order.create_date = Utils.GetUtcTimeDec();
                    robotSession.AddUpdateSessionOrder(order);
                    Log4NetUtility.Debug($"New 《{order.type}》 Price:{order.price.ToString("#0.0000")} id:{Utils.ShortID(order.order_id)}");

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
                        var ticker = robotMarket.GetTicker(Symbol);
                        decimal price = side == "buy" ? ticker.sell : ticker.buy;

                        Trade tradeM = robotTrade.MarketTrade(Symbol, side, price, order.amount);
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
        private void CancelOrders(List<Order> orders)
        {
            foreach (var order in orders)
            {
                CancelOrder(order);
            }
        }
        private void CancelOrder(Order order)
        {
            CancelOrder cancel = robotTrade.CancelOrder(Symbol, order.order_id);
            if (cancel.result)
            {
                robotSession.RemoveSessionOrder(order);
                //DbHelper.DBSaveChange(doubleorder, "UpDate");
                Log4NetUtility.Info("RobotHFT", "Canceled Order Success. id:" + Utils.ShortID(order.order_id) + " Price:" + order.price);
            }
            else
            {
                string errmsg = cancel.error_code + "  " + cancel.msg;
                if (cancel.error_code.Contains("3008"))
                {
                    robotSession.RemoveSessionOrder(order);
                    //sessionOrders.Remove(doubleorder);
                    ////DbHelper.CreateInstance().RemoveOrder(doubleorder);
                    errmsg = "已清除脏数据Order。id:" + Utils.ShortID(order.order_id) + " Price:" + order.price;
                }
                else
                {
                    //errmsg = "撤销重复Order失败。id:" + Utils.ShortID(doubleorder.order_id) + " msg:" + cancel.error_code + cancel.msg;
                }
                Log4NetUtility.Info("CancelOrder", errmsg);
                //DbHelper.CreateInstance().AddErrInfo("缓存单验证", errmsg);

            }


        }
        private Ticker GetTicker()
        {
            var ticker = robotMarket.GetTicker(Symbol);
            while (ticker == null || ticker.last == 0)
            {
                Thread.Sleep(1500);
                ticker = robotMarket.GetTicker(Symbol);
            }
            return ticker;
        }
        private Account GetAccount()
        {
            var account = robotTrade.CurrentAccount;
            while (account == null || account.balances.Count == 0)
            {
                Thread.Sleep(1500);
                account = robotTrade.CurrentAccount;
            }
            return account;
        }
        #endregion

        /// <summary>
        /// 是否调参（平仓过多）
        /// </summary>
        /// <param name="ResetTimes"></param>
        /// <param name="ShockTimes"></param>
        /// <param name="lastReStartTime"></param>
        /// <returns></returns>
        public void CheckTroppoReset(List<decimal> ResetTimes, List<decimal> ShockTimes, DateTime lastReStartTime)
        {
            try
            {
                #region 1 分钟内平仓大于等于2次
                decimal minu = Utils.GetUtcTimeDec(-1 * 60);
                ////最后一次震荡1分钟内，不检测返回
                if (ResetTimes.Max() > minu) return;
                int resetcount = ResetTimes.Count(a => a > minu);
                //一分钟内平仓大于2次，振幅增大
                if (resetcount >= 2)
                {
                    info.SpanPrice = info.SpanPrice * 2;
                    Log4NetUtility.Info("判断平仓过频繁", "大包/大振幅时.1 分钟内平仓大于等于2次！ SpanPrice * 2:{info.SpanPrice}");
                    return;
                }
                #endregion
                #region 2 分钟内平仓大于等于2次(调参1分钟内忽略)
                if (lastReStartTime.AddMinutes(1) < DateTime.Now)
                {
                    minu = Utils.GetUtcTimeDec(-2 * 60);
                    resetcount = ResetTimes.Count(a => a > minu);
                    if (resetcount >= 2)
                    {
                        info.SpanPrice = info.SpanPrice * 2;
                        Log4NetUtility.Info("判断平仓过频繁", "大包/大振幅时.2 分钟内平仓大于等于2次！ SpanPrice * 2:{info.SpanPrice}");
                        return;
                    }
                }
                #endregion
                #region //3 分钟内平仓大于等于5次(调参2分钟内忽略)
                //if (lastReStartTime.AddMinutes(2) < DateTime.Now)
                //{
                //    minu = Utils.GetDateTimeDec(-3 * 60);
                //    resetcount = ResetTimes.Count(a => a > minu);
                //    if (resetcount >= 5)
                //    {
                //        info.SpanPrice = info.SpanPrice * 2;

                //        Log4NetUtility.Info("判断平仓过频繁", "大包/大振幅时.3 分钟内平仓大于等于5次！ SpanPrice * 2:{info.SpanPrice}");
                //        return;
                //    }
                //}
                #endregion
                #region 5 分钟内平仓大于等于3次(调参2分钟内忽略)
                if (lastReStartTime.AddMinutes(2) < DateTime.Now)
                {
                    minu = Utils.GetUtcTimeDec(-5 * 60);
                    resetcount = ResetTimes.Count(a => a > minu);
                    if (resetcount >= 3)
                    {
                        info.SpanPrice = info.SpanPrice * 2;

                        Log4NetUtility.Info("判断平仓过频繁", "大包/大振幅时.5 分钟内平仓大于等于3次！ SpanPrice * 2:{info.SpanPrice}");
                        return;
                    }
                }
                #endregion
                #region //7 分钟内平仓大于等于7次
                //minu = Utils.GetDateTimeDec(-8 * 60);
                //resetcount = ResetTimes.Count(a => a > minu);
                //if (resetcount >= 7)
                //{
                //    info.SpanPrice = info.SpanPrice * 2;

                //    Log4NetUtility.Info("判断平仓过频繁", "大包/大振幅时.7 分钟内平仓大于等于7次！ SpanPrice * 2:{info.SpanPrice}");
                //    return;
                //}
                #endregion
                #region 10分钟内平仓大于等于4次
                minu = Utils.GetUtcTimeDec(-10 * 60);
                resetcount = ResetTimes.Count(a => a > minu);
                if (resetcount >= 4)
                {
                    info.SpanPrice = info.SpanPrice * 2;

                    Log4NetUtility.Info("判断平仓过频繁", "大包/大振幅时.10分钟内平仓大于等于4次！ SpanPrice * 2:{info.SpanPrice}");
                    return;
                }
                #endregion
                #region 交易/平仓< 25
                int count = ResetTimes.Count();
                //if (count >= 2 && info.SpanPrice < 1)
                if (count >= 2)
                {
                    //decimal lasttime = ResetTimes.OrderBy(a => a).ToList()[count - 2];
                    decimal lasttime = ResetTimes.Max();
                    int scount = ShockTimes.Count(a => a > lasttime);
                    if (scount < 25)
                    {
                        info.SpanPrice = info.SpanPrice * 2;
                        Log4NetUtility.Info("判断平仓过频繁", $"交易/平仓:{scount}<25. SpanPrice * 2:{info.SpanPrice}");
                    }
                }
                #endregion
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("CheckTroppoReset", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("CheckTroppoReset", e);

            }
        }

        private decimal GetSpanPrice(decimal format = 0.1m)
        {
            if (info.SpanPrice <= 0.1m) return info.SpanPrice;
            string kr1 = "M1";
            string kr2 = "M15";

            var kline1 = robotMarket.GetKlines(Symbol, kr1) ?? new List<Kline>();
            var kline2 = robotMarket.GetKlines(Symbol, kr2) ?? new List<Kline>();

            decimal width1 = GetKlinePriceWidth(kline1, info.OrderQty, format, 5, 1.5m);
            decimal width15 = GetKlinePriceWidth(kline2, info.OrderQty, format, 5, 1.0m);

            decimal width = Math.Max(width1, width15);

            if (Math.Abs(width - info.SpanPrice) >= info.SpanPrice / 4)
            {
                if (info.Symbol.ToLower().Contains("btcusdt") && width <= 1m)
                    width = 1;
                info.SpanPrice = width;
            }
            Log4NetUtility.Debug($"kline{kr1} width : {width1}  kline{kr2} width : {width15} SpanPrice:{info.SpanPrice}->{width}");
            return info.SpanPrice;
        }

        private decimal GetKlinePriceWidth(List<Kline> klines, int orderQty, decimal format, int count = 5, decimal rTimes = 1)
        {
            var list = klines.OrderBy(a => a.id).Skip(klines.Count - count).ToList();
            decimal max = list.Max(a => a.high);
            decimal min = list.Min(a => a.low);

            var priceWidth = max - min;
            Log4NetUtility.Debug($"max : {max} min : {min} width: {priceWidth}");
            var width = priceWidth / orderQty * rTimes;
            int f = (int)(1 / format);
            f = f < 1 ? 1 : f;
            width = Math.Round(width * f) / f;
            return width < format ? format : width;

        }
    }
}
