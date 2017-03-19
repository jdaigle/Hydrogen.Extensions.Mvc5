using System;
using System.Runtime.ExceptionServices;
using System.Web.Mvc;

namespace Hydrogen.Extensions.Mvc5.Async
{
    public class ExceptionContextEx : ExceptionContext
    {
        public ExceptionContextEx(ControllerContext controllerContext, Exception exception)
            : base(controllerContext, exception)
        {
        }

        public ExceptionDispatchInfo ExceptionDispatchInfo { get; set; }
    }
}
