# 🍓 Focusberry

A cute, local-first ADHD planning app that helps you track tasks, energy, medication, and patterns — all in your browser.

## Features

- 📋 **Master Task List** with priority and scheduling
- 🎯 **Big 3** daily focus system
- 🚬 **Cigarette & Medication Tracking** with streaks
- 🧠 **Brain Dump** for random thoughts
- 📊 **Weekly Analytics** with insights
- 🧪 **Experiments** tracker (14-day auto-review)
- 📅 **Week Planner** with full edit/delete for past days
- 🔐 **Google sign-in** with OAuth 2.0 / OpenID Connect
- ⏱️ **Deep Work Timer** (50-min Pomodoro sprint)
- 🔔 **Browser Notifications** for reminders
- 💾 **Manual Device Sync** with backup codes

## Quick Start

1. Open `adhd_os.html` in your browser, or host the folder for PWA features
2. Start logging today's energy, focus, and tasks
3. Click any day in the week view to edit past days
4. Configure Google sign-in in Settings, then sign in with your Google account

## Configure Google OAuth 2.0 / OpenID Connect

Focusberry uses [Google Identity Services](https://developers.google.com/identity/gsi/web) in the browser. The current implementation is a local-first sign-in experience: it identifies the Google account in this browser, but it does not sync Focusberry data to a server.

### Create a Google web client ID

1. Open the [Google Cloud Console](https://console.cloud.google.com/).
2. Create or select a project.
3. Configure the OAuth consent screen. If the app is external and still in testing, add the Google accounts that should be allowed to sign in as test users.
4. Under **Credentials**, choose **Create credentials → OAuth client ID**.
5. Select **Web application**.
6. Add every development or production origin under **Authorized JavaScript origins**, for example:
   - `http://localhost:8000`
   - `https://your-domain.example`
7. Copy the client ID ending in `.apps.googleusercontent.com`.

This browser-button flow does not use a redirect URI. Do not create or distribute a client secret for this static app. A Google client ID is public configuration, while a client secret must stay on a server.

### Enable sign-in in Focusberry

1. Host the folder over HTTP or HTTPS. Opening `adhd_os.html` as a `file://` URL will not satisfy Google’s origin checks or enable the PWA service worker. For a quick local server, run `python3 -m http.server 8000` from this folder.
2. Open the app and go to **Settings → Google sign-in**.
3. Paste the web client ID and choose **Save client ID**.
4. Use the Google button in the header or Settings to sign in.

The client ID is stored in `localStorage` under `focusberry_google_client_id`. On a successful callback, Focusberry decodes the returned OpenID Connect ID token and checks its issuer, audience, and expiry before storing only the account subject, name, email, picture URL, audience, and email-verification flag under `focusberry_google_profile`. The raw JWT is not stored.

### Security boundary

The browser-side checks make the UI resilient to malformed callbacks; they are not a substitute for server-side token verification because a static page cannot safely verify Google’s JWT signature or create a protected server session. If Focusberry later gets a backend, send the credential to that backend over HTTPS and verify the signature with Google’s current keys plus `iss`, `aud`, `exp`, `email_verified`, and an application-generated `nonce`/state as appropriate. Never authorize API access from decoded browser claims alone, and never put a Google client secret in this repository.

Google Calendar access is a separate feature from identity sign-in. It would require explicit Calendar scopes, a consent flow, secure token storage, and a backend or serverless token exchange; this repository does not request Calendar permissions.
