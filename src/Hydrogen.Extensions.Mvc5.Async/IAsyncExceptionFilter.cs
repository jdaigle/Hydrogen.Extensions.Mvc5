using System.Threading.Tasks;
using System.Web.Mvc;

namespace Hydrogen.Extensions.Mvc5.Async
{
    public interface IAsyncExceptionFilter : IExceptionFilter
    {
        Task OnExceptionAsync(ExceptionContext context);
    }
}
