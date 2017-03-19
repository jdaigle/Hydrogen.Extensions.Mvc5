// Derived from: https://github.com/aspnet/Mvc/blob/af5648c1f7b4668aea77dde8ee1eeb26837e97c0/src/Microsoft.AspNetCore.Mvc.Core/Internal/ControllerActionInvoker.cs
//
// Licensed under the Apache License, Version 2.0.
// Copyright(c) .NET Foundation.All rights reserved.
//
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// these files except in compliance with the License.You may obtain a copy of the
// License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, either express or implied. See the License for the
// specific language governing permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Async;
using System.Web.Mvc.Filters;
using System.Web.Mvc.Routing;
using Hydrogen.Extensions.Mvc5.Async.Internal;
using Microsoft.Web.Infrastructure.DynamicValidationHelper;

namespace Hydrogen.Extensions.Mvc5.Async
{
    public class ControllerActionInvokerEx : AsyncControllerActionInvoker, IAsyncActionInvoker
    {
        public static readonly ControllerActionInvokerEx Instance = new ControllerActionInvokerEx();

        private static readonly Task<bool> _cachedTaskFromResultFalse = Task.FromResult(false);

        private Func<ControllerContext, ActionDescriptor, IEnumerable<Filter>> _getFiltersThunk = FilterProviders.Providers.GetFilters;

        public Task<bool> InvokeActionAsync(ControllerContext controllerContext, string actionName)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException(nameof(controllerContext));
            }

            Debug.Assert(controllerContext.RouteData != null);
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


            var parameters = GetParameterValues(controllerContext, actionDescriptor);
            var filters = _getFiltersThunk(controllerContext, actionDescriptor);
            var invokerInternal = new ControllerActionInvokerInternal(controllerContext, actionDescriptor, parameters, filters);

            return invokerInternal.InvokeFilterPipelineAsync();
        }

        public override IAsyncResult BeginInvokeAction(ControllerContext controllerContext, string actionName, AsyncCallback callback, object state)
        {
            var task = InvokeActionAsync(controllerContext, actionName);
            return ApmAsyncFactory.ToBegin(task, callback, state);
        }

        public override bool EndInvokeAction(IAsyncResult asyncResult)
        {
            return ApmAsyncFactory.ToEnd<bool>(asyncResult);
        }
    }

    internal class ControllerActionInvokerInternal : AsyncControllerActionInvoker
    {
        private readonly ControllerContext _controllerContext;

        private readonly ActionDescriptor _actionDescriptor;
        private IDictionary<string, object> _parameters;

        private AuthenticationContext _authenticationContext;
        private AuthenticationChallengeContext _authenticationChallengeContext;
        private bool authenticationChallengeBypassResultFilters = false;

        private AuthorizationContext _authorizationContext;

        private ExceptionContextEx _exceptionContext;

        private ActionExecutingContext _actionExecutingContext;
        private ActionExecutedContextEx _actionExecutedContext;

        private ResultExecutingContext _resultExecutingContext;
        private ResultExecutedContextEx _resultExecutedContext;

        // Do not make this readonly, it's mutable. We don't want to make a copy.
        // https://blogs.msdn.microsoft.com/ericlippert/2008/05/14/mutating-readonly-structs/
        private FilterCursor _cursor;
        private ActionResult _result;

        public ControllerActionInvokerInternal(
            ControllerContext controllerContext,
            ActionDescriptor actionDescriptor,
            IDictionary<string, object> parameters,
            IEnumerable<Filter> filters)
        {
            _controllerContext = controllerContext;
            _actionDescriptor = actionDescriptor;
            _parameters = parameters;
            _cursor = new FilterCursor(filters);
        }

        public async Task<bool> InvokeFilterPipelineAsync()
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

            return true;
        }

        private Task Next(ref State next, ref Scope scope, ref object state, ref bool isCompleted)
        {
            switch (next)
            {
                case State.InvokeBegin:
                    {
                        goto case State.AuthenticationBegin;
                    }

                case State.AuthenticationBegin:
                    {
                        _cursor.Reset();
                        goto case State.AuthenticationNext;
                    }

                case State.AuthenticationNext:
                    {
                        var current = _cursor.GetNextFilter<IAuthenticationFilter, IAuthenticationFilter>();
                        if (current.Filter != null)
                        {
                            var originalPrincipal = _controllerContext.HttpContext.User;
                            _authenticationContext = new AuthenticationContext(_controllerContext, _actionDescriptor, originalPrincipal);
                            state = current.Filter;
                            goto case State.AuthenticationSync;
                        }
                        else
                        {
                            goto case State.AuthenticationEnd;
                        }
                    }

                case State.AuthenticationSync:
                    {
                        var filter = (IAuthenticationFilter)state;
                        var authenticationContext = _authenticationContext;

                        filter.OnAuthentication(authenticationContext);

                        if (authenticationContext.Result != null)
                        {
                            goto case State.AuthenticationShortCircuit;
                        }

                        var originalPrincipal = _controllerContext.HttpContext.User;
                        var newPrincipal = authenticationContext.Principal;

                        if (newPrincipal != originalPrincipal)
                        {
                            Debug.Assert(_controllerContext.HttpContext != null);
                            _controllerContext.HttpContext.User = newPrincipal;
                            Thread.CurrentPrincipal = newPrincipal;
                        }

                        goto case State.AuthenticationNext;
                    }

                case State.AuthenticationShortCircuit:
                    {
                        Debug.Assert(_authenticationContext?.Result != null);
                        _result = _authenticationContext.Result;
                        authenticationChallengeBypassResultFilters = true;
                        goto case State.AuthenticationChallengeBegin;
                    }

                case State.AuthenticationEnd:
                    {
                        goto case State.AuthorizationBegin;
                    }

                case State.AuthenticationChallengeBegin:
                    {
                        _cursor.Reset();
                        goto case State.AuthenticationChallengeNext;
                    }

                case State.AuthenticationChallengeNext:
                    {
                        var current = _cursor.GetNextFilter<IAuthenticationFilter, IAuthenticationFilter>();
                        if (current.Filter != null)
                        {
                            Debug.Assert(_result != null);
                            _authenticationChallengeContext = new AuthenticationChallengeContext(_controllerContext, _actionDescriptor, _result);
                            state = current.Filter;
                            goto case State.AuthenticationChallengeSync;
                        }
                        else
                        {
                            goto case State.AuthenticationChallengeEnd;
                        }
                    }

                case State.AuthenticationChallengeSync:
                    {
                        Debug.Assert(_authenticationChallengeContext != null);

                        var filter = (IAuthenticationFilter)state;
                        var authenticationChallengeContext = _authenticationChallengeContext;

                        filter.OnAuthenticationChallenge(authenticationChallengeContext);

                        // unlike other filter types, don't short-circuit evaluation when context.Result != null (since it
                        // starts out that way, and multiple filters may add challenges to the result)
                        _result = authenticationChallengeContext.Result ?? _result;

                        goto case State.AuthenticationChallengeNext;
                    }

                case State.AuthenticationChallengeEnd:
                    {
                        Debug.Assert(_result != null);

                        if (authenticationChallengeBypassResultFilters)
                        {
                            // short-circuit before we started invoking the action itself
                            // so bypass any result filters
                            isCompleted = true;
                            InvokeActionResult(_controllerContext, _result);
                            goto case State.InvokeEnd;
                        }

                        goto case State.ResultBegin;
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
                        Debug.Assert(state != null);
                        Debug.Assert(_authorizationContext != null);

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
                        Debug.Assert(state != null);
                        Debug.Assert(_authorizationContext != null);

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
                        Debug.Assert(_authorizationContext?.Result != null);
                        _result = _authorizationContext.Result;
                        authenticationChallengeBypassResultFilters = true;
                        goto case State.AuthenticationChallengeBegin;
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
                            Debug.Assert(scope == Scope.Invoker);
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
                        Debug.Assert(state != null);

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
                        Debug.Assert(state != null);
                        Debug.Assert(_exceptionContext != null);

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
                        Debug.Assert(state != null);

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

                        Debug.Assert(state != null);
                        Debug.Assert(_exceptionContext != null);

                        if (_exceptionContext.Result == null)
                        {
                            _exceptionContext.Result = new EmptyResult();
                        }

                        if (scope == Scope.Invoker)
                        {
                            Debug.Assert(_exceptionContext.Result != null);
                            _result = _exceptionContext.Result;
                        }

                        InvokeActionResult(_controllerContext, _result);

                        goto case State.InvokeEnd;
                    }

                case State.ExceptionEnd:
                    {
                        var exceptionContext = _exceptionContext;

                        if (scope == Scope.Exception)
                        {
                            isCompleted = true;
                            return TaskHelpers.CompletedTask;
                        }

                        if (exceptionContext != null)
                        {
                            if (exceptionContext.Exception == null ||
                                exceptionContext.ExceptionHandled)
                            {
                                goto case State.ExceptionHandled;
                            }

                            Rethrow(exceptionContext);
                            Debug.Fail("unreachable");
                        }

                        goto case State.ResultBegin;
                    }

                case State.ActionBegin:
                    {
                        if (_controllerContext.Controller.ValidateRequest)
                        {
                            ValidateRequest(_controllerContext);
                        }

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
                        Debug.Assert(state != null);
                        Debug.Assert(_actionExecutingContext != null);

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
                        Debug.Assert(state != null);
                        Debug.Assert(_actionExecutingContext != null);

                        var filter = (IAsyncActionFilter)state;

                        if (_actionExecutedContext == null)
                        {
                            // If we get here then the filter didn't call 'next' indicating a short circuit.
                            _actionExecutedContext = new ActionExecutedContextEx(_controllerContext, _actionDescriptor, canceled: true, exception: null)
                            {
                                Result = _actionExecutingContext.Result,
                            };
                        }

                        goto case State.ActionEnd;
                    }

                case State.ActionSyncBegin:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_actionExecutingContext != null);

                        var filter = (IActionFilter)state;
                        var actionExecutingContext = _actionExecutingContext;

                        filter.OnActionExecuting(actionExecutingContext);

                        if (actionExecutingContext.Result != null)
                        {
                            // Short-circuited by setting a result.
                            _actionExecutedContext = new ActionExecutedContextEx(_controllerContext, _actionDescriptor, canceled: true, exception: null)
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
                        Debug.Assert(state != null);
                        Debug.Assert(_actionExecutingContext != null);
                        Debug.Assert(_actionExecutedContext != null);

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
                        if (scope == Scope.Action)
                        {
                            if (_actionExecutedContext == null)
                            {
                                _actionExecutedContext = new ActionExecutedContextEx(_controllerContext, _actionDescriptor, canceled: false, exception: null)
                                {
                                    Result = _result,
                                };
                            }

                            isCompleted = true;
                            return TaskHelpers.CompletedTask;
                        }

                        var actionExecutedContext = _actionExecutedContext;
                        Rethrow(actionExecutedContext);

                        if (actionExecutedContext != null)
                        {
                            _result = actionExecutedContext.Result;
                        }

                        goto case State.AuthenticationChallengeBegin;
                    }

                case State.ResultBegin:
                    {
                        _cursor.Reset();
                        goto case State.ResultNext;
                    }

                case State.ResultNext:
                    {
                        var current = _cursor.GetNextFilter<IResultFilter, IAsyncResultFilter>();
                        if (current.FilterAsync != null)
                        {
                            if (_resultExecutingContext == null)
                            {
                                _resultExecutingContext = new ResultExecutingContext(_controllerContext, _result);
                            }

                            state = current.FilterAsync;
                            goto case State.ResultAsyncBegin;
                        }
                        else if (current.Filter != null)
                        {
                            if (_resultExecutingContext == null)
                            {
                                _resultExecutingContext = new ResultExecutingContext(_controllerContext, _result);
                            }

                            state = current.Filter;
                            goto case State.ResultSyncBegin;
                        }
                        else
                        {
                            goto case State.ResultInside;
                        }
                    }

                case State.ResultAsyncBegin:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_resultExecutingContext != null);

                        var filter = (IAsyncResultFilter)state;
                        var resultExecutingContext = _resultExecutingContext;

                        var task = filter.OnResultExecutionAsync(resultExecutingContext, InvokeNextResultFilterAwaitedAsync);
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ResultAsyncEnd;
                            return task;
                        }

                        goto case State.ResultAsyncEnd;
                    }

                case State.ResultAsyncEnd:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_resultExecutingContext != null);

                        var filter = (IAsyncResultFilter)state;
                        var resultExecutingContext = _resultExecutingContext;
                        var resultExecutedContext = _resultExecutedContext;

                        if (resultExecutedContext == null || resultExecutingContext.Cancel == true)
                        {
                            // Short-circuited by not calling next || Short-circuited by setting Cancel == true
                            _resultExecutedContext = new ResultExecutedContextEx(_controllerContext, resultExecutingContext.Result, canceled: true, exception: null);
                        }

                        goto case State.ResultEnd;
                    }

                case State.ResultSyncBegin:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_resultExecutingContext != null);

                        var filter = (IResultFilter)state;
                        var resultExecutingContext = _resultExecutingContext;

                        filter.OnResultExecuting(resultExecutingContext);

                        if (_resultExecutingContext.Cancel == true)
                        {
                            // Short-circuited by setting Cancel == true
                            _resultExecutedContext = new ResultExecutedContextEx(_controllerContext, resultExecutingContext.Result, canceled: true, exception: null);

                            goto case State.ResultEnd;
                        }

                        var task = InvokeNextResultFilterAsync();
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ResultSyncEnd;
                            return task;
                        }

                        goto case State.ResultSyncEnd;
                    }

                case State.ResultSyncEnd:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_resultExecutingContext != null);
                        Debug.Assert(_resultExecutedContext != null);

                        var filter = (IResultFilter)state;
                        var resultExecutedContext = _resultExecutedContext;

                        filter.OnResultExecuted(resultExecutedContext);

                        goto case State.ResultEnd;
                    }

                case State.ResultInside:
                    {
                        // If we executed result filters then we need to grab the result from there.
                        if (_resultExecutingContext != null)
                        {
                            _result = _resultExecutingContext.Result;
                        }

                        if (_result == null)
                        {
                            // The empty result is always flowed back as the 'executed' result if we don't have one.
                            _result = new EmptyResult();
                        }

                        InvokeActionResult(_controllerContext, _result);

                        goto case State.ResultEnd;
                    }

                case State.ResultEnd:
                    {
                        var result = _result;

                        if (scope == Scope.Result)
                        {
                            if (_resultExecutedContext == null)
                            {
                                _resultExecutedContext = new ResultExecutedContextEx(_controllerContext, result, canceled: false, exception: null);
                            }

                            isCompleted = true;
                            return TaskHelpers.CompletedTask;
                        }

                        Rethrow(_resultExecutedContext);

                        goto case State.InvokeEnd;
                    }

                case State.InvokeEnd:
                    {
                        isCompleted = true;
                        return TaskHelpers.CompletedTask;
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
                _exceptionContext = new ExceptionContextEx(_controllerContext, exception)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
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
                _actionExecutedContext = new ActionExecutedContextEx(_controllerContext, _actionDescriptor, canceled: false, exception: exception)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
            }

            Debug.Assert(_actionExecutedContext != null);
        }

        private async Task<ActionExecutedContext> InvokeNextActionFilterAwaitedAsync()
        {
            Debug.Assert(_actionExecutingContext != null);
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

            Debug.Assert(_actionExecutedContext != null);
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

        private async Task InvokeNextResultFilterAsync()
        {
            try
            {
                var next = State.ResultNext;
                var state = (object)null;
                var scope = Scope.Result;
                var isCompleted = false;
                while (!isCompleted)
                {
                    await Next(ref next, ref scope, ref state, ref isCompleted);
                }
            }
            catch (Exception exception)
            {
                _resultExecutedContext = new ResultExecutedContextEx(_controllerContext, _result, canceled: false, exception: exception)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
            }

            Debug.Assert(_resultExecutedContext != null);
        }

        private async Task<ResultExecutedContext> InvokeNextResultFilterAwaitedAsync()
        {
            Debug.Assert(_resultExecutingContext != null);
            if (_resultExecutingContext.Cancel == true)
            {
                // If we get here, it means that an async filter set cancel == true AND called next().
                // This is forbidden.
                //var message = Resources.FormatAsyncResultFilter_InvalidShortCircuit(
                //    typeof(IAsyncResultFilter).Name,
                //    nameof(ResultExecutingContext.Cancel),
                //    typeof(ResultExecutingContext).Name,
                //    typeof(ResultExecutionDelegate).Name);
                var message = "If we get here, it means that an async filter set cancel == true AND called next().";

                throw new InvalidOperationException(message);
            }

            await InvokeNextResultFilterAsync();

            Debug.Assert(_resultExecutedContext != null);
            return _resultExecutedContext;
        }

        private static void Rethrow(ExceptionContextEx context)
        {
            if (context == null)
            {
                return;
            }

            if (context.ExceptionHandled)
            {
                return;
            }

            if (context.ExceptionDispatchInfo != null)
            {
                context.ExceptionDispatchInfo.Throw();
            }

            if (context.Exception != null)
            {
                throw context.Exception;
            }
        }

        private static void Rethrow(ActionExecutedContextEx context)
        {
            if (context == null)
            {
                return;
            }

            if (context.ExceptionHandled)
            {
                return;
            }

            if (context.ExceptionDispatchInfo != null)
            {
                context.ExceptionDispatchInfo.Throw();
            }

            if (context.Exception != null)
            {
                throw context.Exception;
            }
        }

        private static void Rethrow(ResultExecutedContextEx context)
        {
            if (context == null)
            {
                return;
            }

            if (context.ExceptionHandled)
            {
                return;
            }

            if (context.ExceptionDispatchInfo != null)
            {
                context.ExceptionDispatchInfo.Throw();
            }

            if (context.Exception != null)
            {
                throw context.Exception;
            }
        }

        /// <remarks>
        /// Copied from: System.Web.Mvc.ControllerActionInvoker
        /// </remarks>
        private static void ValidateRequest(ControllerContext controllerContext)
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

            AuthenticationBegin,
            AuthenticationNext,
            AuthenticationSync,
            AuthenticationShortCircuit,
            AuthenticationEnd,

            AuthenticationChallengeBegin,
            AuthenticationChallengeNext,
            AuthenticationChallengeSync,
            AuthenticationChallengeEnd,

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
