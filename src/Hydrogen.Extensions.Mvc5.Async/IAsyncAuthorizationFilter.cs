using System.Threading.Tasks;
using System.Web.Mvc;

namespace Hydrogen.Extensions.Mvc5.Async
{
    public interface IAsyncAuthorizationFilter : IAuthorizationFilter
    {
        Task OnAuthorizationAsync(AuthorizationContext context);
    }
}
