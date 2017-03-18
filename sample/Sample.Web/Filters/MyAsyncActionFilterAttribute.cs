using System.Threading.Tasks;
using System.Web.Mvc;
using Horton.Extensions.Mvc5.Async;

namespace Sample.Web.Filters
{
    public class MyAsyncActionFilterAttribute : AsyncActionFilterAttribute
    {
        public MyAsyncActionFilterAttribute()
        {
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Code that executes _before_ the action

            // invoke the next filter
            await next().ConfigureAwait(false);

            // code that execute _after_ the action
        }
    }
}
