# Mobile Application Stack Design Gate

Status: **PROPOSED — OWNER DECISION REQUIRED**
Scope: Customer-facing Android and iOS application for Local Commerce Platform.
Branch basis: `feature/foundation-domain-architecture`.

## 1. Context

The project is a local-first commerce platform focused initially on Dekernes, Dakahlia, Egypt, with Arabic/English and EGP. The current repository documents an ASP.NET Core/.NET modular-monolith backend, PostgreSQL persistence, and React + TypeScript for web surfaces. It explicitly leaves the mobile stack to a Design Gate.

The owner has clarified that HTML/CSS examples (including the categories and chat screen files shared for review) are visual proposals, not a requirement to preserve their implementation. Product quality and long-term maintainability take priority over maximizing literal HTML/CSS reuse.

The HTML/CSS examples are static presentation references: their controls and sample data do not establish working mobile interactions, API contracts, or backend capabilities.

## 2. Problem

Choose a maintainable approach for a real Android and iOS app without prematurely binding the app to unfinished HTTP/authentication contracts or duplicating business rules from the backend.

## 3. Goals

- Ship one customer app codebase for Android and iOS where practical.
- Deliver a polished mobile-first experience with Arabic/English, RTL/LTR, accessibility, and varied device sizes.
- Reuse product-level assets and decisions from the existing HTML/CSS and Figma work where useful, without treating source markup as a constraint.
- Keep business rules and order authority on the backend.
- Make API access, authentication, error handling, telemetry, and testing consistent.
- Keep the architecture simple; do not introduce microservices or infrastructure solely for hypothetical scale.

## 4. Non-goals for this gate

- Selecting an authentication provider or implementing authentication.
- Approving public HTTP routes or bypassing the API/security gate.
- Implementing live chat, payments, maps, push notifications, or background tracking before their contracts and provider decisions are ready.
- Replacing or modifying existing HTML examples.
- Building merchant, driver, or admin applications as part of the first customer-app spike.
- Claiming App Store / Play Store readiness from a prototype.

## 5. Options

### Option A — React Native + Expo + TypeScript

**Description:** Build native mobile UI using React Native and Expo. Use TypeScript and a mobile-specific component layer. Keep web UI separate where platform primitives differ.

**Benefits**
- Mobile-first native controls and navigation patterns.
- One primary mobile codebase for Android and iOS.
- Strong fit for React/TypeScript familiarity.
- Expo can simplify local development, builds, and common device capabilities.
- Can share API types, validation schemas, domain-neutral utilities, and design tokens with a web client when intentionally structured.

**Costs / risks**
- Existing HTML and CSS cannot be copied verbatim; layouts and styles must be translated to React Native primitives.
- Native dependencies and device capabilities still need platform-specific testing.
- Web and mobile UI components will generally not be identical shared components.

**Fit:** Strong candidate when the quality of the mobile product matters more than literal HTML/CSS reuse.

### Option B — Ionic React + Capacitor

**Description:** Build the UI with React and web technologies, package it for Android and iOS through Capacitor.

**Benefits**
- Higher potential reuse of HTML/CSS and browser-oriented components.
- React/TypeScript web skills and tools transfer directly.
- Can be effective for form-heavy or web-like application surfaces.

**Costs / risks**
- The existing HTML still requires responsive behavior, componentization, state, accessibility, and real integration.
- A WebView-based UI may need additional care for platform-native feel, complex gestures, and performance-sensitive experiences.
- Native capabilities depend on Capacitor plugins and platform testing.

**Fit:** Viable if web-surface reuse becomes a strong business constraint; that constraint is not currently established.

### Option C — WebView wrapper around existing pages

**Description:** Package mostly static web pages inside a mobile shell.

**Benefits**
- Lowest initial conversion effort for a visual demo.

**Costs / risks**
- Does not by itself produce a complete, robust commerce app.
- Static pages remain static until interactions, state, network failures, authentication, and device integration are implemented.
- Higher risk of creating a prototype that looks complete but does not support the real customer journey.

**Fit:** Not recommended as the long-term production foundation. Could be used only for a disposable proof of concept.

## 6. Preliminary recommendation

**Recommend Option A (React Native + Expo + TypeScript) for the customer mobile app**, subject to a small implementation spike and owner approval.

Reasoning: the stated priority is the quality and sustainability of the product, not maximum reuse of HTML/CSS. The app is expected to be a first-class mobile experience. Option B remains a reasonable alternative if the spike uncovers a concrete requirement that materially favors web-view reuse.

This is a proposal, not an accepted ADR. No mobile stack is considered decided until the owner approves it and the decision is recorded in the decision log.

## 7. Proposed proof-of-concept

Create a disposable or focused spike containing:

1. Categories screen translated from `categories-screen.html`.
2. Conversation list and conversation detail layouts translated from `chat-screen.html`, with clearly labelled mock data.
3. Shared design tokens for color, typography, spacing, radii, and semantic states.
4. Arabic RTL and English LTR switching demonstration.
5. Navigation between the two screens.
6. Loading, empty, error, and retry presentation states using mock data.
7. Android and iOS layout verification at representative screen sizes.

The spike must not imply that sending messages, live presence, order tracking, or backend integration works. Those capabilities require their own product/API contracts.

## 8. Acceptance criteria for the spike

- Both screens render without clipping at the tested phone sizes.
- RTL Arabic and LTR English are explicitly checked; text direction is not assumed to be solved by setting one global flag.
- Typography, touch targets, safe areas, scrolling, keyboard behavior, and navigation are reviewed.
- Mock/loading/empty/error states are visibly distinguishable.
- A documented comparison records implementation effort, visual fidelity, accessibility, platform behavior, dependency risk, and maintainability.
- The app can be built and launched on at least one Android target and one iOS target or simulator where the environment permits; any unavailable test environment is recorded as NOT VERIFIED rather than inferred.
- No production API/authentication implementation is introduced before the API/security gate permits it.
- The spike outcome and remaining limitations are documented.

## 9. Dependencies and boundaries

- API/security contracts and authentication remain a separate blocking gate for production integration.
- Use versioned API DTOs/contracts; do not mirror backend domain entities as mobile UI models.
- Backend remains the authority for price, order state, payment state, delivery state, authorization, and idempotency.
- Do not store secrets in the app. Treat the mobile client as an untrusted public client.
- Design system assets may be shared as tokens, but React Native and web presentation components should only be shared where this is technically sound.
- Push notifications, deep links, camera/photo access, location, maps, and payment-provider integration require separate capability-specific decisions and verification.

## 10. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Static mock screens are mistaken for implemented features | Label mock data and list non-functional behaviors explicitly |
| UI is coupled to unfinished API shapes | Use typed adapter boundary and wait for approved contracts |
| Arabic layout appears correct only on one device | Test RTL, long text, system font scaling, safe areas, and multiple screen widths |
| Mobile and web logic diverge | Keep business rules on the backend; share only stable neutral contracts/utilities |
| Provider or native dependency becomes difficult to replace | Isolate platform capability adapters and document dependency choices |
| Prototype grows into production without gates | Define spike exit criteria and explicitly review before productionization |

## 11. Decision required

Owner to choose one:
- **A — Approve React Native + Expo + TypeScript**, proceed to the focused spike.
- **B — Prefer Ionic React + Capacitor**, because web implementation reuse is strategically important.
- **C — Request a comparative spike before choosing.**

Default recommendation: **A**, with the acceptance criteria above.

## 12. Verification status

- Repository architecture and current decision log reviewed on `feature/foundation-domain-architecture`.
- The existing project constitution requires a Design Gate and documented owner decision before a significant architecture choice.
- This document is proposal-only; no mobile application code, authentication provider, or public HTTP endpoint is implemented by this change.
- Android/iOS behavior and buildability are **NOT VERIFIED** until the spike is executed in suitable environments.
