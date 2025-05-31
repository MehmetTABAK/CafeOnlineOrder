self.addEventListener('push', function (event) {
    const data = event.data.json();
    const options = {
        body: data.body,
        icon: '/images/bell.png', // Bildirim simgesi
        badge: '/images/badge.png' // Küçük rozet (isteğe bağlı)
    };

    event.waitUntil(
        self.registration.showNotification(data.title, options)
    );
});
