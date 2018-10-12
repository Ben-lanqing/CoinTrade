using CoreLibrary.DB;
using HFTRobot;
using Lq.Log4Net;
using MarketLibrary.Model;
using MarketRobot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        //static Robot_Market robot = new Robot_Market("FC");
        static void Main(string[] args)
        {
            //using (var db = new TradeDBEntities())
            //{
            //    //db.orders.Add(new order() { orderid = 12345678, platform = "FC", price = 7000 });
            //    //db.SaveChanges();
            //}

            //System.Timers.Timer timer = new System.Timers.Timer();
            //timer.Interval = 5000;
            //timer.Elapsed += Timer_Elapsed;
            //robot.Run(new List<string>() { "btcusdt" });
            //timer.Start();
            while (true)
            {
                Console.ReadLine();
            }
        }

        //private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        //{
        //    var kline = robot.GetKlines("btcusdt", "M1");
        //    //Console.WriteLine($"kline1  Count:{kline.Count} id: {kline.LastOrDefault()?.id} type: {kline.LastOrDefault()?.type}  close: {kline.LastOrDefault()?.close}");
        //    var kline15 = robot.GetKlines("btcusdt", "M15");
        //    //Console.WriteLine($"kline15 Count:{kline15.Count} id: {kline15.LastOrDefault()?.id} type: {kline15.LastOrDefault()?.type} close: {kline15.LastOrDefault()?.close}");
        //    width(kline, kline15);

        //    decimal width1 = GetKlinePriceWidth(kline, 5, 0.5m);
        //    decimal width15 = GetKlinePriceWidth(kline15, 5, 0.25m);
        //    Console.WriteLine($"width1 : {width1}   width15 : {width15}");
        //}

        //private static void width(List<Kline> list, List<Kline> list15)
        //{
        //    list = list.OrderBy(a => a.id).ToList();
        //    var th = list.Skip(list.Count - 5);
        //    decimal max = th.Max(a => a.high);
        //    decimal min = th.Min(a => a.low);
        //    decimal width = max - min;
        //    Console.WriteLine($"M1  max1 : {max} min1 : {min}");
        //    decimal widthF = Math.Round(width * 5) / 5;
        //    Console.WriteLine($"M1  width: {width} widthF: {widthF} count {th.Sum(a => a.count) / 5}");

        //    StringBuilder sb = new StringBuilder();
        //    var first = list.FirstOrDefault();

        //    sb.AppendLine($"M1 0 id: {first.id}  high: {first.high} low: {first.low} count: {first.count}");
        //    sb.AppendLine();

        //    int i = 0;
        //    foreach (var s in th)
        //    {
        //        i++;
        //        sb.AppendLine($"M1 {i} id: {s.id}  high: {s.high} low: {s.low} count: {s.count}");

        //    }
        //    Console.WriteLine($"{sb.ToString()}");

        //    list15 = list15.OrderBy(a => a.id).ToList();

        //    var th15 = list15.Skip(list15.Count - 5);
        //    decimal max15 = th15.Max(a => a.high);
        //    decimal min15 = th15.Min(a => a.low);
        //    Console.WriteLine($"M15  max15 : {max15} min15 : {min15}");
        //    decimal width15 = max15 - min15;
        //    decimal widthF15 = Math.Round(width15 * 5) / 5;
        //    Console.WriteLine($"M15 width: {width15} widthF: {widthF15} count {th15.Sum(a => a.count) / 75}");
        //    StringBuilder sb15 = new StringBuilder();
        //    var first15 = list15.FirstOrDefault();

        //    sb15.AppendLine($"M15 0 id: {first15.id}  high: {first15.high} low: {first15.low} count: {first15.count}");
        //    sb15.AppendLine();

        //    int i15 = 0;
        //    foreach (var s in th15)
        //    {
        //        i15++;
        //        sb15.AppendLine($"M15 {i15} id: {s.id}  high: {s.high} low: {s.low} count: {s.count}");

        //    }
        //    Console.WriteLine($"{sb15.ToString()}");

        //    var w1 = width / 2 / 20; w1 = Math.Round(w1 * 5) / 5;
        //    var w15 = width15 / 2 / 20; w15 = Math.Round(w15 * 5) / 5;
        //    Console.WriteLine($"M1 width: {w1} M15 width: {w15} ");

        //}

        //private static decimal GetKlinePriceWidth(List<Kline> klines, int count = 5, decimal rTimes = 1, decimal format = 0.2m)
        //{
        //    var list = klines.OrderBy(a => a.id).Skip(klines.Count - count).ToList();
        //    decimal max = list.Max(a => a.high);
        //    decimal min = list.Min(a => a.low);

        //    var priceWidth = max - min;
        //    Console.WriteLine($"max : {max} min : {min} width: {priceWidth}");
        //    //return priceWidth;
        //    var width = priceWidth / 10 * rTimes;
        //    int f = (int)(1 / format);
        //    f = f < 1 ? 1 : f;
        //    width = Math.Round(width * f) / f;
        //    return width;
        //}

    }
}
