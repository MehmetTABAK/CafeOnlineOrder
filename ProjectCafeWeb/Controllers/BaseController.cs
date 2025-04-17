using Microsoft.AspNetCore.Mvc;

namespace ProjectCafeWeb.Controllers
{
	public class BaseController : Controller
	{
		protected int? GetCurrentAdminId()
		{
			var idStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
			return int.TryParse(idStr, out var id) ? id : null;
		}
	}
}
