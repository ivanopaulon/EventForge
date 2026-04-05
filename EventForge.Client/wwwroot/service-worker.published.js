// Service worker di produzione con strategia Cache-first.
// Questo file sostituisce service-worker.js al momento della pubblicazione.
// https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');

self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;

// Tipi di asset da includere nella cache offline (app shell Blazor WASM)
const offlineAssetsInclude = [
    /\.dll$/,
    /\.pdb$/,
    /\.wasm/,
    /\.html$/,
    /\.js$/,
    /\.json$/,
    /\.css$/,
    /\.woff$/,
    /\.woff2$/,
    /\.png$/,
    /\.jpe?g$/,
    /\.gif$/,
    /\.ico$/,
    /\.svg$/,
    /\.blat$/,
    /\.dat$/,
    /\.webmanifest$/
];

// Asset da escludere dalla cache (il service worker stesso)
const offlineAssetsExclude = [
    /^service-worker\.js$/
];

async function onInstall(event) {
    console.info('Service worker: Install');

    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));

    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Rimuove le versioni precedenti della cache
    const cacheKeys = await caches.keys();
    await Promise.all(
        cacheKeys
            .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
            .map(key => caches.delete(key))
    );
}

async function onFetch(event) {
    let cachedResponse = null;

    if (event.request.method === 'GET') {
        // Per le richieste di navigazione SPA, serve sempre index.html dalla cache
        const shouldServeIndexHtml = event.request.mode === 'navigate'
            && !event.request.url.includes('/api/')
            && !event.request.url.includes('/hubs/');

        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }

    // Ritorna la risposta dalla cache se disponibile, altrimenti va in rete
    return cachedResponse || fetch(event.request);
}
