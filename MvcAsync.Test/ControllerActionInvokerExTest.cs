using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Xunit;

namespace MvcAsync
{
    public class ControllerActionInvokerExTest
    {
        [Fact]
        public void InvokeAction_ActionNotFound()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();

            // Act
            var retVal = InvokeAction(controllerContext, "ActionNotFound", null, null);

            // Assert
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

            AssertEx.Throws<ThreadAbortException>(() =>
            {
                var retVal = InvokeAction(controllerContext, nameof(TestController.ActionCallsThreadAbort), null, null);
            });
        }

        [Fact]
        public void InvokeAction_AuthorizationFilterShortCircuits()
        {
            var controllerContext = GetControllerContext();

            // Act
            var retVal = InvokeAction(controllerContext, nameof(TestController.AuthorizationFilterShortCircuits), null, null);

            // Assert
            Assert.True(retVal);
            Assert.Equal("From authorization filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_AsyncAuthorizationFilterShortCircuits()
        {
            var controllerContext = GetControllerContext();

            // Act
            var retVal = InvokeAction(controllerContext, nameof(TestController.AsyncAuthorizationFilterShortCircuits), null, null);

            // Assert
            Assert.True(retVal);
            Assert.Equal("From authorization filter", ((TestController)controllerContext.Controller).Log);
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

            [AuthorizationFilterReturnsResult]
            public void AuthorizationFilterShortCircuits()
            {
            }

            [AsyncAuthorizationFilterReturnsResult]
            public Task AsyncAuthorizationFilterShortCircuits()
            {
                return Task.CompletedTask;
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
                return Task.CompletedTask;
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
                return Task.CompletedTask;
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
                return Task.CompletedTask;
            }
        }

        private class LoggingActionResult : ActionResult
        {
            private readonly string _logText;

            public LoggingActionResult(string logText)
            {
                _logText = logText;
            }

            public override void ExecuteResult(ControllerContext context)
            {
                ((TestController)context.Controller).Log = _logText;
            }
        }
    }
}
