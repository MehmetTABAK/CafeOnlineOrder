using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProjectCafeWeb.Models;

namespace ProjectCafeWeb.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Home()
        {
            return View();
        }
    }
}
