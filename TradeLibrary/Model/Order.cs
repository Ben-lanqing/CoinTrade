/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	Order
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/23 11:43:48
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
    [Serializable]
    public class Orders
    {
        public string msg { set; get; }

        public string error_code { set; get; }
        public bool result { set; get; }
        public List<Order> orders { set; get; }

        public Orders()
        {
            error_code = "0";
            result = false;
            orders = new List<Order>();

        }
        public Orders(API.Rest.FCoin.orderInfo info)
        {
            error_code = info.msg;
            result = info.status == 0;
            orders = new List<Order>();
            if (result)
            {
                Order order = new Order();
                order.amount = Convert.ToDecimal(info.data.amount);
                order.avg_price = Convert.ToDecimal(info.data.amount) == 0 ? 0 : Convert.ToDecimal(info.data.executed_value) / Convert.ToDecimal(info.data.amount);
                order.create_date = info.data.created_at;
                order.deal_amount = Convert.ToDecimal(info.data.executed_value);
                order.order_id = info.data.id;
                order.price = Convert.ToDecimal(info.data.price);
                order.status = info.data.state;
                order.symbol = info.data.symbol;
                order.type = info.data.type == "limit" ? info.data.side : $"{info.data.type}_{info.data.side}";
                order.fill_fees = Convert.ToDecimal(info.data.fill_fees);

                orders.Add(order);
            }
        }
        public Orders(API.Rest.FCoin.ordersInfo info)
        {
            msg = info.msg;
            error_code = info.status.ToString();
            result = info.status == 0;
            if (!result || info.data == null)
            {
                orders = new List<Order>();
                return;
            }
            orders = new List<Order>();
            foreach (var item in info.data)
            {
                Order order = new Order();
                order.amount = Convert.ToDecimal(item.amount);
                order.avg_price = Convert.ToDecimal(item.amount) == 0 ? 0 : Convert.ToDecimal(item.executed_value) / Convert.ToDecimal(item.amount);
                order.create_date = item.created_at;
                order.deal_amount = Convert.ToDecimal(item.executed_value);
                order.order_id = item.id;
                order.price = Convert.ToDecimal(item.price);
                order.status = item.state;
                order.symbol = item.symbol;
                order.type = item.type == "limit" ? item.side : $"{item.type}_{item.side}";
                order.fill_fees = Convert.ToDecimal(item.fill_fees);
                orders.Add(order);
            }
        }

    }
    [Serializable]
    public class Order
    {
        /// <summary>
        /// 下单数量
        /// </summary>
        public decimal amount { set; get; }
        /// <summary>
        /// 成交平均价格
        /// </summary>
        public decimal avg_price { set; get; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public decimal create_date { set; get; }
        /// <summary>
        /// 成交量
        /// </summary>
        public decimal deal_amount { set; get; }
        public string order_id { set; get; }
        public decimal price { set; get; }
        public string status { set; get; }
        public string symbol { set; get; }
        /// <summary>
        /// 交易类型buy, sell，market_buy, market_sell，
        /// </summary>
        public string type { set; get; }

        public decimal fill_fees { set; get; }

        public string flag { set; get; }
        public Order() { }

    }

}
