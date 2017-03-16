using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            ControllerActionInvokerEx invoker = new ControllerActionInvokerEx();

            // Act
            var retVal = InvokeAction(invoker, controllerContext, "ActionNotFound", null, null);

            // Assert
            Assert.False(retVal);
        }

        [Fact]
        public void InvokeAction_ActionThrowsException_Handled()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            ControllerActionInvokerEx invoker = new ControllerActionInvokerEx();

            // Act & assert
            var retVal = InvokeAction(invoker, controllerContext, "ActionThrowsExceptionAndIsHandled", null, null);

            Assert.True(retVal);
            Assert.Equal("From exception filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ActionThrowsException_HandledAsync()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            ControllerActionInvokerEx invoker = new ControllerActionInvokerEx();

            // Act & assert
            var retVal = InvokeAction(invoker, controllerContext, "ActionThrowsExceptionAndIsHandledAsync", null, null);

            Assert.True(retVal);
            Assert.Equal("From exception filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ActionThrowsException_NotHandled()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            ControllerActionInvokerEx invoker = new ControllerActionInvokerEx();

            // Act & assert
            AssertEx.Throws<Exception>(() =>
            {
                var retVal = InvokeAction(invoker, controllerContext, "ActionThrowsExceptionAndIsNotHandled", null, null);
            }, @"Some exception text.");
        }

        [Fact]
        public void InvokeAction_ActionThrowsException_NotHandledAsync()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            ControllerActionInvokerEx invoker = new ControllerActionInvokerEx();

            // Act & assert
            AssertEx.Throws<Exception>(() =>
            {
                var retVal = InvokeAction(invoker, controllerContext, "ActionThrowsExceptionAndIsNotHandledAsync", null, null);
            }, @"Some exception text.");
        }

        private static bool InvokeAction(ControllerActionInvokerEx invoker, ControllerContext controllerContext, string actionName, AsyncCallback callback, object state)
        {
            IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, actionName, callback, state);
            return invoker.EndInvokeAction(asyncResult);
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

        private class TestController : AsyncController
        {
            public string Log { get; set; }

            [CustomExceptionFilterHandlesError]
            public void ActionThrowsExceptionAndIsHandledAsync()
            {
                throw new Exception("Some exception text.");
            }

            public void ActionThrowsExceptionAndIsHandledCompleted()
            {
            }

            [CustomAsyncExceptionFilterHandlesError]
            public void ActionThrowsExceptionAndIsHandledAsyncAsync()
            {
                throw new Exception("Some exception text.");
            }

            public void ActionThrowsExceptionAndIsHandledAsyncCompleted()
            {
            }

            [CustomExceptionFilterDoesNotHandleError]
            public void ActionThrowsExceptionAndIsNotHandledAsync()
            {
                throw new Exception("Some exception text.");
            }

            public void ActionThrowsExceptionAndIsNotHandledCompleted()
            {
            }

            [CustomAsyncExceptionFilterDoesNotHandleError]
            public void ActionThrowsExceptionAndIsNotHandledAsyncAsync()
            {
                throw new Exception("Some exception text.");
            }

            public void ActionThrowsExceptionAndIsNotHandledAsyncCompleted()
            {
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
