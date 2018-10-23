/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	Modelhepler
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/21 14:30:03
*Description:	尚未编写描述
*Update History:
*<Author> <date> <Version> <Description>
*更新的版本的作者等信息，新的版本信息往下
*/
using CoreLibrary.DB;
using Lq.Log4Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace CoreLibrary
{
    public class ModelHelper<T>
    {
        public static T Json2Model(string str)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(str) || str.Contains("html"))
                    return default(T);
                return JsonConvert.DeserializeObject<T>(str);
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("ModelHelper", Utils.Exception2String(e));
                Log4NetUtility.Error("ModelHelper", str);
                DbHelper.CreateInstance().AddError("ModelHelper", e);
                return default(T);
            }
        }
        public static string Model2Json(T model)
        {
            try
            {
                return JsonConvert.SerializeObject(model);
            }
            catch (Exception e)
            {
                Log4NetUtility.Error("ModelHelper", Utils.Exception2String(e));
                //Log4NetUtility.Error("ModelHelper", str);
                DbHelper.CreateInstance().AddError("ModelHelper", e);
                return "";
            }
        }

        public static List<T> CloneList(object List)
        {
            using (Stream objectStream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(objectStream, List);
                objectStream.Seek(0, SeekOrigin.Begin);
                return formatter.Deserialize(objectStream) as List<T>;
            }
        }
    }
}
