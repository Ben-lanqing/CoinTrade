/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	CancelOrder
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/23 11:48:39
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

namespace TradeLibrary.Model
{
    public class CancelOrder
    {
        public bool result { set; get; }
        public string error_code { set; get; }

        public string order_id { set; get; }
        public string msg { set; get; }
        public CancelOrder(API.Rest.FCoin.cancelorder info)
        {
            result = info.status == 0;
            error_code = info.status.ToString();
            order_id = info.data;
            msg = info.msg;

        }

    }
}
