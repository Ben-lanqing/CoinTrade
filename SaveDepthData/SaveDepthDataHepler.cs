using CoreLibrary;
using CoreLibrary.DB;
using CoreLibrary.Model;
using Lq.Log4Net;
using MarketLibrary.Model;
using MarketRobot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SaveDepthData
{
    public class SaveDepthDataHepler
    {
        Robot_Market robotMarket;

        List<CoinConfig> coinConfigs;
        string Platform;

        public SaveDepthDataHepler()
        {
            Platform = "FC";
            Init();
        }
        private void Init()
        {
            coinConfigs = CoinConfig.Load();
            robotMarket = new Robot_Market(Platform);

            var list = coinConfigs.Where(a => a.Platform.ToLower() == Platform.ToLower()).Select(a => a.Symbol).ToList();
            robotMarket.Run(new List<string>() { "btcusdt" });
            decimal lastTicker = 0;
            int lastflag = 0;

            decimal count = 0;
            decimal guessR = 0;
            while (true)
            {
                try
                {
                    Thread.Sleep(1000 * 10);
                    if (!robotMarket.Running)
                        continue;
                    var depth = robotMarket.GetDepth("btcusdt");
                    var ticker = robotMarket.GetTicker("btcusdt");
                    #region

                    depth item = new depth();

                    item.date = DateTime.Now;
                    item.id = Utils.GetUtcTimeDec();
                    item.ticker = ticker?.last;
                    item.json = ModelHelper<Depth>.Model2Json(depth); ;
                    if (item.ticker != 0 && !string.IsNullOrEmpty(item.json))
                        DbHelper.CreateInstance().AddDepth(item);

                    #endregion
                    StringBuilder sb = new StringBuilder();
                    if (depth != null && depth.asks.Count > 0 && depth.bids.Count > 0)
                    {
                        var asks = depth.asks.ToArray();
                        var bids = depth.bids.ToArray();
                        var firstA = asks.FirstOrDefault();
                        var firstB = bids.FirstOrDefault();
                        string ss = "----";
                        if (ticker.last > lastTicker * 1.02m) ss = "up";
                        if (ticker.last < lastTicker * 0.98m) ss = "down";

                        string sss = "Guess Wrong!";
                        if (ss == "up" && lastflag == 1)
                        {
                            sss = "Guess Right!"; guessR++;
                        }
                        if (ss == "down" && lastflag == 2)
                        {
                            sss = "Guess Right!"; guessR++;
                        }
                        if (lastflag == 0)
                        {
                            sss = "";
                        }

                        sb.AppendLine($"depth asks:{asks.Count()} bids:{bids.Count()}");
                        sb.AppendLine($"lastTicker :{lastTicker.ToString("0.00")} thisTicker:{ticker.last.ToString("0.00")} price go {ss} {sss}");
                        var rate = count == 0 ? 1 : guessR / count;
                        sb.AppendLine($"GuessCount :{count}  Right:{guessR} Rate:{rate.ToString("p")}");

                        lastTicker = ticker.last;
                        var aSum = asks.Sum(a => a[1]);
                        var bSum = bids.Sum(a => a[1]);
                        sb.AppendLine($"Amount Sum asks:{aSum.ToString("0.000")}   bids:{bSum.ToString("0.000")}");
                        var avA = asks.Sum(a => a[0] * a[1]) / aSum;
                        var avB = bids.Sum(a => a[0] * a[1]) / bSum;
                        sb.AppendLine($"Price Ave  asks:{avA.ToString("0.00")}   bids:{avB.ToString("0.00")}");


                        var lastA = asks.LastOrDefault();
                        var lastB = bids.LastOrDefault();
                        sb.AppendLine($"depth asks from:{lastA[0].ToString("0.000")},{lastA[1].ToString("0.000")} to:{firstA[0].ToString("0.000")},{firstA[1].ToString("0.000")}");
                        sb.AppendLine($"depth bids from:{firstB[0].ToString("0.000")},{firstB[1].ToString("0.000")} to:{lastB[0].ToString("0.000")},{lastB[1].ToString("0.000")}");
                        //sb.AppendLine($"{asks.Where(a => a[0] == 0).Count()}{asks.Where(a => asks.Where(b => b[0] == a[0]).Count() > 1).Count()}");
                        var isUp = aSum * 2.5m < bSum;
                        var isDown = bSum * 2.5m < aSum;
                        var flag = 0; string str = "----";
                        if (isUp) { flag = 1; str = "Maybe Up"; count++; }
                        if (isDown) { flag = 2; str = "Maybe Down"; count++; }

                        sb.AppendLine($"{str}");
                        lastflag = flag;

                        //Console.WriteLine(sb.ToString());
                        Log4NetUtility.Debug(sb.ToString());

                    }
                }
                catch (Exception e)
                {
                    Log4NetUtility.Error("", e.Message);
                }
            }
        }
    }
}
