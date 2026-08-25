## 0.2.6
- Added support for setting a "ResponseListener" via Aptabase.SetResponseListener. This allows you to receive callbacks with the HttpStatusCode for each event sent
- Added support for enabling or disabling the SDK via Aptabase.SetEnabled. This starts/stops polling as well.

## 0.2.5
- Reduced memory allocations by utilizing ListPool and DictionaryPool
- Flush method properly returns a cancellable Task
- CancellationToken handled all the way through to the WebRequest
- Improved error handling and logging

## 0.2.4

- Fixed memory leak with event errors
- Fixed event send error handling
- Reduced logging on errors

## 0.2.3

- Use new session id format

## 0.2.2

- Fixed issue with settings importer

## 0.2.1

- WebGL build handling
- WebRequest helper added

## 0.2.0

- Events are now sent in batches to reduce network overhead
- Automatic flush of events when app loses focus
- While offline, events will be enqueue and sent when the app is back online
- Added an option to set the appVersion during initialization
- Replaced MiniJSON for TinyJSON for better serialization
- Fixed issue with OS version

## 0.0.1

- Initial release