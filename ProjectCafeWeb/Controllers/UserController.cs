using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ProjectCafeDataAccess;
using ProjectCafeEntities;
using ProjectCafeWeb.Helpers;
using ProjectCafeWeb.Models;
using ProjectCafeWeb.Services;

namespace ProjectCafeWeb.Controllers
{
    public class UserController : BaseController
    {
        private readonly NotificationService _notificationService;
        public UserController(ProjectCafeDbContext dbContext, NotificationService notificationService) : base(dbContext)
        {
            _notificationService = notificationService;
        }

        [Route("menu-kategori/{cafeId}/{tableId}")]
        public IActionResult UserMenuCategory(Guid cafeId, Guid tableId)
        {
            var categories = _dbContext.MenuCategory
                .Where(x => x.CafeId == cafeId && x.Active)
                .Select(x => new
                {
                    x.Id,
                    x.CategoryName,
                    x.CategoryImage
                }).ToList();

            ViewBag.CafeId = cafeId;
            ViewBag.TableId = tableId;

            return View(categories);
        }

        [Route("alt-menu-kategori/{cafeId}/{tableId}/{categoryId}")]
        public IActionResult UserSubMenuCategory(Guid categoryId, Guid cafeId, Guid tableId)
        {
            var subCategories = _dbContext.SubMenuCategory
                .Where(x => x.MenuCategoryId == categoryId && x.Active && x.MenuCategory.CafeId == cafeId)
                .Select(x => new
                {
                    x.Id,
                    x.SubCategoryName,
                    x.SubCategoryImage,
                    x.MenuCategory.CafeId
                }).ToList();

            ViewBag.CafeId = cafeId;
            ViewBag.TableId = tableId;
            ViewBag.MenuCategoryId = categoryId;

            return View(subCategories);
        }

        [Route("urunler/{cafeId}/{tableId}/{categoryId}/{subCategoryId}")]
        public IActionResult UserProduct(Guid subCategoryId, Guid cafeId, Guid tableId, Guid categoryId)
        {
            var products = _dbContext.Product
                .Where(x => x.SubMenuCategoryId == subCategoryId && x.Active && x.Stock && x.MenuCategory.CafeId == cafeId)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Image,
                    x.Price,
                    x.IsThereDiscount,
                    x.DiscountRate,
                    x.MenuCategory.CafeId
                }).ToList();

            ViewBag.CafeId = cafeId;
            ViewBag.TableId = tableId;
            ViewBag.CategoryId = categoryId;
            ViewBag.SubCategoryId = subCategoryId;

            return View(products);
        }

        [HttpPost]
        public IActionResult AddToCart(Guid productId)
        {
            var product = _dbContext.Product.FirstOrDefault(x => x.Id == productId && x.Active);
            if (product == null) return NotFound();

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var existingItem = cart.FirstOrDefault(x => x.ProductId == productId);

            double finalPrice = product.Price;
            if (product.IsThereDiscount && product.DiscountRate.HasValue)
            {
                finalPrice = Math.Round(product.Price * (1 - product.DiscountRate.Value / 100), 2);
            }

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = finalPrice,
                    StockCount = product.StockCount,
                    DiscountRate = product.DiscountRate,
                    IsThereDiscount = product.IsThereDiscount,
                    Quantity = 1,
                    Image = product.Image
                });
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return Json(cart); // tüm sepeti dön
        }

        [HttpPost]
        public IActionResult UpdateCartQuantity(Guid productId, int quantity)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            var item = cart.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                item.Quantity = quantity > 0 ? quantity : 1;
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }

            return Json(cart); // güncellenmiş sepeti döndür
        }

        [HttpPost]
        public IActionResult RemoveFromCart(Guid productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var itemToRemove = cart.FirstOrDefault(x => x.ProductId == productId);
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
                return Json(new { success = true, cart });
            }

            return Json(new { success = false });
        }

        [Route("sepet")]
        public IActionResult Cart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            var tableId = HttpContext.Session.GetInt32("TableId");
            var cafeId = HttpContext.Session.GetInt32("CafeId");

            ViewBag.TableId = tableId;
            ViewBag.CafeId = cafeId;

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(Guid cafeId, Guid tableId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart");

            if (cart == null || !cart.Any())
                return Json(new { success = false, message = "Sepet boş." });

            // 📌 Aktif bir DailyReport var mı kontrol et
            var activeReport = _dbContext.DailyReport
                .FirstOrDefault(r => r.CafeId == cafeId && r.EndTime == null);

            if (activeReport == null)
            {
                return Json(new { success = false, message = "Sistem henüz başlatılmadı. Garsona bilgi verebilirsiniz!" });
            }

            // Masa ve bölüm bilgilerini al
            var table = _dbContext.Table
                .Include(t => t.Section) // Section bilgisini include ediyoruz
                .FirstOrDefault(t => t.Id == tableId && t.Active);

            if (table == null)
            {
                return Json(new { success = false, message = "Masa bulunamadı." });
            }

            foreach (var item in cart)
            {
                var product = _dbContext.Product.FirstOrDefault(p => p.Id == item.ProductId);
                if (product == null)
                {
                    return Json(new { success = false, message = $"Ürün bulunamadı: Id={item.ProductId}" });
                }

                // StockCount kontrolü
                int stockCount = product.StockCount ?? int.MaxValue;

                if (item.Quantity <= stockCount)
                {
                    for (int i = 0; i < item.Quantity; i++)
                    {
                        var order = new Order
                        {
                            ProductId = item.ProductId,
                            TableId = tableId,
                            Status = 1,
                            Active = true,
                            RegistrationDate = DateTime.Now,
                            RegistrationUserRole = "Customer"
                        };

                        _dbContext.Order.Add(order);
                    }
                }
                else
                {
                    return Json(new { success = false, message = $"{product.Name} ürününde yeterli stok yok." });
                }
            }

            _dbContext.SaveChanges();
            HttpContext.Session.Remove("Cart");

            if (table.Active && table.Notification)
            {
                // Bildirim mesajını oluştur
                string notificationMessage = $"{table.Section?.Name} bölümündeki {table.Name} masasından sipariş girişi gerçekleşti.";

                // Bildirim gönder
                await _notificationService.SendPushToGarsons(cafeId, "Yeni Sipariş", notificationMessage);
            }

            return Json(new
            {
                success = true,
                message = "Sipariş başarıyla oluşturuldu.",
                redirectUrl = Url.Action("menu-kategori", new { cafeId, tableId })
            });
        }
    }
}
