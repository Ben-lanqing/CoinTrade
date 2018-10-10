/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	candle
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/29 9:45:31
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

namespace MarketLibrary.API.Rest.FCoin
{
    public class candle
    {
        public string msg { set; get; }
        public string type { set; get; }
        public int status { set; get; }
        public List<candleData> data { set; get; }
    }
    public class candleData
    {
        public decimal open { set; get; }
        public decimal close { set; get; }
        public decimal high { set; get; }
        public decimal quote_vol { set; get; }
        public decimal id { set; get; }
        public decimal count { set; get; }
        public decimal low { set; get; }
        public decimal seq { set; get; }
        public decimal base_vol { set; get; }

    }

}
