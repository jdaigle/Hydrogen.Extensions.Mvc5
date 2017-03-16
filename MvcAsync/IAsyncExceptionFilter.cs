using System.Threading.Tasks;
using System.Web.Mvc;

namespace MvcAsync
{
    public interface IAsyncExceptionFilter : IMvcFilter, IExceptionFilter
    {
        Task OnExceptionAsync(ExceptionContext context);
    }
}
