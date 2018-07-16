using Shared.Broker;
using Shared.Models;

namespace Bithumb
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string BithumbSymbol(Instrument instr)
        {
            return $"{instr.First}{instr.Second}";
        }
    }
}


