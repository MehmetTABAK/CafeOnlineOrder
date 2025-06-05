using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectCafeDataAccess;
using ProjectCafeEntities;
using ProjectCafeWeb.Attributes;
using ProjectCafeWeb.ViewModels;

namespace ProjectCafeWeb.Controllers
{
    [Authorize]
    public class AccountController : BaseController
	{
        public AccountController(ProjectCafeDbContext dbContext) : base(dbContext)
        {
        }

        [AuthorizeWithPermission("ViewDailyAccount")]
        [Route("gunluk-islemler")]
        public IActionResult DailyAccount(Guid? rapor)
        {
            var cafeId = GetCurrentCafeId();
            if (cafeId == null)
                return Unauthorized();

            DailyReport report;

            if (rapor != null)
            {
                report = _dbContext.DailyReport
                    .FirstOrDefault(r => r.Id == rapor && r.CafeId == cafeId);
            }
            else
            {
                // Önce aktif raporu al, yoksa en son tamamlanan raporu al
                report = _dbContext.DailyReport
                    .Where(r => r.CafeId == cafeId)
                    .OrderByDescending(r => r.StartTime)
                    .FirstOrDefault();
            }

            if (report == null)
            {
                ViewBag.Message = "Henüz geçmişe veya aktif güne ait bir rapor bulunamadı.";
                return View(new List<DailyAccountViewModel>());
            }

            var payments = _dbContext.Payment
                .Include(p => p.Table).ThenInclude(t => t.Section)
                .Where(p => p.Active &&
                            p.Table.Section.CafeId == cafeId &&
                            p.RegistrationDate >= report.StartTime &&
                            (report.EndTime == null || p.RegistrationDate <= report.EndTime))
                .ToList();

            var admins = _dbContext.Admin.ToDictionary(a => a.Id, a => $"{a.Firstname} {a.Lastname}");

            var dailyDatas = payments.Select(p => new DailyAccountViewModel
            {
                RegistrationDate = p.RegistrationDate,
                TableName = p.Table?.Name ?? "-",
                TotalPrice = p.TotalPrice,
                PaymentMethod = p.Method == 1 ? "Kart" :
                                p.Method == 2 ? "Nakit" :
                                p.Method == 3 ? "İade" :
                                p.Method == 4 ? "İkram" : "-",
                Comment = p.Comment ?? "-",
                RegistrationUserFullName = admins.ContainsKey(p.RegistrationUser) ? admins[p.RegistrationUser] : "-",
                Date = p.RegistrationDate.ToString("dd.MM.yyyy")
            }).OrderBy(p => p.RegistrationDate).ToList();

            var totalCard = payments.Where(p => p.Method == 1).Sum(p => p.TotalPrice);
            var totalCash = payments.Where(p => p.Method == 2).Sum(p => p.TotalPrice);
            var totalRefund = payments.Where(p => p.Method == 3).Sum(p => p.TotalPrice);
            var totalBonus = payments.Where(p => p.Method == 4).Sum(p => p.TotalPrice);

            ViewBag.TotalCard = totalCard;
            ViewBag.TotalCash = totalCash;
            ViewBag.TotalRefund = totalRefund;
            ViewBag.TotalBonus = totalBonus;
            ViewBag.TotalCardAndCash = totalCard + totalCash;

            ViewData["SelectedReport"] = report;
            ViewData["AllReports"] = _dbContext.DailyReport
                .Where(r => r.CafeId == cafeId)
                .OrderByDescending(r => r.StartTime)
                .ToList();

            return View(dailyDatas);
        }

        [AuthorizeWithPermission("StartDay")]
        [HttpPost]
        public IActionResult StartDay()
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var cafeId = GetCurrentCafeId();
            if (cafeId == null) return Unauthorized();

            var activeReport = _dbContext.DailyReport
                .FirstOrDefault(r => r.CafeId == cafeId && r.EndTime == null);

            if (activeReport != null)
            {
                TempData["ErrorMessage"] = "Zaten açık bir gün mevcut.";
                return RedirectToAction("DailyAccount");
            }

            var report = new DailyReport
            {
                Id = Guid.NewGuid(),
                CafeId = cafeId.Value,
                StartTime = DateTime.Now,
                Active = true,
                RegistrationUser = userId.Value,
                RegistrationUserRole = userRole,
                RegistrationDate = DateTime.Now,
            };

            try
            {
                _dbContext.DailyReport.Add(report);
                _dbContext.SaveChanges();

                TempData["SuccessMessage"] = "Gün başarıyla açıldı.";
                return RedirectToAction("DailyAccount", new { rapor = report.Id });
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
            {
                // Guid çakışması durumunda tekrar dene
                return StartDay(); // Recursive çağrı (dikkatli kullanın)
            }
            catch (Exception ex)
            {
                // Diğer hatalar
                return Json(new { success = false, message = $"Gün açılamadı: {ex.Message}" });
            }
        }

        [AuthorizeWithPermission("EndDay")]
        [HttpPost]
        public IActionResult EndDay()
        {
            var cafeId = GetCurrentCafeId();
            if (cafeId == null)
            {
                TempData["ErrorMessage"] = "Kafe bilgisi alınamadı.";
                return RedirectToAction("DailyAccount");
            }

            var activeReport = _dbContext.DailyReport
                .FirstOrDefault(r => r.CafeId == cafeId && r.EndTime == null);

            if (activeReport == null)
            {
                TempData["ErrorMessage"] = "Bu gün zaten kapatılmış.";
                return RedirectToAction("DailyAccount");
            }

            var openTables = _dbContext.Order
                .Include(o => o.Table)
                .ThenInclude(t => t.Section)
                .Where(o => o.Active && o.Status != 5 && o.Table.Section.CafeId == cafeId && o.Table.Active)
                .Select(o => o.Table.Name)
                .Distinct()
                .ToList();

            if (openTables.Any())
            {
                TempData["ErrorMessage"] = $"Gün sonu alınamaz. Kapatılmamış masalar: {string.Join(", ", openTables)}";
                return RedirectToAction("DailyAccount");
            }

            activeReport.EndTime = DateTime.Now;

            try
            {
                _dbContext.SaveChanges();

                TempData["SuccessMessage"] = "Gün başarıyla kapatıldı.";
                return RedirectToAction("DailyAccount", new { rapor = activeReport.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gün kapatılamadı: {ex.Message}" });
            }
            
        }

    }
}
