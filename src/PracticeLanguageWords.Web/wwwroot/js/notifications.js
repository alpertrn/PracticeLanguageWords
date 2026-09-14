// Siteye giren kullanicidan bildirim izni ister, izin verilirse tarayiciyi push
// bildirimlerine abone eder ve aboneligi sunucuya kaydeder. Yalnizca giris yapmis
// kullanicilar icin _Layout.cshtml tarafindan dahil edilir.
(function () {
    'use strict';

    if (!('serviceWorker' in navigator) || !('PushManager' in window) || !('Notification' in window)) {
        return; // Tarayici desteklemiyor (eski tarayici, bazi mobil WebView'lar) - sessizce cik.
    }

    const publicKeyMeta = document.querySelector('meta[name="vapid-public-key"]');
    const publicKey = publicKeyMeta ? publicKeyMeta.content : '';

    if (!publicKey) {
        return; // Sunucu tarafinda VAPID anahtari henuz tanimlanmamis.
    }

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');

    // Sayfa yuklenir yuklenmez calisir; izin zaten verilmisse tekrar sormaz (tarayici
    // Notification.requestPermission'i 'default' disindaki durumlarda hemen cozer).
    registerAndSubscribe();

    async function registerAndSubscribe() {
        try {
            const registration = await navigator.serviceWorker.register('/sw.js');

            if (Notification.permission === 'denied') {
                return; // Kullanici daha once reddetmis - tarayici zaten tekrar sormamiza izin vermez.
            }

            if (Notification.permission === 'default') {
                const permission = await Notification.requestPermission();
                if (permission !== 'granted') {
                    return;
                }
            }

            // Buraya geldiysek permission 'granted'. Abonelik daha once olusturulmus
            // olabilir (ayni tarayici, farkli oturum) - varsa oldugu gibi kullanilir,
            // yoksa yeni olusturulur; her iki durumda da sunucuya kaydedilir (idempotent).
            let subscription = await registration.pushManager.getSubscription();

            if (!subscription) {
                subscription = await registration.pushManager.subscribe({
                    userVisibleOnly: true,
                    applicationServerKey: urlBase64ToUint8Array(publicKey)
                });
            }

            await sendSubscriptionToServer(subscription);
        } catch (error) {
            // Bildirim ozelligi sitenin ana islevi degil - hata olursa sessizce yut,
            // kullaniciyi engelleyici bir uyariyla rahatsiz etmeye gerek yok.
            console.warn('Bildirim aboneligi kurulamadi:', error);
        }
    }

    async function sendSubscriptionToServer(subscription) {
        const json = subscription.toJSON();

        const headers = { 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest' };
        if (tokenInput) {
            headers['X-CSRF-TOKEN'] = tokenInput.value;
        }

        await fetch('/api/notifications/subscribe', {
            method: 'POST',
            headers: headers,
            body: JSON.stringify({
                endpoint: json.endpoint,
                p256dh: json.keys.p256dh,
                auth: json.keys.auth
            })
        });
    }

    // VAPID public key base64url string -> Uint8Array (PushManager.subscribe'in beklegi format).
    function urlBase64ToUint8Array(base64String) {
        const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
        const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);

        for (let i = 0; i < rawData.length; i++) {
            outputArray[i] = rawData.charCodeAt(i);
        }

        return outputArray;
    }
})();
