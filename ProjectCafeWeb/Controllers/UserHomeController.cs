using Microsoft.AspNetCore.Mvc;
using ProjectCafeDataAccess;

namespace ProjectCafeWeb.Controllers
{
    public class UserHomeController : BaseController
    {
        public UserHomeController(ProjectCafeDbContext dbContext) : base(dbContext) { }

        public IActionResult UserHome(int cafeId, int tableId)
        {
            HttpContext.Session.SetInt32("CafeId", cafeId);
            HttpContext.Session.SetInt32("TableId", tableId);
            return View();
        }
    }
}
