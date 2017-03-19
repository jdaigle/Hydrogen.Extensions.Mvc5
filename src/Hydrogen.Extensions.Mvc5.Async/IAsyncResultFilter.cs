using System.Threading.Tasks;
using System.Web.Mvc;

namespace Hydrogen.Extensions.Mvc5.Async
{
    public interface IAsyncResultFilter : IResultFilter
    {
        Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next);
    }
}
