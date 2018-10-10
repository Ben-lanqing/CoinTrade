/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	Ticker
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/21 14:12:13
*Description:	尚未编写描述
*Update History:
*<Author> <date> <Version> <Description>
*更新的版本的作者等信息，新的版本信息往下
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketLibrary.Model
{
    public class Ticker
    {
        public bool result { set; get; }
        public decimal date { set; get; }
        public decimal error_code { set; get; }
        public string msg { set; get; }

        public decimal buy { get; set; }
        public decimal high { get; set; }
        public decimal last { get; set; }
        public decimal low { get; set; }
        public decimal sell { get; set; }
        public decimal vol { get; set; }
        public string type { set; get; }

        public Ticker()
        {
        }
        public Ticker(API.Rest.FCoin.ticker info)
        {
            error_code = info.status;
            result = info.status == 0;
            msg = info.msg;
            if (result)
            {
                date = info.data.seq;
                last = info.data.ticker[0];
                buy = info.data.ticker[2];
                //high = info.ticker.high;
                //low = info.ticker.low;
                sell = info.data.ticker[4];

            }
        }
        public Ticker(API.WebSocket.FCoin.ticker_ws ticker)
        {
            result = ticker != null && ticker.ticker != null;
            if (result)
            {
                date = ticker.seq;
                buy = ticker.ticker[2];
                //high = info.ticker.high;
                last = ticker.ticker[0];
                //low = info.ticker.low;
                sell = ticker.ticker[4];
                type = ticker.type;
            }
        }
    }
}
