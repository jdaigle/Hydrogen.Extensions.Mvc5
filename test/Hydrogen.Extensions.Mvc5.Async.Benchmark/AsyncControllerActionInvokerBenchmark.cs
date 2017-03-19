using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Async;
using System.Web.Mvc.Filters;
using System.Web.Routing;
using BenchmarkDotNet.Attributes;
using Moq;

namespace Hydrogen.Extensions.Mvc5.Async.Benchmark
{
    [Config(typeof(Config))]
    public class AsyncControllerActionInvokerBenchmark
    {
        private ControllerContext _controllerContext;
        private RequestContext _requestContext;

        [Setup]
        public void Setup()
        {
            _controllerContext = GetControllerContext();
            _requestContext = GetRequestContext(nameof(TestController.NormalAction));
        }

        [Benchmark]
        public void InvokeAction_ControllerActionInvokerEx()
        {
            _controllerContext.Controller = new TestController();
            var actionInvoker = ControllerActionInvokerEx.Instance;

            IAsyncResult asyncResult = actionInvoker.BeginInvokeAction(_controllerContext, nameof(TestController.NormalAction), null, null);
            var retVal = actionInvoker.EndInvokeAction(asyncResult);
        }

        [Benchmark]
        public void IAsyncController_BeginExecute_ControllerActionInvokerEx()
        {
            var controller = new TestController() as IAsyncController;
            ((Controller)controller).ActionInvoker = ControllerActionInvokerEx.Instance;

            IAsyncResult asyncResult = controller.BeginExecute(_requestContext, null, null);
            controller.EndExecute(asyncResult);
        }

        [Benchmark]
        public void InvokeAction_AsyncControllerActionInvoker()
        {
            _controllerContext.Controller = new TestController();
            var actionInvoker = new AsyncControllerActionInvoker();

            IAsyncResult asyncResult = actionInvoker.BeginInvokeAction(_controllerContext, nameof(TestController.NormalAction), null, null);
            var retVal = actionInvoker.EndInvokeAction(asyncResult);
        }

        [Benchmark]
        public void IAsyncController_BeginExecute_AsyncControllerActionInvoker()
        {
            var controller = new TestController() as IAsyncController;
            ((Controller)controller).ActionInvoker = new AsyncControllerActionInvoker();

            IAsyncResult asyncResult = controller.BeginExecute(_requestContext, null, null);
            controller.EndExecute(asyncResult);
        }

        private static ControllerContext GetControllerContext()
        {
            Mock<HttpRequestBase> mockHttpRequest = new Mock<HttpRequestBase>();
            Mock<HttpContextBase> mockHttpContext = new Mock<HttpContextBase>();
            mockHttpContext.SetupGet(t => t.Request).Returns(mockHttpRequest.Object);

            return new ControllerContext()
            {
                Controller = new TestController(),
                HttpContext = mockHttpContext.Object
            };
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
            [NoOpActionFilter]
            [NoOpResultFilter]
            public async Task<ActionResult> NormalAction()
            {
                return await Task.FromResult(new LoggingActionResult("From action"));
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
                public void OnActionExecuted(ActionExecutedContext filterContext)
                {
                }

                public void OnActionExecuting(ActionExecutingContext filterContext)
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
