/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	Kline
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/29 10:27:16
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
    public class Kline
    {
        public decimal date { set; get; }
        public string type { set; get; }

        public decimal open { set; get; }
        public decimal close { set; get; }
        public decimal high { set; get; }
        public decimal quote_vol { set; get; }
        public decimal id { set; get; }
        public decimal count { set; get; }
        public decimal low { set; get; }
        public decimal seq { set; get; }
        public decimal base_vol { set; get; }

        public Kline()
        {
        }
        public static List<Kline> Klines(API.Rest.FCoin.candle info, string type)
        {
            List<Kline> list = new List<Kline>();
            var result = info.status == 0;
            if (result)
            {
                foreach (var c in info.data)
                {
                    Kline k = new Kline();
                    k.type = type;
                    k.date = c.id;
                    k.open = c.open;
                    k.close = c.close;
                    k.high = c.high;
                    k.count = c.count;
                    k.id = c.id;
                    k.low = c.low;
                    k.seq = c.seq;
                    k.base_vol = c.base_vol;
                    k.quote_vol = c.quote_vol;
                    list.Add(k);
                }
            }
            return list;
        }
        public Kline(API.WebSocket.FCoin.candle_ws info)
        {
            var result = info != null;
            if (result)
            {
                type = info.type.Replace("candle.", "");
                date = info.id;
                type = info.type;
                open = info.open;
                close = info.close;
                high = info.high;
                count = info.count;
                id = info.id;
                low = info.low;
                seq = info.seq;
                base_vol = info.base_vol;
                quote_vol = info.quote_vol;

            }
        }

    }
}
