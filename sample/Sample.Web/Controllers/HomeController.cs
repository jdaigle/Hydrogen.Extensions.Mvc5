using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Sample.Web.Filters;

namespace Sample.Web.Controllers
{
    [MyAsyncActionFilter]
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            await Task.FromResult(0);
            return View();
        }

        public ActionResult About()
        {
            throw new Exception("testing");
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}