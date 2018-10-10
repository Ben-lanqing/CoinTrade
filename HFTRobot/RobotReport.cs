/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	RobotReport
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/28 14:10:07
*Description:	尚未编写描述
*Update History:
*<Author> <date> <Version> <Description>
*更新的版本的作者等信息，新的版本信息往下
*/
using CoreLibrary;
using CoreLibrary.DB;
using Lq.Log4Net;
using MarketLibrary.Model;
using MarketRobot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeLibrary.Model;
using TradeRobot;

namespace HFTRobot
{
    public class Robot_Report
    {
        public bool IsRunning { set; get; }

        /// <summary>
        /// 运行时间
        /// </summary>
        public TimeSpan HourOpenTimeSpans { set; get; }

        /// <summary>
        /// 小时开始时净利润
        /// </summary>
        public decimal HourOpenEarn { set; get; }
        /// <summary>
        /// 小时开始时震荡次数
        /// </summary>
        public long OpenShockCount { set; get; }
        /// <summary>
        /// 小时开始时平仓次数
        /// </summary>
        public long OpenResetCount { set; get; }

        System.Timers.Timer timer;
        Robot_Trade robotTrade;
        Robot_Session robotSession;
        Robot_Market robotMarket;
        HFTInfo info;

        public Robot_Report(Robot_Trade robotTrade, Robot_Session robotSession, Robot_Market robotMarket, HFTInfo info)
        {
            HourOpenEarn = 0;
            OpenShockCount = OpenResetCount = 0;
            HourOpenTimeSpans = TimeSpan.Zero;
            timer = new System.Timers.Timer(60 * 1000);
            timer.Elapsed += Timer_Elapsed; ;
            this.robotTrade = robotTrade;
            this.robotSession = robotSession;
            this.robotMarket = robotMarket;
            this.info = info;

        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            string reportStr = "";
            try
            {
                var account = robotTrade.CurrentAccount;
                var ticker = robotMarket.GetTicker(info.Symbol);
                if (DateTime.Now.Minute == 0)
                {
                    //整点日志
                    reportStr = GetReport(info, account, ticker, true);
                }
                else
                {
                    //分钟日志
                    reportStr = GetReport(info, account, ticker, false);
                }

                Log4NetUtility.Info("ReportDetail ", reportStr);
            }
            catch (Exception ex)
            {
                Log4NetUtility.Error("ReportTimer", Utils.Exception2String(ex));
            }

        }

        public string GetReport(HFTInfo info, Account account, Ticker ticker, bool hourFlag = false)
        {
            try
            {
                #region 
                TimeSpan RunningTimeSpans = DateTime.Now - info.Open_Time;
                //string runningTime = string.Format("{0} {1}:{2}:{3}", RunningTimeSpans.Days, RunningTimeSpans.Hours, RunningTimeSpans.Minutes, RunningTimeSpans.Seconds);
                string runningTime = RunningTimeSpans.ToString(@"d\ hh\:mm\:ss");

                decimal net = account.GetNet(info.Symbol, ticker.last);
                if (info.Open_Fund == 0) info.Open_Fund = net;

                decimal total_Earn = net - info.Open_Fund;

                //decimal realnet = net + info.total_fees;
                decimal realnet = net;
                decimal realEarn = realnet - info.Open_Fund;
                //资金基数
                // decimal baseFund = info.TradeQty * (info.OrderQty + 2) * 2 * ticker.last;
                decimal baseFund = info.Open_Fund;
                //天数
                decimal total_dates = (decimal)RunningTimeSpans.TotalDays;/*(decimal)(DateTime.Now - info.Open_Time).TotalMinutes / 1440m*/;

                decimal totalEarn_Ave_Daily = total_dates == 0 ? 0 : total_Earn / total_dates;
                decimal totalEarn_Rate_Daily = totalEarn_Ave_Daily / baseFund * 100;
                decimal totalEarn_Rate_Year = totalEarn_Rate_Daily * 365;

                decimal realEarn_Ave_Daily = total_dates == 0 ? 0 : realEarn / total_dates;
                decimal realEarn_Rate_Daily = realEarn_Ave_Daily / baseFund * 100;
                decimal realEarn_Rate_Year = realEarn_Rate_Daily * 365;


                decimal hour_realearn = realEarn - HourOpenEarn;
                decimal hour_averealearn = 0;
                if (HourOpenTimeSpans != TimeSpan.Zero)
                {
                    double hour_minutes = (RunningTimeSpans - HourOpenTimeSpans).TotalMinutes;
                    decimal running_hours = (decimal)hour_minutes / 60m;
                    hour_averealearn = hour_realearn / running_hours * 24;
                }
                decimal hour_rate_daily = hour_averealearn / baseFund * 100;
                long hour_shockCount = info.dealCount - OpenShockCount;
                long hour_resetCount = info.resetCount - OpenResetCount;
                string hStr = "This";
                if (hourFlag)
                {
                    OpenShockCount = info.dealCount;
                    OpenResetCount = info.resetCount;
                    HourOpenEarn = realEarn;
                    HourOpenTimeSpans = RunningTimeSpans;
                    hStr = "Last";
                }
                //当前币数
                decimal coinCount = account.GetFreeCoin(info.Symbol) + account.GetFreezedCoin(info.Symbol);
                //decimal price_earn = (ticker.last - info.Open_Price) * coinCount;
                decimal trade_earn = (info.dealCount - 45 * info.resetCount) * info.TradeQty * info.SpanPrice;
                decimal price_earn = realEarn - trade_earn;

                decimal trade_earn_Ave_Daily = total_dates == 0 ? 0 : trade_earn / total_dates;
                decimal trade_earn_Rate_Daily = trade_earn_Ave_Daily / baseFund * 100;
                decimal trade_earn_Rate_Year = trade_earn_Rate_Daily * 365;

                //decimal reset_loss = price_earn + trade_earn - total_Earn - info.total_fees;
                int buy_num = robotSession.CountOrder(true);
                int sell_num = robotSession.CountOrder(false);
                #endregion

                #region 
                StringBuilder sb = new StringBuilder();
                sb.AppendLine();
                if (hourFlag)
                {
                    sb.AppendLine("*****************************************************************");
                    sb.AppendLine($"运行时间:   {Utils.Format(runningTime)} 开盘价:   {Utils.Format(info.Open_Price.ToString("#0.00"))} 当前价: {Utils.Format(ticker.last.ToString("#0.00"))}");
                    sb.AppendLine($"净资产:     {Utils.Format(realnet.ToString("#0.00"))} 振幅:     {Utils.Format(info.SpanPrice)}");
                    sb.AppendLine($"震荡:       {Utils.Format(info.dealCount)} 平仓:     {Utils.Format(info.resetCount)}");
                    sb.AppendLine($"小时震荡:   {Utils.Format(hour_shockCount)} 小时平仓: {Utils.Format(hour_resetCount)}");
                    sb.AppendLine($"净盈利  :   {Utils.Format("【" + realEarn.ToString("#0.0000") + "】")} 日盈利率: {Utils.Format(realEarn_Rate_Daily.ToString("#0.00") + "%")} 年化:   {Utils.Format(realEarn_Rate_Year.ToString("#0.00") + "%")}");
                    //sb.AppendLine($"交易盈利:  {Utils.Format("[" + trade_earn.ToString("#0.0000") + "]")} 日盈利率:  {Utils.Format("[" + trade_earn_Rate_Daily.ToString("#0.00") + "%" + "]")} 年化:  {Utils.Format(trade_earn_Rate_Year.ToString("#0.00") + "%")} ");
                    //sb.AppendLine($"价格盈利:  {Utils.Format("[" + price_earn.ToString("#0.0000") + "]")} 交易损失:  {Utils.Format(info.total_fees.ToString("#0.0000"))}");

                }
                sb.AppendLine("*****************************************************************");
                sb.AppendLine($"RunTime:    {Utils.Format(runningTime)} OpenPrice:   {Utils.Format(info.Open_Price.ToString("#0.00"))} TickerPrice: {Utils.Format(ticker.last.ToString("#0.00"))}");
                sb.AppendLine($"OpenFund:   {Utils.Format(info.Open_Fund.ToString("0.0000"))} NetFund:     {Utils.Format(net.ToString("0.0000"))} NetRealFund: {Utils.Format(realnet.ToString("0.0000"))}");
                sb.AppendLine($"TradeQty:   {Utils.Format(info.TradeQty)} SpanPrice:   {Utils.Format(info.SpanPrice)} OrderQty:    {Utils.Format(info.OrderQty)} FloatPrice:  {Utils.Format(info.floatPrice.ToString("0.00"))}");
                //sb.AppendLine($"FreeFund:   {Utils.Format(account.GetFreeFund().ToString("0.0000"))} FreezedFund: {Utils.Format(account.GetFreezedFund().ToString("0.0000"))} FreeCoin:    {Utils.Format(account.GetFreeCoin(info.Symbol).ToString("0.0000"))} FreezedCoin:  {Utils.Format(account.GetFreezedCoin(info.Symbol).ToString("0.0000"))}");
                //sb.AppendLine();
                //sb.AppendLine($"{hStr}Hour:   {Utils.Format(hour_realearn.ToString("#0.0000"))} HourEarnAve: {Utils.Format(hour_averealearn.ToString("#0.0000"))} HEarn_AveD:  {Utils.Format(hour_rate_daily.ToString("#0.0000"))}");
                sb.AppendLine($"HDealCount: {Utils.Format(hour_shockCount)} HResetCount: {Utils.Format(hour_resetCount)} DealCount:   {Utils.Format(info.dealCount)} ResetCount:  {Utils.Format(info.resetCount)}");
                //sb.AppendLine();
                sb.AppendLine($"Earn:       {Utils.Format(total_Earn.ToString("#0.0000"))} RateDaily:   {Utils.Format(totalEarn_Rate_Daily.ToString("#0.00") + "%")} RateYear:    {Utils.Format(totalEarn_Rate_Year.ToString("#0.00") + "%")}");
                //sb.AppendLine($"RealEarn:   {Utils.Format("[" + realEarn.ToString("#0.0000") + "]")} RRateDaily:  {Utils.Format("[" + realEarn_Rate_Daily.ToString("#0.00") + "%" + "]")} RRateYear:   {Utils.Format(realEarn_Rate_Year.ToString("#0.00") + "%")}");
                //sb.AppendLine($"TradeEarn:  {Utils.Format("[" + trade_earn.ToString("#0.0000") + "]")} TRateDaily:  {Utils.Format("[" + trade_earn_Rate_Daily.ToString("#0.00") + "%" + "]")} trade_earn_Rate_Year:  {Utils.Format(trade_earn_Rate_Year.ToString("#0.00") + "%")} ");
                //sb.AppendLine($"PriceEarn:  {Utils.Format("[" + price_earn.ToString("#0.0000") + "]")} TradeLoss:   {Utils.Format(info.total_fees.ToString("#0.0000"))}");
                    report report = new report();

                var LogOrders = robotSession.LogOrders();
                sb.AppendLine(LogOrders);
                var DBOrdersInfostr = DBOrdersInfo(info.Open_Time, ticker, ref report);
                sb.AppendLine(DBOrdersInfostr);

                sb.AppendLine("*****************************************************************");
                #region 保存数据库
                if (true)
                {
                    report.id = Utils.GetUtcTimeDec();
                    report.date = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    //report.Earn = total_Earn;
                    report.NetFund = net;
                    report.OpenFund = info.Open_Fund;
                    report.OpenPrice = info.Open_Price;
                    report.Open_Time = info.Open_Time;

                    report.OrderQty = info.OrderQty;
                    report.RateYear = totalEarn_Rate_Year;
                    report.runningTime = runningTime;
                    report.SpanPrice = info.SpanPrice;
                    report.TickerPrice = ticker.last;
                    report.TradeQty = info.TradeQty;
                    report.type = "minReport";
                    report.HResetCount= hour_resetCount;
                    report.DealCount = info.dealCount;
                    report.ResetCount = info.resetCount;
                    report.RealEarn = realEarn;
                    report.HDealCount = hour_shockCount;
                    report.LogOrders = LogOrders;
                    report.DBOrdersInfo = DBOrdersInfostr;

                    DbHelper.CreateInstance().AddReport(report);
                }
                #endregion
                return sb.ToString();
                #endregion
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("GetReport", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("GetReport", e);

                return null;
            }
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

        private string DBOrdersInfo(DateTime openTime, Ticker ticker,ref report report)
        {
            StringBuilder sb = new StringBuilder();
            //decimal startdate = Utils.GetUtcTimeDec(-(DateTime.Now - openTime).TotalSeconds);
            //var orders = DbHelper.CreateInstance().GetDBOrders(startdate);
            var orders = DbHelper.CreateInstance().GetDBOrders(openTime);
            string strOpen = DBOrdersInfo(orders, ticker,ref report);

            orders = orders.Where(a => a.createdate >= Utils.GetUtcTimeDec(-60 * 60 * 24)).ToList();
            string str24H = DBOrdersInfo(orders, ticker);

            orders = orders.Where(a => a.createdate >= Utils.GetUtcTimeDec(-60 * 60)).ToList();
            string strH = DBOrdersInfo(orders, ticker);
            return $"Open: {strOpen}24H:  {str24H}Hour: {strH}";


        }
        private string DBOrdersInfo(List<order> orders, Ticker ticker,ref report report)
        {
            var buyorders = orders.Where(a => a.side == "buy");
            var sellorders = orders.Where(a => a.side == "sell");

            decimal buyFund = buyorders.Sum(a => a.amount ?? 0 * a.price ?? 0);
            decimal sellFund = sellorders.Sum(a => a.amount ?? 0 * a.price ?? 0);

            var earnT = sellFund - buyFund;
            decimal buyfees = buyorders.Sum(a => a.fees ?? 0);
            decimal sellfees = sellorders.Sum(a => a.fees ?? 0);
            var fees = buyfees * ticker.last + sellfees;
            decimal total = earnT - fees;
            decimal rate = total / info.Open_Fund;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"buyOrders: {buyorders.Count()} sellOrders: {sellorders.Count()}");
            sb.AppendLine($"Earn：     [{total.ToString("0.0000")}] Rate：[{rate.ToString("p")}]");
            sb.AppendLine($"RealEarn:  {earnT.ToString("0.0000")}    TradeFees: {fees.ToString("0.0000")}");
            report.Earn = earnT;
            return sb.ToString();
        }
        private string DBOrdersInfo(List<order> orders, Ticker ticker)
        {
            var buyorders = orders.Where(a => a.side == "buy");
            var sellorders = orders.Where(a => a.side == "sell");

            decimal buyAmt = buyorders.Sum(a => a.amount ?? 0 * a.price ?? 0);
            decimal sellAmt = sellorders.Sum(a => a.amount ?? 0 * a.price ?? 0);

            var earn = sellAmt - buyAmt;
            decimal buyfees = buyorders.Sum(a => a.fees ?? 0);
            decimal sellfees = sellorders.Sum(a => a.fees ?? 0);
            var fees = buyfees * ticker.last + sellfees;
            decimal total = earn - fees;
            decimal rate = total / info.Open_Fund;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"buyOrders: {buyorders.Count()} sellOrders: {sellorders.Count()}");
            sb.AppendLine($"Earn：     [{total.ToString("0.0000")}] Rate：[{rate.ToString("p")}]");
            sb.AppendLine($"RealEarn:  {earn.ToString("0.0000")}    TradeFees: {fees.ToString("0.0000")}");

            return sb.ToString();
        }

    }
}
