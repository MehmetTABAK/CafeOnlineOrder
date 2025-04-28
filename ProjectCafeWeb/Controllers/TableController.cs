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

		//[HttpPost]
		//public IActionResult UpdateOrderStatus(int orderId)
		//{
		//	var userId = GetCurrentUserId();
		//var userRole = GetCurrentUserRole();
		//var cafeId = GetCurrentCafeId();
		//	if (userId == null || cafeId == null)
		//		return Unauthorized();

		//	var order = _dbContext.Order
		//		.Include(o => o.Product)
		//		.FirstOrDefault(o => o.Id == orderId);

		//	if (order == null || order.Status == 3) return BadRequest();

		//	order.Status = 3;

		//	var payments = _dbContext.Payment
		//		.Where(p => p.TableId == order.TableId && p.Active)
		//		.OrderByDescending(p => p.RegistrationDate)
		//		.ToList();

		//	double remainingRefund = order.Product?.Price ?? 0;

		//	foreach (var payment in payments)
		//	{
		//		if (remainingRefund == 0) break;

		//		if (payment.TotalPrice <= remainingRefund)
		//		{
		//			remainingRefund -= payment.TotalPrice;
		//			payment.TotalPrice = 0;
		//			payment.Active = false;
		//			payment.CorrectionUser = adminId;
		//			payment.CorrectionDate = DateTime.Now;
		//		}
		//		else
		//		{
		//			payment.TotalPrice -= remainingRefund;
		//			remainingRefund = 0;
		//			payment.CorrectionUser = adminId;
		//			payment.CorrectionDate = DateTime.Now;
		//		}
		//	}

		//	_dbContext.SaveChanges();
		//	return Ok();
		//}

		//[HttpPost]
		//public IActionResult CancelAllPaidOrders(int tableId)
		//{
		//	var adminId = GetCurrentAdminId();
		//	if (adminId == null)
		//		return Unauthorized();

		//	var paidOrders = _dbContext.Order
		//		.Where(o => o.TableId == tableId && o.Status == 4 && o.Active)
		//		.ToList();

		//	if (!paidOrders.Any())
		//		return Ok();

		//	foreach (var order in paidOrders)
		//	{
		//		order.Status = 3;
		//	}

		//	var latestActivePayment = _dbContext.Payment
		//		.Where(p => p.TableId == tableId && p.Active)
		//		.OrderByDescending(p => p.RegistrationDate)
		//		.FirstOrDefault();

		//	if (latestActivePayment != null)
		//	{
		//		latestActivePayment.Active = false;
		//		latestActivePayment.TotalPrice = 0;
		//		latestActivePayment.CorrectionUser = adminId;
		//		latestActivePayment.CorrectionDate = DateTime.Now;
		//	}

		//	_dbContext.SaveChanges();
		//	return Ok();
		//}

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

		//[AuthorizeWithPermission("CancelOrders")]
		//[HttpPost]
		//public JsonResult CancelOrders(List<int> orderIds)
		//{
		//	var userId = GetCurrentUserId();
		//var userRole = GetCurrentUserRole();
		//var cafeId = GetCurrentCafeId();
		//	if (userId == null || cafeId == null)
		//		return Unauthorized();

		//	if (orderIds == null || !orderIds.Any())
		//		return Json(new { success = false });

		//	var orders = _dbContext.Order.Where(o => orderIds.Contains(o.Id)).ToList();

		//	foreach (var order in orders)
		//	{
		//		order.Active = false;
		//		order.CorrectionUser = adminId;
		//		order.CorrectionDate = DateTime.Now;
		//	}

		//	_dbContext.SaveChanges();

		//	return Json(new { success = true });
		//}

		[HttpGet]
		public JsonResult GetMenuCategories()
		{
			var categories = _dbContext.MenuCategory
				.Where(x => x.Active)
				.Select(x => new { x.Id, x.CategoryName })
				.ToList();

			return Json(categories);
		}

		[HttpGet]
		public JsonResult GetSubMenuCategories(int categoryId)
		{
			var subCategories = _dbContext.SubMenuCategory
				.Where(x => x.MenuCategoryId == categoryId && x.Active)
				.Select(x => new { x.Id, x.SubCategoryName })
				.ToList();

			return Json(subCategories);
		}

		[HttpGet]
		public JsonResult GetProducts(int subCategoryId)
		{
			var products = _dbContext.Product
				.Where(x => x.SubMenuCategoryId == subCategoryId && x.Active && x.Stock)
				.Select(x => new { x.Id, x.Name, x.Price })
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
