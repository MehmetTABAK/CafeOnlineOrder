using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectCafeDataAccess;
using ProjectCafeEntities;
using ProjectCafeWeb.Attributes;
using System.Text.Json;

namespace ProjectCafeWeb.Controllers
{
	[Authorize]
	public class ProfileController : BaseController
	{
		private readonly ProjectCafeDbContext _dbContext;

		public ProfileController(ProjectCafeDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		public IActionResult MyProfile()
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return RedirectToAction("SignIn", "Login");

			var admin = _dbContext.Admin
				.Where(mc => mc.Active && mc.Id == adminId)
				.ToList();

			return View(admin);
		}

		[HttpPost]
		public async Task<IActionResult> UpdateAdmin([FromBody] Admin admin)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var existing = await _dbContext.Admin
				.FirstOrDefaultAsync(mc => mc.Id == admin.Id);

			if (existing == null)
				return Json(new { success = false, message = "Güncellenecek admin bulunamadı!" });

			if (ModelState.IsValid)
			{
				existing.Firstname = admin.Firstname;
				existing.Lastname = admin.Lastname;
				existing.Image = admin.Image;
				existing.Email = admin.Email;
				existing.Password = admin.Password;
				existing.CorrectionUser = adminId.Value;
				existing.CorrectionDate = DateTime.Now;

				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Admin başarıyla güncellendi!" });
			}
			return Json(new { success = false, message = "Admin güncellenemedi!" });
		}

		[AuthorizeWithPermission("ViewWorker")]
		public IActionResult WorkerProfile()
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return RedirectToAction("SignIn", "Login");

			List<Worker> workers = _dbContext.Worker.Include(mc => mc.Cafe)
				.Where(mc => mc.Active && mc.Cafe.AdminId == adminId)
				.ToList();

			return View(workers);
		}

		[AuthorizeWithPermission("AddWorker")]
		[HttpPost]
		public async Task<IActionResult> AddWorker([FromBody] Worker worker)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var cafeId = _dbContext.Cafe
				.Where(c => c.AdminId == adminId)
				.Select(c => c.Id)
				.FirstOrDefault();

			if (cafeId == 0)
				return Json(new { success = false, message = "Kafe bulunamadı!" });

			if (ModelState.IsValid)
			{
				worker.CafeId = cafeId;
				worker.RegistrationUser = adminId.Value;
				worker.RegistrationDate = DateTime.Now;

				if (worker.RolePermissions == null)
					worker.RolePermissions = "[]";

				_dbContext.Worker.Add(worker);
				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Çalışan başarıyla eklendi!" });
			}

			return Json(new { success = false, message = "Çalışan eklenemedi!" });
		}

		[AuthorizeWithPermission("UpdateWorker")]
		[HttpPost]
		public async Task<IActionResult> UpdateWorker([FromBody] Worker worker)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var existing = await _dbContext.Worker
				.Include(mc => mc.Cafe)
				.FirstOrDefaultAsync(mc => mc.Id == worker.Id && mc.Cafe.AdminId == adminId);

			if (existing == null)
				return Json(new { success = false, message = "Güncellenecek çalışan bulunamadı!" });

			if (ModelState.IsValid)
			{
				existing.Firstname = worker.Firstname;
				existing.Lastname = worker.Lastname;
				existing.Image = worker.Image;
				existing.Email = worker.Email;
				existing.Password = worker.Password;
				existing.RolePermissions = worker.RolePermissions ?? "[]";
				existing.CorrectionUser = adminId.Value;
				existing.CorrectionDate = DateTime.Now;

				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Çalışan başarıyla güncellendi!" });
			}
			return Json(new { success = false, message = "Çalışan güncellenemedi!" });
		}

		[AuthorizeWithPermission("DeleteWorker")]
		[HttpPost]
		public IActionResult DeleteWorker([FromBody] JsonElement request)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var workerId = request.GetProperty("id").GetInt32();

			var worker = _dbContext.Worker
				.Include(mc => mc.Cafe)
				.FirstOrDefault(mc => mc.Id == workerId && mc.Cafe.AdminId == adminId);

			if (worker == null)
				return Json(new { success = false, message = "Çalışan bulunamadı veya yetkiniz yok." });

			worker.Active = false;
			worker.CorrectionUser = adminId.Value;
			worker.CorrectionDate = DateTime.Now;
			_dbContext.SaveChanges();

			return Json(new { success = true, message = "Çalışan silindi!" });
		}

		[AuthorizeWithPermission("CafeProfile")]
		public IActionResult CafeProfile()
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			List<Cafe> cafes = _dbContext.Cafe
				.Include(c => c.Sections)
				.ThenInclude(s => s.Tables)
				.Where(c => c.Active && c.AdminId == adminId)
				.ToList();

			return View(cafes);
		}

		[AuthorizeWithPermission("UpdateCafe")]
		[HttpPost]
		public async Task<IActionResult> UpdateCafe([FromBody] Cafe cafe)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var existingCafe = await _dbContext.Cafe
				.FirstOrDefaultAsync(c => c.Id == cafe.Id && c.AdminId == adminId);

			if (existingCafe == null)
				return Json(new { success = false, message = "Kafe bulunamadı veya yetkiniz yok." });

			if (ModelState.IsValid)
			{
				existingCafe.Name = cafe.Name;
				existingCafe.Image = cafe.Image;
				existingCafe.Location = cafe.Location;
				existingCafe.CorrectionUser = adminId.Value;
				existingCafe.CorrectionDate = DateTime.Now;

				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Kafe bilgileri güncellendi!" });
			}
			return Json(new { success = false, message = "Kafe bilgileri güncellenemedi!" });
		}

		[AuthorizeWithPermission("AddSection")]
		[HttpPost]
		public async Task<IActionResult> AddSection([FromBody] Section section)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var cafeId = _dbContext.Cafe
				.Where(c => c.AdminId == adminId)
				.Select(c => c.Id)
				.FirstOrDefault();

			if (cafeId == 0)
				return Json(new { success = false, message = "Bölüm bulunamadı!" });

			if (ModelState.IsValid)
			{
				section.CafeId = cafeId;
				section.RegistrationUser = adminId.Value;
				section.RegistrationDate = DateTime.Now;
				section.Active = true;

				_dbContext.Section.Add(section);
				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Bölüm başarıyla eklendi!" });
			}
			return Json(new { success = false, message = "Bölüm eklenemedi!" });
		}

		[AuthorizeWithPermission("UpdateSection")]
		[HttpPost]
		public async Task<IActionResult> UpdateSection([FromBody] Section section)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var existing = await _dbContext.Section
				.Include(s => s.Cafe)
				.FirstOrDefaultAsync(s => s.Id == section.Id && s.Cafe.AdminId == adminId);

			if (existing == null)
				return Json(new { success = false, message = "Bölüm bulunamadı veya yetkiniz yok." });

			if (ModelState.IsValid)
			{
				existing.Name = section.Name;
				existing.Image = section.Image;
				existing.CorrectionUser = adminId.Value;
				existing.CorrectionDate = DateTime.Now;

				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Bölüm başarıyla güncellendi!" });
			}
			return Json(new { success = false, message = "Bölüm güncellenemedi!" });
		}

		[AuthorizeWithPermission("DeleteSection")]
		[HttpPost]
		public IActionResult DeleteSection([FromBody] JsonElement request)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			int sectionId = request.GetProperty("id").GetInt32();

			var section = _dbContext.Section
				.Include(s => s.Cafe)
				.Include(sc => sc.Tables)
				.FirstOrDefault(s => s.Id == sectionId && s.Cafe.AdminId == adminId);

			if (section == null)
				return Json(new { success = false, message = "Bölüm bulunamadı veya yetkiniz yok." });

			section.Active = false;
			section.CorrectionUser = adminId.Value;
			section.CorrectionDate = DateTime.Now;

			foreach (var table in section.Tables ?? new List<Table>())
			{
				table.Active = false;
				table.CorrectionUser = adminId.Value;
				table.CorrectionDate = DateTime.Now;
			}

			_dbContext.SaveChanges();

			return Json(new { success = true, message = "Bölüm ve bölüme ait masalar silindi!" });
		}

		[AuthorizeWithPermission("AddTable")]
		[HttpPost]
		public async Task<IActionResult> AddTable([FromBody] Table table)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var section = await _dbContext.Section
				.Include(s => s.Cafe)
				.FirstOrDefaultAsync(s => s.Id == table.SectionId && s.Cafe.AdminId == adminId);

			if (section == null)
				return Json(new { success = false, message = "Yetkiniz olmayan bir bölüme masa ekleyemezsiniz." });

			if (ModelState.IsValid)
			{
				table.RegistrationUser = adminId.Value;
				table.RegistrationDate = DateTime.Now;
				table.Active = true;

				_dbContext.Table.Add(table);
				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Masa başarıyla eklendi!" });
			}

			return Json(new { success = false, message = "Masa eklenemedi!" });
		}

		[AuthorizeWithPermission("UpdateTable")]
		[HttpPost]
		public async Task<IActionResult> UpdateTable([FromBody] Table table)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var existing = await _dbContext.Table
				.Include(t => t.Section)
				.ThenInclude(s => s.Cafe)
				.FirstOrDefaultAsync(t => t.Id == table.Id && t.Section.Cafe.AdminId == adminId);

			if (existing == null)
				return Json(new { success = false, message = "Masa bulunamadı veya yetkiniz yok." });

			if (ModelState.IsValid)
			{
				existing.Name = table.Name;
				existing.CorrectionUser = adminId.Value;
				existing.CorrectionDate = DateTime.Now;

				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Masa güncellendi!" });
			}
			return Json(new { success = false, message = "Masa güncellenemedi!" });
		}

		[AuthorizeWithPermission("DeleteTable")]
		[HttpPost]
		public IActionResult DeleteTable([FromBody] JsonElement request)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			int tableId = request.GetProperty("id").GetInt32();

			var table = _dbContext.Table
				.Include(t => t.Section)
				.ThenInclude(s => s.Cafe)
				.FirstOrDefault(t => t.Id == tableId && t.Section.Cafe.AdminId == adminId);

			if (table == null)
				return Json(new { success = false, message = "Masa bulunamadı veya yetkiniz yok." });

			table.Active = false;
			table.CorrectionUser = adminId.Value;
			table.CorrectionDate = DateTime.Now;

			_dbContext.SaveChanges();

			return Json(new { success = true, message = "Masa silindi!" });
		}
	}
}
