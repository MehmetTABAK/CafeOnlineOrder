using Newtonsoft.Json;
using ProjectCafeDataAccess;
using System.Linq;
using WebPush;

namespace ProjectCafeWeb.Services
{
    public class NotificationService
    {
        private readonly ProjectCafeDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public NotificationService(ProjectCafeDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task SendPushToGarsons(Guid cafeId, string title, string message)
        {
            var garsonSubs = _dbContext.NotificationSubscription
                .Where(x => x.Worker.CafeId == cafeId && x.Active)
                .ToList();

            var publicKey = _configuration["PushNotification:PublicKey"];
            var privateKey = _configuration["PushNotification:PrivateKey"];

            var vapidDetails = new VapidDetails(
                "mailto:mail@example.com",
                publicKey,
                privateKey
            );

            var webPushClient = new WebPushClient();

            foreach (var sub in garsonSubs)
            {
                var pushSubscription = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                var payload = JsonConvert.SerializeObject(new { title = title, body = message });

                try
                {
                    await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                }
                catch
                {
                    // loglama yapılabilir
                }
            }
        }
    }
}
