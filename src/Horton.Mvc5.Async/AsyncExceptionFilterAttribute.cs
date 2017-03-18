using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Horton.Mvc5.Async.Internal;

namespace Horton.Mvc5.Async
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AsyncExceptionFilterAttribute : FilterAttribute, IAsyncExceptionFilter, IExceptionFilter
    {

        public virtual void OnException(ExceptionContext context)
        {
        }

        public virtual Task OnExceptionAsync(ExceptionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            OnException(context);
            return TaskHelpers.CompletedTask;
        }
    }
}
