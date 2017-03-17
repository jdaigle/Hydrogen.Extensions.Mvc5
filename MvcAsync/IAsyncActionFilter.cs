using System.Threading.Tasks;
using System.Web.Mvc;

namespace MvcAsync
{
    public interface IAsyncActionFilter : IActionFilter
    {
        Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next);
    }
}
