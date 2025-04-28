using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectCafeDataAccess;
using System.Security.Claims;

namespace ProjectCafeWeb.Controllers
{
	public class BaseController : Controller
	{
		protected readonly ProjectCafeDbContext _dbContext;

		public BaseController(ProjectCafeDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		protected int? GetCurrentUserId()
        {
            var idStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            return int.TryParse(idStr, out var id) ? id : null;
        }

		protected string? GetCurrentUserRole()
		{
			return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
		}

		protected int? GetCurrentCafeId()
		{
			var userId = GetCurrentUserId();
			var role = GetCurrentUserRole();

			if (userId == null || role == null)
				return null;

			if (role == "Admin")
			{
				var cafe = _dbContext.Cafe.FirstOrDefault(c => c.AdminId == userId);
				return cafe?.Id;
			}
			else if (role == "Worker")
			{
				var worker = _dbContext.Worker.FirstOrDefault(w => w.Id == userId);
				return worker?.CafeId;
			}

			return null;
		}
	}
}
