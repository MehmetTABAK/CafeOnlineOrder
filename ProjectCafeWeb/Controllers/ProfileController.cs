using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ProjectCafeDataAccess;
using ProjectCafeEntities;
using ProjectCafeWeb.Attributes;
using System.Text.Json;

namespace ProjectCafeWeb.Controllers
{
	[Authorize]
	public class ProfileController : BaseController
	{
		public ProfileController(ProjectCafeDbContext dbContext) : base(dbContext)
		{
		}

		public IActionResult MyProfile()
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var admin = _dbContext.Admin
				.Where(mc => mc.Active && mc.Id == userId)
				.ToList();

			return View(admin);
		}

		[HttpPost]
		public async Task<IActionResult> UpdateAdmin([FromBody] Admin admin)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			if (userId == null)
				return Unauthorized();

			var existing = await _dbContext.Admin
				.FirstOrDefaultAsync(mc => mc.Id == admin.Id);

			if (existing == null)
				return Json(new { success = false, message = "Güncellenecek admin bulunamadı!" });

            ModelState.Remove(nameof(admin.RegistrationUserRole));

            if (ModelState.IsValid)
			{
				existing.Firstname = admin.Firstname;
				existing.Lastname = admin.Lastname;
				existing.Image = admin.Image;
				existing.Email = admin.Email;
				existing.Password = admin.Password;
				existing.CorrectionUser = userId.Value;
				existing.CorrectionUserRole = userRole;
				existing.CorrectionDate = DateTime.Now;

				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Admin başarıyla güncellendi!" });
			}
			return Json(new { success = false, message = "Admin güncellenemedi!" });
		}

		[AuthorizeWithPermission("ViewWorker")]
		public IActionResult WorkerProfile()
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			List<Worker> workers = _dbContext.Worker.Include(mc => mc.Cafe)
				.Where(mc => mc.Active && mc.Cafe.Id == cafeId)
				.ToList();

			return View(workers);
		}

		[AuthorizeWithPermission("AddWorker")]
		[HttpPost]
		public async Task<IActionResult> AddWorker([FromBody] Worker worker)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var cafeIds = _dbContext.Cafe
				.Where(c => c.Id == cafeId)
				.Select(c => c.Id)
				.FirstOrDefault();

			if (cafeIds == 0)
				return Json(new { success = false, message = "Kafe bulunamadı!" });

            ModelState.Remove(nameof(worker.RegistrationUserRole));

            if (ModelState.IsValid)
			{
				worker.CafeId = cafeIds;
				worker.RegistrationUser = userId.Value;
				worker.RegistrationUserRole = userRole;
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
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var existing = await _dbContext.Worker
				.Include(mc => mc.Cafe)
				.FirstOrDefaultAsync(mc => mc.Id == worker.Id && mc.Cafe.Id == cafeId);

			if (existing == null)
				return Json(new { success = false, message = "Güncellenecek çalışan bulunamadı!" });

            ModelState.Remove(nameof(worker.RegistrationUserRole));

            if (ModelState.IsValid)
			{
				existing.Firstname = worker.Firstname;
				existing.Lastname = worker.Lastname;
				existing.Image = worker.Image;
				existing.Email = worker.Email;
				existing.Password = worker.Password;
				existing.RolePermissions = worker.RolePermissions ?? "[]";
				existing.CorrectionUser = userId.Value;
				existing.CorrectionUserRole = userRole;
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
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var workerId = request.GetProperty("id").GetInt32();

			var worker = _dbContext.Worker
				.Include(mc => mc.Cafe)
				.FirstOrDefault(mc => mc.Id == workerId && mc.Cafe.Id == cafeId);

			if (worker == null)
				return Json(new { success = false, message = "Çalışan bulunamadı veya yetkiniz yok." });

			worker.Active = false;
			worker.CorrectionUser = userId.Value;
			worker.CorrectionUserRole = userRole;
			worker.CorrectionDate = DateTime.Now;
			_dbContext.SaveChanges();

			return Json(new { success = true, message = "Çalışan silindi!" });
		}

		[AuthorizeWithPermission("CafeProfile")]
		public IActionResult CafeProfile()
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			List<Cafe> cafes = _dbContext.Cafe
				.Include(c => c.Sections)
				.ThenInclude(s => s.Tables)
				.Where(c => c.Active && c.Id == cafeId)
				.ToList();

			return View(cafes);
		}

		[AuthorizeWithPermission("UpdateCafe")]
		[HttpPost]
		public async Task<IActionResult> UpdateCafe([FromBody] Cafe cafe)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var existingCafe = await _dbContext.Cafe
				.FirstOrDefaultAsync(c => c.Id == cafe.Id && c.Id == cafeId);

			if (existingCafe == null)
				return Json(new { success = false, message = "Kafe bulunamadı veya yetkiniz yok." });

            ModelState.Remove(nameof(cafe.RegistrationUserRole));

            if (ModelState.IsValid)
			{
				existingCafe.Name = cafe.Name;
				existingCafe.Image = cafe.Image;
				existingCafe.Location = cafe.Location;
				existingCafe.CorrectionUser = userId.Value;
				existingCafe.CorrectionUserRole = userRole;
				existingCafe.CorrectionDate = DateTime.Now;

				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Kafe bilgileri güncellendi!" });
			}
			return Json(new { success = false, message = "Kafe bilgileri güncellenemedi!" });
		}

		[AuthorizeWithPermission("AddSection")]
		[HttpPost]
		public async Task<IActionResult> AddSection([FromBody] ProjectCafeEntities.Section section)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var cafeIds = _dbContext.Cafe
				.Where(c => c.Id == cafeId)
				.Select(c => c.Id)
				.FirstOrDefault();

			if (cafeIds == 0)
				return Json(new { success = false, message = "Bölüm bulunamadı!" });

            ModelState.Remove(nameof(section.RegistrationUserRole));

            if (ModelState.IsValid)
			{
				section.CafeId = cafeIds;
				section.RegistrationUser = userId.Value;
				section.RegistrationUserRole = userRole;
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
		public async Task<IActionResult> UpdateSection([FromBody] ProjectCafeEntities.Section section)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var existing = await _dbContext.Section
				.Include(s => s.Cafe)
				.FirstOrDefaultAsync(s => s.Id == section.Id && s.Cafe.Id == cafeId);

			if (existing == null)
				return Json(new { success = false, message = "Bölüm bulunamadı veya yetkiniz yok." });

            ModelState.Remove(nameof(section.RegistrationUserRole));

            if (ModelState.IsValid)
			{
				existing.Name = section.Name;
				existing.Image = section.Image;
				existing.CorrectionUser = userId.Value;
				existing.CorrectionUserRole = userRole;
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
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			int sectionId = request.GetProperty("id").GetInt32();

			var section = _dbContext.Section
				.Include(s => s.Cafe)
				.Include(sc => sc.Tables)
				.FirstOrDefault(s => s.Id == sectionId && s.Cafe.Id == cafeId);

			if (section == null)
				return Json(new { success = false, message = "Bölüm bulunamadı veya yetkiniz yok." });

			section.Active = false;
			section.CorrectionUser = userId.Value;
			section.CorrectionUserRole = userRole;
			section.CorrectionDate = DateTime.Now;

			foreach (var table in (section.Tables ?? new List<ProjectCafeEntities.Table>()))
			{
				table.Active = false;
				table.CorrectionUser = userId.Value;
				table.CorrectionUserRole = userRole;
				table.CorrectionDate = DateTime.Now;
			}

			_dbContext.SaveChanges();

			return Json(new { success = true, message = "Bölüm ve bölüme ait masalar silindi!" });
		}

		[AuthorizeWithPermission("AddTable")]
		[HttpPost]
		public async Task<IActionResult> AddTable([FromBody] ProjectCafeEntities.Table table)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var section = await _dbContext.Section
				.Include(s => s.Cafe)
				.FirstOrDefaultAsync(s => s.Id == table.SectionId && s.Cafe.Id == cafeId);

			if (section == null)
				return Json(new { success = false, message = "Yetkiniz olmayan bir bölüme masa ekleyemezsiniz." });

            ModelState.Remove(nameof(table.RegistrationUserRole));

            if (ModelState.IsValid)
			{
				table.RegistrationUser = userId.Value;
				table.RegistrationUserRole = userRole;
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
		public async Task<IActionResult> UpdateTable([FromBody] ProjectCafeEntities.Table table)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var existing = await _dbContext.Table
				.Include(t => t.Section)
				.ThenInclude(s => s.Cafe)
				.FirstOrDefaultAsync(t => t.Id == table.Id && t.Section.Cafe.Id == cafeId);

			if (existing == null)
				return Json(new { success = false, message = "Masa bulunamadı veya yetkiniz yok." });

            ModelState.Remove(nameof(table.RegistrationUserRole));

            if (ModelState.IsValid)
			{
				existing.Name = table.Name;
				existing.CorrectionUser = userId.Value;
				existing.CorrectionUserRole = userRole;
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
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			int tableId = request.GetProperty("id").GetInt32();

			var table = _dbContext.Table
				.Include(t => t.Section)
				.ThenInclude(s => s.Cafe)
				.FirstOrDefault(t => t.Id == tableId && t.Section.Cafe.Id == cafeId);

			if (table == null)
				return Json(new { success = false, message = "Masa bulunamadı veya yetkiniz yok." });

			table.Active = false;
			table.CorrectionUser = userId.Value;
			table.CorrectionUserRole = userRole;
			table.CorrectionDate = DateTime.Now;

			_dbContext.SaveChanges();

			return Json(new { success = true, message = "Masa silindi!" });
		}
	}
}
