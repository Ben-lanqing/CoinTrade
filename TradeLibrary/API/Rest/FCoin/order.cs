/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	order
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/23 11:55:26
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

namespace TradeLibrary.API.Rest.FCoin
{
    public class ordersInfo
    {
        public int status { set; get; }
        public string msg { set; get; }

        public List<order> data { set; get; }

    }
    public class orderInfo
    {
        public int status { set; get; }
        public string msg { set; get; }
        public order data { set; get; }

    }

    public class order
    {
        public string amount { set; get; }
        public decimal created_at { set; get; }
        public string executed_value { set; get; }
        public string id { set; get; }
        public string price { set; get; }
        public string symbol { set; get; }
        public string type { set; get; }
        public string side { set; get; }
        public string state { set; get; }
        public string filled_amount { set; get; }
        public string fill_fees { set; get; }

    }

}
