using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProjectCafeWeb.Models;

namespace ProjectCafeWeb.Controllers
{
    public class HomeController : Controller
    {
        [Route("kafe-yonetim-sayfasi")]
        public IActionResult Home()
        {
            return View();
        }
    }
}
