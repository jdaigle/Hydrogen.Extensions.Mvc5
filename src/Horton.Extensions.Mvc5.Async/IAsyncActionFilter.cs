using System.Threading.Tasks;
using System.Web.Mvc;

namespace Horton.Extensions.Mvc5.Async
{
    public interface IAsyncActionFilter : IActionFilter
    {
        Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next);
    }
}
