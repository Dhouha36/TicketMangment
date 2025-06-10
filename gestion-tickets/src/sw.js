/* src/sw.js */
self.addEventListener('install', event => {
  // On active immédiatement le SW
  self.skipWaiting();
});

self.addEventListener('activate', event => {
  // On prend le contrôle immédiatement
  event.waitUntil(self.clients.claim());
});

self.addEventListener('push', event => {
  console.log('[SW] Push reçu :', event);
  const data = event.data?.json() || {};
  console.log('[SW] Payload :', data);
  const title = data.title || data.body || 'Nouvelle notification';
  const options = {
    body:    data.body || data.message || '',
    tag: `notif-${data.id}`,
    renotify: true,
    icon:    '/assets/icons/icon-192.png',
    badge:   '/assets/icons/badge-72.png',
    data:    data.url || '/',
    vibrate: [100, 50, 100],
  };
  console.log('Notification.permission =', Notification.permission);
  event.waitUntil(
    self.registration.showNotification(title, options)
      .then(() => console.log('[SW] Notification affichée'))
      .catch(err => console.error('[SW] Erreur showNotification :', err))
  );
});

self.addEventListener('notificationclick', event => {
  event.notification.close();
  const url = event.notification.data;

  event.waitUntil(
    clients.matchAll({ type: 'window' }).then(list => {
      for (const win of list) {
        if (win.url === url) {
          return win.focus();
        }
      }
      return clients.openWindow(url);
    })
  );
});
