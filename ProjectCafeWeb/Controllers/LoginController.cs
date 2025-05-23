using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ProjectCafeDataAccess;
using ProjectCafeEntities;
using System.Security.Claims;

namespace ProjectCafeWeb.Controllers
{
	public class LoginController : Controller
	{
		private readonly ProjectCafeDbContext _dbContext;

		public LoginController(ProjectCafeDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		[HttpGet]
        public IActionResult SignIn()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> SignIn(string email, string password)
		{
			var user = _dbContext.Admin
				.AsEnumerable()
				.FirstOrDefault(x =>
					x.Email.Equals(email, StringComparison.Ordinal) &&
					x.Password.Equals(password, StringComparison.Ordinal) &&
					x.Active);

			string role = "Admin";
			List<string> permissions = new();

			if (user == null)
			{
				var worker = _dbContext.Worker
					.AsEnumerable()
					.FirstOrDefault(x =>
						x.Email.Equals(email, StringComparison.Ordinal) &&
						x.Password.Equals(password, StringComparison.Ordinal) &&
						x.Active);

				if (worker != null)
				{
					role = "Worker";

					if (!string.IsNullOrEmpty(worker.RolePermissions))
					{
						permissions = JsonConvert.DeserializeObject<List<string>>(worker.RolePermissions) ?? new List<string>();
					}

					var claims = new List<Claim>
					{
						new Claim(ClaimTypes.Name, worker.Email),
						new Claim("UserId", worker.Id.ToString()),
						new Claim("FullName", worker.Firstname + " " + worker.Lastname),
						new Claim(ClaimTypes.Role, role),
						new Claim("Permissions", JsonConvert.SerializeObject(permissions))
					};

					var claimsIdentity = new ClaimsIdentity(claims, "cookieAuth");
					var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

					await HttpContext.SignInAsync("cookieAuth", claimsPrincipal);

					return RedirectToAction("Home", "Home");
				}
				else
				{
					ViewBag.LoginError = "Email veya şifre yanlış.";
					return View();
				}
			}

			var adminClaims = new List<Claim>
			{
				new Claim(ClaimTypes.Name, user.Email),
				new Claim("UserId", user.Id.ToString()),
				new Claim("FullName", user.Firstname + " " + user.Lastname),
				new Claim(ClaimTypes.Role, role),
				new Claim("Permissions", JsonConvert.SerializeObject(permissions))
			};

			var adminClaimsIdentity = new ClaimsIdentity(adminClaims, "cookieAuth");
			var adminClaimsPrincipal = new ClaimsPrincipal(adminClaimsIdentity);

			await HttpContext.SignInAsync("cookieAuth", adminClaimsPrincipal);

			return RedirectToAction("Home", "Home");
		}

		public async Task<IActionResult> LogOut()
		{
			await HttpContext.SignOutAsync("cookieAuth");
			return RedirectToAction("Home", "Home");
		}

		//[HttpPost]
		//public IActionResult Register(string firstname, string lastname, string email, string password, string passwordConfirm)
		//{
		//	if (password != passwordConfirm)
		//	{
		//		ViewBag.RegisterError = "Şifre ve şifre onayı eşleşmiyor.";
		//		return View("SignIn");
		//	}

		//	var admin = new Admin
		//	{
		//		Firstname = firstname,
		//		Lastname = lastname,
		//		Email = email,
		//		Password = password,
		//		Active = false,
		//		RegistrationUser = 0,
		//		RegistrationDate = DateTime.Now
		//	};

		//	_dbContext.Admin.Add(admin);
		//	_dbContext.SaveChanges();

		//	ViewBag.RegisterSuccess = "Kayıt başarıyla tamamlandı. Giriş yapabilirsiniz.";
		//	return View("SignIn");
		//}
	}
}