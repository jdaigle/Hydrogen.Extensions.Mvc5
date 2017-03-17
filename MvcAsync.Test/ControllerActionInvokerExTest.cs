using System;
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
            // Arrange
            ControllerContext controllerContext = GetControllerContext();

            // Act & assert
            var retVal = InvokeAction(controllerContext, "ActionThrowsExceptionAndIsHandled", null, null);

            Assert.True(retVal);
            Assert.Equal("From exception filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ActionThrowsException_HandledAsync()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();

            // Act & assert
            var retVal = InvokeAction(controllerContext, "ActionThrowsExceptionAndIsHandledAsync", null, null);

            Assert.True(retVal);
            Assert.Equal("From exception filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ActionThrowsException_NotHandled()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();

            // Act & assert
            AssertEx.Throws<Exception>(() =>
            {
                var retVal = InvokeAction(controllerContext, "ActionThrowsExceptionAndIsNotHandled", null, null);
            }, @"Some exception text.");
        }

        [Fact]
        public void InvokeAction_ActionThrowsException_NotHandledAsync()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();

            // Act & assert
            AssertEx.Throws<Exception>(() =>
            {
                var retVal = InvokeAction(controllerContext, "ActionThrowsExceptionAndIsNotHandledAsync", null, null);
            }, @"Some exception text.");
        }

        private static bool InvokeAction(ControllerContext controllerContext, string actionName, AsyncCallback callback, object state)
        {
            var invoker = new ControllerActionInvokerEx();
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
