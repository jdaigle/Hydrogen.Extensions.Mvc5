using System.Threading.Tasks;
using System.Web.Mvc;

namespace Horton.Extensions.Mvc5.Async
{
    public interface IAsyncAuthorizationFilter : IAuthorizationFilter
    {
        Task OnAuthorizationAsync(AuthorizationContext context);
    }
}
