![Aptabase](https://aptabase.com/og.png)

# Unity Engine SDK for Aptabase

Instrument your Unity Engine project with Aptabase, an Open Source, Privacy-First and Simple Analytics for Mobile, Desktop and Web Apps.

## Install

You can install the package via [Unity Package Manager](https://docs.unity3d.com/Manual/upm-ui.html).

Go to `Window` > `Package Manager` > `+` > `Add Package from git URL` and enter https://github.com/aptabase/aptabase-unity.git

## Project settings

First, you need to get your `App Key` from Aptabase, you can find it in the `Instructions` menu on the left side menu.

Then you have to set it inside the settings file located at `Aptabase/Reosources/AptabaseSettings.Asset` inside your `App Key` field.

Based on the key, your `Host` will be selected. In the case of self-hosted versions a new `SelfHostURL` field will appear for input.

App Version is automatically detected, but you can override it with the `AppVersion` field. You will need to provide a `AppBuildNumber` as it may vary across different platforms. This allows you to specify a platform-specific build number to ensure accurate version tracking and compatibility.

Events are batched and sent every 60 seconds in production and 2 seconds in development by default. You can override these values with the `FlushInterval` field by inputting desired time in milliseconds.

## Usage

The Aptabase SDK will seamlessly run in the background as soon as your app starts up. To effortlessly log events, you can use the following code snippet. The Props parameter is optional and can be left empty if not needed.

```csharp
Aptabase.TrackEvent("app_started", new Dictionary<string, object>
{
    {"hello", "world"}
});
```

If you want to manually flush the event queue you can use 
```csharp
Aptabase.Flush();
```

A few important notes:

1. The SDK will automatically enhance the event with some useful information, like the OS, the app version, and other things.
2. You're in control of what gets sent to Aptabase. This SDK does not automatically track any events, you need to record events manually.
   - Because of this, it's generally recommended to at least track an event at startup
3. You do not need to await the record event calls, they will run in the background.
4. Only strings and numbers values are allowed on custom properties

## Preparing for Submission to Apple App Store

When submitting your app to the Apple App Store, you'll need to fill out the `App Privacy` form. You can find all the answers on our [How to fill out the Apple App Privacy when using Aptabase](https://aptabase.com/docs/apple-app-privacy) guide.
