// HaveASeat Service Worker - PWA + Push Notifications
const CACHE_NAME = 'haveaseat-v1';
const STATIC_ASSETS = [
    '/',
    '/css/site.css',
    '/favicon.ico'
];

// Install - cache risorse statiche
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(STATIC_ASSETS))
            .then(() => self.skipWaiting())
    );
});

// Activate - pulizia cache vecchie
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(
                keys.filter(key => key !== CACHE_NAME)
                    .map(key => caches.delete(key))
            )
        ).then(() => self.clients.claim())
    );
});

// Fetch - network first, cache fallback
self.addEventListener('fetch', event => {
    // Skip non-GET requests
    if (event.request.method !== 'GET') return;

    // Skip API calls and dynamic content
    const url = new URL(event.request.url);
    if (url.pathname.startsWith('/api/') ||
        url.pathname.startsWith('/Search/') ||
        url.pathname.startsWith('/Partner/')) {
        return;
    }

    event.respondWith(
        fetch(event.request)
            .then(response => {
                // Cache successful responses for static assets
                if (response.status === 200 && STATIC_ASSETS.some(a => url.pathname === a)) {
                    const responseClone = response.clone();
                    caches.open(CACHE_NAME).then(cache => cache.put(event.request, responseClone));
                }
                return response;
            })
            .catch(() => caches.match(event.request))
    );
});

// Push notification received
self.addEventListener('push', event => {
    let data = { title: 'HaveASeat', body: 'Hai una nuova notifica' };

    if (event.data) {
        try {
            data = event.data.json();
        } catch (e) {
            data.body = event.data.text();
        }
    }

    const options = {
        body: data.body || data.message || 'Nuova notifica',
        icon: '/favicon.ico',
        badge: '/favicon.ico',
        tag: data.tag || 'haveaseat-notification',
        data: {
            url: data.url || '/'
        },
        actions: data.actions || [],
        requireInteraction: data.requireInteraction || false
    };

    event.waitUntil(
        self.registration.showNotification(data.title || 'HaveASeat', options)
    );
});

// Click sulla notifica
self.addEventListener('notificationclick', event => {
    event.notification.close();

    const url = event.notification.data?.url || '/';

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then(windowClients => {
                // Se c'è già una finestra aperta, focus su quella
                for (const client of windowClients) {
                    if (client.url.includes(self.location.origin) && 'focus' in client) {
                        client.navigate(url);
                        return client.focus();
                    }
                }
                // Altrimenti apri una nuova finestra
                return clients.openWindow(url);
            })
    );
});
