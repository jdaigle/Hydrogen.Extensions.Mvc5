using System.Threading.Tasks;
using System.Web.Mvc;

namespace MvcAsync
{
    public interface IAsyncAuthorizationFilter : IAuthorizationFilter
    {
        Task OnAuthorizationAsync(AuthorizationContext context);
    }
}
