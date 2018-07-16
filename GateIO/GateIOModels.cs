using Shared.Broker;
using Shared.Models;
using System;
using System.Collections.Generic;

namespace GateIO
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
        public MarketBook market;
        public int lvl;
        public int id;
        public Subscribe(Instrument instr, SubscriptionModel model, MarketBook markt, int lvl, int id)
        {
            this.pair = instr;
            this.mode = model;
            this.market = markt;
            this.lvl = lvl;
            this.id = id;
        }
        public Subscribe()
        {
        }
    }

    public class Bid
    {
        public double price;
        public double size;
    }

    public class Ask
    {
        public double price;
        public double size;
    }

    public class AskBidResponse
    {
        public List<List<string>> asks { get; set; }
        public List<List<string>> bids { get; set; }
    }

    public static class Utilities
    {
        public static string GateIOSymbol(Instrument instr)
        {
            return $"{instr.First}_{instr.Second}";
        }
    }

    public class Result
    {
        public int period { get; set; }
        public string open { get; set; }
        public string close { get; set; }
        public string high { get; set; }
        public string low { get; set; }
        public string last { get; set; }
        public string change { get; set; }
        public string quoteVolume { get; set; }
        public string baseVolume { get; set; }
        public string status { get; set; }
    }

    public class GateIOResponse
    {
        public string error { get; set; }
        public Result result { get; set; }
        public int id { get; set; }
    }

    public class GateIOUpdateResponse
    {
        public string method { get; set; }
        public List<object> @params { get; set; }
        public object id { get; set; }
    }
}

