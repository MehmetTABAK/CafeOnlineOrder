using Microsoft.AspNetCore.Mvc;

namespace ProjectCafeWeb.Controllers
{
	public class LoginController : Controller
	{
		public IActionResult SignIn()
		{
			return View();
		}
	}
}
