using Microsoft.AspNetCore.Mvc;
using ProjectCafeDataAccess;

namespace ProjectCafeWeb.Controllers
{
    public class UserHomeController : BaseController
    {
        public UserHomeController(ProjectCafeDbContext dbContext) : base(dbContext) { }

        public IActionResult UserHome(Guid cafeId, Guid tableId)
        {
            HttpContext.Session.SetString("CafeId", cafeId.ToString());
            HttpContext.Session.SetString("TableId", tableId.ToString());
            return View();
        }
    }
}
