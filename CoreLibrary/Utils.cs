/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	Utils
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/23 16:18:47
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

namespace CoreLibrary
{
    public static class Utils
    {
        public static double DateTime2Double(TimeSpan addTS)
        {
            DateTime time = DateTime.Now + addTS;
            string timeStr = time.ToString("yyyyMMddHHmmss");
            return double.Parse(timeStr);

        }

        public static string Format(string data, int length = 11)
        {
            int d = data.Length - length;
            if (d >= 0) return data;
            for (int i = 1; i <= -d; i++)
            {
                data += " ";
            }
            return data;
        }

        public static string Format(decimal data, int length = 11)
        {
            string dataStr = data.ToString("0.0000");
            return Format(dataStr, length);
        }
        public static string Format(int data, int length = 11)
        {
            string dataStr = data.ToString();
            return Format(dataStr, length);
        }
        public static string Format(long data, int length = 11)
        {
            string dataStr = data.ToString();
            return Format(dataStr, length);
        }

        public static string Format(double data, int length = 11)
        {
            string dataStr = data.ToString();
            return Format(dataStr, length);
        }


        public static string GetUtcTimeStr(double addSec = 0) => ((DateTime.UtcNow.AddSeconds(addSec).Ticks - new DateTime(1970, 1, 1).Ticks) / 10000).ToString("0");
        public static decimal GetUtcTimeDec(double addSec = 0) => (DateTime.UtcNow.AddSeconds(addSec).Ticks - new DateTime(1970, 1, 1).Ticks) / 10000;

        public static DateTime GetDateTimeByStr(string timeStr)
        {
            long timespan = 0;
            long.TryParse(timeStr, out timespan);
            TimeSpan ts = new TimeSpan(timespan * 10000);
            return new DateTime(1970, 1, 1) + ts;

        }
        public static DateTime GetDateTimeByStr(decimal time)
        {
            TimeSpan ts = new TimeSpan((long)time * 10000);
            return new DateTime(1970, 1, 1) + ts;
        }
        public static string ShortID(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "";
            if (id.Count() <= 15) return "id";
            return id.Remove(6, id.Count() - 6);
        }
        public static string Exception2String(Exception e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Message:{e.Message}");
            sb.AppendLine($"StackTrace:{e.StackTrace}");
            var ex = e.InnerException;
            while (ex != null)
            {
                sb.AppendLine($"Message:{ex.Message}");
                sb.AppendLine($"StackTrace:{ex.StackTrace}");
                sb.AppendLine("------------------------------");

                ex = ex.InnerException;
            }
            sb.AppendLine("================================");

            return sb.ToString();
        }

    }
}
