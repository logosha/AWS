using Shared.Broker;
using Shared.Models;

namespace QuadrigaCX
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string QuadrigaCXSymbol(Instrument instr)
        {
            return $"{instr.First}_{instr.Second}";
        }
    }

    public class TickerResponse
    {
        public string high { get; set; }
        public string last { get; set; }
        public string timestamp { get; set; }
        public string volume { get; set; }
        public string vwap { get; set; }
        public string low { get; set; }
        public string ask { get; set; }
        public string bid { get; set; }
    }
}


