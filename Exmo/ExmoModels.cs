using Shared.Broker;
using Shared.Models;
using System.Collections.Generic;

namespace Exmo
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string ExmoSymbol(Instrument instr)
        {
            return $"{instr.First}_{instr.Second}";
        }
    }

    public class Ticker
    {
        public string buy_price { get; set; }
        public string sell_price { get; set; }
        public string last_trade { get; set; }
        public string high { get; set; }
        public string low { get; set; }
        public string avg { get; set; }
        public string vol { get; set; }
        public string vol_curr { get; set; }
        public int updated { get; set; }
    }
}
