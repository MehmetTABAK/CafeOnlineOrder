using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProjectCafeDataAccess;
using ProjectCafeEntities;
using ProjectCafeWeb.ViewModels;
using System.Configuration;
using System.Net;
using WebPush;

namespace ProjectCafeWeb.Controllers
{
    public class NotificationController : BaseController
    {
        private readonly IConfiguration _configuration;
        public NotificationController(ProjectCafeDbContext dbContext, IConfiguration configuration) : base(dbContext)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<ActionResult> Subscribe([FromBody] NotificationSubscriptionViewModel model)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (!userId.HasValue)
                return Unauthorized();

            // Garsona ait aktif kayıt var mı kontrol et
            var existing = _dbContext.NotificationSubscription
                .FirstOrDefault(x => x.WorkerId == userId.Value && x.Active);

            if (existing == null)
            {
                // Kayıt yoksa yeni kayıt oluştur
                var subscription = new NotificationSubscription
                {
                    WorkerId = userId.Value,
                    Endpoint = model.Endpoint,
                    P256dh = model.P256dh,
                    Auth = model.Auth,
                    Active = true,
                    RegistrationUser = userId.Value,
                    RegistrationUserRole = userRole,
                    RegistrationDate = DateTime.Now,
                };

                _dbContext.NotificationSubscription.Add(subscription);
            }
            else
            {
                // Kayıt varsa ve güncel değilse güncelle
                if (existing.Endpoint != model.Endpoint || existing.P256dh != model.P256dh || existing.Auth != model.Auth)
                {
                    existing.Endpoint = model.Endpoint;
                    existing.P256dh = model.P256dh;
                    existing.Auth = model.Auth;
                    existing.CorrectionUser = userId.Value;
                    existing.CorrectionUserRole = userRole;
                    existing.CorrectionDate = DateTime.Now;
                }
                // Aksi halde zaten güncel, hiçbir şey yapma
            }

            await _dbContext.SaveChangesAsync();
            return Ok();
        }


        public ActionResult PublicKey()
        {
            var publicKey = _configuration["PushNotification:PublicKey"];
            return Content(publicKey);
        }
    }
}
