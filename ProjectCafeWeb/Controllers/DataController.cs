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
			List<MenuCategory> menuCategories = _dbContext.MenuCategory.ToList();
			return View(menuCategories);
		}

		public IActionResult SubMenuCategory()
		{
			List<SubMenuCategory> subMenuCategories = _dbContext.SubMenuCategory
				.Include(p => p.MenuCategory)
				.ToList();
			return View(subMenuCategories);
		}

		public IActionResult Product()
		{
			List<Product> products = _dbContext.Product
				.Include(p => p.SubMenuCategory)
				.Include(p => p.MenuCategory)
				.Where(p => p.Active)
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
	}
}
