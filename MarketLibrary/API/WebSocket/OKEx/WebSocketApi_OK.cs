/*
*Copyright (C),2015-2016,Tellyes Scientific.
*File Name: 	WebSocketApi_FC
*Author:		王滨
*Version:		V1.00
*Date:			2018/8/21 16:21:37
*Description:	尚未编写描述
*Update History:
*<Author> <date> <Version> <Description>
*更新的版本的作者等信息，新的版本信息往下
*/
using CoreLibrary;
using Lq.Log4Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace MarketLibrary.API.WebSocket
{
    public class WebSocketApi_OK
    {
        WebSocketSharp.WebSocket websocket;
        Dictionary<string, string> topicDic = new Dictionary<string, string>();
        bool isOpened = false;
        string WEBSOCKET_API = "wss://real.okex.com:10441/websocket";
        System.Timers.Timer heartBeatTimer = new System.Timers.Timer(2000); //
        long countConnect = 0;

        public event EventHandler<OKMessageReceivedEventArgs> OnMessage;
        public event EventHandler OnClosed;


        public bool Connnet()
        {
            try
            {
                websocket = new WebSocketSharp.WebSocket(WEBSOCKET_API);

                websocket.OnError += (sender, e) =>
                {
                    Console.WriteLine("Error:" + e.Exception.Message.ToString());
                    Log4NetUtility.Debug("OnError", e.Exception.Message);
                };
                websocket.OnOpen += OnOpened;
                websocket.OnClose += Websocket_Closed; ;
                websocket.OnMessage += ReceviedMsg;
                websocket.ConnectAsync();
                while (!websocket.IsAlive)
                {
                    Console.WriteLine("Waiting WebSocket connnet......");
                    Thread.Sleep(1000);
                }
                heartBeatTimer.Elapsed += new System.Timers.ElapsedEventHandler(heatBeat);
                //heartBeatTimer.Start();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:" + ex.Message);
                Log4NetUtility.Error("WebSocketApi_OK", Utils.Exception2String(ex));

            }
            return true;
        }
        private void heatBeat(object sender, System.Timers.ElapsedEventArgs e)
        {
            websocket.Send("{'event':'ping'}");
        }

        private void Websocket_Closed(object sender, EventArgs e)
        {
            heartBeatTimer.Close();
            Console.WriteLine($"Websocket_Closed");
            OnClosed?.Invoke(null, null);
            websocket.ConnectAsync();
            countConnect++;
            while (!websocket.IsAlive)
            {
                Console.WriteLine("Waiting WebSocket connnet......");
                Thread.Sleep(1000);
            }

        }

        /// <summary>
        /// 连通WebSocket，发送订阅消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnOpened(object sender, EventArgs e)
        {
            Console.WriteLine($"OnOpened Topics Count:{topicDic.Count}");
            isOpened = true;
            foreach (var item in topicDic)
            {
                SendSubscribeTopic(item.Value);
            }
            heartBeatTimer.Start();
        }

        /// <summary>
        /// 响应心跳包&接收消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void ReceviedMsg(object sender, MessageEventArgs args)
        {
            if (args.Data.IndexOf("pong") != -1 || args.Data.IndexOf("addChannel") != -1) //响应心跳包
            {
                return;
            }
            ////if (args.Data.IndexOf("topics") != -1)
            ////{
            ////    Console.WriteLine($"ReceviedMsg:{args.Data}");
            ////    return;

            ////}
            ////if (args.Data.Contains("msg"))
            ////{
            ////    Console.WriteLine($"ReceviedMsg:{args.Data}");
            ////    return;

            ////}

            OnMessage?.Invoke(null, new OKMessageReceivedEventArgs(args.Data));


        }

        public void Subscribe(string msg)
        {
            if (topicDic.ContainsKey(msg))
                return;
            topicDic.Add(msg, msg);
            if (isOpened)
            {
                SendSubscribeTopic(msg);
            }
        }
        public void SendSubscribeTopic(string msg)
        {
            websocket.Send(msg);
            if (!msg.Contains("ping"))
            {
                Console.WriteLine(msg);
            }
        }

    }

    public class OKMessageReceivedEventArgs : EventArgs
    {
        public OKMessageReceivedEventArgs(string message)
        {
            this.Message = message;
        }

        public string Message { get; set; }
    }

}
