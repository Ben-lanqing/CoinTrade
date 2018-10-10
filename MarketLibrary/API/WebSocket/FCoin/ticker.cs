/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	ticker
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/21 16:17:55
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
    public class ticker_ws
    {
        public decimal[] ticker { set; get; }
        public string type { set; get; }
        public decimal seq { set; get; }

    }
}
