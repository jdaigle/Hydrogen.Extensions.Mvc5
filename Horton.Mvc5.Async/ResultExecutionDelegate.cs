using System.Threading.Tasks;
using System.Web.Mvc;

namespace Horton.Mvc5.Async
{
    public delegate Task<ResultExecutedContext> ResultExecutionDelegate();
}