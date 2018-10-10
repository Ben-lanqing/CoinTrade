/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	candle_ws
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/29 10:35:22
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

namespace MarketLibrary.API.WebSocket.FCoin
{
    public class candle_ws
    {
        public string type { set; get; }
        public decimal seq { set; get; }
        public decimal id { set; get; }
        public decimal open { set; get; }
        public decimal close { set; get; }
        public decimal high { set; get; }
        public decimal quote_vol { set; get; }
        public decimal count { set; get; }
        public decimal low { set; get; }
        public decimal base_vol { set; get; }

    }
}
