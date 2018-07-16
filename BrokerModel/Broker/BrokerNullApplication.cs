using System.Collections.Generic;
using System.Linq;
using Shared.Interfaces;
using Shared.Models;

namespace Shared.Broker
{
    public class BrokerNullApplication : IBrokerApplication
    {
        ILogger _logger;
        public BrokerNullApplication(ILogger logger) { _logger = logger; }
        public void OnEvent(string exchangeName, BrokerEvent what, string details)
        {
            _logger.Log(LogPriority.Debug, $"Broker({exchangeName}) Event | {what.ToString()} - {details}");
        }

        public void OnReport(string exchangeName, string symbol, MarketBook topOfBook)
        {
            _logger.Log(LogPriority.Debug, $"Broker({exchangeName}) " +
                $"Pair: {symbol}  " +
                $"Ask: {topOfBook.AskPrice} , " +
                $" Bid: {topOfBook.BidPrice} ," +
                $"AskSise: {topOfBook.AskSize} , " +
                $" BidSise: {topOfBook.BidSize}");
        }
    }
}
