using System.Threading.Tasks;
using System.Web.Mvc;

namespace MvcAsync
{
    public delegate Task<ResultExecutedContext> ResultExecutionDelegate();
}