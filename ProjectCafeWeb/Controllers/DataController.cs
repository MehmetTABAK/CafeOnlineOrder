using Azure.Core;
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
	public class DataController : BaseController
	{
		private readonly ProjectCafeDbContext _dbContext;

		public DataController(ProjectCafeDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		[AuthorizeWithPermission("ViewMenuCategory")]
		public IActionResult MenuCategory()
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return RedirectToAction("SignIn", "Login");

			List<MenuCategory> menuCategories = _dbContext.MenuCategory.Include(mc => mc.Cafe)
				.Where(mc => mc.Active && mc.Cafe.AdminId == adminId)
				.ToList();

			return View(menuCategories);
		}

		[AuthorizeWithPermission("AddMenuCategory")]
		[HttpPost]
		public async Task<IActionResult> AddMenuCategory([FromBody] MenuCategory menuCategory)
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
				menuCategory.CafeId = cafeId;
				menuCategory.RegistrationUser = adminId.Value;
				menuCategory.RegistrationDate = DateTime.Now;
				// Ürünü veritabanına kaydet
				_dbContext.MenuCategory.Add(menuCategory);
				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Üst menü başarıyla eklendi!" });
			}
			return Json(new { success = false, message = "Üst menü eklenemedi!" });
		}

		[AuthorizeWithPermission("UpdateMenuCategory")]
		[HttpPost]
		public async Task<IActionResult> UpdateMenuCategory([FromBody] MenuCategory menuCategory)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var existing = await _dbContext.MenuCategory
				.Include(mc => mc.Cafe)
				.FirstOrDefaultAsync(mc => mc.Id == menuCategory.Id && mc.Cafe.AdminId == adminId);

			if (existing == null)
				return Json(new { success = false, message = "Güncellenecek menü bulunamadı!" });

			if (ModelState.IsValid)
			{
				existing.CategoryName = menuCategory.CategoryName;
				existing.CategoryImage = menuCategory.CategoryImage;
				existing.CorrectionUser = adminId.Value;
				existing.CorrectionDate = DateTime.Now;

				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Üst menü başarıyla güncellendi!" });
			}
			return Json(new { success = false, message = "Üst menü güncellenemedi!" });
		}

		[AuthorizeWithPermission("DeleteMenuCategory")]
		[HttpPost]
		public IActionResult DeleteMenuCategory([FromBody] JsonElement request)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var menuCategoryId = request.GetProperty("id").GetInt32();

			var menuCategory = _dbContext.MenuCategory
				.Include(mc => mc.Cafe)
				.Include(mc => mc.SubMenuCategories)
					.ThenInclude(sc => sc.Products)
				.FirstOrDefault(mc => mc.Id == menuCategoryId && mc.Cafe.AdminId == adminId);

			if (menuCategory == null)
				return Json(new { success = false, message = "Menü bulunamadı veya yetkiniz yok." });

			menuCategory.Active = false;
			menuCategory.CorrectionUser = adminId.Value;
			menuCategory.CorrectionDate = DateTime.Now;

			// Alt menüler + ürünleri de pasifleştir
			foreach (var sub in menuCategory.SubMenuCategories ?? new List<SubMenuCategory>())
			{
				sub.Active = false;
				sub.CorrectionUser = adminId.Value;
				sub.CorrectionDate = DateTime.Now;

				foreach (var product in sub.Products ?? new List<Product>())
				{
					product.Active = false;
					product.CorrectionUser = adminId.Value;
					product.CorrectionDate = DateTime.Now;
				}
			}

			_dbContext.SaveChanges();

			return Json(new { success = true, message = "Üst menü ve bağlı alt menüler ile ürünler pasifleştirildi!" });
		}

		[AuthorizeWithPermission("ViewSubMenuCategory")]
		public IActionResult SubMenuCategory()
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return RedirectToAction("SignIn", "Login");

			var cafeIds = _dbContext.Cafe
				.Where(c => c.AdminId == adminId)
				.Select(c => c.Id)
				.ToList();

			var allCategories = _dbContext.MenuCategory
				.Where(mc => mc.Active && cafeIds.Contains(mc.CafeId))
				.ToList();

			List<SubMenuCategory> subMenuCategories = _dbContext.SubMenuCategory
				.Include(p => p.MenuCategory)
				.Where(p => p.Active && allCategories.Select(c => c.Id).Contains(p.MenuCategoryId))
				.ToList();

			ViewBag.AllMenuCategories = allCategories;

			return View(subMenuCategories);
		}

		[AuthorizeWithPermission("AddSubMenuCategory")]
		[HttpPost]
		public async Task<IActionResult> AddSubMenuCategory([FromBody] SubMenuCategory subMenuCategory)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var menuCategory = await _dbContext.MenuCategory
				.Include(mc => mc.Cafe)
				.FirstOrDefaultAsync(mc => mc.Id == subMenuCategory.MenuCategoryId && mc.Cafe.AdminId == adminId);

			if (menuCategory == null)
				return Json(new { success = false, message = "Yetkiniz olmayan bir menüye alt menü ekleyemezsiniz." });

			if (ModelState.IsValid)
			{
				subMenuCategory.RegistrationUser = adminId.Value;
				subMenuCategory.RegistrationDate = DateTime.Now;
				subMenuCategory.Active = true;
				// Ürünü veritabanına kaydet
				_dbContext.SubMenuCategory.Add(subMenuCategory);
				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Alt menü başarıyla eklendi!" });
			}

			return Json(new { success = false, message = "Alt menü eklenemedi!" });
		}

		[AuthorizeWithPermission("UpdateSubMenuCategory")]
		[HttpPost]
		public async Task<IActionResult> UpdateSubMenuCategory([FromBody] SubMenuCategory subMenuCategory)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var existing = await _dbContext.SubMenuCategory
				.Include(sm => sm.MenuCategory).ThenInclude(mc => mc.Cafe)
				.FirstOrDefaultAsync(sm => sm.Id == subMenuCategory.Id && sm.MenuCategory.Cafe.AdminId == adminId);

			if (existing == null)
				return Json(new { success = false, message = "Alt menü bulunamadı veya yetkiniz yok." });

			if (ModelState.IsValid)
			{
				existing.SubCategoryName = subMenuCategory.SubCategoryName;
				existing.SubCategoryImage = subMenuCategory.SubCategoryImage;
				existing.MenuCategoryId = subMenuCategory.MenuCategoryId;
				existing.CorrectionUser = adminId.Value;
				existing.CorrectionDate = DateTime.Now;

				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Alt menü başarıyla güncellendi!" });
			}
			return Json(new { success = false, message = "Alt menü güncellenemedi!" });
		}

		[AuthorizeWithPermission("DeleteSubMenuCategory")]
		[HttpPost]
		public IActionResult DeleteSubMenuCategory([FromBody] JsonElement request)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var subMenuCategoryId = request.GetProperty("id").GetInt32();

			var subMenuCategory = _dbContext.SubMenuCategory
				.Include(sc => sc.MenuCategory).ThenInclude(mc => mc.Cafe)
				.Include(sc => sc.Products)
				.FirstOrDefault(sc => sc.Id == subMenuCategoryId && sc.MenuCategory.Cafe.AdminId == adminId);

			if (subMenuCategory == null)
				return Json(new { success = false, message = "Alt menü bulunamadı veya yetkiniz yok." });

			subMenuCategory.Active = false;
			subMenuCategory.CorrectionUser = adminId.Value;
			subMenuCategory.CorrectionDate = DateTime.Now;

			// Alt ürünleri de pasifleştir
			foreach (var product in subMenuCategory.Products ?? new List<Product>())
			{
				product.Active = false;
				product.CorrectionUser = adminId.Value;
				product.CorrectionDate = DateTime.Now;
			}

			_dbContext.SaveChanges();

			return Json(new { success = true, message = "Alt menü ve ürünler pasifleştirildi!" });
		}

		[AuthorizeWithPermission("ViewProduct")]
		public IActionResult Product()
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null) return RedirectToAction("SignIn", "Login");

			var cafeIds = _dbContext.Cafe
				.Where(c => c.AdminId == adminId)
				.Select(c => c.Id)
				.ToList();

			var allCategories = _dbContext.MenuCategory
				.Where(mc => mc.Active && cafeIds.Contains(mc.CafeId))
				.ToList();

			var menuCategoryIds = allCategories.Select(c => c.Id).ToList();

			var allSubCategories = _dbContext.SubMenuCategory
				.Include(sc => sc.MenuCategory)
				.Where(sc => sc.Active && menuCategoryIds.Contains(sc.MenuCategoryId))
				.ToList();

			List<Product> products = _dbContext.Product
				.Include(p => p.SubMenuCategory).ThenInclude(sm => sm.MenuCategory).ThenInclude(mc => mc.Cafe)
				.Where(p => p.Active && cafeIds.Contains(p.SubMenuCategory.MenuCategory.CafeId))
				.ToList();

			ViewBag.AllMenuCategories = allCategories;

			ViewBag.AllSubMenuCategories = allSubCategories;

			return View(products);
		}

		[AuthorizeWithPermission("AddProduct")]
		[HttpPost]
		public async Task<IActionResult> AddProduct([FromBody] Product product)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var subCategory = await _dbContext.SubMenuCategory
				.Include(sc => sc.MenuCategory).ThenInclude(mc => mc.Cafe)
				.FirstOrDefaultAsync(sc => sc.Id == product.SubMenuCategoryId && sc.MenuCategory.Cafe.AdminId == adminId);

			if (subCategory == null)
				return Json(new { success = false, message = "Yetkisiz alt menü, ürün eklenemedi!" });

			if (ModelState.IsValid)
			{
				product.RegistrationUser = adminId.Value;
				product.RegistrationDate = DateTime.Now;
				product.Active = true;

				_dbContext.Product.Add(product);
				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Ürün başarıyla eklendi!" });
			}

			return Json(new { success = false, message = "Ürün eklenemedi!" });
		}

		[AuthorizeWithPermission("UpdateProduct")]
		[HttpPost]
		public async Task<IActionResult> UpdateProduct([FromBody] Product product)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var existing = await _dbContext.Product
				.Include(p => p.SubMenuCategory).ThenInclude(sc => sc.MenuCategory).ThenInclude(mc => mc.Cafe)
				.FirstOrDefaultAsync(p => p.Id == product.Id && p.SubMenuCategory.MenuCategory.Cafe.AdminId == adminId);

			if (existing == null)
				return Json(new { success = false, message = "Ürün bulunamadı veya yetkiniz yok." });

			if (ModelState.IsValid)
			{
				existing.Name = product.Name;
				existing.Price = product.Price;
				existing.Image = product.Image;
				existing.IsThereDiscount = product.IsThereDiscount;
				existing.DiscountRate = product.DiscountRate;
				existing.MenuCategoryId = product.MenuCategoryId;
				existing.SubMenuCategoryId = product.SubMenuCategoryId;
				existing.CorrectionUser = adminId.Value;
				existing.CorrectionDate = DateTime.Now;

				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Ürün başarıyla güncellendi!" });
			}

			return Json(new { success = false, message = "Ürün güncellenemedi!" });
		}

		[AuthorizeWithPermission("UpdateStockProduct")]
		[HttpPost]
		public IActionResult UpdateStock([FromBody] JsonElement request)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var productId = request.GetProperty("id").GetInt32();
			var stockStatus = request.GetProperty("stock").GetBoolean();

			var product = _dbContext.Product
				.Include(p => p.SubMenuCategory).ThenInclude(sc => sc.MenuCategory).ThenInclude(mc => mc.Cafe)
				.FirstOrDefault(p => p.Id == productId && p.SubMenuCategory.MenuCategory.Cafe.AdminId == adminId);

			if (product == null)
				return Json(new { success = false, message = "Ürün bulunamadı veya yetkiniz yok." });

			product.Stock = stockStatus;
			product.CorrectionUser = adminId.Value;
			product.CorrectionDate = DateTime.Now;

			_dbContext.SaveChanges();
			return Json(new { success = true });
		}

		[AuthorizeWithPermission("DeleteProduct")]
		[HttpPost]
		public IActionResult DeleteProduct([FromBody] JsonElement request)
		{
			var adminId = GetCurrentAdminId();
			if (adminId == null)
				return Unauthorized();

			var productId = request.GetProperty("id").GetInt32();

			var product = _dbContext.Product
				.Include(p => p.SubMenuCategory).ThenInclude(sc => sc.MenuCategory).ThenInclude(mc => mc.Cafe)
				.FirstOrDefault(p => p.Id == productId && p.SubMenuCategory.MenuCategory.Cafe.AdminId == adminId);

			if (product == null)
				return Json(new { success = false, message = "Ürün bulunamadı veya yetkiniz yok." });

			product.Active = false;
			product.CorrectionUser = adminId.Value;
			product.CorrectionDate = DateTime.Now;

			_dbContext.SaveChanges();
			return Json(new { success = true, message = "Ürün başarıyla silindi!" });
		}
	}
}
