using CoreLibrary;
using MarketLibrary;
using MarketLibrary.API.WebSocket.FCoin;
using MarketLibrary.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MarketRobot
{
    public class Robot_Market
    {
        public bool Running { set; get; }

        List<string> SymbolList;
        bool IsWS;
        Dictionary<string, Ticker> Tickerdic;
        Dictionary<string, Depth> Depthdic;
        Dictionary<string, List<Kline>> Klinedic;
        string platform;
        MarketHepler marketHepler;

        Timer tickerTimer;
        Timer heplerTimer;


        public Robot_Market(string platform)
        {
            if (string.IsNullOrEmpty(platform)) throw (new Exception("para is null"));
            this.platform = platform;
            marketHepler = new MarketHepler(platform);
            IsWS = true;
            SymbolList = new List<string>();
            Tickerdic = new Dictionary<string, Ticker>();
            Depthdic = new Dictionary<string, Depth>();
            Klinedic = new Dictionary<string, List<Kline>>();
            InitTimer();
            InitWsEvent();
        }

        private void InitTimer()
        {
            // 初始化系统计时器配置
            tickerTimer = new System.Timers.Timer(1000);
            tickerTimer.Elapsed += TickerTimer_Elapsed;
            heplerTimer = new System.Timers.Timer(1000 * 60 * 5);
            heplerTimer.Elapsed += HeplerTimer_Elapsed; ;
        }


        private void InitWsEvent()
        {
            marketHepler.OnMessage += MarketHepler_OnMessage;
            marketHepler.OnClosed += MarketHepler_OnClosed;
        }

        private void MarketHepler_OnClosed(object sender, EventArgs e)
        {
            Console.WriteLine($"FCMarket_OnClosed-");
        }
        private void MarketHepler_OnMessage(object sender, MarketLibrary.API.WebSocket.FCMessageReceivedEventArgs e)
        {
            #region ticker
            if (e.Message.Contains("ticker"))
            {
                var model = ModelHelper<ticker_ws>.Json2Model(e.Message);
                if (model != null)
                {
                    Ticker ticker = new Ticker(model);
                    if (ticker != null && !string.IsNullOrEmpty(ticker.type))
                    {
                        string sy = ticker.type.Replace("ticker.", "");
                        updateTicker(sy, ticker);
                    }
                }
            }
            #endregion
            #region depth
            else if (e.Message.Contains("depth"))
            {
                var model = ModelHelper<depth_ws>.Json2Model(e.Message);
                if (model != null)
                {
                    Depth depth = new Depth(model);
                    if (depth != null && !string.IsNullOrEmpty(depth.type))
                    {
                        string symbol = depth.type.Replace("depth.L150.", "");

                        updateDepth(symbol, depth);
                    }
                }

            }
            #endregion
            #region candle
            else if (e.Message.Contains("candle"))
            {
                //Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} msg: {Utils.Exception2String(e)}  ");
                var model = ModelHelper<candle_ws>.Json2Model(e.Message);
                if (model != null)
                {
                    Kline kline = new Kline(model);
                    if (kline != null && !string.IsNullOrEmpty(kline.type))
                    {
                        string type = kline.type.Replace("candle.", "");

                        updateKline(type, kline);
                    }
                }
            }
            #endregion
            else Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} msg: {e.Message}  ");

        }
        private void updateTicker(string symbol, Ticker ticker)
        {
            try
            {
                Tickerdic[symbol] = ticker;
            }
            catch (Exception e)
            {
                throw (e);
            }
        }
        private void updateDepth(string symbol, Depth depth)
        {
            try
            {
                Depthdic[symbol] = depth;
            }
            catch (Exception e)
            {
                throw (e);
            }
        }
        private void updateKline(string type, Kline kline)
        {
            try
            {
                List<Kline> list = Klinedic[type];
                int count = list.Count;
                var k = list.FirstOrDefault(a => a.id == kline.id);
                if (k != null)
                {
                    list.Remove(k);
                }
                list.Add(kline);
                //list = list.OrderBy(a => a.id).ToList();
                list = list.OrderBy(a => a.id).ToList();
                if (count > 60)
                {
                    list.RemoveRange(0, count - 60);
                }
            }
            catch (Exception e)
            {
                throw (e);
            }
        }

        private void TickerTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Parallel.ForEach(Tickerdic, (dic, lookup) =>
            {
                Ticker ticker = marketHepler?.GetTicker(platform, dic.Key);
                if (ticker != null && ticker.result)
                {
                    updateTicker(dic.Key, ticker);
                }
            });
        }
        private void HeplerTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (marketHepler?.IsOpened != true)
                {
                    Stop();
                    marketHepler.OnMessage -= MarketHepler_OnMessage;
                    marketHepler.OnClosed -= MarketHepler_OnClosed;
                    marketHepler = null;
                    marketHepler = new MarketHepler(platform);
                    InitWsEvent();
                    Run(SymbolList, IsWS);
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        public void Run(List<string> symbols, bool isWS = true)
        {
            SymbolList = symbols;
            IsWS = isWS;
            foreach (var sy in symbols)
            {
                var ticker = marketHepler.GetTicker(platform, sy);
                Tickerdic.Add(sy, ticker);
                Depthdic.Add(sy, null);
                var KlineM1 = marketHepler.GetKline(platform, sy, "M1");
                var KlineM15 = marketHepler.GetKline(platform, sy, "M15");
                Klinedic.Add("M1." + sy, KlineM1);
                Klinedic.Add("M15." + sy, KlineM15);
            }
            if (isWS)
            {
                DepthSubscribe(symbols);
                TickerSubscribe(symbols);
                KlineSubscribe(symbols);
            }
            else
            {
                tickerTimer.Start();
            }
            Running = true;
        }
        public void Stop()
        {
            Running = false;
            tickerTimer.Stop();
        }

        public void DepthSubscribe(List<string> symbols)
        {
            foreach (var sy in symbols)
            {
                marketHepler?.DepthSub(sy);
            }
        }
        public void TickerSubscribe(List<string> symbols)
        {
            foreach (var sy in symbols)
            {
                marketHepler?.TickerSub(sy);
            }
        }
        public void KlineSubscribe(List<string> symbols)
        {
            foreach (var sy in symbols)
            {
                marketHepler?.KlineSub(sy, "M1");
                marketHepler?.KlineSub(sy, "M15");
            }
        }
        public Ticker GetTicker(string symbol)
        {
            if (!Tickerdic.Keys.Contains(symbol)) return null;
            return Tickerdic[symbol];
        }
        public Depth GetDepth(string symbol)
        {
            if (!Depthdic.Keys.Contains(symbol)) return null;
            return Depthdic[symbol];
        }
        public List<Kline> GetKlines(string symbol, string resolution)
        {
            string type = $"{resolution}.{symbol}";
            if (!Klinedic.Keys.Contains(type)) return null;
            return Klinedic[type];
        }
    }
}
