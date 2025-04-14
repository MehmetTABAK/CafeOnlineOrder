using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectCafeDataAccess;
using ProjectCafeEntities;
using System.Text.Json;

namespace ProjectCafeWeb.Controllers
{
	public class DataController : Controller
	{
		private readonly ProjectCafeDbContext _dbContext;

		public DataController(ProjectCafeDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		public IActionResult MenuCategory()
		{
			List<MenuCategory> menuCategories = _dbContext.MenuCategory
				.Where(p => p.Active)
				.ToList();
			return View(menuCategories);
		}

		[HttpPost]
		public async Task<IActionResult> AddMenuCategory([FromBody] MenuCategory menuCategory)
		{
			if (ModelState.IsValid)
			{
				menuCategory.CafeId = 1;
				menuCategory.RegistrationUser = 1;
				menuCategory.RegistrationDate = DateTime.Now;
				// Ürünü veritabanına kaydet
				_dbContext.MenuCategory.Add(menuCategory);
				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Üst menü başarıyla eklendi!" });
			}

			return Json(new { success = false, message = "Üst menü eklenemedi!" });
		}

		[HttpPost]
		public async Task<IActionResult> UpdateMenuCategory([FromBody] MenuCategory menuCategory)
		{
			if (ModelState.IsValid)
			{
				var existingProduct = await _dbContext.MenuCategory.FindAsync(menuCategory.Id);
				if (existingProduct != null)
				{
					existingProduct.CategoryName = menuCategory.CategoryName;
					existingProduct.CategoryImage = menuCategory.CategoryImage;
					existingProduct.CafeId = menuCategory.CafeId;
					existingProduct.CorrectionUser = 1;
					existingProduct.CorrectionDate = DateTime.Now;

					await _dbContext.SaveChangesAsync();
					return Json(new { success = true, message = "Üst menü başarıyla güncellendi!" });
				}
				return Json(new { success = false, message = "Üst menü bulunamadı!" });
			}
			return Json(new { success = false, message = "Üst menü güncellenemedi!" });
		}

		[HttpPost]
		public IActionResult DeleteMenuCategory([FromBody] JsonElement request)
		{
			var menuCategoryId = request.GetProperty("id").GetInt32();
			var menuCategory = _dbContext.MenuCategory.Find(menuCategoryId);
			if (menuCategory != null)
			{
				menuCategory.Active = false;
				menuCategory.CorrectionUser = 1;
				menuCategory.CorrectionDate = DateTime.Now;
				_dbContext.SaveChanges();
				return Json(new { success = true, message = "Üst menü başarıyla silindi!" });
			}
			return Json(new { success = false, message = "Üst menü bulunamadı!" });
		}

		public IActionResult SubMenuCategory()
		{
			List<SubMenuCategory> subMenuCategories = _dbContext.SubMenuCategory
				.Include(p => p.MenuCategory)
				.Where(p => p.Active)
				.ToList();

			ViewBag.AllMenuCategories = _dbContext.MenuCategory
				.Where(mc => mc.Active)
				.ToList();

			return View(subMenuCategories);
		}

		[HttpPost]
		public async Task<IActionResult> AddSubMenuCategory([FromBody] SubMenuCategory subMenuCategory)
		{
			if (ModelState.IsValid)
			{
				subMenuCategory.RegistrationUser = 1;
				subMenuCategory.RegistrationDate = DateTime.Now;
				// Ürünü veritabanına kaydet
				_dbContext.SubMenuCategory.Add(subMenuCategory);
				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Alt menü başarıyla eklendi!" });
			}

			return Json(new { success = false, message = "Alt menü eklenemedi!" });
		}

		[HttpPost]
		public async Task<IActionResult> UpdateSubMenuCategory([FromBody] SubMenuCategory subMenuCategory)
		{
			if (ModelState.IsValid)
			{
				var existingProduct = await _dbContext.SubMenuCategory.FindAsync(subMenuCategory.Id);
				if (existingProduct != null)
				{
					existingProduct.SubCategoryName = subMenuCategory.SubCategoryName;
					existingProduct.SubCategoryImage = subMenuCategory.SubCategoryImage;
					existingProduct.MenuCategoryId = subMenuCategory.MenuCategoryId;
					existingProduct.CorrectionUser = 1;
					existingProduct.CorrectionDate = DateTime.Now;

					await _dbContext.SaveChangesAsync();
					return Json(new { success = true, message = "Alt menü başarıyla güncellendi!" });
				}
				return Json(new { success = false, message = "Alt menü bulunamadı!" });
			}
			return Json(new { success = false, message = "Alt menü güncellenemedi!" });
		}

		[HttpPost]
		public IActionResult DeleteSubMenuCategory([FromBody] JsonElement request)
		{
			var subMenuCategoryId = request.GetProperty("id").GetInt32();
			var subMenuCategory = _dbContext.SubMenuCategory.Find(subMenuCategoryId);
			if (subMenuCategory != null)
			{
				subMenuCategory.Active = false;
				subMenuCategory.CorrectionUser = 1;
				subMenuCategory.CorrectionDate = DateTime.Now;
				_dbContext.SaveChanges();
				return Json(new { success = true, message = "Alt menü başarıyla silindi!" });
			}
			return Json(new { success = false, message = "Alt menü bulunamadı!" });
		}

		public IActionResult Product()
		{
			List<Product> products = _dbContext.Product
				.Include(p => p.SubMenuCategory)
				.Include(p => p.MenuCategory)
				.Where(p => p.Active)
				.ToList();

			ViewBag.AllMenuCategories = _dbContext.MenuCategory
				.Where(mc => mc.Active)
				.ToList();

			ViewBag.AllSubMenuCategories = _dbContext.SubMenuCategory
				.Include(sc => sc.MenuCategory)
				.Where(sc => sc.Active)
				.ToList();

			return View(products);
		}

		[HttpPost]
		public async Task<IActionResult> AddProduct([FromBody] Product product)
		{
			if (ModelState.IsValid)
			{
				product.RegistrationUser = 1;
				product.RegistrationDate = DateTime.Now;
				// Ürünü veritabanına kaydet
				_dbContext.Product.Add(product);
				await _dbContext.SaveChangesAsync();
				return Json(new { success = true, message = "Ürün başarıyla eklendi!" });
			}

			return Json(new { success = false, message = "Ürün eklenemedi!" });
		}

		[HttpPost]
		public async Task<IActionResult> UpdateProduct([FromBody] Product product)
		{
			if (ModelState.IsValid)
			{
				var existingProduct = await _dbContext.Product.FindAsync(product.Id);
				if (existingProduct != null)
				{
					existingProduct.Name = product.Name;
					existingProduct.Price = product.Price;
					existingProduct.Image = product.Image;
					existingProduct.IsThereDiscount = product.IsThereDiscount;
					existingProduct.DiscountRate = product.DiscountRate;
					existingProduct.MenuCategoryId = product.MenuCategoryId;
					existingProduct.SubMenuCategoryId = product.SubMenuCategoryId;
					existingProduct.CorrectionUser = 1;
					existingProduct.CorrectionDate = DateTime.Now;

					await _dbContext.SaveChangesAsync();
					return Json(new { success = true, message = "Ürün başarıyla güncellendi!" });
				}
				return Json(new { success = false, message = "Ürün bulunamadı!" });
			}
			return Json(new { success = false, message = "Ürün güncellenemedi!" });
		}

		[HttpPost]
		public IActionResult UpdateStock([FromBody] JsonElement request)
		{
			var productId = request.GetProperty("id").GetInt32();
			var stockStatus = request.GetProperty("stock").GetBoolean();

			var product = _dbContext.Product.Find(productId);
			if (product != null)
			{
				product.Stock = stockStatus;
				product.CorrectionUser = 1;
				product.CorrectionDate = DateTime.Now;
				_dbContext.SaveChanges();
				return Json(new { success = true });
			}
			return Json(new { success = false });
		}

		[HttpPost]
		public IActionResult DeleteProduct([FromBody] JsonElement request)
		{
			var productId = request.GetProperty("id").GetInt32();
			var product = _dbContext.Product.Find(productId);
			if (product != null)
			{
				product.Active = false;
				product.CorrectionUser = 1;
				product.CorrectionDate = DateTime.Now;
				_dbContext.SaveChanges();
				return Json(new { success = true, message = "Ürün başarıyla silindi!" });
			}
			return Json(new { success = false, message = "Ürün bulunamadı!" });
		}
	}
}
