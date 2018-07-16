using CryptoExchanges2.Database;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using TeaTime;

namespace CryptoExchanges2
{
    public class TeaTickStoreService : ITickStoreService
    {
        private string _db_directory;
        public TeaTickStoreService(string db_directory)
        {
            _db_directory = db_directory;
        }

        public void Add(Instrument instr, Tick tick)
        {
            var fileName = getFileBy(instr);
            if (!File.Exists(fileName))
            {
                using (var tf = TeaFile<Tick>.Create(fileName))
                {
                    tf.Write(tick);
                }
            }
            else
            {
                using (var tf = TeaFile<Tick>.Append(fileName))
                {
                    tf.Write(tick);
                }
            }
        }

        public void Add(Instrument instr, IEnumerable<Tick> tick)
        {
            throw new NotImplementedException();
        }

        public void Clear(Instrument instr)
        {
            var fileName = getFileBy(instr);

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }

        public IEnumerable<Tick> Get(Instrument instr)
        {
            var fileName = getFileBy(instr);

            using (var tf = TeaFile<Tick>.OpenWrite(fileName))
            {
                foreach (var item in tf.Items)
                {
                    yield return item;
                } 
            }
        }

        private string getFileBy(Instrument instr)
        {
            return _db_directory + instr.Exchange + "_" + instr.First + instr.Second + ".tea";
        }
    }
}
