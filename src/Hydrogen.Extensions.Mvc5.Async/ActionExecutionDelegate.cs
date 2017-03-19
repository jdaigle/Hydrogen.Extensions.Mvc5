using System.Threading.Tasks;
using System.Web.Mvc;

namespace Hydrogen.Extensions.Mvc5.Async
{
    public delegate Task<ActionExecutedContext> ActionExecutionDelegate();
}
