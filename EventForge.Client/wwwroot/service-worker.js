// Service worker di sviluppo: non effettua caching.
// Per il comportamento offline completo, vedere service-worker.published.js
// che viene usato automaticamente durante la pubblicazione.
// https://aka.ms/blazor-offline-considerations

self.addEventListener('fetch', () => { });
