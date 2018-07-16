﻿using System;
using Shared;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared.Models;
using Shared.Broker;


namespace Bittrex
{
    class BittrexSessionException : Exception
    { }

    public class Utils
    {
        static public string BittrexSymbol(Instrument instr)
        {
            //return $"{instr.First}-{instr.Second}";
            return $"{instr.First}-{instr.Second}";
            // Bittrex uses not ETHBTC but BTCETH, but it provides rate as ETHBTC
        }
    }

    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
        public GenericRequest request;
        public string Id;
        public Market market;
        public Subscribe(Instrument instr, SubscriptionModel model)
        {
            this.pair = instr;
            this.mode = model;
            this.Id = instr.Id();
            switch (model)
            {
            case SubscriptionModel.TopBook:
                {
                    this.request = new TickerRequest(Utils.BittrexSymbol(instr));
                    market = new Market();
                    break;
                }
            }
        }
    }

    #region --------  Request classes

    public class GenericRequest
    {
        public string request;
        public string nonce;
        public Dictionary<string, object> options = new Dictionary<string, object>();
    }

    public class TickerRequest : GenericRequest
    {
        public TickerRequest(string pair)
        {
            this.request = "/public/getticker" + "?market=" + pair;
        }
    }

    public class InstrumentRequest : GenericRequest
    {
        public InstrumentRequest()
        {
            this.request = "/public/getmarkets";
        }
    }

    #endregion

    public class Market
    {
        public int nonce;
        public SortedDictionary<double, double> ask_list;
        public SortedDictionary<double, double> bid_list;
        public Market()
        {
            ask_list = new SortedDictionary<double, double>();
            bid_list = new SortedDictionary<double, double>();
        }
    }

    public enum msgType
    {
        FullSnapShot = 2,
        Update = 3
    }


    public enum UpdMarket
    {
        Add = 0,
        Remove = 1,
        Update = 2
    }

    public class Keys
    {
        public static string Ask = "A";
        public static string Available = "a";
        public static string Bid = "B";
        public static string Balance = "b";
        public static string Closed = "C";
        public static string Currency = "c";
        public static string CancelInitiated = "CI";
        public static string Deltas = "D";
        public static string Delta = "d";
        public static string OrderDeltaType = "DT";
        public static string Exchange = "E";
        public static string ExchangeDeltaType = "e";
        public static string FillType = "F";
        public static string FillId = "FI";
        public static string Fills = "f";
        public static string OpenBuyOrders = "G";
        public static string OpenSellOrders = "g";
        public static string High = "H";
        public static string AutoSell = "h";
        public static string Id = "I";
        public static string IsOpen = "i";
        public static string Condition = "J";
        public static string ConditionTarget = "j";
        public static string ImmediateOrCancel = "K";
        public static string IsConditional = "k";
        public static string Low = "L";
        public static string Last = "l";
        public static string MarketName = "M";
        public static string BaseVolume = "m";
        public static string Nonce = "N";
        public static string CommissionPaid = "n";
        public static string Orders = "O";
        public static string Order = "o";
        public static string OrderType = "OT";
        public static string OrderUuid = "OU";
        public static string Price = "P";
        public static string CryptoAddress = "p";
        public static string PrevDay = "PD";
        public static string PricePerUnit = "PU";
        public static string Quantity = "Q";
        public static string QuantityRemaining = "q";
        public static string Rate = "R";
        public static string Requested = "r";
        public static string Sells = "S";
        public static string Summaries = "s";
        public static string TimeStamp = "T";
        public static string Total = "t";
        public static string Type = "TY";
        public static string Uuid = "U";
        public static string Updated = "u";
        public static string Volume = "V";
        public static string AccountId = "W";
        public static string AccountUuid = "w";
        public static string Limit = "X";
        public static string Created = "x";
        public static string Opened = "Y";
        public static string State = "y";
        public static string Buys = "Z";
        public static string Pending = "z";
    }
}
