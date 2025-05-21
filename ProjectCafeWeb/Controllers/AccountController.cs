using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectCafeDataAccess;
using ProjectCafeEntities;
using ProjectCafeWeb.Attributes;
using ProjectCafeWeb.ViewModels;

namespace ProjectCafeWeb.Controllers
{
	public class AccountController : BaseController
	{
        public AccountController(ProjectCafeDbContext dbContext) : base(dbContext)
        {
        }

        [AuthorizeWithPermission("ViewDailyAccount")]
		public IActionResult DailyAccount(DateTime? selectedDate)
		{
            var cafeId = GetCurrentCafeId();
            if (cafeId == null)
                return Unauthorized();

            var paymentsQuery = _dbContext.Payment
				.Include(p => p.Table)
                .ThenInclude(t => t.Section)
				.Where(p => p.Active &&
                        p.Table != null &&
                        p.Table.Section != null &&
                        p.Table.Section.CafeId == cafeId);

			if (selectedDate.HasValue)
			{
				paymentsQuery = paymentsQuery.Where(d => d.RegistrationDate.Date == selectedDate.Value.Date);
			}
			else
			{
				var latestActivePayment = _dbContext.Payment
					.Where(p => p.Active)
					.OrderByDescending(p => p.RegistrationDate)
					.FirstOrDefault();

				if (latestActivePayment != null)
				{
					paymentsQuery = paymentsQuery.Where(d => d.RegistrationDate.Date == latestActivePayment.RegistrationDate.Date);
				}
				else
				{
					paymentsQuery = Enumerable.Empty<Payment>().AsQueryable();
				}
			}

			var admins = _dbContext.Admin.ToDictionary(a => a.Id, a => $"{a.Firstname} {a.Lastname}");

			var dailyDatas = paymentsQuery
				.OrderBy(d => d.RegistrationDate)
				.ToList()
				.Select(item => new DailyAccountViewModel
				{
					RegistrationDate = item.RegistrationDate,
					TableName = item.Table?.Name ?? "-",
					TotalPrice = item.TotalPrice,
					PaymentMethod = item.Method == 1 ? "Kart" : item.Method == 2 ? "Nakit" : item.Method == 3 ? "İade" : item.Method == 4 ? "İkram" : "-",
					RegistrationUserFullName = admins.ContainsKey(item.RegistrationUser) ? admins[item.RegistrationUser] : "-",
					//CorrectionUserFullName = item.CorrectionUser.HasValue && admins.ContainsKey(item.CorrectionUser.Value) ? admins[item.CorrectionUser.Value] : "-",
					//CorrectionDate = item.CorrectionDate?.ToString("dd.MM.yyyy HH:mm") ?? "-",
					Date = item.RegistrationDate.ToString("dd.MM.yyyy")
				}).ToList();

			ViewData["SelectedDate"] = selectedDate;

			return View(dailyDatas);
		}
	}
}
