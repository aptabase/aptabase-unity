## 0.3.1

- Fix `AptabaseSettings.asset` not being created on install when the project contains another asset whose type is also named `Settings` (#17). The importer now verifies a found asset actually is the Aptabase `Settings` type before skipping creation.

## 0.3.0

- Add `Aptabase.TrackError(Exception, fatal)` for reporting handled errors and crashes as structured error reports (error type, message, stack trace, severity, kind)
- Add optional automatic crash reporting via the `EnableCrashReporting` setting (uncaught exceptions logged by Unity, process-terminating exceptions and unobserved Task exceptions)
- Report only the first occurrence of each unique error per session, so a throwing `Update()` doesn't flood the error quota
- Include `isDebug` in error reports so Editor/development-build errors are kept separate from production data server-side
- `Aptabase.SetEnabled(false)` also drops error reports
- Fix `Aptabase.Flush()` throwing when the SDK failed to initialize
- Abort and dispose web requests when the `CancellationToken` fires, instead of leaving them running and re-sending the batch later
- Align the reported SDK version with the package version
- Document the `AptabaseSDK` namespace in the README samples (#8)
- The `DEV` region host now points to `https://localhost:3000`, matching the local backend's HTTPS endpoint; https requests to a loopback address trust its self-signed development certificate

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