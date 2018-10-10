/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	HFTInfo
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/24 14:00:33
*Description:	尚未编写描述
*Update History:
*<Author> <date> <Version> <Description>
*更新的版本的作者等信息，新的版本信息往下
*/
using CoreLibrary.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeLibrary.Model;

namespace HFTRobot
{
    public class HFTInfo
    {
        #region config
        /// <summary>
        /// 交易对
        /// </summary>
        public string Symbol { set; get; }
        /// <summary>
        /// 挂单价差（交易振幅）
        /// </summary>
        public decimal SpanPrice { set; get; }
        /// <summary>
        /// 单笔挂单交易数量
        /// </summary>
        public decimal TradeQty { set; get; }
        /// <summary>
        /// 单边挂单数
        /// </summary>
        public int OrderQty { set; get; }
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime Open_Time { set; get; }
        public decimal Open_Price { set; get; }
        public decimal Open_Fund { set; get; }
        public decimal Base_Fund { set; get; }
        public decimal Base_BTC { set; get; }
        public decimal Open_Coin { set; get; }
        public decimal Open_SpanPrice { set; get; }
        #endregion

        #region Cache
        /// <summary>
        /// 最后成交价格
        /// </summary>
        public decimal lastPrice { set; get; }
        /// <summary>
        /// 缓存订单列表
        /// </summary>
        //public List<Order> orderList { set; get; }

        /// <summary>
        /// 浮标价格
        /// </summary>
        public decimal floatPrice { set; get; }
        /// <summary>
        /// 最后调参时间
        /// </summary>
        public DateTime lastReStartTime { set; get; }
        /// <summary>
        /// 买单数
        /// </summary>
        public int buyOrderCount { set; get; }
        /// <summary>
        /// 卖单数
        /// </summary>
        public int sellOrderCount { set; get; }


        public List<decimal> ResetTimes { set; get; }
        public List<decimal> ShockTimes { set; get; }
        #endregion

        #region count

        public long dealUCount { set; get; }
        public long dealDCount { set; get; }
        public long dealCount { set; get; }
        public long resetCount { set; get; }
        public long priceCount { set; get; }
        public long lastCount { set; get; }

        public int ShockCount_tmp { set; get; }
        public int UpCount_tmp { set; get; }
        public int DownCount_tmp { set; get; }

        public decimal total_fees { set; get; }

        #endregion
        public HFTInfo(Config config)
        {
            lastReStartTime = DateTime.Now;
            Open_Time = config.Open_Time ?? DateTime.Now;
            OrderQty = (int)(config.Open_PacketNum ?? 10);
            this.Symbol = config.Symbol;
            this.Open_SpanPrice =this.SpanPrice = config.Open_FloatAMT ?? 0.5m;
            //orderList = new List<Order>();
            ResetTimes = new List<decimal>();
            ShockTimes = new List<decimal>();
        }

    }
}
