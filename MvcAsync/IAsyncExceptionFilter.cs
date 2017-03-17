using System.Threading.Tasks;
using System.Web.Mvc;

namespace MvcAsync
{
    public interface IAsyncExceptionFilter : IExceptionFilter
    {
        Task OnExceptionAsync(ExceptionContext context);
    }
}
