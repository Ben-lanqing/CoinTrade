/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	Config
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/24 11:47:56
*Description:	尚未编写描述
*Update History:
*<Author> <date> <Version> <Description>
*更新的版本的作者等信息，新的版本信息往下
*/
using Lq.Log4Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLibrary.Model
{
    public class Config
    {
        public string Platform { set; get; }
        public string User { set; get; }
        public string ApiKey { set; get; }
        public string SecretKey { set; get; }
        public string Symbol { set; get; }
        public string CoinPairs { set; get; }

        public DateTime? Open_Time { set; get; }
        public decimal? Open_Fund { set; get; }
        public decimal? Open_Price { set; get; }
        public decimal? Open_FloatAMT { set; get; }
        public decimal? Open_PacketNum { set; get; }
        public bool? UseDataBase { set; get; }
        public int? Port { set; get; }
        public decimal? PriceRate { set; get; }
        public decimal? TradeFee { set; get; }
        public decimal? TradeQty { set; get; }



        public static Config LoadConfig()
        {
            try
            {
                Log4NetUtility.Info("加载系统配置", "开始加载系统配置");

                string conFilePath = AppDomain.CurrentDomain.BaseDirectory + "config.json";
                if (!File.Exists(conFilePath))
                {
                    throw (new Exception("配置文件不存在"));
                }
                StreamReader sr = new StreamReader(conFilePath, Encoding.Default);
                string jsonStr = sr.ReadToEnd();
                Config SysConfig = JsonConvert.DeserializeObject<Config>(jsonStr);
                Log4NetUtility.Info("加载系统配置", "已成功加载系统配置");
                return SysConfig;
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("加载系统配置", "加载配置异常：" + Utils.Exception2String(e));
                throw (e);
            }
        }

    }
}
