using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Async;
using System.Web.Mvc.Filters;
using System.Web.Mvc.Routing;
using Microsoft.Web.Infrastructure.DynamicValidationHelper;

namespace MvcAsync
{
    public class AsyncControllerActionInvokerEx : AsyncControllerActionInvoker, IAsyncActionInvoker
    {
        private static readonly Task<bool> _cachedTaskFromResultTrue = Task.FromResult(true);
        private static readonly Task<bool> _cachedTaskFromResultFalse = Task.FromResult(false);

        public Task<bool> InvokeActionAsync(ControllerContext controllerContext, string actionName)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException(nameof(controllerContext));
            }

            Contract.Assert(controllerContext.RouteData != null);
            if (string.IsNullOrEmpty(actionName) && !controllerContext.RouteData.HasDirectRouteMatch())
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(actionName));
            }

            var controllerDescriptor = GetControllerDescriptor(controllerContext);
            var actionDescriptor = FindAction(controllerContext, controllerDescriptor, actionName);
            if (actionDescriptor == null)
            {
                return _cachedTaskFromResultFalse;
            }

            var filterInfo = GetFilters(controllerContext, actionDescriptor);

            try
            {
                var authenticationContext = InvokeAuthenticationFilters(controllerContext, filterInfo.AuthenticationFilters, actionDescriptor);
                if (authenticationContext.Result != null)
                {
                    // An authentication filter signaled that we should short-circuit the request. Let all
                    // authentication filters contribute to an action result (to combine authentication
                    // challenges). Then, run this action result.
                    var challengeContext = InvokeAuthenticationFiltersChallenge(controllerContext, filterInfo.AuthenticationFilters, actionDescriptor, authenticationContext.Result);
                    InvokeActionResult(controllerContext, challengeContext.Result ?? authenticationContext.Result);
                    return _cachedTaskFromResultTrue;
                }

                AuthorizationContext authorizationContext = InvokeAuthorizationFilters(controllerContext, filterInfo.AuthorizationFilters, actionDescriptor);
                if (authorizationContext.Result != null)
                {
                    // An authorization filter signaled that we should short-circuit the request. Let all
                    // authentication filters contribute to an action result (to combine authentication
                    // challenges). Then, run this action result.
                    var challengeContext = InvokeAuthenticationFiltersChallenge(controllerContext, filterInfo.AuthenticationFilters, actionDescriptor, authorizationContext.Result);
                    InvokeActionResult(controllerContext, challengeContext.Result ?? authorizationContext.Result);
                    return _cachedTaskFromResultTrue;
                }

                if (controllerContext.Controller.ValidateRequest)
                {
                    ValidateRequest(controllerContext);
                }

                var parameters = GetParameterValues(controllerContext, actionDescriptor);
                return InvokeInvokeActionMethodWithFiltersAsync(controllerContext, filterInfo, actionDescriptor, parameters);
            }
            catch (ThreadAbortException)
            {
                // This type of exception occurs as a result of Response.Redirect(), but we special-case so that
                // the filters don't see this as an error.
                throw;
            }
            catch (Exception ex)
            {
                // something blew up, so execute the exception filters
                var exceptionContext = InvokeExceptionFilters(controllerContext, filterInfo.ExceptionFilters, ex);
                if (!exceptionContext.ExceptionHandled)
                {
                    throw;
                }
                InvokeActionResult(controllerContext, exceptionContext.Result);
            }

            return _cachedTaskFromResultTrue;
        }

        public async Task<bool> InvokeInvokeActionMethodWithFiltersAsync(ControllerContext controllerContext, FilterInfo filterInfo, ActionDescriptor actionDescriptor, IDictionary<string, object> parameters)
        {
            try
            {
                var postActionContext = await InvokeActionMethodWithFiltersAsync(controllerContext, filterInfo.ActionFilters, actionDescriptor, parameters).ConfigureAwait(false);

                // The action succeeded. Let all authentication filters contribute to an action
                // result (to combine authentication challenges; some authentication filters need
                // to do negotiation even on a successful result). Then, run this action result.
                AuthenticationChallengeContext postChallengeContext
                    = InvokeAuthenticationFiltersChallenge(controllerContext, filterInfo.AuthenticationFilters, actionDescriptor, postActionContext.Result);

                InvokeActionResultWithFilters(controllerContext, filterInfo.ResultFilters, postChallengeContext.Result ?? postActionContext.Result);
            }
            catch (ThreadAbortException)
            {
                // This type of exception occurs as a result of Response.Redirect(), but we special-case so that
                // the filters don't see this as an error.
                throw;
            }
            catch (Exception ex)
            {
                // something blew up, so execute the exception filters
                ExceptionContext exceptionContext = InvokeExceptionFilters(controllerContext, filterInfo.ExceptionFilters, ex);
                if (!exceptionContext.ExceptionHandled)
                {
                    throw;
                }
                InvokeActionResult(controllerContext, exceptionContext.Result);
            }

            return true;
        }


        public Task<ActionExecutedContext> InvokeActionMethodWithFiltersAsync(ControllerContext controllerContext, IList<IActionFilter> filters, ActionDescriptor actionDescriptor, IDictionary<string, object> parameters)
        {
            AsyncInvocationWithFilters invocation = new AsyncInvocationWithFilters(this, controllerContext, actionDescriptor, filters, parameters);

            const int StartingFilterIndex = 0;
            return invocation.InvokeActionMethodFilterAsynchronouslyRecursive(StartingFilterIndex);
        }

        public virtual Task<ActionResult> InvokeActionMethodAsync(ControllerContext controllerContext, ActionDescriptor actionDescriptor, IDictionary<string, object> parameters)
        {
            if (actionDescriptor is AsyncActionDescriptor asyncActionDescriptor)
            {
                return InvokeAsynchronousActionMethod(controllerContext, asyncActionDescriptor, parameters);
            }
            else
            {
                return Task.FromResult(base.InvokeActionMethod(controllerContext, actionDescriptor, parameters));
            }
        }

        private async Task<ActionResult> InvokeAsynchronousActionMethod(ControllerContext controllerContext, AsyncActionDescriptor actionDescriptor, IDictionary<string, object> parameters)
        {
            // We need to wrap the APM BeginExecute/EndExecute in a Task that we can await
            object returnValue = await Task.Factory.FromAsync(actionDescriptor.BeginExecute, actionDescriptor.EndExecute, controllerContext, parameters, null).ConfigureAwait(false);
            ActionResult result = base.CreateActionResult(controllerContext, actionDescriptor, returnValue);
            return result;
        }

        // Large and passed to many function calls, so keep as a reference type to minimize copying
        private class AsyncInvocationWithFilters
        {
            private readonly AsyncControllerActionInvokerEx _invoker;
            private readonly ControllerContext _controllerContext;
            private readonly ActionDescriptor _actionDescriptor;
            private readonly IList<IActionFilter> _filters;
            private readonly IDictionary<string, object> _parameters;
            private readonly int _filterCount;
            private readonly ActionExecutingContext _preContext;

            internal AsyncInvocationWithFilters(AsyncControllerActionInvokerEx invoker, ControllerContext controllerContext, ActionDescriptor actionDescriptor, IList<IActionFilter> filters, IDictionary<string, object> parameters)
            {
                Contract.Assert(invoker != null);
                Contract.Assert(controllerContext != null);
                Contract.Assert(actionDescriptor != null);
                Contract.Assert(filters != null);
                Contract.Assert(parameters != null);

                _invoker = invoker;
                _controllerContext = controllerContext;
                _actionDescriptor = actionDescriptor;
                _filters = filters;
                _parameters = parameters;

                _preContext = new ActionExecutingContext(controllerContext, actionDescriptor, parameters);
                // For IList<T> it is faster to cache the count
                _filterCount = _filters.Count;
            }

            internal async Task<ActionExecutedContext> InvokeActionMethodFilterAsynchronouslyRecursive(int filterIndex)
            {
                // Performance-sensitive

                // For compatability, the following behavior must be maintained
                //   The OnActionExecuting events must fire in forward order
                //   The Begin and End events must fire
                //   The OnActionExecuted events must fire in reverse order
                //   Earlier filters can process the results and exceptions from the handling of later filters
                // This is achieved by calling recursively and moving through the filter list forwards

                // If there are no more filters to recurse over, create the main result
                if (filterIndex > _filterCount - 1)
                {
                    var result = await _invoker.InvokeActionMethodAsync(_controllerContext, _actionDescriptor, _parameters).ConfigureAwait(false);
                    return new ActionExecutedContext(_controllerContext, _actionDescriptor, canceled: false, exception: null)
                    {
                        Result = result,
                    };
                }

                // Otherwise process the filters recursively
                IActionFilter filter = _filters[filterIndex];
                ActionExecutingContext preContext = _preContext;
                filter.OnActionExecuting(preContext);
                if (preContext.Result != null)
                {
                    return new ActionExecutedContext(preContext, preContext.ActionDescriptor, canceled: true, exception: null)
                    {
                        Result = preContext.Result
                    };
                }

                // Use the filters in forward direction
                int nextFilterIndex = filterIndex + 1;

                ActionExecutedContext postContext;
                bool wasError = true;

                try
                {
                    postContext = await InvokeActionMethodFilterAsynchronouslyRecursive(nextFilterIndex).ConfigureAwait(false);
                    wasError = false;
                }
                catch (ThreadAbortException)
                {
                    // This type of exception occurs as a result of Response.Redirect(), but we special-case so that
                    // the filters don't see this as an error.
                    postContext = new ActionExecutedContext(preContext, preContext.ActionDescriptor, canceled: false, exception: null);
                    filter.OnActionExecuted(postContext);
                    throw;
                }
                catch (Exception ex)
                {
                    postContext = new ActionExecutedContext(preContext, preContext.ActionDescriptor, canceled: false, exception: ex);
                    filter.OnActionExecuted(postContext);
                    if (!postContext.ExceptionHandled)
                    {
                        throw;
                    }
                }
                if (!wasError)
                {
                    filter.OnActionExecuted(postContext);
                }

                return postContext;
            }
        }

        public override IAsyncResult BeginInvokeAction(ControllerContext controllerContext, string actionName, AsyncCallback callback, object state)
        {
            // See: http://blog.stephencleary.com/2012/07/async-interop-with-iasyncresult.html
            // and: https://social.msdn.microsoft.com/Forums/en-US/9535a4a6-6218-45fe-aa45-79332b9e5b88/trampolining-considerations-for-apm-wrappers?forum=async
            // and: https://github.com/StephenCleary/AsyncEx/blob/master/src/Nito.AsyncEx.Tasks/Interop/ApmAsyncFactory.cs

            var task = InvokeActionAsync(controllerContext, actionName);
            var tcs = new TaskCompletionSource<bool>(state, TaskCreationOptions.RunContinuationsAsynchronously);
            SynchronizationContextSwitcher.NoContext(() => CompleteAsync(task, callback, tcs));
            return tcs.Task;
        }

        // `async void` is on purpose, to raise `callback` exceptions directly on the thread pool.
        private static async void CompleteAsync(Task<bool> task, AsyncCallback callback, TaskCompletionSource<bool> tcs)
        {
            try
            {
                tcs.TrySetResult(await task.ConfigureAwait(false));
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

        public override bool EndInvokeAction(IAsyncResult asyncResult)
        {
            // Wait and Unwrap any Exceptions
            return ((Task<bool>)asyncResult).GetAwaiter().GetResult();
        }

        /// <remarks>
        /// Copied from: System.Web.Mvc.ControllerActionInvoker
        /// </remarks>
        internal static void ValidateRequest(ControllerContext controllerContext)
        {
            if (controllerContext.IsChildAction)
            {
                return;
            }

            // Tolerate null HttpContext for testing
            HttpContext currentContext = HttpContext.Current;
            if (currentContext != null)
            {
                ValidationUtility.EnableDynamicValidation(currentContext);
            }

            controllerContext.HttpContext.Request.ValidateInput();
        }
    }
}
