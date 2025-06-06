﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
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

		protected Guid? GetCurrentUserId()
        {
            var idStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            return Guid.TryParse(idStr, out var id) ? id : null;
        }

		protected string? GetCurrentUserRole()
		{
			return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
		}

		protected Guid? GetCurrentCafeId()
		{
			var userId = GetCurrentUserId();
			var role = GetCurrentUserRole();

			if (userId != null && role != null)
            {
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
            }

            // Eğer login yapılmamışsa (anonim müşteri ise) Session'dan al
            var sessionCafeIdString = HttpContext.Session.GetString("CafeId");

            if (Guid.TryParse(sessionCafeIdString, out Guid cafeGuid))
            {
                return cafeGuid;
            }
            return null;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            var role = GetCurrentUserRole();

            if (role != "Admin")
            {
                var cafeId = GetCurrentCafeId();

                if (cafeId == null)
                {
                    base.OnActionExecuting(context);
                    return;
                }

                var allowedIpPrefix = _dbContext.Cafe
                    .Where(c => c.Id == cafeId)
                    .Select(c => c.AllowedIpPrefix)
                    .FirstOrDefault();

                bool isInAllowedNetwork = ip != null && (
                    ip == "::1" || ip == "127.0.0.1" ||
                    (!string.IsNullOrEmpty(allowedIpPrefix) && ip.StartsWith(allowedIpPrefix))
                );

                if (!isInAllowedNetwork)
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
                    return;
                }
            }

            base.OnActionExecuting(context);
        }

        public bool IsDuplicateKeyException(DbUpdateException ex)
        {
            return ex.InnerException is SqlException sqlEx &&
                   (sqlEx.Number == 2627 || sqlEx.Number == 2601); // PK veya unique key violation
        }
    }
}
