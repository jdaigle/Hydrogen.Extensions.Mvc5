using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Filters;
using Hydrogen.Extensions.Mvc5.Async.Internal;
using Moq;
using Xunit;

namespace Hydrogen.Extensions.Mvc5.Async
{
    public class ControllerActionInvokerExTest
    {
        public ControllerActionInvokerExTest()
        {
            GlobalFilters.Filters.Clear();
        }

        [Fact]
        public void InvokeAction_ThrowsIfActionNameIsEmpty()
        {
            Assert.Throws<ArgumentException>("actionName", () =>
            {
                InvokeAction(new ControllerContext(), "", null, null);
            });
        }

        [Fact]
        public void InvokeAction_ThrowsIfActionNameIsNull()
        {
            Assert.Throws<ArgumentException>("actionName", () =>
            {
                InvokeAction(new ControllerContext(), null, null, null);
            });
        }

        [Fact]
        public void InvokeAction_ThrowsIfControllerContextIsNull()
        {
            Assert.Throws<ArgumentNullException>("controllerContext", () =>
            {
                InvokeAction(null, nameof(TestController.NormalAction), null, null);
            });
        }

        [Fact]
        public void InvokeAction_ActionNotFound()
        {
            ControllerContext controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, "ActionNotFound", null, null);

            Assert.False(retVal);
        }

        [Fact]
        public void InvokeAction_ActionThrowsException_Handled()
        {
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.ActionThrowsExceptionAndIsHandled), null, null);

            Assert.True(retVal);
            Assert.Equal("From exception filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ActionThrowsException_HandledAsync()
        {
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.ActionThrowsExceptionAndIsHandledAsync), null, null);

            Assert.True(retVal);
            Assert.Equal("From exception filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ActionThrowsException_NotHandled()
        {
            var controllerContext = GetControllerContext();

            AssertEx.Throws<Exception>(() =>
            {
                var retVal = InvokeAction(controllerContext, nameof(TestController.ActionThrowsExceptionAndIsNotHandled), null, null);
            }, @"Some exception text.");
        }

        [Fact]
        public void InvokeAction_ActionThrowsException_NotHandledAsync()
        {
            var controllerContext = GetControllerContext();

            AssertEx.Throws<Exception>(() =>
            {
                var retVal = InvokeAction(controllerContext, nameof(TestController.ActionThrowsExceptionAndIsNotHandledAsync), null, null);
            }, @"Some exception text.");
        }

        [Fact]
        public void InvokeAction_ActionThrowsException_ThreadAbort()
        {
            var controllerContext = GetControllerContext();

            Assert.Throws<ThreadAbortException>(() =>
            {
                var retVal = InvokeAction(controllerContext, nameof(TestController.ActionCallsThreadAbort), null, null);
            });
        }

        [Fact]
        public void InvokeAction_AuthenticationFilterShortCircuits()
        {
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.AuthenticationFilterShortCircuits), null, null);

            Assert.True(retVal);
            Assert.Equal("From authentication filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_AuthenticationFilterChallenges()
        {
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.AuthenticationFilterChallenges), null, null);

            Assert.True(retVal);
            Assert.Equal("From authentication filter challenge", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_AuthenticationFilterChallengesWithResultFilter()
        {
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.InvokeAction_AuthenticationFilterChallengesWithResultFilter), null, null);

            Assert.True(retVal);
            Assert.Equal("From authentication filter challenge with result filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_AuthenticationFilterShortCircuitsAndChallenges()
        {
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.AuthenticationFilterShortCircuitsAndChallenges), null, null);

            Assert.True(retVal);
            Assert.Equal("From authentication filter challenge", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_AuthorizationFilterShortCircuits()
        {
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.AuthorizationFilterShortCircuits), null, null);

            Assert.True(retVal);
            Assert.Equal("From authorization filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_AsyncAuthorizationFilterShortCircuits()
        {
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.AsyncAuthorizationFilterShortCircuits), null, null);

            Assert.True(retVal);
            Assert.Equal("From authorization filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_AuthorizationFilterShortCircuitsAndChallenges()
        {
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.AuthorizationFilterShortCircuitsAndChallenges), null, null);

            Assert.True(retVal);
            Assert.Equal("From authentication filter challenge", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_AsyncAuthorizationFilterShortCircuitsAndChallenges()
        {
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.AsyncAuthorizationFilterShortCircuitsAndChallenges), null, null);

            Assert.True(retVal);
            Assert.Equal("From authentication filter challenge", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_NormalAction()
        {
            ControllerContext controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.NormalAction), null, null);

            Assert.True(retVal);
            Assert.Equal("From action", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_OverrideFindAction()
        {
            ControllerContext controllerContext = GetControllerContext();
            var invoker = new ControllerActionInvokerExWithCustomFindAction();

            IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, "Non-ExistantAction", callback: null, state: null);
            var retVal = invoker.EndInvokeAction(asyncResult);

            Assert.True(retVal);
            Assert.Equal("From action", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_RequestValidationFails()
        {
            var controllerContext = GetControllerContext(passesRequestValidation: false);

            Assert.Throws<HttpRequestValidationException>(() =>
            {
                var retVal = InvokeAction(controllerContext, nameof(TestController.NormalAction), null, null);
            });
        }

        [Fact]
        public void InvokeAction_ResultThrowsException_Handled()
        {
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.ResultThrowsExceptionAndIsHandled), null, null);

            Assert.True(retVal);
            Assert.Equal("From exception filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ResultThrowsException_HandledAsync()
        {
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.ResultThrowsExceptionAndIsHandledAsync), null, null);

            Assert.True(retVal);
            Assert.Equal("From exception filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ResultThrowsException_NotHandled()
        {
            var controllerContext = GetControllerContext();

            AssertEx.Throws<Exception>(() =>
            {
                InvokeAction(controllerContext, nameof(TestController.ResultThrowsExceptionAndIsNotHandled), null, null);
            }, @"Some exception text.");
        }

        [Fact]
        public void InvokeAction_ResultThrowsException_NotHandledAsync()
        {
            var controllerContext = GetControllerContext();

            AssertEx.Throws<Exception>(() =>
            {
                InvokeAction(controllerContext, nameof(TestController.ResultThrowsExceptionAndIsNotHandledAsync), null, null);
            }, @"Some exception text.");
        }

        [Fact]
        public void InvokeAction_ResultThrowsException_ThreadAbort()
        {
            var controllerContext = GetControllerContext();

            Assert.Throws<ThreadAbortException>(() =>
            {
                InvokeAction(controllerContext, nameof(TestController.ResultCallsThreadAbort), null, null);
            });
        }

        [Fact]
        public void InvokeAction_ActionFilter_OnActionExecuting_ShortCircuits()
        {
            var controllerContext = GetControllerContext();
            GlobalFilters.Filters.Add(new ActionFilterOnActionExecutingShortCircuits());

            var retVal = InvokeAction(controllerContext, nameof(TestController.NormalAction), null, null);

            Assert.True(retVal);
            Assert.Equal($"Called from {nameof(ActionFilterOnActionExecutingShortCircuits)}.{nameof(ActionFilterOnActionExecutingShortCircuits.OnActionExecuting)}", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_AsyncActionFilter_OnActionExecuting_ShortCircuits()
        {
            var controllerContext = GetControllerContext();
            GlobalFilters.Filters.Add(new AsyncActionFilterOnActionExecutingShortCircuits());

            var retVal = InvokeAction(controllerContext, nameof(TestController.NormalAction), null, null);

            Assert.True(retVal);
            Assert.Equal($"Called from {nameof(AsyncActionFilterOnActionExecutingShortCircuits)}.{nameof(AsyncActionFilterOnActionExecutingShortCircuits.OnActionExecutionAsync)} before next", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ActionFilter_OnActionExecuted_ShortCircuits()
        {
            var controllerContext = GetControllerContext();
            GlobalFilters.Filters.Add(new ActionFilterOnActionExecutedShortCircuits());

            var retVal = InvokeAction(controllerContext, nameof(TestController.NormalAction), null, null);

            Assert.True(retVal);
            Assert.Equal($"Called from {nameof(ActionFilterOnActionExecutedShortCircuits)}.{nameof(ActionFilterOnActionExecutedShortCircuits.OnActionExecuted)}", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_AsyncActionFilter_OnActionExecuted_ShortCircuits()
        {
            var controllerContext = GetControllerContext();
            GlobalFilters.Filters.Add(new AsyncActionFilterOnActionExecutedShortCircuits());

            var retVal = InvokeAction(controllerContext, nameof(TestController.NormalAction), null, null);

            Assert.True(retVal);
            Assert.Equal($"Called from {nameof(AsyncActionFilterOnActionExecutedShortCircuits)}.{nameof(AsyncActionFilterOnActionExecutedShortCircuits.OnActionExecutionAsync)} after next", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ThrowsOnActionExecutingException_Handled()
        {
            var actionLog = new List<string>();
            var actionFilter = new ActionFilterImpl()
            {
                OnActionExecutingImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuting");
                },
                OnActionExecutedImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuted");
                    Assert.Equal("Some exception text.", filterContext.Exception.Message);
                    filterContext.ExceptionHandled = true;
                    filterContext.Result = new LoggingActionResult("Handled Exception");
                }
            };
            GlobalFilters.Filters.Add(actionFilter);
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.ActionThrowsExceptionAndIsNotHandled), null, null);

            Assert.True(retVal);
            Assert.Equal(new[] {
                "OnActionExecuting",
                "OnActionExecuted" }, actionLog.ToArray());
            Assert.Equal("Handled Exception", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ThrowsOnActionExecutingException_HandledAsync()
        {
            var actionLog = new List<string>();
            var actionFilter = new AsyncActionFilterImpl()
            {
                OnActionExecutingImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuting");
                },
                OnActionExecutedImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuted");
                    Assert.Equal("Some exception text.", filterContext.Exception.Message);
                    filterContext.ExceptionHandled = true;
                    filterContext.Result = new LoggingActionResult("Handled Exception");
                }
            };
            GlobalFilters.Filters.Add(actionFilter);
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.ActionThrowsExceptionAndIsNotHandled), null, null);

            Assert.True(retVal);
            Assert.Equal(new[] {
                "OnActionExecuting",
                "OnActionExecuted" }, actionLog.ToArray());
            Assert.Equal("Handled Exception", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ThrowsOnActionExecutingException_HandledByEarlier()
        {
            var actionLog = new List<string>();
            var filter1 = new ActionFilterImpl()
            {
                OnActionExecutingImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuting1");
                },
                OnActionExecutedImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuted1");
                    Assert.Equal("Some exception text.", filterContext.Exception.Message);
                    filterContext.ExceptionHandled = true;
                    filterContext.Result = new LoggingActionResult("Handled Exception");
                }
            };
            var filter2 = new ActionFilterImpl()
            {
                OnActionExecutingImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuting2");
                },
                OnActionExecutedImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuted2");
                }
            };
            GlobalFilters.Filters.Add(filter1);
            GlobalFilters.Filters.Add(filter2);
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.ActionThrowsExceptionAndIsNotHandled), null, null);

            Assert.True(retVal);
            Assert.Equal(new[] {
                "OnActionExecuting1",
                "OnActionExecuting2",
                "OnActionExecuted2",
                "OnActionExecuted1" }, actionLog.ToArray());
            Assert.Equal("Handled Exception", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ThrowsOnActionExecutingException_HandledByEarlierAsync()
        {
            var actionLog = new List<string>();
            var filter1 = new AsyncActionFilterImpl()
            {
                OnActionExecutingImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuting1");
                },
                OnActionExecutedImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuted1");
                    Assert.Equal("Some exception text.", filterContext.Exception.Message);
                    filterContext.ExceptionHandled = true;
                    filterContext.Result = new LoggingActionResult("Handled Exception");
                }
            };
            var filter2 = new AsyncActionFilterImpl()
            {
                OnActionExecutingImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuting2");
                },
                OnActionExecutedImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuted2");
                }
            };
            GlobalFilters.Filters.Add(filter1);
            GlobalFilters.Filters.Add(filter2);
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.ActionThrowsExceptionAndIsNotHandled), null, null);

            Assert.True(retVal);
            Assert.Equal(new[] {
                "OnActionExecuting1",
                "OnActionExecuting2",
                "OnActionExecuted2",
                "OnActionExecuted1" }, actionLog.ToArray());
            Assert.Equal("Handled Exception", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ThrowsOnActionExecutingException_HandledByLater()
        {
            var actionLog = new List<string>();
            var filter1 = new ActionFilterImpl()
            {
                OnActionExecutingImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuting1");
                },
                OnActionExecutedImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuted1");
                    }
            };
            var filter2 = new ActionFilterImpl()
            {
                OnActionExecutingImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuting2");
                },
                OnActionExecutedImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuted2");
                    Assert.Equal("Some exception text.", filterContext.Exception.Message);
                    filterContext.ExceptionHandled = true;
                    filterContext.Result = new LoggingActionResult("Handled Exception");
                }
            };
            GlobalFilters.Filters.Add(filter1);
            GlobalFilters.Filters.Add(filter2);
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.ActionThrowsExceptionAndIsNotHandled), null, null);

            Assert.True(retVal);
            Assert.Equal(new[] {
                "OnActionExecuting1",
                "OnActionExecuting2",
                "OnActionExecuted2",
                "OnActionExecuted1" }, actionLog.ToArray());
            Assert.Equal("Handled Exception", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ThrowsOnActionExecutingException_HandledByLaterAsync()
        {
            var actionLog = new List<string>();
            var filter1 = new AsyncActionFilterImpl()
            {
                OnActionExecutingImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuting1");
                },
                OnActionExecutedImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuted1");
                }
            };
            var filter2 = new AsyncActionFilterImpl()
            {
                OnActionExecutingImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuting2");
                },
                OnActionExecutedImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuted2");
                    Assert.Equal("Some exception text.", filterContext.Exception.Message);
                    filterContext.ExceptionHandled = true;
                    filterContext.Result = new LoggingActionResult("Handled Exception");
                }
            };
            GlobalFilters.Filters.Add(filter1);
            GlobalFilters.Filters.Add(filter2);
            var controllerContext = GetControllerContext();

            var retVal = InvokeAction(controllerContext, nameof(TestController.ActionThrowsExceptionAndIsNotHandled), null, null);

            Assert.True(retVal);
            Assert.Equal(new[] {
                "OnActionExecuting1",
                "OnActionExecuting2",
                "OnActionExecuted2",
                "OnActionExecuted1" }, actionLog.ToArray());
            Assert.Equal("Handled Exception", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ThrowsOnActionExecutingException_NotHandled()
        {
            var actionLog = new List<string>();
            var actionFilter = new ActionFilterImpl()
            {
                OnActionExecutingImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuting1");
                },
                OnActionExecutedImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuted1");
                }
            };
            GlobalFilters.Filters.Add(actionFilter);
            var controllerContext = GetControllerContext();

            AssertEx.Throws<Exception>(() =>
            {
                var retVal = InvokeAction(controllerContext, nameof(TestController.ActionThrowsExceptionAndIsNotHandled), null, null);
            }, "Some exception text.");

            Assert.Equal(new[] {
                "OnActionExecuting1",
                "OnActionExecuted1" }, actionLog.ToArray());
        }

        [Fact]
        public void InvokeAction_ThrowsOnActionExecutingException_NotHandledAsync()
        {
            var actionLog = new List<string>();
            var actionFilter = new AsyncActionFilterImpl()
            {
                OnActionExecutingImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuting1");
                },
                OnActionExecutedImpl = filterContext =>
                {
                    actionLog.Add("OnActionExecuted1");
                }
            };
            GlobalFilters.Filters.Add(actionFilter);
            var controllerContext = GetControllerContext();

            AssertEx.Throws<Exception>(() =>
            {
                var retVal = InvokeAction(controllerContext, nameof(TestController.ActionThrowsExceptionAndIsNotHandled), null, null);
            }, "Some exception text.");

            Assert.Equal(new[] {
                "OnActionExecuting1",
                "OnActionExecuted1" }, actionLog.ToArray());
        }

        private static bool InvokeAction(ControllerContext controllerContext, string actionName, AsyncCallback callback, object state)
        {
            try
            {
                var invoker = new ControllerActionInvokerEx();
                IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, actionName, callback, state);
                return invoker.EndInvokeAction(asyncResult);
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort(); // for testing, we don't actually want to abort the thread for the test, but we still want to see the exception if thrown
                throw;
            }
        }

        private static ControllerContext GetControllerContext(bool passesRequestValidation = true)
        {
            Mock<HttpContextBase> mockHttpContext = new Mock<HttpContextBase>();
            if (passesRequestValidation)
            {
#pragma warning disable 618
                mockHttpContext.Setup(o => o.Request.ValidateInput()).AtMostOnce();
#pragma warning restore 618
            }
            else
            {
                mockHttpContext.Setup(o => o.Request.ValidateInput()).Throws(new HttpRequestValidationException());
            }

            return new ControllerContext()
            {
                Controller = new TestController(),
                HttpContext = mockHttpContext.Object
            };
        }

        public class ControllerActionInvokerExWithCustomFindAction : ControllerActionInvokerEx
        {
            protected override ActionDescriptor FindAction(ControllerContext controllerContext, ControllerDescriptor controllerDescriptor, string actionName)
            {
                return base.FindAction(controllerContext, controllerDescriptor, "NormalAction");
            }
        }

        private class TestController : Controller
        {
            public string Log { get; set; }

            [CustomExceptionFilterHandlesError]
            public Task ActionThrowsExceptionAndIsHandled()
            {
                throw new Exception("Some exception text.");
            }

            [CustomAsyncExceptionFilterHandlesError]
            public Task ActionThrowsExceptionAndIsHandledAsync()
            {
                throw new Exception("Some exception text.");
            }

            [CustomExceptionFilterDoesNotHandleError]
            public Task ActionThrowsExceptionAndIsNotHandled()
            {
                throw new Exception("Some exception text.");
            }

            [CustomAsyncExceptionFilterDoesNotHandleError]
            public Task ActionThrowsExceptionAndIsNotHandledAsync()
            {
                throw new Exception("Some exception text.");
            }

            public Task<ActionResult> ActionCallsThreadAbort()
            {
                Thread.CurrentThread.Abort();
                return null;
            }

            [AuthenticationFilterReturnsResult]
            public Task AuthenticationFilterShortCircuits()
            {
                return TaskHelpers.CompletedTask;
            }

            [AuthenticationFilterChallengeSetsResult]
            public Task AuthenticationFilterChallenges()
            {
                return TaskHelpers.CompletedTask;
            }

            [AuthenticationFilterChallengeSetsResult]
            [ResultFilterMutatesResult]
            public Task InvokeAction_AuthenticationFilterChallengesWithResultFilter()
            {
                return TaskHelpers.CompletedTask;
            }

            [AuthenticationFilterReturnsResult]
            [AuthenticationFilterChallengeSetsResult]
            public Task AuthenticationFilterShortCircuitsAndChallenges()
            {
                return TaskHelpers.CompletedTask;
            }

            [AuthorizationFilterReturnsResult]
            public Task AuthorizationFilterShortCircuits()
            {
                return TaskHelpers.CompletedTask;
            }

            [AsyncAuthorizationFilterReturnsResult]
            public Task AsyncAuthorizationFilterShortCircuits()
            {
                return TaskHelpers.CompletedTask;
            }

            [AuthenticationFilterChallengeSetsResult]
            [AuthorizationFilterReturnsResult]
            public Task AuthorizationFilterShortCircuitsAndChallenges()
            {
                return TaskHelpers.CompletedTask;
            }
            [AuthenticationFilterChallengeSetsResult]
            [AsyncAuthorizationFilterReturnsResult]
            public Task AsyncAuthorizationFilterShortCircuitsAndChallenges()
            {
                return TaskHelpers.CompletedTask;
            }

            public ActionResult NormalAction()
            {
                return new LoggingActionResult("From action");
            }

            [CustomExceptionFilterHandlesError]
            public async Task<ActionResultWhichThrowsException> ResultThrowsExceptionAndIsHandled()
            {
                return await Task.FromResult(new ActionResultWhichThrowsException());
            }

            [CustomAsyncExceptionFilterHandlesError]
            public async Task<ActionResult> ResultThrowsExceptionAndIsHandledAsync()
            {
                return await Task.FromResult(new ActionResultWhichThrowsException());
            }

            [CustomExceptionFilterDoesNotHandleError]
            public async Task<ActionResult> ResultThrowsExceptionAndIsNotHandled()
            {
                return await Task.FromResult(new ActionResultWhichThrowsException());
            }

            [CustomAsyncExceptionFilterDoesNotHandleError]
            public async Task<ActionResult> ResultThrowsExceptionAndIsNotHandledAsync()
            {
                return await Task.FromResult(new ActionResultWhichThrowsException());
            }

            public ActionResult ResultCallsThreadAbort()
            {
                return new ActionResultWhichCallsThreadAbort();
            }
        }

        private class ResultFilterMutatesResultAttribute : FilterAttribute, IResultFilter
        {
            public void OnResultExecuting(ResultExecutingContext filterContext)
            {
                (filterContext.Result as LoggingActionResult).LogText += " with result filter";
            }

            public void OnResultExecuted(ResultExecutedContext filterContext)
            {
            }
        }

        private class AuthenticationFilterReturnsResultAttribute : FilterAttribute, IAuthenticationFilter
        {
            public void OnAuthentication(AuthenticationContext filterContext)
            {
                filterContext.Result = new LoggingActionResult("From authentication filter");
            }

            public void OnAuthenticationChallenge(AuthenticationChallengeContext filterContext)
            {
            }
        }

        private class AuthenticationFilterChallengeSetsResultAttribute : FilterAttribute, IAuthenticationFilter
        {
            public void OnAuthentication(AuthenticationContext filterContext)
            {
            }

            public void OnAuthenticationChallenge(AuthenticationChallengeContext filterContext)
            {
                filterContext.Result = new LoggingActionResult("From authentication filter challenge");
            }
        }

        private class CustomExceptionFilterHandlesErrorAttribute : FilterAttribute, IExceptionFilter
        {
            public void OnException(ExceptionContext filterContext)
            {
                filterContext.ExceptionHandled = true;
                filterContext.Result = new LoggingActionResult("From exception filter");
            }
        }

        private class CustomAsyncExceptionFilterHandlesErrorAttribute : FilterAttribute, IExceptionFilter, IAsyncExceptionFilter
        {
            public void OnException(ExceptionContext filterContext)
            {
                throw new NotSupportedException();
            }

            public Task OnExceptionAsync(ExceptionContext context)
            {
                context.ExceptionHandled = true;
                context.Result = new LoggingActionResult("From exception filter");
                return TaskHelpers.CompletedTask;
            }
        }

        private class CustomExceptionFilterDoesNotHandleErrorAttribute : FilterAttribute, IExceptionFilter
        {
            public void OnException(ExceptionContext filterContext)
            {
            }
        }

        private class CustomAsyncExceptionFilterDoesNotHandleErrorAttribute : FilterAttribute, IExceptionFilter, IAsyncExceptionFilter
        {
            public void OnException(ExceptionContext filterContext)
            {
                throw new NotSupportedException();
            }

            public Task OnExceptionAsync(ExceptionContext context)
            {
                return TaskHelpers.CompletedTask;
            }
        }

        private class AuthorizationFilterReturnsResultAttribute : FilterAttribute, IAuthorizationFilter
        {
            public void OnAuthorization(AuthorizationContext filterContext)
            {
                filterContext.Result = new LoggingActionResult("From authorization filter");
            }
        }

        private class AsyncAuthorizationFilterReturnsResultAttribute : FilterAttribute, IAsyncAuthorizationFilter
        {
            public void OnAuthorization(AuthorizationContext filterContext)
            {
                throw new NotSupportedException();
            }

            public Task OnAuthorizationAsync(AuthorizationContext context)
            {
                context.Result = new LoggingActionResult("From authorization filter");
                return TaskHelpers.CompletedTask;
            }
        }

        private class LoggingActionResult : ActionResult
        {
            public LoggingActionResult(string logText)
            {
                LogText = logText;
            }

            public string LogText { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                ((TestController)context.Controller).Log = LogText;
            }
        }

        private class ActionResultWhichThrowsException : ActionResult
        {
            public override void ExecuteResult(ControllerContext context)
            {
                throw new Exception("Some exception text.");
            }
        }

        private class ActionResultWhichCallsThreadAbort : ActionResult
        {
            public override void ExecuteResult(ControllerContext context)
            {
                Thread.CurrentThread.Abort();
            }
        }

        private class ActionFilterOnActionExecutingShortCircuits : IActionFilter
        {
            public void OnActionExecuting(ActionExecutingContext filterContext)
            {
                filterContext.Result = new LoggingActionResult($"Called from {nameof(ActionFilterOnActionExecutingShortCircuits)}.{nameof(ActionFilterOnActionExecutingShortCircuits.OnActionExecuting)}");
            }

            public void OnActionExecuted(ActionExecutedContext filterContext)
            {
            }
        }

        private class AsyncActionFilterOnActionExecutingShortCircuits : IAsyncActionFilter
        {
            public void OnActionExecuting(ActionExecutingContext filterContext)
            {
                throw new NotSupportedException();
            }

            public void OnActionExecuted(ActionExecutedContext filterContext)
            {
                throw new NotSupportedException();
            }

            public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                context.Result = new LoggingActionResult($"Called from {nameof(AsyncActionFilterOnActionExecutingShortCircuits)}.{nameof(AsyncActionFilterOnActionExecutingShortCircuits.OnActionExecutionAsync)} before next");
                return TaskHelpers.CompletedTask;
            }
        }

        private class ActionFilterOnActionExecutedShortCircuits : IActionFilter
        {
            public void OnActionExecuting(ActionExecutingContext filterContext)
            {
            }

            public void OnActionExecuted(ActionExecutedContext filterContext)
            {
                filterContext.Result = new LoggingActionResult($"Called from {nameof(ActionFilterOnActionExecutedShortCircuits)}.{nameof(ActionFilterOnActionExecutedShortCircuits.OnActionExecuted)}");
            }
        }

        private class AsyncActionFilterOnActionExecutedShortCircuits : IAsyncActionFilter
        {
            public void OnActionExecuting(ActionExecutingContext filterContext)
            {
                throw new NotSupportedException();
            }

            public void OnActionExecuted(ActionExecutedContext filterContext)
            {
                throw new NotSupportedException();
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var actionExecutedContext = await next();
                actionExecutedContext.Result = new LoggingActionResult($"Called from {nameof(AsyncActionFilterOnActionExecutedShortCircuits)}.{nameof(AsyncActionFilterOnActionExecutedShortCircuits.OnActionExecutionAsync)} after next");
            }
        }

        private class ActionFilterImpl : IActionFilter, IResultFilter
        {
            public Action<ActionExecutingContext> OnActionExecutingImpl { get; set; }

            public void OnActionExecuting(ActionExecutingContext filterContext)
            {
                OnActionExecutingImpl?.Invoke(filterContext);
            }

            public Action<ActionExecutedContext> OnActionExecutedImpl { get; set; }

            public void OnActionExecuted(ActionExecutedContext filterContext)
            {
                OnActionExecutedImpl?.Invoke(filterContext);
            }

            public Action<ResultExecutingContext> OnResultExecutingImpl { get; set; }

            public void OnResultExecuting(ResultExecutingContext filterContext)
            {
                OnResultExecutingImpl?.Invoke(filterContext);
            }

            public Action<ResultExecutedContext> OnResultExecutedImpl { get; set; }

            public void OnResultExecuted(ResultExecutedContext filterContext)
            {
                OnResultExecutedImpl?.Invoke(filterContext);
            }
        }

        private class AsyncActionFilterImpl : IAsyncActionFilter, IAsyncResultFilter
        {
            public Action<ActionExecutingContext> OnActionExecutingImpl { get; set; }

            public void OnActionExecuting(ActionExecutingContext filterContext)
            {
                OnActionExecutingImpl?.Invoke(filterContext);
            }

            public Action<ActionExecutedContext> OnActionExecutedImpl { get; set; }

            public void OnActionExecuted(ActionExecutedContext filterContext)
            {
                OnActionExecutedImpl?.Invoke(filterContext);
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                OnActionExecuting(context);
                if (context.Result == null)
                {
                    OnActionExecuted(await next().ConfigureAwait(false));
                }
            }

            public Action<ResultExecutingContext> OnResultExecutingImpl { get; set; }

            public void OnResultExecuting(ResultExecutingContext filterContext)
            {
                OnResultExecutingImpl?.Invoke(filterContext);
            }

            public Action<ResultExecutedContext> OnResultExecutedImpl { get; set; }

            public void OnResultExecuted(ResultExecutedContext filterContext)
            {
                OnResultExecutedImpl?.Invoke(filterContext);
            }

            public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
            {
                OnResultExecuting(context);
                if (!context.Cancel)
                {
                    OnResultExecuted(await next().ConfigureAwait(false));
                }
            }
        }
    }
}
