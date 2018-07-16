using Shared.Broker;
using Shared.Models;

namespace BitcoinIndonesia
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string BitcoinIndonesiaSymbol(Instrument instr)
        {
            return $"{instr.First}_{instr.Second}";
        }
    }

    public class Ticker
    {
        public string high { get; set; }
        public string low { get; set; }
        public string vol_btc { get; set; }
        public string vol_idr { get; set; }
        public string last { get; set; }
        public string buy { get; set; }
        public string sell { get; set; }
        public long server_time { get; set; }
    }

    public class TickerResponse
    {
        public Ticker ticker { get; set; }
    }
}


