using CoreLibrary;
using CoreLibrary.DB;
using Lq.Log4Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TradeLibrary.Model;
using TradeRobot;

namespace HFTRobot
{
    public class Robot_Current
    {
        #region 
        Robot_Trade robotTrade;
        Robot_Session robotSession;

        string symbol;
        HFTInfo info;
        System.Timers.Timer timer;

        //bool checkRunning = false;
        long planDoTime = -1;
        object planObjectLock = new object();
        //List<Order> sessionOrders;
        List<Order> currentOrder;
        List<Order> filledOrders;
        #endregion

        public List<Order> FilledSessionOrders { set; get; }
        public bool IsRunning { set; get; }
        public event EventHandler<CurrentEventArgs> CurrentEvent;

        public Robot_Current(Robot_Trade robotTrade, Robot_Session robotSession, HFTInfo info)
        {
            this.robotTrade = robotTrade;
            this.robotSession = robotSession;
            this.info = info;
            symbol = info.Symbol;
            timer = new System.Timers.Timer(1000);
            timer.Elapsed += Timer_Elapsed;
            IsRunning = false;
            FilledSessionOrders = new List<Order>();
            //sessionOrders = new List<Order>();
            currentOrder = new List<Order>();
            filledOrders = new List<Order>();
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


        void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            long dotime = DateTime.Now.Ticks;
            DoPlan(dotime);
        }

        private void DoPlan(long dotime)
        {
            #region ObjectLock
            if (planDoTime == -1)
            {
                lock (planObjectLock)
                {
                    if (planDoTime == -1)
                    {
                        planDoTime = dotime;
                    }
                }
            }
            #endregion
            if (planDoTime == dotime)
            {
                try
                {
                    GetData();
                    // 当前订单不在缓存中
                    robotSession.CheckLostOrders(currentOrder);
                    //遍历缓存订单对比当前订单，处理成交订单
                    if (filledOrders.Count != 0)
                    {
                        UpdateFilledSessionOrders();
                        robotSession.FilledSessionOrders(FilledSessionOrders, currentOrder);
                    }
                    if (CheckLittleTrade(info.ResetTimes, info.ShockTimes))
                    {
                        CurrentEvent?.Invoke(null, new CurrentEventArgs(CurrentEventType.LittleTrade, null, null));
                    }
                    LimitFilledSessionOrders();
                }
                #region catch finally
                catch (Exception e)
                {
                    ////DbHelper.CreateInstance().AddErrInfo("DoPlan", e);
                    Log4NetUtility.Error("DoPlan", Utils.Exception2String(e));
                    DbHelper.CreateInstance().AddError("DoPlan", e);
                }
                finally
                {
                    planDoTime = -1;
                }
                #endregion
            }
        }
        private void GetData()
        {
            //sessionOrders = robotSession.SessionOrders;
            currentOrder = robotTrade.CurrentOrders(symbol);
            filledOrders = robotTrade.FilledOrders(symbol);
            //无缓存订单，新建委托订单并缓存，返回
            //if (sessionOrders.Count == 0 || currentOrder.Count == 0)
            if (currentOrder.Count == 0)
            {
                throw (new Exception("current Order err."));
            }
        }
        private void UpdateFilledSessionOrders()
        {
            Parallel.ForEach(filledOrders, (order, loop) =>
            {
                if (order == null) return;
                if (!FilledSessionOrders.Exists(a => a != null && a.order_id == order.order_id))
                {
                    if (order == null) return;
                    FilledSessionOrders.Add(order);
                    decimal fee = order.fill_fees;
                    if (order.type == "buy")
                    {
                        fee = fee * order.price;
                    }
                    info.total_fees += fee;

                }
            });

        }
        private void LimitFilledSessionOrders()
        {
            FilledSessionOrders.RemoveAll(a => a == null);
            int filledSessionOrdersCount = FilledSessionOrders.Count;
            if (filledSessionOrdersCount > 2000)
            {
                FilledSessionOrders.RemoveRange(0, filledSessionOrdersCount - 2000);
            }
        }

        /// <summary>
        /// 是否调参（交易过少）
        /// </summary>
        /// <param name="ResetTimes"></param>
        /// <param name="ShockTimes"></param>
        /// <returns></returns>
        private bool CheckLittleTrade(List<decimal> ResetTimes, List<decimal> ShockTimes)
        {
            try
            {
                ResetTimes = ResetTimes ?? new List<decimal>();
                ShockTimes = ShockTimes ?? new List<decimal>();
                //已为最低配置，不可调0.25 10
                #region 获取统计数据 resetcount、shockcount
                //默认统计时间5分钟 1->0.5 
                int minutes = 5;
                //0.5振幅时统计10钟 0.5->0.25 
                if (info.SpanPrice <= info.Open_SpanPrice / 2)
                { minutes = 10; }
                //小于0.5振幅统计时间15分钟 10P->5P
                if (info.SpanPrice < info.Open_SpanPrice / 3)
                { minutes = 15; }
                //时间点minu
                decimal minu = Utils.GetUtcTimeDec(-minutes * 60);
                int resetcount = ResetTimes.Count(a => a > minu);
                int shockcount = ShockTimes.Count(a => a > minu);
                #endregion
                //1、若有平仓。不调参
                if (resetcount > 0) return false;
                //2、刚开始运行时间不足
                if (ShockTimes.Count == 0 || ShockTimes.Min() > minu) return false;
                //3、若震荡大于阈值，不算过小。不调参
                if (minutes == 5 && shockcount >= 2) return false;
                if (minutes == 10 && shockcount >= 5) return false;
                if (minutes == 15 && shockcount >= 7) return false;
                #region 获取行情数据 maxPrice、minPrice、diff、pdiff
                decimal diff = 0;
                decimal time = Utils.GetUtcTimeDec(-minutes * 60);
                List<Order> oreders = FilledSessionOrders.Where(a => a.create_date >= time).ToList();
                if (oreders == null || oreders.Count == 0)
                {
                    diff = 0;
                    Log4NetUtility.Info("行情平缓", $"{minutes}分钟内，{resetcount}次平仓，{shockcount}次震荡,SpanPrice:{info.SpanPrice}");
                    return true;

                }
                else
                {
                    decimal maxPrice = oreders.Max(a => a.price);
                    decimal minPrice = oreders.Min(a => a.price);
                    diff = maxPrice - minPrice;//价差
                }
                decimal pdiff = info.SpanPrice * info.OrderQty * 2;//包差
                #endregion
                //4、行情价差小于等于包范围的1/3时。调参
                if (diff <= pdiff / 3)
                {
                    if (info.SpanPrice >= 0.25m)//非最小振幅
                    {
                        //降振幅
                        //info.SpanPrice = info.SpanPrice / 2;
                        //if (info.SpanPrice == 0.5m)//0.5->0.25时先增加包数
                        //{
                        //    result.PacketNumResult = AnalyzePropertyResultEnum.Up;
                        //    result.TradeQTYResult = AnalyzePropertyResultEnum.Down;
                        //}
                        Log4NetUtility.Info("行情平缓", string.Format("{0}分钟内，{1}次平仓，{2}次震荡,行情价差{3}，包差{4},SpanPrice:{5}", minutes, resetcount, shockcount, diff, pdiff, info.SpanPrice));
                        return true;
                    }
                    //else//最小振幅0.25.降包、增量
                    //{
                    //result.PacketNumResult = AnalyzePropertyResultEnum.Down;
                    //result.TradeQTYResult = AnalyzePropertyResultEnum.Up;
                    //}
                    //Log4NetUtility.Info("行情平缓", string.Format("{0}分钟内，{1}次平仓，{2}次震荡,行情价差{3}，包差{4},SpanPrice:{5}", minutes, resetcount, shockcount, diff, pdiff, info.SpanPrice));
                }
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("行情平缓", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("CheckLittleTrade", e);
            }
            return false;

        }

    }
}
