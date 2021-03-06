﻿/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	Class1
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/27 10:08:58
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
using TradeLibrary.Model;

namespace SessionRobot
{
    public class SessionEventArgs : EventArgs
    {

        public SessionEvent EventType { set; get; }
        public List<Order> OrderList { set; get; }
        public string Message { get; set; }

        public SessionEventArgs(SessionEvent type, string message = null, List<Order> list = null)
        {
            EventType = type;
            Message = message;
            OrderList = list ?? new List<Order>();
        }

    }

}
