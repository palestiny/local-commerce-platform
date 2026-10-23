# Customer Mobile Foundation — Implementation Record

Status: **IMPLEMENTED IN A FEATURE BRANCH — RUNTIME VERIFICATION PENDING**

Branch: `feature/customer-mobile-foundation`

## Owner direction

The owner approved moving from static HTML/CSS screen proposals toward a real Android/iOS customer mobile app. Existing HTML/CSS files remain unchanged as design references. The initial product direction is a broad local-services and commerce platform, not a single service category.

## Technology choice for this implementation

- React Native
- Expo SDK 57
- TypeScript
- Expo Router
- SDK-aligned dependencies, with `npx expo install --fix` recommended after install

SDK 57 is used for this first runnable spike because the current Expo documentation states that SDK 58 was released on 2026-10-15 but its Expo Go store release is still pending. This choice is for development accessibility, not a claim that SDK 57 is the newest SDK.

## Implemented initial surfaces

- Home with search over local demo data.
- Category discovery and local category filtering.
- Listing details.
- Favorites in in-memory state.
- Orders empty state.
- Arabic-first content and an Arabic/English UI toggle.
- Shared color, spacing, and radius tokens.
- Explicit demo-data and non-production-action disclaimers.

## Explicitly not implemented

- Backend/API integration.
- Authentication or authorization.
- Real service request/order creation.
- Payments, chat, booking, delivery tracking, push notifications, or persistent favorites.
- Verified business listings, ratings, reviews, prices, and distances.

The demo catalog is illustrative and must not be represented as live business data.

## Architecture boundaries

- Mobile UI must not become authoritative for prices, permissions, order state, payment state, or delivery state.
- Future API access must go through a typed adapter and only use approved versioned contracts.
- Do not add an authentication provider or public API endpoint as part of this UI foundation.
- Merchant, driver, and administration apps remain out of this first customer-app scope.

## Acceptance and verification status

| Check | Status |
|---|---|
| Dedicated feature branch created | PASS |
| Initial mobile source/configuration added | PASS |
| Original HTML/CSS references preserved | PASS |
| No production API/auth endpoint added | PASS |
| Dependency installation | NOT VERIFIED |
| TypeScript check | NOT VERIFIED |
| Expo doctor | NOT VERIFIED |
| Android launch on a device/emulator | NOT VERIFIED |
| iOS launch on a simulator/device | NOT VERIFIED |
| RTL/LTR layout on physical devices | NOT VERIFIED |

## Local verification

From repository root on Windows PowerShell:

```powershell
cd apps/customer-mobile
npm install
npx expo install --fix
npm run typecheck
npx expo-doctor
npx expo start
```

Run on Android with a compatible Expo Go build or a development build. If the installed Expo Go does not support the selected SDK, align the app SDK with the installed client or use a development build; do not report a successful device test until it has actually run.

## Next engineering gate

1. Install dependencies and run typecheck/Expo diagnostics.
2. Fix every reproducible compile/runtime defect.
3. Review visual hierarchy and RTL/LTR behavior on an actual phone.
4. Translate remaining key screens from the HTML/CSS reference set into native screens.
5. Separately close API/security and authentication contracts before real request integration.
