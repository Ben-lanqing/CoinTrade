using CoreLibrary.Model;
using HFTRobot;
using MarketRobot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TradeRobot;

namespace CoinTrade
{
    class Program
    {

        static void Main(string[] args)
        {
            RobotHFT hft = new RobotHFT();
            hft.Run();

            while (true)
            {
                Console.ReadLine();
            }

        }

    }
}
