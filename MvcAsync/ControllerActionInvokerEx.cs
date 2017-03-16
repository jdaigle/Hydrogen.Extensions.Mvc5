using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Mvc.Async;

namespace MvcAsync
{
    public class ControllerActionInvokerEx : AsyncControllerActionInvoker, IAsyncActionInvoker
    {

    }

    internal class ControllerActionInvokerInternal : AsyncControllerActionInvoker, IAsyncActionInvoker
    {
        private readonly ControllerContext _controllerContext;

        private readonly ActionDescriptor _actionDescriptor;
        private Dictionary<string, object> _parameters;

        private AuthorizationContext _authorizationContext;

        private ExceptionContext _exceptionContext;

        private ActionExecutingContext _actionExecutingContext;
        private ActionExecutedContext _actionExecutedContext;

        private ResultExecutingContext _resultExecutingContext;
        private ResultExecutedContext _resultExecutedContext;

        // Do not make this readonly, it's mutable. We don't want to make a copy.
        // https://blogs.msdn.microsoft.com/ericlippert/2008/05/14/mutating-readonly-structs/
        private FilterCursor _cursor;
        private ActionResult _result;

        public ControllerActionInvokerInternal(
            ControllerContext controllerContext,
            ActionDescriptor actionDescriptor,
            FilterInfo filters)
        {
            _controllerContext = controllerContext;
            _actionDescriptor = actionDescriptor;
        }

        private async Task InvokeFilterPipelineAsync()
        {
            var next = State.InvokeBegin;

            // The `scope` tells the `Next` method who the caller is, and what kind of state to initialize to
            // communicate a result. The outermost scope is `Scope.Invoker` and doesn't require any type
            // of context or result other than throwing.
            var scope = Scope.Invoker;

            // The `state` is used for internal state handling during transitions between states. In practice this
            // means storing a filter instance in `state` and then retrieving it in the next state.
            var state = (object)null;

            // `isCompleted` will be set to true when we've reached a terminal state.
            var isCompleted = false;

            while (!isCompleted)
            {
                await Next(ref next, ref scope, ref state, ref isCompleted);
            }
        }

        private Task Next(ref State next, ref Scope scope, ref object state, ref bool isCompleted)
        {
            switch (next)
            {
                case State.InvokeBegin:
                    {
                        goto case State.AuthorizationBegin;
                    }

                case State.AuthorizationBegin:
                    {
                        _cursor.Reset();
                        goto case State.AuthorizationNext;
                    }

                case State.AuthorizationNext:
                    {
                        var current = _cursor.GetNextFilter<IAuthorizationFilter, IAsyncAuthorizationFilter>();
                        if (current.FilterAsync != null)
                        {
                            if (_authorizationContext == null)
                            {
                                _authorizationContext = new AuthorizationContext(_controllerContext, _actionDescriptor);
                            }

                            state = current.FilterAsync;
                            goto case State.AuthorizationAsyncBegin;
                        }
                        else if (current.Filter != null)
                        {
                            if (_authorizationContext == null)
                            {
                                _authorizationContext = new AuthorizationContext(_controllerContext, _actionDescriptor);
                            }

                            state = current.Filter;
                            goto case State.AuthorizationSync;
                        }
                        else
                        {
                            goto case State.AuthorizationEnd;
                        }
                    }

                case State.AuthorizationAsyncBegin:
                    {
                        var filter = (IAsyncAuthorizationFilter)state;
                        var authorizationContext = _authorizationContext;

                        var task = filter.OnAuthorizationAsync(authorizationContext);
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.AuthorizationAsyncEnd;
                            return task;
                        }

                        goto case State.AuthorizationAsyncEnd;
                    }

                case State.AuthorizationAsyncEnd:
                    {
                        var filter = (IAsyncAuthorizationFilter)state;
                        var authorizationContext = _authorizationContext;

                        if (authorizationContext.Result != null)
                        {
                            goto case State.AuthorizationShortCircuit;
                        }

                        goto case State.AuthorizationNext;
                    }

                case State.AuthorizationSync:
                    {
                        var filter = (IAuthorizationFilter)state;
                        var authorizationContext = _authorizationContext;

                        filter.OnAuthorization(authorizationContext);

                        if (authorizationContext.Result != null)
                        {
                            goto case State.AuthorizationShortCircuit;
                        }

                        goto case State.AuthorizationNext;
                    }

                case State.AuthorizationShortCircuit:
                    {
                        // If an authorization filter short circuits, the result is the last thing we execute
                        isCompleted = true;
                        InvokeActionResult(_controllerContext, _authorizationContext.Result);
                        goto case State.InvokeEnd;
                    }

                case State.AuthorizationEnd:
                    {
                        goto case State.ExceptionBegin;
                    }

                case State.ExceptionBegin:
                    {
                        _cursor.Reset();
                        goto case State.ExceptionNext;
                    }

                case State.ExceptionNext:
                    {
                        var current = _cursor.GetNextFilter<IExceptionFilter, IAsyncExceptionFilter>();
                        if (current.FilterAsync != null)
                        {
                            state = current.FilterAsync;
                            goto case State.ExceptionAsyncBegin;
                        }
                        else if (current.Filter != null)
                        {
                            state = current.Filter;
                            goto case State.ExceptionSyncBegin;
                        }
                        else if (scope == Scope.Exception)
                        {
                            // All exception filters are on the stack already - so execute the 'inside'.
                            goto case State.ExceptionInside;
                        }
                        else
                        {
                            // There are no exception filters - so jump right to 'inside'.
                            goto case State.ActionBegin;
                        }
                    }

                case State.ExceptionAsyncBegin:
                    {
                        var task = InvokeNextExceptionFilterAsync();
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ExceptionAsyncResume;
                            return task;
                        }

                        goto case State.ExceptionAsyncResume;
                    }

                case State.ExceptionAsyncResume:
                    {
                        var filter = (IAsyncExceptionFilter)state;
                        var exceptionContext = _exceptionContext;

                        // When we get here we're 'unwinding' the stack of exception filters. If we have an unhandled exception,
                        // we'll call the filter. Otherwise there's nothing to do.
                        if (exceptionContext?.Exception != null && !exceptionContext.ExceptionHandled)
                        {
                            var task = filter.OnExceptionAsync(exceptionContext);
                            if (task.Status != TaskStatus.RanToCompletion)
                            {
                                next = State.ExceptionAsyncEnd;
                                return task;
                            }

                            goto case State.ExceptionAsyncEnd;
                        }

                        goto case State.ExceptionEnd;
                    }

                case State.ExceptionAsyncEnd:
                    {
                        var filter = (IAsyncExceptionFilter)state;
                        var exceptionContext = _exceptionContext;

                        if (exceptionContext.Exception == null || exceptionContext.ExceptionHandled)
                        {
                            // We don't need to do anthing to trigger a short circuit. If there's another
                            // exception filter on the stack it will check the same set of conditions
                            // and then just skip itself.
                        }

                        goto case State.ExceptionEnd;
                    }

                case State.ExceptionSyncBegin:
                    {
                        var task = InvokeNextExceptionFilterAsync();
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ExceptionSyncEnd;
                            return task;
                        }

                        goto case State.ExceptionSyncEnd;
                    }

                case State.ExceptionSyncEnd:
                    {
                        var filter = (IExceptionFilter)state;
                        var exceptionContext = _exceptionContext;

                        // When we get here we're 'unwinding' the stack of exception filters. If we have an unhandled exception,
                        // we'll call the filter. Otherwise there's nothing to do.
                        if (exceptionContext?.Exception != null && !exceptionContext.ExceptionHandled)
                        {
                            filter.OnException(exceptionContext);

                            if (exceptionContext.Exception == null || exceptionContext.ExceptionHandled)
                            {
                                // We don't need to do anthing to trigger a short circuit. If there's another
                                // exception filter on the stack it will check the same set of conditions
                                // and then just skip itself.
                            }
                        }

                        goto case State.ExceptionEnd;
                    }

                case State.ExceptionInside:
                    {
                        goto case State.ActionBegin;
                    }

                case State.ExceptionHandled:
                    {
                        // We arrive in this state when an exception happened, but was handled by exception filters
                        // either by setting ExceptionHandled, or nulling out the Exception or setting a result
                        // on the ExceptionContext.
                        //
                        // We need to execute the result (if any) and then exit gracefully which unwinding Resource 
                        // filters.

                        if (_exceptionContext.Result == null)
                        {
                            _exceptionContext.Result = new EmptyResult();
                        }

                        if (scope == Scope.Invoker)
                        {
                            _result = _exceptionContext.Result;
                        }

                        InvokeActionResult(_controllerContext, _result);
                        //var task = InvokeResultAsync(_exceptionContext.Result);
                        //if (task.Status != TaskStatus.RanToCompletion)
                        //{
                        //    next = State.ResourceInsideEnd;
                        //    return task;
                        //}

                        goto case State.InvokeEnd;
                    }

                case State.ExceptionEnd:
                    {
                        var exceptionContext = _exceptionContext;

                        if (scope == Scope.Exception)
                        {
                            isCompleted = true;
                            return Task.CompletedTask;
                        }

                        if (exceptionContext != null)
                        {
                            if (exceptionContext.Result != null ||
                                exceptionContext.Exception == null ||
                                exceptionContext.ExceptionHandled)
                            {
                                goto case State.ExceptionHandled;
                            }

                            Rethrow(exceptionContext);
                        }

                        goto case State.ResultBegin;
                    }

                case State.ActionBegin:
                    {
                        _cursor.Reset();
                        goto case State.ActionNext;
                    }

                case State.ActionNext:
                    {
                        var current = _cursor.GetNextFilter<IActionFilter, IAsyncActionFilter>();
                        if (current.FilterAsync != null)
                        {
                            if (_actionExecutingContext == null)
                            {
                                _actionExecutingContext = new ActionExecutingContext(_controllerContext, _actionDescriptor, _parameters);
                            }

                            state = current.FilterAsync;
                            goto case State.ActionAsyncBegin;
                        }
                        else if (current.Filter != null)
                        {
                            if (_actionExecutingContext == null)
                            {
                                _actionExecutingContext = new ActionExecutingContext(_controllerContext, _actionDescriptor, _parameters);
                            }

                            state = current.Filter;
                            goto case State.ActionSyncBegin;
                        }
                        else
                        {
                            goto case State.ActionInside;
                        }
                    }

                case State.ActionAsyncBegin:
                    {
                        var filter = (IAsyncActionFilter)state;
                        var actionExecutingContext = _actionExecutingContext;

                        var task = filter.OnActionExecutionAsync(actionExecutingContext, InvokeNextActionFilterAwaitedAsync);
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ActionAsyncEnd;
                            return task;
                        }

                        goto case State.ActionAsyncEnd;
                    }

                case State.ActionAsyncEnd:
                    {
                        var filter = (IAsyncActionFilter)state;

                        if (_actionExecutedContext == null)
                        {
                            // If we get here then the filter didn't call 'next' indicating a short circuit.
                            _actionExecutedContext = new ActionExecutedContext(_controllerContext, _actionDescriptor, canceled: true, exception: null)
                            {
                                Result = _actionExecutingContext.Result,
                            };
                        }

                        goto case State.ActionEnd;
                    }

                case State.ActionSyncBegin:
                    {
                        var filter = (IActionFilter)state;
                        var actionExecutingContext = _actionExecutingContext;

                        filter.OnActionExecuting(actionExecutingContext);

                        if (actionExecutingContext.Result != null)
                        {
                            // Short-circuited by setting a result.
                            _actionExecutedContext = new ActionExecutedContext(_controllerContext, _actionDescriptor, canceled: true, exception: null)
                            {
                                Result = _actionExecutingContext.Result,
                            };

                            goto case State.ActionEnd;
                        }

                        var task = InvokeNextActionFilterAsync();
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ActionSyncEnd;
                            return task;
                        }

                        goto case State.ActionSyncEnd;
                    }

                case State.ActionSyncEnd:
                    {
                        var filter = (IActionFilter)state;
                        var actionExecutedContext = _actionExecutedContext;

                        filter.OnActionExecuted(actionExecutedContext);

                        goto case State.ActionEnd;
                    }

                case State.ActionInside:
                    {
                        var task = InvokeActionMethodAsync();
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ActionEnd;
                            return task;
                        }

                        goto case State.ActionEnd;
                    }

                case State.ActionEnd:
                    {
                        throw new NotImplementedException();
                    }

                case State.ResultBegin:
                    {
                        _cursor.Reset();
                        throw new NotImplementedException();
                    }

                case State.InvokeEnd:
                    {
                        isCompleted = true;
                        return Task.CompletedTask;
                    }

                default:
                    throw new InvalidOperationException();
            }
        }

        private async Task InvokeNextExceptionFilterAsync()
        {
            try
            {
                var next = State.ExceptionNext;
                var state = (object)null;
                var scope = Scope.Exception;
                var isCompleted = false;
                while (!isCompleted)
                {
                    await Next(ref next, ref scope, ref state, ref isCompleted);
                }
            }
            catch (Exception exception)
            {
                _exceptionContext = new ExceptionContext(_controllerContext, exception);
            }
        }

        private async Task InvokeNextActionFilterAsync()
        {
            try
            {
                var next = State.ActionNext;
                var state = (object)null;
                var scope = Scope.Action;
                var isCompleted = false;
                while (!isCompleted)
                {
                    await Next(ref next, ref scope, ref state, ref isCompleted);
                }
            }
            catch (Exception exception)
            {
                _actionExecutedContext = new ActionExecutedContext(_controllerContext, _actionDescriptor, canceled: false, exception: exception);
            }
        }

        private async Task<ActionExecutedContext> InvokeNextActionFilterAwaitedAsync()
        {
            if (_actionExecutingContext.Result != null)
            {
                // If we get here, it means that an async filter set a result AND called next(). This is forbidden.
                //var message = Resources.FormatAsyncActionFilter_InvalidShortCircuit(
                //    typeof(IAsyncActionFilter).Name,
                //    nameof(ActionExecutingContext.Result),
                //    typeof(ActionExecutingContext).Name,
                //    typeof(ActionExecutionDelegate).Name);
                var message = "If we get here, it means that an async filter set a result AND called next(). This is forbidden.";

                throw new InvalidOperationException(message);
            }

            await InvokeNextActionFilterAsync();

            return _actionExecutedContext;
        }

        private async Task InvokeActionMethodAsync()
        {
            if (_actionDescriptor is AsyncActionDescriptor asyncActionDescriptor)
            {
                object returnValue = await Task.Factory.FromAsync(asyncActionDescriptor.BeginExecute, asyncActionDescriptor.EndExecute, _controllerContext, _parameters, null).ConfigureAwait(false);
                _result = CreateActionResult(_controllerContext, _actionDescriptor, returnValue);
            }
            else
            {
                _result = InvokeActionMethod(_controllerContext, _actionDescriptor, _parameters);
            }
        }

        private static void Rethrow(ExceptionContext context)
        {
            if (context == null)
            {
                return;
            }

            if (context.ExceptionHandled)
            {
                return;
            }

            if (context.Exception != null)
            {
                throw context.Exception;
            }
        }

        private enum Scope
        {
            Invoker,
            Exception,
            Action,
            Result,
        }

        private enum State
        {
            InvokeBegin,

            AuthorizationBegin,
            AuthorizationNext,
            AuthorizationAsyncBegin,
            AuthorizationAsyncEnd,
            AuthorizationSync,
            AuthorizationShortCircuit,
            AuthorizationEnd,

            ExceptionBegin,
            ExceptionNext,
            ExceptionAsyncBegin,
            ExceptionAsyncResume,
            ExceptionAsyncEnd,
            ExceptionSyncBegin,
            ExceptionSyncEnd,
            ExceptionInside,
            ExceptionHandled,
            ExceptionEnd,

            ActionBegin,
            ActionNext,
            ActionAsyncBegin,
            ActionAsyncEnd,
            ActionSyncBegin,
            ActionSyncEnd,
            ActionInside,
            ActionEnd,

            ResultBegin,
            ResultNext,
            ResultAsyncBegin,
            ResultAsyncEnd,
            ResultSyncBegin,
            ResultSyncEnd,
            ResultInside,
            ResultEnd,

            InvokeEnd,
        }
    }

}
