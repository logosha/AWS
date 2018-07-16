using Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shared.Broker
{
    public enum BrokerEvent
    {
        ConnectorStarted,
        ConnectorStopped,
        SessionLogon,
        SessionLogout,
        InternalError,
        ParseError,
        Info,
        Debug,
        CoinSubscribed,
        CoinUnsubscribed,
        CoinSubscribedFault,
        CoinUnsubscribedFault,
        SubscribedCoinIsNotValid,
    };

    public interface IBrokerApplication
    {
        void OnEvent  (string exchangeName, BrokerEvent what, string details);
        void OnReport(string exchangeName, string symbol, MarketBook topOfBook);
    }

    public enum SubscriptionModel
    {
        TopBook,
    };

    public enum PropertyName
    {
        IsWebsoketSupport
    }

    public interface IBrokerClient
    {
        Task<MarketBook?> GetTicker(Instrument instr);
        Task Subscribe(Instrument instr, SubscriptionModel model);
        Task Unsibscribe(Instrument instr, SubscriptionModel model);
        Task<IEnumerable<Instrument>> GetInstruments();
        string Name { get; }
        bool? IsConnected { get; }
        Task Start();
        Task Stop();
        bool? IsStarted { get; }
        bool GetProperty(PropertyName name);
    }
}

