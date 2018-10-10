/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	CoinConfig
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/23 9:06:54
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
    public class CoinConfig
    {
        public string Platform { set; get; }
        public string Title { set; get; }
        public string Symbol { set; get; }

        public string CoinA { set; get; }
        public string CoinB { set; get; }

        public decimal AmountLimit { set; get; }
        public decimal PriceLimit { set; get; }
        public decimal BasePriceWidth { set; get; }


        public decimal FormatAmount2D(decimal value)
        {
            int v = (int)(value * AmountLimit);
            return v / AmountLimit;

        }
        public string FormatAmount2S(decimal value)
        {
            decimal fv = FormatAmount2D(value);
            return fv.ToString();
        }
        public decimal FormatPrice2D(decimal value)
        {
            int v = (int)(value * PriceLimit);
            return v / PriceLimit;

        }
        public string FormatPrice2S(decimal value)
        {
            decimal fv = FormatPrice2D(value);
            return fv.ToString();
        }

        public static string Format(decimal format, decimal value)
        {
            int v = (int)(value * format);
            decimal fv = v / format;
            return fv.ToString();
        }
        public static List<CoinConfig> Load()
        {
            try
            {
                string conFilePath = AppDomain.CurrentDomain.BaseDirectory + "CoinConfigs.json";
                if (!File.Exists(conFilePath))
                {
                    throw (new Exception("CoinConfigs配置文件不存在"));
                }
                StreamReader sr = new StreamReader(conFilePath, Encoding.Default);
                string jsonStr = sr.ReadToEnd();
                var list = JsonConvert.DeserializeObject<List<CoinConfig>>(jsonStr);
                if (list != null && list.Count > 0)
                {
                    Log4NetUtility.Info("加载系统配置", "已成功加载系统配置CoinConfigs");
                    return list;
                }
                else
                {
                    throw (new Exception("CoinConfigs配置文件异常"));
                }
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("加载系统配置CoinConfigs", "加载配置异常：" + Utils.Exception2String(e));
                throw (e);
            }
        }

    }
}
