using Microsoft.AspNetCore.Mvc;

namespace ProjectCafeWeb.Controllers
{
	public class TableController : Controller
	{
		public IActionResult Table()
		{
			return View();
		}
	}
}
