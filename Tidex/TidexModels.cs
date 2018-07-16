using Newtonsoft.Json;
using Shared.Broker;
using Shared.Models;
using System.Collections.Generic;

namespace Tidex
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string TidexSymbol(Instrument instr)
        {
            return $"{instr.First}_{instr.Second}";
        }
    }

    public class Symbol
    {
        public double high { get; set; }
        public double low { get; set; }
        public double avg { get; set; }
        public double vol { get; set; }
        public double vol_cur { get; set; }
        public double last { get; set; }
        public double buy { get; set; }
        public double sell { get; set; }
        public double updated { get; set; }
    }

    public class TickerResponse
    {
        public Symbol symbol { get; set; }
    }

  
    public class Instruments
    {
        [JsonProperty("server_time")]
        public long ServerTime { get; set; }

        [JsonProperty("pairs")]
        public Dictionary<string, Pair> Pairs { get; set; }
    }

    public partial class Pair
    {
        public int decimal_places { get; set; }
        public double min_price { get; set; }
        public double max_price { get; set; }
        public double min_amount { get; set; }
        public double max_amount { get; set; }
        public double min_total { get; set; }
        public int hidden { get; set; }
        public double fee { get; set; }
    }
}



