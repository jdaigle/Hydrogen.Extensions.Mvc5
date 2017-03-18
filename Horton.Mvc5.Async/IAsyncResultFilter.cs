using System.Threading.Tasks;
using System.Web.Mvc;

namespace Horton.Mvc5.Async
{
    public interface IAsyncResultFilter : IResultFilter
    {
        Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next);
    }
}
