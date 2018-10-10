using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using log4net;
using log4net.Config;
using log4net.Appender;
using log4net.Core;
using System.IO;
using log4net.Repository.Hierarchy;

namespace Lq.Log4Net
{
    /// <summary>
    /// Log4Net日志工具类
    /// </summary>
    public class Log4NetUtility
    {
        private static ILog fileLogger = LogManager.GetLogger("FileLogLogger");
        private static ILog errLogger = LogManager.GetLogger("ErrFileLogLogger");
        private static ILog debugLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        static Log4NetUtility()
        {
            string binPath = System.AppDomain.CurrentDomain.BaseDirectory;
            string webFileName = binPath + @"bin\Config\log4net.cfg.xml";
            string appFileName = binPath + @"Config\log4net.cfg.xml";
            if (File.Exists(webFileName))
                log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(webFileName));
            else if (File.Exists(appFileName))
                log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(appFileName));
            else
                Console.WriteLine("找不到Log4net配置文件");
        }
        public static void DebugLog(String message)
        {
            debugLogger.Debug(message);
        }
        public static void Debug(object o, string message, Exception exception)
        {
            Type type = o.GetType();
            fileLogger.Debug(string.Format("[{0}.{1}] - {2}", type.Namespace, type.Name, message), exception);
        }
        public static void Debug(object o, String message)
        {
            Type type = o.GetType();
            fileLogger.Debug(string.Format("[{0}.{1}] - {2}", type.Namespace, type.Name, message));
        }
        public static void Debug(String typeName, String message, Exception exception)
        {
            fileLogger.Debug(string.Format("[{0}] - {1}", typeName, message), exception);
        }
        public static void Debug(String typeName, String message)
        {
            fileLogger.Debug(string.Format("[{0}] - {1}", typeName, message));
        }
        public static void Debug(String message)
        {
            fileLogger.Debug(message);
        }
        public static void Info(object o, String message, Exception exception)
        {
            Type type = o.GetType();
            fileLogger.Info(string.Format("[{0}.{1}] - {2}", type.Namespace, type.Name, message), exception);
        }
        public static void Info(object o, String message)
        {
            Type type = o.GetType();
            fileLogger.Info(string.Format("[{0}.{1}] - {2}", type.Namespace, type.Name, message));
        }
        public static void Info(string typeName, String message, Exception exception)
        {
            fileLogger.Info(string.Format("[{0}] - {1}", typeName, message), exception);
        }
        public static void Info(string typeName, String message)
        {
            fileLogger.Info(string.Format("[{0}] - {1}", typeName, message));
        }
        public static void Warn(object o, String message, Exception exception)
        {
            Type type = o.GetType();
            fileLogger.Warn(string.Format("[{0}.{1}] - {2}", type.Namespace, type.Name, message), exception);
        }
        public static void Warn(object o, String message)
        {
            Type type = o.GetType();
            fileLogger.Warn(string.Format("[{0}.{1}] - {2}", type.Namespace, type.Name, message));
        }
        public static void Warn(string typeName, String message, Exception exception)
        {
            fileLogger.Warn(string.Format("[{0}] - {1}", typeName, message), exception);
        }
        public static void Warn(string typeName, String message)
        {
            fileLogger.Warn(string.Format("[{0}] - {1}", typeName, message));
        }
        public static void Error(object o, String message, Exception exception)
        {
            Type type = o.GetType();
            fileLogger.Error(string.Format("[{0}.{1}] - {2}", type.Namespace, type.Name, message), exception);
        }
        public static void Error(object o, String message)
        {
            Type type = o.GetType();
            fileLogger.Error(string.Format("[{0}.{1}] - {2}", type.Namespace, type.Name, message));
        }
        public static void Error(string typeName, String message, Exception exception)
        {
            fileLogger.Error(string.Format("[{0}] - {1}", typeName, message), exception);
        }
        public static void Error(string typeName, String message)
        {
            errLogger.Error(string.Format("[{0}] - {1}", typeName, message));
        }
        public static void Fatal(object o, String message, Exception exception)
        {
            Type type = o.GetType();
            fileLogger.Fatal(string.Format("[{0}.{1}] - {2}", type.Namespace, type.Name, message), exception);
        }
        public static void Fatal(object o, String message)
        {
            Type type = o.GetType();
            fileLogger.Fatal(string.Format("[{0}.{1}] - {2}", type.Namespace, type.Name, message));
        }
        public static void Fatal(string typeName, String message, Exception exception)
        {
            fileLogger.Fatal(string.Format("[{0}] - {1}", typeName, message), exception);
        }
        public static void Fatal(string typeName, String message)
        {
            fileLogger.Fatal(string.Format("[{0}] - {1}", typeName, message));
        }
    }
}
