using System.Threading.Tasks;
using System.Web.Mvc;

namespace Horton.Mvc5.Async
{
    public interface IAsyncExceptionFilter : IExceptionFilter
    {
        Task OnExceptionAsync(ExceptionContext context);
    }
}
