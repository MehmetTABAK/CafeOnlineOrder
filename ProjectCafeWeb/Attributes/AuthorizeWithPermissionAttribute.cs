using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;

namespace ProjectCafeWeb.Attributes
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
	public class AuthorizeWithPermissionAttribute : Attribute, IAuthorizationFilter
	{
		private readonly string _requiredPermission;

		public AuthorizeWithPermissionAttribute(string requiredPermission)
		{
			_requiredPermission = requiredPermission;
		}

		public void OnAuthorization(AuthorizationFilterContext context)
		{
			var user = context.HttpContext.User;

			if (!user.Identity.IsAuthenticated)
			{
				context.Result = new RedirectToActionResult("SignIn", "Login", null);
				return;
			}

			var role = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

			if (role == "Admin")
			{
				return;
			}

			if (role == "Worker")
			{
				var permissionsClaim = user.Claims.FirstOrDefault(c => c.Type == "Permissions")?.Value;

				if (permissionsClaim != null)
				{
					var permissions = JsonConvert.DeserializeObject<List<string>>(permissionsClaim);

					if (permissions != null && permissions.Contains(_requiredPermission))
					{
						return;
					}
				}

				context.Result = new ForbidResult();
			}
		}
	}
}
