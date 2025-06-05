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
		public DataController(ProjectCafeDbContext dbContext) : base(dbContext)
		{
		}

		[AuthorizeWithPermission("ViewMenuCategory")]
        [Route("menu-kategori")]
        public IActionResult MenuCategory()
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			List<MenuCategory> menuCategories = _dbContext.MenuCategory.Include(mc => mc.Cafe)
				.Where(mc => mc.Active && mc.Cafe.Id == cafeId)
				.ToList();

			return View(menuCategories);
		}

		[AuthorizeWithPermission("AddMenuCategory")]
		[HttpPost]
		public async Task<IActionResult> AddMenuCategory([FromBody] MenuCategory menuCategory)
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

			if (cafeIds == null)
				return Json(new { success = false, message = "Kafe bulunamadı!" });

            ModelState.Remove(nameof(menuCategory.RegistrationUserRole));

            if (ModelState.IsValid)
			{
                menuCategory.Id = Guid.NewGuid();
                menuCategory.CafeId = cafeIds;
				menuCategory.RegistrationUser = userId.Value;
				menuCategory.RegistrationUserRole = userRole;
				menuCategory.RegistrationDate = DateTime.Now;

                try
                {
                    // Ürünü veritabanına kaydet
                    _dbContext.MenuCategory.Add(menuCategory);
                    await _dbContext.SaveChangesAsync();
                    return Json(new { success = true, message = "Üst menü başarıyla eklendi!" });
                }
                catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
                {
                    // Guid çakışması durumunda tekrar dene
                    return await AddMenuCategory(menuCategory); // Recursive çağrı (dikkatli kullanın)
                }
                catch (Exception ex)
                {
                    // Diğer hatalar
                    return Json(new { success = false, message = $"Üst menü eklenemedi: {ex.Message}" });
                }
			}
			return Json(new { success = false, message = "Üst menü eklenemedi!" });
		}

		[AuthorizeWithPermission("UpdateMenuCategory")]
		[HttpPost]
		public async Task<IActionResult> UpdateMenuCategory([FromBody] MenuCategory menuCategory)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var existing = await _dbContext.MenuCategory
				.Include(mc => mc.Cafe)
				.FirstOrDefaultAsync(mc => mc.Id == menuCategory.Id && mc.Cafe.Id == cafeId);

			if (existing == null)
				return Json(new { success = false, message = "Güncellenecek menü bulunamadı!" });

            ModelState.Remove(nameof(menuCategory.RegistrationUserRole));

            if (ModelState.IsValid)
			{
				existing.CategoryName = menuCategory.CategoryName;
				existing.CategoryImage = menuCategory.CategoryImage;
				existing.CorrectionUser = userId.Value;
				existing.CorrectionUserRole = userRole;
				existing.CorrectionDate = DateTime.Now;

				try
				{
                    await _dbContext.SaveChangesAsync();
                    return Json(new { success = true, message = "Üst menü başarıyla güncellendi!" });
                }
				catch (Exception ex)
				{
                    return Json(new { success = false, message = $"Üst menü güncellenemedi: {ex.Message}" });
                }
			}
			return Json(new { success = false, message = "Üst menü güncellenemedi!" });
		}

		[AuthorizeWithPermission("DeleteMenuCategory")]
		[HttpPost]
		public IActionResult DeleteMenuCategory([FromBody] JsonElement request)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var menuCategoryId = Guid.Parse(request.GetProperty("id").GetString());

            var menuCategory = _dbContext.MenuCategory
				.Include(mc => mc.Cafe)
				.Include(mc => mc.SubMenuCategories)
					.ThenInclude(sc => sc.Products)
				.FirstOrDefault(mc => mc.Id == menuCategoryId && mc.Cafe.Id == cafeId);

			if (menuCategory == null)
				return Json(new { success = false, message = "Menü bulunamadı veya yetkiniz yok." });

			menuCategory.Active = false;
			menuCategory.CorrectionUser = userId.Value;
			menuCategory.CorrectionUserRole = userRole;
			menuCategory.CorrectionDate = DateTime.Now;

			// Alt menüler + ürünleri de pasifleştir
			foreach (var sub in menuCategory.SubMenuCategories ?? new List<SubMenuCategory>())
			{
				sub.Active = false;
				sub.CorrectionUser = userId.Value;
				sub.CorrectionUserRole = userRole;
				sub.CorrectionDate = DateTime.Now;

				foreach (var product in sub.Products ?? new List<Product>())
				{
					product.Active = false;
					product.CorrectionUser = userId.Value;
					product.CorrectionUserRole = userRole;
					product.CorrectionDate = DateTime.Now;
				}
			}

			try
			{
                _dbContext.SaveChanges();
                return Json(new { success = true, message = "Üst menü ve bağlı alt menüler ile ürünler pasifleştirildi!" });
            }
			catch (Exception ex)
			{
                return Json(new { success = false, message = $"Üst menü ve bağlı alt menüler ile ürünler pasifleştirilemedi: {ex.Message}" });
            }
		}

		[AuthorizeWithPermission("ViewSubMenuCategory")]
        [Route("alt-menu-kategori")]
        public IActionResult SubMenuCategory()
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var cafeIds = _dbContext.Cafe
				.Where(c => c.Id == cafeId)
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
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var menuCategory = await _dbContext.MenuCategory
				.Include(mc => mc.Cafe)
				.FirstOrDefaultAsync(mc => mc.Id == subMenuCategory.MenuCategoryId && mc.Cafe.Id == cafeId);

			if (menuCategory == null)
				return Json(new { success = false, message = "Yetkiniz olmayan bir menüye alt menü ekleyemezsiniz." });

            ModelState.Remove(nameof(subMenuCategory.RegistrationUserRole));

            if (ModelState.IsValid)
			{
				subMenuCategory.Id = Guid.NewGuid();
				subMenuCategory.RegistrationUser = userId.Value;
				subMenuCategory.RegistrationUserRole = userRole;
				subMenuCategory.RegistrationDate = DateTime.Now;
				subMenuCategory.Active = true;

                try
                {
                    // Ürünü veritabanına kaydet
                    _dbContext.SubMenuCategory.Add(subMenuCategory);
                    await _dbContext.SaveChangesAsync();
                    return Json(new { success = true, message = "Alt menü başarıyla eklendi!" });
                }
                catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
                {
                    // Guid çakışması durumunda tekrar dene
                    return await AddSubMenuCategory(subMenuCategory); // Recursive çağrı (dikkatli kullanın)
                }
                catch (Exception ex)
                {
                    // Diğer hatalar
                    return Json(new { success = false, message = $"Alt menü eklenemedi: {ex.Message}" });
                }
			}

			return Json(new { success = false, message = "Alt menü eklenemedi!" });
		}

		[AuthorizeWithPermission("UpdateSubMenuCategory")]
		[HttpPost]
		public async Task<IActionResult> UpdateSubMenuCategory([FromBody] SubMenuCategory subMenuCategory)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var existing = await _dbContext.SubMenuCategory
				.Include(sm => sm.MenuCategory).ThenInclude(mc => mc.Cafe)
				.FirstOrDefaultAsync(sm => sm.Id == subMenuCategory.Id && sm.MenuCategory.Cafe.Id == userId);

			if (existing == null)
				return Json(new { success = false, message = "Alt menü bulunamadı veya yetkiniz yok." });

            ModelState.Remove(nameof(subMenuCategory.RegistrationUserRole));

            if (ModelState.IsValid)
			{
				existing.SubCategoryName = subMenuCategory.SubCategoryName;
				existing.SubCategoryImage = subMenuCategory.SubCategoryImage;
				existing.MenuCategoryId = subMenuCategory.MenuCategoryId;
				existing.CorrectionUser = userId.Value;
				existing.CorrectionUserRole = userRole;
				existing.CorrectionDate = DateTime.Now;

				try
				{
                    await _dbContext.SaveChangesAsync();
                    return Json(new { success = true, message = "Alt menü başarıyla güncellendi!" });
                }
				catch (Exception ex)
				{
                    return Json(new { success = false, message = $"Alt menü güncellenemedi: {ex.Message}" });
                }
			}
			return Json(new { success = false, message = "Alt menü güncellenemedi!" });
		}

		[AuthorizeWithPermission("DeleteSubMenuCategory")]
		[HttpPost]
		public IActionResult DeleteSubMenuCategory([FromBody] JsonElement request)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var subMenuCategoryId = Guid.Parse(request.GetProperty("id").GetString());

            var subMenuCategory = _dbContext.SubMenuCategory
				.Include(sc => sc.MenuCategory).ThenInclude(mc => mc.Cafe)
				.Include(sc => sc.Products)
				.FirstOrDefault(sc => sc.Id == subMenuCategoryId && sc.MenuCategory.Cafe.Id == userId);

			if (subMenuCategory == null)
				return Json(new { success = false, message = "Alt menü bulunamadı veya yetkiniz yok." });

			subMenuCategory.Active = false;
			subMenuCategory.CorrectionUser = userId.Value;
			subMenuCategory.CorrectionUserRole = userRole;
			subMenuCategory.CorrectionDate = DateTime.Now;

			// Alt ürünleri de pasifleştir
			foreach (var product in subMenuCategory.Products ?? new List<Product>())
			{
				product.Active = false;
				product.CorrectionUser = userId.Value;
				product.CorrectionUserRole = userRole;
				product.CorrectionDate = DateTime.Now;
			}

			try
			{
                _dbContext.SaveChanges();
                return Json(new { success = true, message = "Alt menü ve ürünler pasifleştirildi!" });
            }
			catch (Exception ex)
			{
                return Json(new { success = false, message = $"Alt menü ve ürünler pasifleştirilemedi: {ex.Message}" });
            }
		}

		[AuthorizeWithPermission("ViewProduct")]
        [Route("urunler")]
        public IActionResult Product()
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var cafeIds = _dbContext.Cafe
				.Where(c => c.Id == cafeId)
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
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var subCategory = await _dbContext.SubMenuCategory
				.Include(sc => sc.MenuCategory).ThenInclude(mc => mc.Cafe)
				.FirstOrDefaultAsync(sc => sc.Id == product.SubMenuCategoryId && sc.MenuCategory.Cafe.Id == cafeId);

			if (subCategory == null)
				return Json(new { success = false, message = "Yetkisiz alt menü, ürün eklenemedi!" });

            ModelState.Remove(nameof(product.RegistrationUserRole));

            if (ModelState.IsValid)
			{
				product.Id = Guid.NewGuid();
				product.RegistrationUser = userId.Value;
				product.RegistrationUserRole = userRole;
				product.RegistrationDate = DateTime.Now;
				product.Active = true;

				try
				{
                    _dbContext.Product.Add(product);
                    await _dbContext.SaveChangesAsync();
                    return Json(new { success = true, message = "Ürün başarıyla eklendi!" });
                }
                catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
                {
                    // Guid çakışması durumunda tekrar dene
                    return await AddProduct(product); // Recursive çağrı (dikkatli kullanın)
                }
                catch (Exception ex)
                {
                    // Diğer hatalar
                    return Json(new { success = false, message = $"Ürün eklenemedi: {ex.Message}" });
                }
            }

			return Json(new { success = false, message = "Ürün eklenemedi!" });
		}

		[AuthorizeWithPermission("UpdateProduct")]
		[HttpPost]
		public async Task<IActionResult> UpdateProduct([FromBody] Product product)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var existing = await _dbContext.Product
				.Include(p => p.SubMenuCategory).ThenInclude(sc => sc.MenuCategory).ThenInclude(mc => mc.Cafe)
				.FirstOrDefaultAsync(p => p.Id == product.Id && p.SubMenuCategory.MenuCategory.Cafe.Id == cafeId);

			if (existing == null)
				return Json(new { success = false, message = "Ürün bulunamadı veya yetkiniz yok." });

            ModelState.Remove(nameof(product.RegistrationUserRole));

            if (ModelState.IsValid)
			{
				existing.Name = product.Name;
				existing.Price = product.Price;
				existing.StockCount = product.StockCount;
				existing.Image = product.Image;
				existing.IsThereDiscount = product.IsThereDiscount;
				existing.DiscountRate = product.DiscountRate;
				existing.MenuCategoryId = product.MenuCategoryId;
				existing.SubMenuCategoryId = product.SubMenuCategoryId;
				existing.CorrectionUser = userId.Value;
				existing.CorrectionUserRole = userRole;
				existing.CorrectionDate = DateTime.Now;

				try
				{
                    await _dbContext.SaveChangesAsync();
                    return Json(new { success = true, message = "Ürün başarıyla güncellendi!" });
                }
				catch (Exception ex)
				{
                    return Json(new { success = false, message = $"Ürün güncellenemedi: {ex.Message}" });
                }
			}

			return Json(new { success = false, message = "Ürün güncellenemedi!" });
		}

		[AuthorizeWithPermission("UpdateStockProduct")]
		[HttpPost]
		public IActionResult UpdateStock([FromBody] JsonElement request)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var productId = Guid.Parse(request.GetProperty("id").GetString());
            var stockStatus = request.GetProperty("stock").GetBoolean();

			var product = _dbContext.Product
				.Include(p => p.SubMenuCategory).ThenInclude(sc => sc.MenuCategory).ThenInclude(mc => mc.Cafe)
				.FirstOrDefault(p => p.Id == productId && p.SubMenuCategory.MenuCategory.Cafe.Id == cafeId);

			if (product == null)
				return Json(new { success = false, message = "Ürün bulunamadı veya yetkiniz yok." });

			product.Stock = stockStatus;
			product.CorrectionUser = userId.Value;
			product.CorrectionUserRole = userRole;
			product.CorrectionDate = DateTime.Now;

            try
            {
                _dbContext.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Stok güncellenemedi: {ex.Message}" });
            }
		}

		[AuthorizeWithPermission("DeleteProduct")]
		[HttpPost]
		public IActionResult DeleteProduct([FromBody] JsonElement request)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var productId = Guid.Parse(request.GetProperty("id").GetString());

            var product = _dbContext.Product
				.Include(p => p.SubMenuCategory).ThenInclude(sc => sc.MenuCategory).ThenInclude(mc => mc.Cafe)
				.FirstOrDefault(p => p.Id == productId && p.SubMenuCategory.MenuCategory.Cafe.Id == cafeId);

			if (product == null)
				return Json(new { success = false, message = "Ürün bulunamadı veya yetkiniz yok." });

			product.Active = false;
			product.CorrectionUser = userId.Value;
			product.CorrectionUserRole = userRole;
			product.CorrectionDate = DateTime.Now;

            try
            {
                _dbContext.SaveChanges();
                return Json(new { success = true, message = "Ürün başarıyla silindi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ürün silinemedi: {ex.Message}" });
            }
		}
	}
}
