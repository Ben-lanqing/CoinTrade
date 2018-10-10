/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	RestApi_FC
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/21 14:17:41
*Description:	尚未编写描述
*Update History:
*<Author> <date> <Version> <Description>
*更新的版本的作者等信息，新的版本信息往下
*/
using CoreLibrary;
using CoreLibrary.DB;
using CoreLibrary.Model;
using Lq.Log4Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TradeLibrary.API.Rest
{
    public class RestApi_BC
    {
        private string secret_key;
        private string api_key;
        private string url_prex;

        /// <summary>
        /// 现货获取用户信息URL
        /// </summary>
        private const string USERINFO_URL = "/v1/account/accounts";
        /// <summary>
        /// 现货 下单交易URL
        /// </summary>
        private const string TRADE_URL = "/v2/orders";
        /// <summary>
        /// 现货 批量获取用户订单URL
        /// </summary>
        private const string ORDERS_INFO_URL = "/v2/orders";

        public RestApi_BC(string url_prex, string api_key, string secret_key)
        {
            this.api_key = api_key;
            this.secret_key = secret_key;
            this.url_prex = url_prex;
        }

        public string userinfo()
        {
            string result = "";
            try
            {
                var method = "GET";
                //string timeSpamt = GetDateTime();
                //var sign = CreateSign(method, USERINFO_URL, secret_key, timeSpamt, null);
                //WebHeaderCollection header = new WebHeaderCollection();
                //header.Add("FC-ACCESS-KEY", api_key);
                //header.Add("FC-ACCESS-SIGNATURE", sign);
                //header.Add("FC-ACCESS-TIMESTAMP", timeSpamt);
                Dictionary<string, object> paras = new Dictionary<string, object>();
                paras.Add("AccessKey", api_key);
                paras.Add("SignatureMethod", "HmacSHA256");
                paras.Add("SignatureVersion", "V1.0");
                paras.Add("Timestamp", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                paras.Add("Signature", null);
                int statusCode;
                result = RequestDataSync($"{url_prex}{USERINFO_URL}", method, paras, null, out statusCode);
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("userinfo", Utils.Exception2String(e));
                Log4NetUtility.Error("userinfo", result);
                DbHelper.CreateInstance().AddError("userinfo", e);
            }
            return result;

        }
        public string trade(string symbol, string side, string type, string price, string amount)
        {
            string result = "";
            try
            {
                var method = "POST";
                string timeSpamt = GetDateTime();
                // 构造参数签名
                Dictionary<string, object> paras = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(amount))
                {
                    paras.Add("amount", amount);
                }
                if (!string.IsNullOrEmpty(price))
                {
                    paras.Add("price", price);
                }
                if (!string.IsNullOrEmpty(side))
                {
                    paras.Add("side", side);
                }
                if (!string.IsNullOrEmpty(symbol))
                {
                    paras.Add("symbol", symbol);
                }
                if (!string.IsNullOrEmpty(type))
                {
                    paras.Add("type", type);
                }

                if (type == "market")
                {
                    paras.Remove("price");
                }

                var sign = CreateSign(method, TRADE_URL, secret_key, timeSpamt, paras);
                WebHeaderCollection header = new WebHeaderCollection();
                header.Add("FC-ACCESS-KEY", api_key);
                header.Add("FC-ACCESS-SIGNATURE", sign);
                header.Add("FC-ACCESS-TIMESTAMP", timeSpamt);

                int statusCode;
                result = RequestDataSync($"{url_prex}{TRADE_URL}", method, paras, header, out statusCode);
                return result;
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("userinfo", Utils.Exception2String(e));
                Log4NetUtility.Error("userinfo", result);
                DbHelper.CreateInstance().AddError("userinfo", e);
            }

            return result;
        }
        public string cancel_order(string order_id)
        {
            string result = "";
            try
            {
                var method = "POST";
                string timeSpamt = GetDateTime();
                // 构造参数签名
                //Dictionary<string, object> paras = new Dictionary<string, object>();
                //if (!string.IsNullOrEmpty(order_id))
                //{
                //    paras.Add("order_id", order_id);
                //}
                var sign = CreateSign(method, ORDERS_INFO_URL + "/" + order_id + "/submit-cancel", secret_key, timeSpamt, null);
                WebHeaderCollection header = new WebHeaderCollection();
                header.Add("FC-ACCESS-KEY", api_key);
                header.Add("FC-ACCESS-SIGNATURE", sign);
                header.Add("FC-ACCESS-TIMESTAMP", timeSpamt);

                int statusCode;
                result = RequestDataSync($"{url_prex}{ORDERS_INFO_URL}/{order_id}/submit-cancel", method, null, header, out statusCode);
            }
            catch (Exception e)
            {
                throw e;
            }
            return result;
        }
        public string order_info(string order_id)
        {
            string result = "";
            try
            {
                var method = "GET";
                string timeSpamt = GetDateTime();
                // 构造参数签名
                //Dictionary<string, object> paras = new Dictionary<string, object>();
                //if (!string.IsNullOrEmpty(order_id))
                //{
                //    paras.Add("order_id", order_id);
                //}
                var sign = CreateSign(method, ORDERS_INFO_URL + "/" + order_id, secret_key, timeSpamt, null);
                WebHeaderCollection header = new WebHeaderCollection();
                header.Add("FC-ACCESS-KEY", api_key);
                header.Add("FC-ACCESS-SIGNATURE", sign);
                header.Add("FC-ACCESS-TIMESTAMP", timeSpamt);

                int statusCode;
                result = RequestDataSync($"{url_prex}{ORDERS_INFO_URL}/{order_id}", method, null, header, out statusCode);
                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
            return result;
        }

        /// <summary>
        /// 批量获取用户订单
        /// </summary>
        /// <param name="type">查询类型 0:未成交，未成交 1:完全成交，已撤销</param>
        /// <param name="symbol">btc_usd: 比特币 ltc_usd: 莱特币</param>
        /// <param name="order_id">订单ID(多个订单ID中间以","分隔,一次最多允许查询50个订单)</param>
        /// limit=20&states=submitted,partial_filled&symbol=ethusdt
        /// <returns></returns>
        public string orders_info(string limit, string states, string symbol, string after, string before)
        {
            string result = "";
            try
            {
                var method = "GET";
                string timeSpamt = GetDateTime();
                // 构造参数签名
                Dictionary<string, object> paras = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(after))
                {
                    paras.Add("after", after);
                }
                if (!string.IsNullOrEmpty(before))
                {
                    paras.Add("before", before);
                }

                if (!string.IsNullOrEmpty(limit))
                {
                    paras.Add("limit", limit);
                }
                if (!string.IsNullOrEmpty(states))
                {
                    paras.Add("states", states);
                }
                if (!string.IsNullOrEmpty(symbol))
                {
                    paras.Add("symbol", symbol);
                }
                var sign = CreateSign(method, ORDERS_INFO_URL, secret_key, timeSpamt, paras);
                //var sign = CreateSign(method, "/v2/orders?limit=20&states=submitted&symbol=ethusdt", secret_key, timeSpamt, null);
                WebHeaderCollection header = new WebHeaderCollection();
                header.Add("FC-ACCESS-KEY", api_key);
                header.Add("FC-ACCESS-SIGNATURE", sign);
                header.Add("FC-ACCESS-TIMESTAMP", timeSpamt);

                int statusCode;
                result = RequestDataSync($"{url_prex}{ORDERS_INFO_URL}?{ConvertQueryString(paras, true)}", method, null, header, out statusCode);
                //result = RequestDataSync("https://api.fcoin.com/v2/orders?limit=20&states=submitted&symbol=ethusdt", method, null, header, out statusCode);
                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
            return result;
        }

        #region private
        private string GetDateTime() => ((DateTime.UtcNow.Ticks - new DateTime(1970, 1, 1).Ticks) / 10000).ToString("0");

        private string RequestDataSync(string url, string method, Dictionary<string, object> param, WebHeaderCollection headers, out int httpCode)
        {
            string resp = string.Empty;
            httpCode = 200;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "application/json;charset=UTF-8";
            //request.Headers.Add("Accept-Encoding", "gzip");
            request.Method = method;

            if (headers != null)
            {
                foreach (var key in headers.AllKeys)
                {
                    request.Headers.Add(key, headers[key]);
                }
            }
            try
            {
                if (method == "POST" && param != null)
                {
                    byte[] bs = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(param));
                    request.ContentLength = bs.Length;
                    using (var reqStream = request.GetRequestStream())
                    {
                        reqStream.Write(bs, 0, bs.Length);
                    }
                }
                //如果是Get 请求参数附加在URL之后
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response == null)
                        throw new Exception("Response is null");
                    resp = GetResponseBody(response);
                    httpCode = (int)response.StatusCode;
                }
            }
            catch (WebException ex)
            {
                using (HttpWebResponse response = ex.Response as HttpWebResponse)
                {
                    if (response == null)
                        throw new Exception("Response is null");
                    resp = GetResponseBody(response);
                    httpCode = (int)response.StatusCode;
                }
            }
            return resp;
        }
        private string GetResponseBody(HttpWebResponse response)
        {
            var readStream = new Func<Stream, string>((stream) =>
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            });

            using (var responseStream = response.GetResponseStream())
            {
                //if (response.ContentEncoding.ToLower().Contains("gzip"))
                //{
                //    using (GZipStream stream = new GZipStream(responseStream, CompressionMode.Decompress))
                //    {
                //        return readStream(stream);
                //    }
                //}
                //if (response.ContentEncoding.ToLower().Contains("deflate"))
                //{
                //    using (DeflateStream stream = new DeflateStream(responseStream, CompressionMode.Decompress))
                //    {
                //        return readStream(stream);
                //    }
                //}
                return readStream(responseStream);
            }
        }

        private string CreateSign(string method, string action, string secretKey, string timeSpamp, Dictionary<string, object> data)
        {
            string convertQueryString = "";
            string hashSource = $"{method}{url_prex}{action}";
            if (data != null)
            {
                convertQueryString = ConvertQueryString(data, true);
            }
            if (method == "POST")
            {
                hashSource = $"{hashSource}{timeSpamp }{convertQueryString}";
            }
            else
            {
                if (convertQueryString != "")
                {
                    convertQueryString = $"?{convertQueryString}";
                }
                hashSource = $"{hashSource}{convertQueryString}{timeSpamp }";
            }
            string str1 = Convert.ToBase64String(Encoding.UTF8.GetBytes(hashSource));

            var hmacsha1 = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(str1));
            byte[] hashData = hmacsha1.ComputeHash(stream);
            return Convert.ToBase64String(hashData);
        }

        private string ConvertQueryString(Dictionary<string, object> data, bool urlencode = false)
        {
            var stringbuilder = new StringBuilder();
            foreach (var item in data)
            {
                //stringbuilder.AppendFormat("{0}={1}&", item.Key, urlencode ? Uri.EscapeDataString(item.Value.ToString()) : item.Value.ToString());
                stringbuilder.AppendFormat("{0}={1}&", item.Key, item.Value.ToString());
            }
            stringbuilder.Remove(stringbuilder.Length - 1, 1);
            return stringbuilder.ToString();
        }


        #endregion
    }
}
