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
using Lq.Log4Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketLibrary.API.Rest
{
    public class RestApi_FC
    {
        private string secret_key;
        private string api_key;
        private string url_prex;

        /// <summary>
        /// 现货行情URL
        /// </summary>
        private const string TICKER_URL = "/v2/market/ticker/";
        private const string CANDLES_URL = "/v2/market/candles/";

        public RestApi_FC(string url_prex, string api_key, string secret_key)
        {
            this.api_key = api_key;
            this.secret_key = secret_key;
            this.url_prex = url_prex;
        }

        /// <summary>
        /// 行情
        /// </summary>
        /// <param name="symbol">btc_usd:比特币    ltc_usd :莱特币</param>
        /// <returns></returns>
        public string ticker(string symbol)
        {
            string result = "";
            try
            {
                HttpUtilManager httpUtil = HttpUtilManager.getInstance();

                result = httpUtil.requestHttpGet(url_prex, TICKER_URL + symbol, "");
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("RestApi_FC", result);
                throw e;
            }
            return result;
        }
        public string candles(string symbol, string resolution,int limit)
        {
            string result = "";
            try
            {
                HttpUtilManager httpUtil = HttpUtilManager.getInstance();
                string url = $"{CANDLES_URL}/{resolution}/{symbol}";
                result = httpUtil.requestHttpGet(url_prex, url, "limit = "+ limit);
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("RestApi_FC", result);

                throw e;
            }
            return result;
        }

    }
}
