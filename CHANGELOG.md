## 0.2.0

- Events are now sent in batches to reduce network overhead
- Automatic flush of events when app loses focus
- While offline, events will be enqueue and sent when the app is back online
- Added an option to set the appVersion during init
- Replaced MiniJSON for TinyJSON for better serialization

## 0.0.1

- Initial release