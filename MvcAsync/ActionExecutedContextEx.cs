using System;
using System.Runtime.ExceptionServices;
using System.Web.Mvc;

namespace MvcAsync
{
    public class ActionExecutedContextEx : ActionExecutedContext
    {
        public ActionExecutedContextEx(ControllerContext controllerContext, ActionDescriptor actionDescriptor, bool canceled, Exception exception)
            : base(controllerContext, actionDescriptor, canceled, exception)
        {
        }

        public ExceptionDispatchInfo ExceptionDispatchInfo { get; set; }
    }
}
