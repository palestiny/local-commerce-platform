# Baladna Customer Mobile App

Initial customer-app foundation for Android and iOS using React Native, Expo SDK 57, TypeScript, and Expo Router.

## Current scope

- Home screen with local search over demo listings.
- Categories screen with category selection and local filtering.
- Listing detail screen.
- Favorites stored in in-memory app state.
- Orders empty state.
- Arabic-first UI with a language toggle to English.
- Shared color, spacing, and radius tokens.
- Clear notices that listing data, prices, ratings, and actions are demo-only.

## Run locally

Requirements: Node.js LTS and npm.

From the repository root:

```powershell
cd apps/customer-mobile
npm install
npx expo install --fix
npm run typecheck
npx expo start
```

For an Android phone, install a compatible Expo Go build and scan the QR code. If the installed Expo Go version does not support SDK 57, align the app SDK with the Expo Go version or use a development build. Do not silently upgrade individual Expo native packages outside the SDK compatibility set.

## Important limitations

- No backend/API integration is implemented.
- No real authentication, request submission, chat, booking, payment, delivery tracking, or push notifications.
- Demo listing names, ratings, review counts, distances, and prices are illustrative and are not verified real-world data.
- Favorites exist only in memory and reset when the app process restarts.
- Android/iOS builds, RTL behavior on physical devices, and TypeScript verification are **NOT VERIFIED** until run in a suitable local environment.

## Architecture boundary

UI screens call no backend directly. Future integration should go through a typed API adapter after the API/security contracts are approved. The backend remains authoritative for prices, authorization, order state, payment state, and delivery state.
