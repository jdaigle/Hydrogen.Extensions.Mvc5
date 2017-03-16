using System.Threading.Tasks;
using System.Web.Mvc;

namespace MvcAsync
{
    public interface IAsyncResultFilter : IMvcFilter, IResultFilter
    {
        Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next);
    }
}
