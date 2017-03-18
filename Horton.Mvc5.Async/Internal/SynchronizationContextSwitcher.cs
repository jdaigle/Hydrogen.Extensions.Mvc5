// Derived from: https://github.com/StephenCleary/AsyncEx/blob/edb2c6b66d41471008a56e4098f9670b5143617e/src/Nito.AsyncEx.Tasks/SynchronizationContextSwitcher.cs
// The MIT License (MIT)
//
// Copyright (c) 2014 StephenCleary
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Threading;

namespace Horton.Mvc5.Async.Internal
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
