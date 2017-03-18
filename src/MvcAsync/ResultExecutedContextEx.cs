using System;
using System.Runtime.ExceptionServices;
using System.Web.Mvc;

namespace MvcAsync
{
    public class ResultExecutedContextEx : ResultExecutedContext
    {
        public ResultExecutedContextEx(ControllerContext controllerContext, ActionResult result, bool canceled, Exception exception)
            : base(controllerContext, result, canceled, exception)
        {
        }

        public ExceptionDispatchInfo ExceptionDispatchInfo { get; set; }
    }
}
