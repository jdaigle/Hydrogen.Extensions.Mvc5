using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Async;
using System.Web.Mvc.Filters;
using System.Web.Routing;
using Moq;
using Xunit;

namespace Horton.Extensions.Mvc5.Async
{
    public class AsyncControllerTest
    {
        [Fact]
        public void BeginExecute_AsyncControllerActionInvoker_NormalAction()
        {
            var requestContext = GetRequestContext(nameof(TestController.NormalAction));
            var controller = new TestController() as IAsyncController;
            ((Controller)controller).ActionInvoker = new AsyncControllerActionInvoker();

            IAsyncResult asyncResult = controller.BeginExecute(requestContext, null, null);
            controller.EndExecute(asyncResult);

            Assert.Equal("From action", ((TestController)controller).Log);
        }

        [Fact]
        public void BeginExecute_ControllerActionInvokerEx_NormalAction()
        {
            var requestContext = GetRequestContext(nameof(TestController.NormalAction));
            var controller = new TestController() as IAsyncController;
            ((Controller)controller).ActionInvoker = new ControllerActionInvokerEx();

            IAsyncResult asyncResult = controller.BeginExecute(requestContext, null, null);
            controller.EndExecute(asyncResult);

            Assert.Equal("From action", ((TestController)controller).Log);
        }

        private static RequestContext GetRequestContext(string actionName = null)
        {
            Mock<HttpRequestBase> mockHttpRequest = new Mock<HttpRequestBase>();
            Mock<HttpContextBase> mockHttpContext = new Mock<HttpContextBase>();
            mockHttpContext.SetupGet(t => t.Request).Returns(mockHttpRequest.Object);
            RouteData routeData = new RouteData();
            if (actionName != null)
            {
                routeData.Values["action"] = actionName;
            }

            return new RequestContext(mockHttpContext.Object, routeData);
        }

        private class TestController : Controller
        {
            public string Log { get; set; }

            [NoOpExceptionFilter]
            [NoOpAuthenticationFilter]
            [NoOpAuthorizationFilter]
            [NoOpActionFilter(Order = 1)]
            [NoOpAsyncActionFilter(Order = 2)]
            [NoOpActionFilterEx(Order = 3)]
            [NoOpResultFilter]
            public ActionResult NormalAction()
            {
                return new LoggingActionResult("From action");
            }

            public class LoggingActionResult : ActionResult
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

            public class NoOpActionFilterAttribute : FilterAttribute, IActionFilter
            {
                public void OnActionExecuting(ActionExecutingContext filterContext)
                {
                }

                public void OnActionExecuted(ActionExecutedContext filterContext)
                {
                }
            }

            public class NoOpAsyncActionFilterAttribute : AsyncActionFilterAttribute
            {
                public override void OnActionExecuting(ActionExecutingContext filterContext)
                {
                }

                public override void OnActionExecuted(ActionExecutedContext filterContext)
                {
                }
            }

            public class NoOpActionFilterExAttribute : ActionFilterAttribute
            {
                public override void OnActionExecuting(ActionExecutingContext filterContext)
                {
                }

                public override void OnActionExecuted(ActionExecutedContext filterContext)
                {
                }
            }

            public class NoOpResultFilterAttribute : FilterAttribute, IResultFilter
            {
                public void OnResultExecuted(ResultExecutedContext filterContext)
                {
                }

                public void OnResultExecuting(ResultExecutingContext filterContext)
                {
                }
            }

            public class NoOpExceptionFilterAttribute : FilterAttribute, IExceptionFilter
            {
                public void OnException(ExceptionContext filterContext)
                {
                }
            }

            public class NoOpAuthorizationFilterAttribute : FilterAttribute, IAuthorizationFilter
            {
                public void OnAuthorization(AuthorizationContext filterContext)
                {
                }
            }

            public class NoOpAuthenticationFilterAttribute : FilterAttribute, IAuthenticationFilter
            {
                public void OnAuthentication(AuthenticationContext filterContext)
                {
                }

                public void OnAuthenticationChallenge(AuthenticationChallengeContext filterContext)
                {
                }
            }
        }
    }
}
