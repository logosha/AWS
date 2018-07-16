using Shared.Broker;
using Shared.Models;

namespace Liqui
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string LiquiSymbol(Instrument instr)
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
        public int updated { get; set; }
    }
}
