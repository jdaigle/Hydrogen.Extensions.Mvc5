using System;
using System.Threading;

/*
 * Based on: https://github.com/StephenCleary/AsyncEx/blob/edb2c6b66d41471008a56e4098f9670b5143617e/src/Nito.AsyncEx.Tasks/SynchronizationContextSwitcher.cs
 */
namespace MvcAsync
{
    public sealed class SynchronizationContextSwitcher : IDisposable
    {
        private readonly SynchronizationContext _oldContext;

        private readonly object _disposeLock = new object();

        private bool _disposed = false;

        public SynchronizationContextSwitcher(SynchronizationContext newContext)
        {
            _oldContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(newContext);
        }

        void IDisposable.Dispose()
        {
            if (_disposed)
            {
                return;
            }
            lock (_disposeLock)
            {
                if (_disposed)
                {
                    return;
                }
                SynchronizationContext.SetSynchronizationContext(_oldContext);
                _disposed = true;
            }
        }

        public static void NoContext(Action action)
        {
            using (new SynchronizationContextSwitcher(null))
            {
                action();
            }
        }
    }
}
