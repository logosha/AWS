using Shared.Broker;
using Shared.Models;
using System.Collections.Generic;

namespace Poloniex
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string PoloniexSymbol(Instrument instr)
        {
            return $"{instr.First}_{instr.Second}";
        }
    }

    public class Pair
    {
        public double last { get; set; }
        public double lowestAsk { get; set; }
        public double highestBid { get; set; }
        public double percentChange { get; set; }
        public double baseVolume { get; set; }
        public double quoteVolume { get; set; }
    }

    public class Index
    {
        public Dictionary<string, int> channels;
        public Index()
        {
            channels = new Dictionary<string, int>
        {

            { "BTC_BCN",  7},
            { "BTC_BELA", 8},
            {"BTC_BLK",  10},
            {"BTC_BTCD", 12},
            {"BTC_BTM",  13},
            {"BTC_BTS",  14},
            {"BTC_BURST",15},
            {"BTC_CLAM", 20},
            {"BTC_DASH", 24},
            {"BTC_DGB",  25},
            {"BTC_DOGE", 27},
            {"BTC_EMC2", 28},
            {"BTC_FLDC ",31},
            {"BTC_FLO",  32},
            {"BTC_GAME", 38},
            {"BTC_GRC",  40},
            {"BTC_HUC",  43},
            {"BTC_LTC",  50},
            {"BTC_MAID", 51},
            {"BTC_OMNI", 58},
            {"BTC_NAV",  61},
            {"BTC_NEOS", 63},
            {"BTC_NMC",  64},
            {"BTC_NXT",  69},
            {"BTC_PINK", 73},
            {"BTC_POT",  74},
            {"BTC_PPC",  75},
            {"BTC_RIC",  83},
            {"BTC_STR",  89},
            {"BTC_SYS",  92},
            {"BTC_VIA",  97},
            {"BTC_XVC",  98},
            {"BTC_VRC",  99},
            {"BTC_VTC",  100},
            {"BTC_XBC",  104},
            {"BTC_XCP",  108},
            {"BTC_XEM",  112},
            {"BTC_XMR ", 114},
            {"BTC_XPM",  116},
            {"BTC_XRP",  117},
            {"USDT_BTC", 121},
            {"USDT_DASH", 122},
            {"USDT_LTC", 123},
            {"USDT_NXT", 124},
            {"USDT_STR", 125},
            {"USDT_XMR", 126},
            {"USDT_XRP", 127},
            {"XMR_BCN",  129},
            {"XMR_BLK",  130},
            {"XMR_BTCD", 131 },
            {"XMR_DASH", 132},
            {"XMR_LTC",  137},
            {"XMR_MAID",  138},
            {"XMR_NXT",  140},
            {"BTC_ETH",  148},
            {"USDT_ETH", 149},
            {"BTC_SC",  150},
            {"BTC_BCY",  151},
            {"BTC_EXP",  153},
            {"BTC_FCT",  155},
            {"BTC_RADS", 158},
            {"BTC_AMP",  160},
            {"BTC_DCR",  162},
            {"BTC_LSK",  163},
            {"ETH_LSK",  166},
            {"BTC_LBC",  167},
            {"BTC_STEEM",168},
            {"ETH_STEEM",169},
            {"BTC_SBD",  170},
            {"BTC_ETC",  171},
            {"ETH_ETC",  172},
            {"USDT_ETC", 173},
            {"BTC_REP",  174},
            {"USDT_REP", 175},
            {"ETH_REP",  176},
            {"BTC_ARDR", 177},
            {"BTC_ZEC",  178},
            {"ETH_ZEC",  179},
            {"USDT_ZEC", 180},
            {"XMR_ZEC",  181},
            {"BTC_STRAT",182},
            {"BTC_NXC",  183},
            {"BTC_PASC", 184},
            {"BTC_GNT",  185},
            {"ETH_GNT",  186},
            {"BTC_GNO",  187},
            {"ETH_GNO",  188},
            {"BTC_BCH",  189},
            {"ETH_BCH",  190},
            {"USDT_BCH",  191},
            {"BTC_ZRX",  192},
            {"ETH_ZRX",  193},
            {"BTC_CVC",  194},
            {"ETH_CVC",  195},
            {"BTC_OMG",  196},
            {"ETH_OMG",  197},
            {"BTC_GAS",  198},
            {"ETH_GAS",  199},
            {"BTC_STORJ",200}
        };

        }
    }
}
