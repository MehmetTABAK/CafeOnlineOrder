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
	public class TableController : BaseController
	{
		public TableController(ProjectCafeDbContext dbContext) : base(dbContext)
		{
		}

		[AuthorizeWithPermission("ViewTable")]
		public IActionResult Table()
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			List<Table> tables = _dbContext.Table
				.Include(t => t.Section)
				.Include(t => t.Orders)
				.Where(t => t.Active && t.Section.Cafe.Id == cafeId)
				.ToList();

			return View(tables);
		}

		[AuthorizeWithPermission("ViewTableDetails")]
		[HttpGet]
		public IActionResult GetTableDetails(int tableId)
		{
			var table = _dbContext.Table
				.Include(t => t.Section)
				.Include(t => t.Orders)
				.ThenInclude(o => o.Product)
				.FirstOrDefault(t => t.Id == tableId);

			var allTables = _dbContext.Table
				.Where(t => t.Active && t.Id != tableId)
				.Select(t => new { t.Id, t.Name })
				.ToList();

			// Status 5 olan siparişleri filtrele
			var filteredOrders = table.Orders
				.Where(o => o.Active && o.Status != 5)
				.Select(o => new
				{
					o.Id,
					productName = o.Product.Name,
					price = o.Product.Price,
					o.Status
				});

			return Json(new
			{
				name = table.Name,
				sectionName = table.Section?.Name,
				orders = filteredOrders,
				allTables
			});
		}

		[AuthorizeWithPermission("Payment")]
		[HttpPost]
		public IActionResult PaySelectedOrdersSplit(List<int> orderIds, double cardAmount, double cashAmount)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var orders = _dbContext.Order
				.Include(o => o.Product)
				.Where(o => orderIds.Contains(o.Id) && o.Status != 4)
				.ToList();

			if (!orders.Any())
				return BadRequest("Seçili ürünler bulunamadı.");

			var tableId = orders.First().TableId;
			var total = orders.Sum(o => o.Product.Price);
			if (cardAmount + cashAmount != total)
				return BadRequest("Toplam tutar eşleşmiyor.");

			if (cardAmount > 0)
			{
				_dbContext.Payment.Add(new Payment
				{
					TableId = tableId,
					Method = 1,
					TotalPrice = cardAmount,
					Active = true,
					RegistrationUser = userId.Value,
					RegistrationUserRole = userRole,
					RegistrationDate = DateTime.Now
				});
			}

			if (cashAmount > 0)
			{
				_dbContext.Payment.Add(new Payment
				{
					TableId = tableId,
					Method = 2,
					TotalPrice = cashAmount,
					Active = true,
					RegistrationUser = userId.Value,
					RegistrationUserRole = userRole,
					RegistrationDate = DateTime.Now
				});
			}

			orders.ForEach(o => o.Status = 4);
			_dbContext.SaveChanges();
			return Ok();
		}

		[AuthorizeWithPermission("Payment")]
		[HttpPost]
		public IActionResult CompletePayment(int tableId, byte method)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var unpaidOrders = _dbContext.Order
				.Include(o => o.Product)
				.Where(o => o.TableId == tableId && o.Status != 4 && o.Active)
				.ToList();

			if (!unpaidOrders.Any())
				return BadRequest("Tüm ürünler zaten ödenmiş.");

			var total = unpaidOrders.Sum(o => o.Product.Price);

			_dbContext.Payment.Add(new Payment
			{
				TableId = tableId,
				Method = method,
				TotalPrice = total,
				Active = true,
				RegistrationUser = userId.Value,
				RegistrationUserRole = userRole,
				RegistrationDate = DateTime.Now
			});

			foreach (var order in unpaidOrders)
				order.Status = 4;

			_dbContext.SaveChanges();
			return Ok();
		}

		[AuthorizeWithPermission("MoveTableAndOrders")]
		[HttpPost]
		public IActionResult MoveSelectedOrders(int toId, List<int> orderIds)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var orders = _dbContext.Order.Where(o => orderIds.Contains(o.Id)).ToList();
			foreach (var order in orders)
			{
				order.TableId = toId;
				order.CorrectionUser = userId.Value;
				order.CorrectionUserRole = userRole;
				order.CorrectionDate = DateTime.Now;
			}
			_dbContext.SaveChanges();
			return Ok();
		}

		[AuthorizeWithPermission("MoveTableAndOrders")]
		[HttpPost]
		public IActionResult MoveTable(int fromId, int toId)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var orders = _dbContext.Order.Where(o => o.TableId == fromId).ToList();
			foreach (var order in orders)
			{
				order.TableId = toId;
				order.CorrectionUser = userId.Value;
				order.CorrectionUserRole = userRole;
				order.CorrectionDate = DateTime.Now;
			}
			_dbContext.SaveChanges();
			return Ok();
		}

		[AuthorizeWithPermission("CloseTable")]
		[HttpPost]
		public IActionResult CloseTable([FromBody] Table table)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var tableId = _dbContext.Table
				.Include(t => t.Orders)
				.FirstOrDefault(t => t.Id == table.Id);

			if (tableId == null)
				return Json(new { success = false, message = "Masa bulunamadı." });

			// Masadaki tüm siparişlerin status değerini 5 yap
			foreach (var order in tableId.Orders)
			{
				order.Status = 5;
				order.CorrectionUser = userId.Value;
				order.CorrectionUserRole = userRole;
				order.CorrectionDate = DateTime.Now;
			}

			_dbContext.SaveChanges();

			return Json(new { success = true, message = "Masa başarıyla kapatıldı." });
		}

		[AuthorizeWithPermission("ReturnOrders")]
		[HttpPost]
		public JsonResult ReturnOrders(List<int> orderIds, int tableId, byte method)
		{
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var cafeId = GetCurrentCafeId();

            if (orderIds == null || !orderIds.Any())
                return Json(new { success = false });

            var orders = _dbContext.Order
                .Include(o => o.Product)
                .Where(o => orderIds.Contains(o.Id))
                .ToList();

            foreach (var order in orders)
            {
                order.Status = 6;
                order.CorrectionUser = userId.Value;
                order.CorrectionUserRole = userRole;
                order.CorrectionDate = DateTime.Now;
            }

            var total = orders.Sum(o => o.Product.Price);

            _dbContext.Payment.Add(new Payment
            {
                TableId = tableId,
                Method = method,
                TotalPrice = total,
                Active = true,
                RegistrationUser = userId.Value,
                RegistrationUserRole = userRole,
                RegistrationDate = DateTime.Now
            });

            _dbContext.SaveChanges();

            return Json(new { success = true });
        }

        [AuthorizeWithPermission("CancelOrders")]
        [HttpPost]
        public JsonResult CancelOrders(List<int> orderIds)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var cafeId = GetCurrentCafeId();

            if (orderIds == null || !orderIds.Any())
                return Json(new { success = false });

            var orders = _dbContext.Order
				.Include(o => o.Product)
				.ThenInclude(p => p.MenuCategory)
				.Where(o => orderIds.Contains(o.Id) && o.Active && o.Product != null && o.Product.MenuCategory.CafeId == cafeId)
				.ToList();

            foreach (var order in orders)
            {
                order.Status = 7;
                order.CorrectionUser = userId.Value;
                order.CorrectionUserRole = userRole;
                order.CorrectionDate = DateTime.Now;

                // Eğer ürün stok takibindeyse, stok adedini geri ekle
                var product = order.Product;
                if (product != null && product.StockCount.HasValue)
                {
                    product.StockCount += 1;
                    product.Stock = true; // stok tekrar mevcut hale gelebilir
                }
            }

            _dbContext.SaveChanges();

            return Json(new { success = true });
        }

        [AuthorizeWithPermission("BonusOrders")]
        [HttpPost]
        public JsonResult BonusOrders(List<int> orderIds, int tableId, byte method)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var cafeId = GetCurrentCafeId();

            if (orderIds == null || !orderIds.Any())
                return Json(new { success = false });

            var orders = _dbContext.Order
                .Include(o => o.Product)
                .Where(o => orderIds.Contains(o.Id))
                .ToList();

            foreach (var order in orders)
            {
                order.Status = 8;
                order.CorrectionUser = userId.Value;
                order.CorrectionUserRole = userRole;
                order.CorrectionDate = DateTime.Now;
            }

            var total = orders.Sum(o => o.Product.Price);

            _dbContext.Payment.Add(new Payment
            {
                TableId = tableId,
                Method = method,
                TotalPrice = total,
                Active = true,
                RegistrationUser = userId.Value,
                RegistrationUserRole = userRole,
                RegistrationDate = DateTime.Now
            });

            _dbContext.SaveChanges();

            return Json(new { success = true });
        }

        [HttpGet]
		public JsonResult GetMenuCategories()
		{
            var cafeId = GetCurrentCafeId();

            var categories = _dbContext.MenuCategory
				.Where(x => x.Active && x.CafeId == cafeId)
				.Select(x => new { x.Id, x.CategoryName })
				.ToList();

            return Json(categories);
		}

        [HttpGet]
		public JsonResult GetSubMenuCategories(int categoryId)
		{
            var cafeId = GetCurrentCafeId();

            var subCategories = _dbContext.SubMenuCategory
				.Where(x => x.MenuCategoryId == categoryId && x.Active && x.MenuCategory.CafeId == cafeId)
				.Select(x => new { x.Id, x.SubCategoryName })
				.ToList();

            return Json(subCategories);
		}

		[HttpGet]
		public JsonResult GetProducts(int subCategoryId)
		{
            var cafeId = GetCurrentCafeId();

            var products = _dbContext.Product
				.Where(x =>
					x.SubMenuCategoryId == subCategoryId &&
					x.Active && x.Stock &&
					x.MenuCategory != null &&
					x.MenuCategory.CafeId == cafeId)
				.Select(x => new { x.Id, x.Name, x.Price, x.StockCount })
				.ToList();

            return Json(products);
		}

		[AuthorizeWithPermission("AddOrders")]
		[HttpPost]
		public JsonResult AddOrders(List<int> productIds, int tableId)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();

			foreach (var productId in productIds)
			{
                var product = _dbContext.Product.FirstOrDefault(x => x.Id == productId && x.Active && x.MenuCategory.CafeId == cafeId);

                if (product == null)
                    continue;

                // Stok varsa ve sayısı 0'dan büyükse azalt
                if (product.StockCount.HasValue && product.StockCount > 0)
                {
                    product.StockCount -= 1;

                    // Eğer stok sıfıra düşerse stok kalmadı anlamında Stock'u false yap
                    if (product.StockCount == 0)
                    {
                        product.Stock = false;
                    }
                }

                var order = new Order
				{
					ProductId = productId,
					TableId = tableId,
					Status = 3,
					Active = true,
					RegistrationUser = userId.Value,
					RegistrationUserRole = userRole,
					RegistrationDate = DateTime.Now
				};
				_dbContext.Order.Add(order);
			}

            _dbContext.SaveChanges();
			return Json(new { success = true });
		}

	}
}
