using CoreLibrary;
using CoreLibrary.DB;
using Lq.Log4Net;
using MarketLibrary.API.Rest;
using MarketLibrary.API.WebSocket;
using MarketLibrary.API.WebSocket.FCoin;
using MarketLibrary.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketLibrary
{
    public class MarketHepler
    {
        public string Platform { set; get; }
        public bool IsOpened
        {
            get
            {
                return wsApi_FC == null ? false : wsApi_FC.isOpened;
            }
        }

        private static string url_prex_FC = "https://api.fcoin.com";
        private static string url_prex_BC = "https://open-api.becent.com";

        RestApi_FC getRequest_FC;
        WebSocketApi_FC wsApi_FC;
        public event EventHandler<FCMessageReceivedEventArgs> OnMessage;
        public event EventHandler OnClosed;
        public MarketHepler(string platform)
        {
            Platform = platform;

            if (!string.IsNullOrEmpty(platform))
            {
                switch (platform)
                {
                    case "FC":
                        getRequest_FC = new RestApi_FC(url_prex_FC, null, null);
                        WebSocketInit();


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
        private void WebSocketInit()
        {
            wsApi_FC = new WebSocketApi_FC();
            wsApi_FC.Connnet();
            wsApi_FC.OnMessage += WsApi_FC_OnMessage;
            wsApi_FC.OnClosed += WsApi_FC_OnClosed;
        }

        private void WsApi_FC_OnClosed(object sender, EventArgs e)
        {
            Console.WriteLine($"FCMarket_OnClosed-");
            OnClosed?.Invoke(null, null);
            wsApi_FC.ReConnnet();
            wsApi_FC.OnMessage += WsApi_FC_OnMessage;
            wsApi_FC.OnClosed += WsApi_FC_OnClosed;

        }
        private void WsApi_FC_OnMessage(object sender, FCMessageReceivedEventArgs e)
        {
            //Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} msg: {Utils.Exception2String(e)}  ");
            OnMessage?.Invoke(null, e);
        }

        public Ticker GetTicker(string platform, string symbol)
        {
            try
            {
                switch (platform)
                {
                    //case "OK":
                    //    return GetTicker_OK(symbol);
                    //case "BA":
                    //    return GetDepth_BA(symbol, size);
                    case "HB":
                    //return GetTicker_OK(symbol);
                    case "FC":
                        return GetTicker_FC(symbol);
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("GetTicker", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("GetTicker", e);
                return null;

            }
        }
        public Ticker GetTicker_FC(string symbol)
        {
            string JsonStr = getRequest_FC.ticker(symbol);
            var data = ModelHelper<API.Rest.FCoin.ticker>.Json2Model(JsonStr);
            return new Ticker(data);

        }
        public List<Kline> GetKline(string platform, string symbol, string resolution, int limit = 60)
        {
            try
            {
                switch (platform)
                {
                    //case "OK":
                    //    return GetTicker_OK(symbol);
                    //case "BA":
                    //    return GetDepth_BA(symbol, size);
                    case "HB":
                    //return GetTicker_OK(symbol);
                    case "FC":
                        return GetKline_FC(symbol, resolution, limit);
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("GetKline", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("GetKline", e);
                return null;

            }
        }
        public List<Kline> GetKline_FC(string symbol, string resolution, int limit)
        {
            string type = $"{resolution}.{symbol}";
            string JsonStr = getRequest_FC.candles(symbol, resolution, limit);
            var data = ModelHelper<API.Rest.FCoin.candle>.Json2Model(JsonStr);
            return Kline.Klines(data, type);

        }
        public Depth GetDepth(string platform, string symbol)
        {
            try
            {
                switch (platform)
                {
                    //case "OK":
                    //    return GetTicker_OK(symbol);
                    //case "BA":
                    //    return GetDepth_BA(symbol, size);
                    case "HB":
                    //return GetTicker_OK(symbol);
                    case "FC":
                        return GetDepth_FC(symbol);
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("GetDepth", Utils.Exception2String(e));
                DbHelper.CreateInstance().AddError("GetDepth", e);
                return null;

            }
        }
        public Depth GetDepth_FC(string symbol)
        {
            string JsonStr = getRequest_FC.depht(symbol);
            var data = ModelHelper<API.Rest.FCoin.depth>.Json2Model(JsonStr);
            return new Depth(data);

        }

        public void DepthSub(string symbol)
        {
            string topic = $"depth.L150.{symbol}";
            var msg = "{\"cmd\":\"sub\",\"args\":[\"" + topic + "\"]}";
            wsApi_FC.Subscribe(msg);
        }
        public void TickerSub(string symbol)
        {
            string topic = $"ticker.{symbol}";
            var msg = "{\"cmd\":\"sub\",\"args\":[\"" + topic + "\"]}";
            wsApi_FC.Subscribe(msg);
        }
        public void KlineSub(string symbol, string resolution)
        {
            string topic = $"candle.{resolution}.{symbol}";
            var msg = "{\"cmd\":\"sub\",\"args\":[\"" + topic + "\"]}";
            wsApi_FC.Subscribe(msg);
        }
    }
}
