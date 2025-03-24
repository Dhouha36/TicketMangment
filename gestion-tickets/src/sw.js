// sw.js
self.addEventListener('push', event => {
  let data = {};
  if (event.data) {
    data = event.data.json();
  }
  const title = data.title || 'Notification';
  const options = {
    body: data.body,
    icon: data.icon || '/assets/icons/icon-72x72.png'
  };
  event.waitUntil(self.registration.showNotification(title, options));
});
