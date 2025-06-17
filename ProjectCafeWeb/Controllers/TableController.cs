using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProjectCafeDataAccess;
using ProjectCafeEntities;
using ProjectCafeWeb.Attributes;
using System.Collections.Generic;
using System.Globalization;
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
        [Route("masalar")]
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
                .OrderBy(t => t.Name)
                .ToList();

			return View(tables);
		}

		[AuthorizeWithPermission("ViewTableDetails")]
        [HttpGet]
        public IActionResult GetTableDetails(Guid tableId)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var cafeId = GetCurrentCafeId();

            if (userId == null || cafeId == null)
                return Unauthorized();

            var table = _dbContext.Table
                .Include(t => t.Section)
                .ThenInclude(s => s.Cafe)
                .Include(t => t.Orders)
                .ThenInclude(o => o.Product)
                .FirstOrDefault(t => t.Id == tableId);

            if (table == null || table.Section?.CafeId != cafeId)
                return NotFound();

            // Sadece aynı cafeye ait ve aktif olan masaları getir
            var allTables = _dbContext.Table
                .Include(t => t.Section)
                .Where(t => t.Active && t.Section.CafeId == cafeId && t.Id != tableId)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    SectionName = t.Section != null ? t.Section.Name : null
                })
                .ToList();

            var result = new
            {
                name = table.Name,
                sectionName = table.Section?.Name,
                orders = table.Orders
                    .Where(o => o.Active && o.Status != 5)
                    .Select(o => new
                    {
                        o.Id,
                        productName = o.Product.Name,
                        o.Status,
                        Price = o.Product.IsThereDiscount && o.Product.DiscountRate.HasValue
                            ? o.Product.Price * (1 - o.Product.DiscountRate.Value / 100.0)
                            : o.Product.Price
                    }).ToList(),
                allTables = allTables
            };

            return Json(result);
        }

        [AuthorizeWithPermission("Payment")]
		[HttpPost]
		public IActionResult PaySelectedOrdersSplit(List<Guid> orderIds, string cardAmount, string cashAmount)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var orders = _dbContext.Order
				.Include(o => o.Product)
				.Where(o => orderIds.Contains(o.Id) && o.Status == 3)
				.ToList();

			if (!orders.Any() || orderIds.Count != orders.Count)
                return Json(new { success = false, message = "Teslim edildi durumunda olmayan ürünlerden ödeme alınamaz." });

            var tableId = orders.First().TableId;
			var total = orders.Sum(o => o.Product.IsThereDiscount && o.Product.DiscountRate.HasValue
                            ? o.Product.Price * (1 - o.Product.DiscountRate.Value / 100.0)
                            : o.Product.Price);

            var card = double.Parse(cardAmount.Replace(',', '.'), CultureInfo.InvariantCulture);
            var cash = double.Parse(cashAmount.Replace(',', '.'), CultureInfo.InvariantCulture);

            if (Math.Abs((card + cash) - total) > 0.01)
                return BadRequest("Toplam tutar eşleşmiyor.");

            if (card > 0)
			{
				_dbContext.Payment.Add(new Payment
				{
                    Id = Guid.NewGuid(),
					TableId = tableId,
					Method = 1,
					TotalPrice = card,
					Comment = null,
					Active = true,
					RegistrationUser = userId.Value,
					RegistrationUserRole = userRole,
					RegistrationDate = DateTime.Now
				});
			}

			if (cash > 0)
			{
				_dbContext.Payment.Add(new Payment
				{
                    Id = Guid.NewGuid(),
					TableId = tableId,
					Method = 2,
					TotalPrice = cash,
                    Comment = null,
                    Active = true,
					RegistrationUser = userId.Value,
					RegistrationUserRole = userRole,
					RegistrationDate = DateTime.Now
				});
			}

			orders.ForEach(o => o.Status = 4);

            try
            {
                _dbContext.SaveChanges();
                return Json(new { success = true });
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
            {
                // Guid çakışması durumunda tekrar dene
                return PaySelectedOrdersSplit(orderIds, cardAmount, cashAmount); // Recursive çağrı (dikkatli kullanın)
            }
            catch (Exception ex)
            {
                // Diğer hatalar
                return Json(new { success = false, message = $"Ödeme sırasında bir sorun oluştu: {ex.Message}" });
            }

        }

		[AuthorizeWithPermission("Payment")]
		[HttpPost]
		public IActionResult CompletePayment(Guid tableId, byte method)
		{
			var userId = GetCurrentUserId();
			var userRole = GetCurrentUserRole();
			var cafeId = GetCurrentCafeId();
			if (userId == null || cafeId == null)
				return Unauthorized();

			var unpaidOrders = _dbContext.Order
				.Include(o => o.Product)
				.Where(o => o.TableId == tableId && o.Status == 3 && o.Active)
				.ToList();

			if (!unpaidOrders.Any())
                return Json(new { success = false, message = "Teslim edildi durumunda olmayan ürünlerden ödeme alınamaz." });

            var total = unpaidOrders.Sum(o => o.Product.IsThereDiscount && o.Product.DiscountRate.HasValue
                            ? o.Product.Price * (1 - o.Product.DiscountRate.Value / 100.0)
                            : o.Product.Price);

			_dbContext.Payment.Add(new Payment
			{
                Id = Guid.NewGuid(),
				TableId = tableId,
				Method = method,
				TotalPrice = total,
                Comment = null,
                Active = true,
				RegistrationUser = userId.Value,
				RegistrationUserRole = userRole,
				RegistrationDate = DateTime.Now
			});

			foreach (var order in unpaidOrders)
				order.Status = 4;

            try
            {
                _dbContext.SaveChanges();
                return Json(new { success = true });
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
            {
                // Guid çakışması durumunda tekrar dene
                return CompletePayment(tableId, method); // Recursive çağrı (dikkatli kullanın)
            }
            catch (Exception ex)
            {
                // Diğer hatalar
                return Json(new { success = false, message = $"Ödeme sırasında bir sorun oluştu: {ex.Message}" });
            }
        }

		[AuthorizeWithPermission("MoveTableAndOrders")]
		[HttpPost]
		public IActionResult MoveSelectedOrders(Guid toId, List<Guid> orderIds)
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
		public IActionResult MoveTable(Guid fromId, Guid toId)
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

        [AuthorizeWithPermission("TurnOfNotification")]
        [HttpPost]
        public IActionResult UpdateNotification([FromBody] JsonElement request)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var cafeId = GetCurrentCafeId();
            if (userId == null || cafeId == null)
                return Unauthorized();

            var tableId = Guid.Parse(request.GetProperty("id").GetString());
            var notificationStatus = request.GetProperty("notification").GetBoolean();

            var table = _dbContext.Table
                .Include(p => p.Section).ThenInclude(mc => mc.Cafe)
                .FirstOrDefault(p => p.Id == tableId && p.Section.Cafe.Id == cafeId);

            if (table == null)
                return Json(new { success = false, message = "Masa bulunamadı veya yetkiniz yok." });

            table.Notification = notificationStatus;
            table.CorrectionUser = userId.Value;
            table.CorrectionUserRole = userRole;
            table.CorrectionDate = DateTime.Now;

            _dbContext.SaveChanges();
            return Json(new { success = true });
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

            var hasPendingOrders = tableId.Orders.Any(o => o.Active && (o.Status == 1 || o.Status == 2 || o.Status == 3));
            if (hasPendingOrders)
            {
                return Json(new { success = false, message = "Masada henüz tamamlanmamış sipariş(ler) var. Lütfen önce işlem tamamlayın." });
            }

            foreach (var order in tableId.Orders)
            {
                order.Status = 5;
                order.Active = false;
                order.CorrectionUser = userId.Value;
                order.CorrectionUserRole = userRole;
                order.CorrectionDate = DateTime.Now;
            }

            _dbContext.SaveChanges();

            return Json(new { success = true, message = "Masa başarıyla kapatıldı." });
        }

        [AuthorizeWithPermission("ConfirmOrder")]
        [HttpPost]
        public JsonResult ConfirmOrders(List<Guid> orderIds)
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
                order.Status = 2;
                order.CorrectionUser = userId.Value;
                order.CorrectionUserRole = userRole;
                order.CorrectionDate = DateTime.Now;

                // Eğer ürün stok takibindeyse, stok adedini geri ekle
                var product = order.Product;
                if (product != null && product.StockCount.HasValue)
                {
                    product.StockCount -= 1;
                    if (product.StockCount == 0)
                        product.Stock = false;
                }
            }

            _dbContext.SaveChanges();

            return Json(new { success = true });
        }

        [AuthorizeWithPermission("DeliverTheOrder")]
        [HttpPost]
        public JsonResult DeliverTheOrders(List<Guid> orderIds)
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
                order.Status = 3;
                order.CorrectionUser = userId.Value;
                order.CorrectionUserRole = userRole;
                order.CorrectionDate = DateTime.Now;
            }

            _dbContext.SaveChanges();

            return Json(new { success = true });
        }

        [AuthorizeWithPermission("ReturnOrders")]
		[HttpPost]
		public JsonResult ReturnOrders(List<Guid> orderIds, Guid tableId, byte method, string comment)
		{
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var cafeId = GetCurrentCafeId();

            if (orderIds == null || !orderIds.Any())
                return Json(new { success = false });

            var orders = _dbContext.Order
                .Include(o => o.Product)
                .Where(o => orderIds.Contains(o.Id) && o.Active && o.Product != null && o.Product.MenuCategory.CafeId == cafeId)
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
                Id = Guid.NewGuid(),
                TableId = tableId,
                Method = method,
                TotalPrice = total,
                Comment = comment,
                Active = true,
                RegistrationUser = userId.Value,
                RegistrationUserRole = userRole,
                RegistrationDate = DateTime.Now
            });

            try
            {
                _dbContext.SaveChanges();
                return Json(new { success = true });
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
            {
                // Guid çakışması durumunda tekrar dene
                return ReturnOrders(orderIds, tableId, method, comment); // Recursive çağrı (dikkatli kullanın)
            }
            catch (Exception ex)
            {
                // Diğer hatalar
                return Json(new { success = false, message = $"İade işlemi sırasında hata oluştu: {ex.Message}" });
            }
        }

        [AuthorizeWithPermission("CancelOrders")]
        [HttpPost]
        public JsonResult CancelOrders(List<Guid> orderIds)
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
                // Eğer ürün stok takibindeyse, stok adedini geri ekle
                var product = order.Product;
                if (product != null && product.StockCount.HasValue)
                {
                    if (order.Status == 2 || order.Status == 3)
                    {
                        product.StockCount += 1;
                        product.Stock = true; // stok tekrar mevcut hale gelebilir
                    }
                }

                order.Status = 7;
                order.CorrectionUser = userId.Value;
                order.CorrectionUserRole = userRole;
                order.CorrectionDate = DateTime.Now;
            }

            _dbContext.SaveChanges();

            return Json(new { success = true });
        }

        [AuthorizeWithPermission("BonusOrders")]
        [HttpPost]
        public JsonResult BonusOrders(List<Guid> orderIds, Guid tableId, byte method)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var cafeId = GetCurrentCafeId();

            if (orderIds == null || !orderIds.Any())
                return Json(new { success = false });

            var orders = _dbContext.Order
                .Include(o => o.Product)
                .Where(o => orderIds.Contains(o.Id) && o.Active && o.Product != null && o.Product.MenuCategory.CafeId == cafeId)
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
                Id = Guid.NewGuid(),
                TableId = tableId,
                Method = method,
                TotalPrice = total,
                Comment = null,
                Active = true,
                RegistrationUser = userId.Value,
                RegistrationUserRole = userRole,
                RegistrationDate = DateTime.Now
            });

            try
            {
                _dbContext.SaveChanges();
                return Json(new { success = true });
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
            {
                // Guid çakışması durumunda tekrar dene
                return BonusOrders(orderIds, tableId, method); // Recursive çağrı (dikkatli kullanın)
            }
            catch (Exception ex)
            {
                // Diğer hatalar
                return Json(new { success = false, message = $"İkram işlemi sırasında bir hata oluştu: {ex.Message}" });
            }
        }

        [HttpGet]
        public JsonResult GetDayStatus()
        {
            var cafeId = GetCurrentCafeId();
            var activeReport = _dbContext.DailyReport
                .FirstOrDefault(r => r.CafeId == cafeId && r.EndTime == null);

            return Json(new
            {
                isDayOpen = activeReport != null
            });
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
		public JsonResult GetSubMenuCategories(Guid categoryId)
		{
            var cafeId = GetCurrentCafeId();

            var subCategories = _dbContext.SubMenuCategory
				.Where(x => x.MenuCategoryId == categoryId && x.Active && x.MenuCategory.CafeId == cafeId)
				.Select(x => new { x.Id, x.SubCategoryName })
				.ToList();

            return Json(subCategories);
		}

		[HttpGet]
		public JsonResult GetProducts(Guid subCategoryId)
		{
            var cafeId = GetCurrentCafeId();

            var products = _dbContext.Product
				.Where(x =>
					x.SubMenuCategoryId == subCategoryId &&
					x.Active && x.Stock &&
					x.MenuCategory != null &&
					x.MenuCategory.CafeId == cafeId)
				.Select(x => new { 
                    x.Id,
                    x.Name,
                    Price = x.IsThereDiscount && x.DiscountRate.HasValue
                            ? x.Price * (1 - x.DiscountRate.Value / 100.0)
                            : x.Price,
                    x.StockCount
                })
				.ToList();

            return Json(products);
		}

        [AuthorizeWithPermission("AddOrders")]
        [HttpPost]
        public JsonResult AddOrders(List<Guid> productIds, Guid tableId)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var cafeId = GetCurrentCafeId();

            // 📌 Aktif bir DailyReport var mı kontrol et
            var activeReport = _dbContext.DailyReport
                .FirstOrDefault(r => r.CafeId == cafeId && r.EndTime == null);

            if (activeReport == null)
            {
                return Json(new { success = false, message = "Gün başlatılmadı veya zaten gün sonu alındı. Sipariş eklenemez." });
            }

            foreach (var productId in productIds)
            {
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    TableId = tableId,
                    Status = 1,
                    Active = true,
                    RegistrationUser = userId.Value,
                    RegistrationUserRole = userRole,
                    RegistrationDate = DateTime.Now
                };
                try
                {
                    _dbContext.Order.Add(order);
                }
                catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
                {
                    // Guid çakışması durumunda tekrar dene
                    return AddOrders(productIds, tableId); // Recursive çağrı (dikkatli kullanın)
                }
                catch (Exception ex)
                {
                    // Diğer hatalar
                    return Json(new { success = false, message = $"Ürün eklenemedi: {ex.Message}" });
                }
            }
            
            _dbContext.SaveChanges();
            return Json(new { success = true });
        }
    }
}
