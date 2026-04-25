const CACHE_NAME = 'pm-operator-v1';
const STATIC_ASSETS = [
    '/',
    '/app.css',
    '/ProcessManager.Web.styles.css',
    '/_framework/blazor.web.js'
];

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => cache.addAll(STATIC_ASSETS))
    );
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k)))
        )
    );
    self.clients.claim();
});

self.addEventListener('fetch', (event) => {
    const url = new URL(event.request.url);

    if (event.request.method !== 'GET') return;
    if (url.pathname.startsWith('/api/')) return;
    if (url.pathname.startsWith('/_blazor')) return;

    event.respondWith(
        fetch(event.request).then(response => {
            if (response.ok && (url.pathname.endsWith('.css') || url.pathname.endsWith('.js') ||
                url.pathname.endsWith('.woff2') || url.pathname.endsWith('.png') || url.pathname.endsWith('.ico'))) {
                const clone = response.clone();
                caches.open(CACHE_NAME).then(cache => cache.put(event.request, clone));
            }
            return response;
        }).catch(() => caches.match(event.request))
    );
});
