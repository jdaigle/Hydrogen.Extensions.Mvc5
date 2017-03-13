using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Async;
using System.Web.Mvc.Filters;
using BenchmarkDotNet.Attributes;
using Moq;

namespace MvcAsync.Benchmark
{
    public class AsyncControllerActionInvokerBenchmark
    {
        private ControllerContext controllerContext;

        [Setup]
        public void Setup()
        {
            controllerContext = GetControllerContext();
        }

        [Benchmark]
        public void AsyncControllerActionInvoker_BeginInvokeAction_NormalAction()
        {
            controllerContext.Controller = new TestController();
            AsyncControllerActionInvoker invoker = new AsyncControllerActionInvoker();

            IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, "NormalAction", null, null);
            bool retVal = invoker.EndInvokeAction(asyncResult);
        }

        [Benchmark]
        public void AsyncControllerActionInvokerEx_BeginInvokeAction_NormalAction()
        {
            controllerContext.Controller = new TestController();
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, "NormalAction", null, null);
            bool retVal = invoker.EndInvokeAction(asyncResult);
        }

        [Benchmark]
        public async Task AsyncControllerActionInvokerEx_InvokeAction_NormalAction()
        {
            controllerContext.Controller = new TestController();
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            bool retVal = await invoker.InvokeActionAsync(controllerContext, "NormalAction");
        }

        private static ControllerContext GetControllerContext(bool passesRequestValidation = true)
        {
            Mock<HttpContextBase> mockHttpContext = new Mock<HttpContextBase>();
            if (passesRequestValidation)
            {
#pragma warning disable 618
                mockHttpContext.Setup(o => o.Request.ValidateInput());
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

            [NoOpExceptionFilter]
            [NoOpAuthenticationFilter]
            [NoOpAuthorizationFilter]
            [NoOpActionFilter]
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
