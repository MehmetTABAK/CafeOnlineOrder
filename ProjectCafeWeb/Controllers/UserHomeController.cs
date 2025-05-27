using Microsoft.AspNetCore.Mvc;

namespace ProjectCafeWeb.Controllers
{
    public class UserHomeController : Controller
    {
        public IActionResult UserHome(int cafeId, int tableId)
        {
            HttpContext.Session.SetInt32("CafeId", cafeId);
            HttpContext.Session.SetInt32("TableId", tableId);
            return View();
        }
    }
}
