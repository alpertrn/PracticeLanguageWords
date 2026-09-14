// PracticeLanguageWords service worker.
// wwwroot kokunde durmasi onemli: bir service worker yalnizca kendi konumu ve altindaki
// yollari kontrol edebilir (scope), /css/js/sw.js gibi bir alt klasorde olsaydi tum siteyi
// degil sadece o klasoru kapsardi.
//
// Bu dosyanin tek isi: sunucudan gelen push bildirimini gostermek ve tiklaninca
// uygulamaya donmek. Cache/offline calisma (PWA) bilincli olarak kapsam disi tutuldu.

self.addEventListener('push', function (event) {
    var data = {};

    try {
        data = event.data ? event.data.json() : {};
    } catch (e) {
        data = { title: 'PracticeLanguageWords', body: event.data ? event.data.text() : '' };
    }

    var title = data.title || 'PracticeLanguageWords';
    var options = {
        body: data.body || '',
        data: { url: data.url || '/' }
    };

    event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    var url = (event.notification.data && event.notification.data.url) || '/';

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (windowClients) {
            for (var i = 0; i < windowClients.length; i++) {
                var client = windowClients[i];
                if (client.url.indexOf(self.location.origin) === 0 && 'focus' in client) {
                    if ('navigate' in client) {
                        try { client.navigate(url); } catch (e) { /* eski tarayici - sadece odaklan */ }
                    }
                    return client.focus();
                }
            }

            if (clients.openWindow) {
                return clients.openWindow(url);
            }
        })
    );
});
