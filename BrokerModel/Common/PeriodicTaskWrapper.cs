using System.Threading;
using System.Threading.Tasks;
using Shared.Interfaces;
using System;

namespace Shared.Common
{
    public class PeriodicTaskWrapper : IPeriodicTask
    {
        private CancellationToken _token;
        private ILogger _loger;
        private readonly string _name;
        private Task _task;

        public PeriodicTaskWrapper(ILogger loger, string Name)
        {
            _loger = loger;
            _name = Name;
        }

        public bool IsCancellationRequested { get { return _token.IsCancellationRequested; } }

        public Task Task { get { return _task; } }

        public async Task Start(PeriodicTaskAction<object, CancellationToken> action, PeriodicTaskParams param, CancellationToken _token)
        {
            PeriodicTaskAction<object, CancellationToken> _action = action;
            object o = null;

            if (param.delay != 0)
                await Task.Delay(param.delay);

            _task = Task.Run(async () =>
            {
                while (!_token.IsCancellationRequested)
                {
                    try
                    {
                        await _action(o, _token);
                    }
                    catch (Exception ex)
                    {
                        _loger.Log(LogPriority.Debug, $"Exception: {ex.Message} {ex.StackTrace}", _name);
                    }
                    finally
                    {
                        await Task.Delay(param.period, _token);
                    }
                }
            }

            );
        }
    }
}

