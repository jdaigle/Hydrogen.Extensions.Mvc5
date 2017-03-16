using System.Threading.Tasks;
using System.Web.Mvc;

namespace MvcAsync
{
    public interface IAsyncAuthorizationFilter : IMvcFilter, IAuthorizationFilter
    {
        Task OnAuthorizationAsync(AuthorizationContext context);
    }
}
