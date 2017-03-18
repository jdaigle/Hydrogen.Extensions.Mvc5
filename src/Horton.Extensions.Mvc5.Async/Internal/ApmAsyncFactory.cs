// Derived from: https://github.com/StephenCleary/AsyncEx/blob/edb2c6b66d41471008a56e4098f9670b5143617e/src/Nito.AsyncEx.Tasks/Interop/ApmAsyncFactory.cs
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
using System.Threading.Tasks;

namespace Horton.Extensions.Mvc5.Async.Internal
{
    /// <summary>
    /// Creation methods for tasks wrapping the Asynchronous Programming Model (APM), and APM wrapper methods around tasks.
    /// </summary>
    public static class ApmAsyncFactory
    {
        /// <summary>
        /// Wraps a <see cref="Task"/> into the Begin method of an APM pattern.
        /// </summary>
        /// <param name="task">The task to wrap.</param>
        /// <param name="callback">The callback method passed into the Begin method of the APM pattern.</param>
        /// <param name="state">The state passed into the Begin method of the APM pattern.</param>
        /// <returns>The asynchronous operation, to be returned by the Begin method of the APM pattern.</returns>
        public static IAsyncResult ToBegin(Task task, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<object>(state/* TODO: Requires .NET 4.6, TaskCreationOptions.RunContinuationsAsynchronously*/);
            SynchronizationContextSwitcher.NoContext(() => CompleteAsync(task, callback, tcs));
            return tcs.Task;
        }

        // `async void` is on purpose, to raise `callback` exceptions directly on the thread pool.
        private static async void CompleteAsync(Task task, AsyncCallback callback, TaskCompletionSource<object> tcs)
        {
            try
            {
                await task.ConfigureAwait(false);
                tcs.TrySetResult(null);
            }
            catch (OperationCanceledException/* ex*/)
            {
                tcs.TrySetCanceled();
                // TODO: Requires .NET 4.6
                // tcs.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            finally
            {
                callback?.Invoke(tcs.Task);
            }
        }

        /// <summary>
        /// Wraps a <see cref="Task"/> into the End method of an APM pattern.
        /// </summary>
        /// <param name="asyncResult">The asynchronous operation returned by the matching Begin method of this APM pattern.</param>
        /// <returns>The result of the asynchronous operation, to be returned by the End method of the APM pattern.</returns>
        public static void ToEnd(IAsyncResult asyncResult)
        {
            ((Task)asyncResult).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Wraps a <see cref="Task{TResult}"/> into the Begin method of an APM pattern.
        /// </summary>
        /// <param name="task">The task to wrap. May not be <c>null</c>.</param>
        /// <param name="callback">The callback method passed into the Begin method of the APM pattern.</param>
        /// <param name="state">The state passed into the Begin method of the APM pattern.</param>
        /// <returns>The asynchronous operation, to be returned by the Begin method of the APM pattern.</returns>
        public static IAsyncResult ToBegin<TResult>(Task<TResult> task, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<TResult>(state/* TODO: Requires .NET 4.6, TaskCreationOptions.RunContinuationsAsynchronously*/);
            SynchronizationContextSwitcher.NoContext(() => CompleteAsync(task, callback, tcs));
            return tcs.Task;
        }

        // `async void` is on purpose, to raise `callback` exceptions directly on the thread pool.
        private static async void CompleteAsync<TResult>(Task<TResult> task, AsyncCallback callback, TaskCompletionSource<TResult> tcs)
        {
            try
            {
                tcs.TrySetResult(await task.ConfigureAwait(false));
            }
            catch (OperationCanceledException/* ex*/)
            {
                tcs.TrySetCanceled();
                // TODO: Requires .NET 4.6
                // tcs.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            finally
            {
                callback?.Invoke(tcs.Task);
            }
        }

        /// <summary>
        /// Wraps a <see cref="Task{TResult}"/> into the End method of an APM pattern.
        /// </summary>
        /// <param name="asyncResult">The asynchronous operation returned by the matching Begin method of this APM pattern.</param>
        /// <returns>The result of the asynchronous operation, to be returned by the End method of the APM pattern.</returns>
        public static TResult ToEnd<TResult>(IAsyncResult asyncResult)
        {
            return ((Task<TResult>)asyncResult).GetAwaiter().GetResult();
        }
    }
}
