using Shared.Broker;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Allcoin
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string AllcoinSymbol(Instrument instr)
        {
            return $"{instr.First}2{instr.Second}";
        }
    }

    public class Data
    {
        public double high { get; set; }
        public double low { get; set; }
        public string buy { get; set; }
        public string sell { get; set; }
        public double last { get; set; }
        public double vol { get; set; }
    }

    public class TickerResponse
    {
        public int code { get; set; }
        public string msg { get; set; }
        public Data data { get; set; }
    }
}


