using Shared.Models;
using System.Collections.Generic;

namespace CryptoExchanges2.Database
{
    public interface ITickStoreService
    {
        void Clear(Instrument instr);
        void Add(Instrument instr, Tick tick);
        void Add(Instrument instr, IEnumerable<Tick> tick);
        IEnumerable<Tick> Get(Instrument instr);
    }
}
