using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Mvc.Async;
using System.Web.Mvc.Routing;
using System.Web.Routing;

namespace MvcAsync
{
    public class AsyncControllerEx : Controller
    {
        public Task ExecuteAsync(RequestContext requestContext)
        {
            if (DisableAsyncSupport)
            {
                // For backwards compat, we can disallow async support and just chain to the sync Execute() function.
                Execute(requestContext);
                return Task.CompletedTask;
            }

            if (requestContext == null)
            {
                throw new ArgumentNullException(nameof(requestContext));
            }

            // Support Asynchronous behavior. 
            // Execute/ExecuteCore are no longer called.

            //VerifyExecuteCalledOnce();
            Initialize(requestContext);

            string actionName = GetActionName(RouteData);
            IActionInvoker invoker = ActionInvoker;
            if (invoker is AsyncControllerActionInvokerEx asyncInvokerEx)
            {
                return asyncInvokerEx.InvokeActionAsync(ControllerContext, actionName);
            }
            if (invoker is IAsyncActionInvoker asyncInvoker)
            {
                // asynchronous invocation
                return InvokeWrappedIAsyncActionInvoker(asyncInvoker, actionName);
            }
            else
            {
                // synchronous invocation
                if (!invoker.InvokeAction(ControllerContext, actionName))
                {
                    HandleUnknownAction(actionName);
                }
                return Task.CompletedTask;
            }
        }

        private string GetActionName(RouteData routeData)
        {
            // If this is an attribute routing match then the 'RouteData' has a list of sub-matches rather than
            // the traditional controller and action values. When the match is an attribute routing match
            // we'll pass null to the action selector, and let it choose a sub-match to use.
            if (routeData.HasDirectRouteMatch())
            {
                return null;
            }
            else
            {
                return routeData.GetRequiredString("action");
            }
        }

        private async Task InvokeWrappedIAsyncActionInvoker(IAsyncActionInvoker asyncInvoker, string actionName)
        {
            var actionHandled = await Task.Factory.FromAsync(asyncInvoker.BeginInvokeAction, asyncInvoker.EndInvokeAction, ControllerContext, actionName, null).ConfigureAwait(false);
            if (!actionHandled)
            {
                HandleUnknownAction(actionName);
            }
        }

        protected override IAsyncResult BeginExecute(RequestContext requestContext, AsyncCallback callback, object state)
        {
            // See: http://blog.stephencleary.com/2012/07/async-interop-with-iasyncresult.html
            // and: https://social.msdn.microsoft.com/Forums/en-US/9535a4a6-6218-45fe-aa45-79332b9e5b88/trampolining-considerations-for-apm-wrappers?forum=async
            // and: https://github.com/StephenCleary/AsyncEx/blob/master/src/Nito.AsyncEx.Tasks/Interop/ApmAsyncFactory.cs

            var task = ExecuteAsync(requestContext);
            var tcs = new TaskCompletionSource<object>(state, TaskCreationOptions.RunContinuationsAsynchronously);
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
            catch (OperationCanceledException ex)
            {
                tcs.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            finally
            {
                if (callback != null)
                {
                    callback.Invoke(tcs.Task);
                }
            }
        }

        protected override void EndExecute(IAsyncResult asyncResult)
        {
            // Wait and Unwrap any Exceptions
            ((Task)asyncResult).GetAwaiter().GetResult();
        }
    }
}
