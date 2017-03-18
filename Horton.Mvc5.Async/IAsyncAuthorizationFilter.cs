using System.Threading.Tasks;
using System.Web.Mvc;

namespace Horton.Mvc5.Async
{
    public interface IAsyncAuthorizationFilter : IAuthorizationFilter
    {
        Task OnAuthorizationAsync(AuthorizationContext context);
    }
}
