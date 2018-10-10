/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	ticker
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/21 14:33:15
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

namespace MarketLibrary.API.Rest.Becent
{
    public class ticker
    {
        public string msg { set; get; }

        public int status { set; get; }
        public tickerData data { set; get; }

    }
    public class tickerData
    {
        public decimal seq { set; get; }
        public List<decimal> ticker { set; get; }

    }
}
