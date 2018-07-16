using Shared.Broker;
using Shared.Models;
using System;

namespace BitFlyer
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

    public static class Utilities
    {
        public static string BitFlyerSymbol(Instrument instr)
        {
            return $"{instr.First}_{instr.Second}";
        }
    }

    public class Message
    {
        public string product_code { get; set; }
        public DateTime timestamp { get; set; }
        public int tick_id { get; set; }
        public double best_bid { get; set; }
        public double best_ask { get; set; }
        public double best_bid_size { get; set; }
        public double best_ask_size { get; set; }
        public double total_bid_depth { get; set; }
        public double total_ask_depth { get; set; }
        public double ltp { get; set; }
        public double volume { get; set; }
        public double volume_by_product { get; set; }
    }

    public class Params
    {
        public string channel { get; set; }
        public Message message { get; set; }
    }

    public class TickerResponse
    {
        public string jsonrpc { get; set; }
        public string method { get; set; }
        public Params @params { get; set; }
        public long id { get; set; }
        public bool result { get; set; }
    }

    public class InstrumentResponse
    {
        public string product_code { get; set; }
        public string alias { get; set; }
    }
}

