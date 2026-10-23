# بلدنا — Product Vision & Service Ecosystem Proposal

**Status:** Proposal for owner review  
**Repository:** `palestiny/local-commerce-platform`  
**Design reference branch:** `design-reference-import`  
**Market starting point:** Dekernes / Dakahlia, Egypt  
**Languages:** Arabic and English  
**Currency:** EGP initially  
**Important:** This document records product direction and options. It does not approve every feature for immediate implementation, replace the existing API contract gates, or authorize production changes.

## 1. Product vision

Baladna is intended to become a comprehensive local-services and commerce platform that helps residents discover, compare, contact, book, buy from, and receive services from local providers through one coherent experience.

It should be open to useful services beyond the initial examples. The platform should be designed for expansion across service categories, providers, areas, languages, and eventually countries, while launching in manageable, measurable stages.

**Product principle:** Broad vision; modular architecture; phased delivery. A category can be a candidate without being promised for launch.

## 2. Proposed service ecosystem

### A. Local shopping and everyday needs
- Grocery stores, supermarkets, mini-markets
- Fruit and vegetable shops
- Butchers, poultry and fish shops
- Bakeries, sweets, cafés and restaurants
- Clothing, shoes, accessories and gifts
- Electronics, phones, appliances and repair accessories
- Home supplies, furniture and décor
- Books, stationery, toys and school supplies
- Flowers, plants and agricultural supplies where appropriate

### B. Food and goods delivery
- Store delivery and order tracking
- Scheduled delivery, where providers support it
- Pickup / collect-from-store
- Multi-provider delivery operations as a later capability
- Clear delivery fees, service areas, availability and cancellation rules

### C. Health and wellbeing
- Clinics, doctors, dental clinics and specialties
- Pharmacies and medical supplies, subject to applicable rules
- Labs, imaging centers and home sampling, where legally and operationally supported
- Physiotherapy, nutrition and non-emergency wellness services
- Appointment requests, opening hours, contact information and accessibility details
- Emergency information must not be represented as a substitute for official emergency services or medical advice

### D. Home and personal services
- Plumbing, electrical work, carpentry and maintenance
- Appliance, phone and computer repair
- Cleaning, pest control and moving services
- Beauty salons, barbers, skincare and personal care
- Tailoring, laundry and dry cleaning
- Photography, event services and other bookable local professionals

### E. Education and training
- Tutors and private lessons
- Learning centers, language courses and vocational training
- Online and in-person class discovery
- Course schedules, inquiries, booking or enrollment as a later domain
- Parent/student features only if the education module is approved and designed with privacy safeguards

### F. Transport and mobility
- Local delivery and courier services
- Moving and freight providers
- Vehicle maintenance, tires, batteries and spare parts
- Car wash and detailing
- Taxi/ride services only after regulatory, safety, insurance and operational review

### G. Professional and business services
- Accountants, legal professionals and business consultants
- Printing, advertising, design and IT services
- Employment and local opportunities, with moderation and fraud controls
- Business-to-business supplier discovery as a potential later module

### H. Community, leisure and lifestyle
- Gyms, sports clubs and activities
- Entertainment venues and family activities
- Events, workshops and local classes
- Tourism and nearby places of interest
- Community announcements and local news only with clear moderation, sourcing and editorial responsibility

### I. Public, civic and daily-life information
- Government offices and public-service directories
- Required documents, published procedures, official links and working hours
- Public utilities and service contact directories
- Local notices and service disruptions when information can be verified
- Never imply official affiliation or promise transaction completion unless an authorized integration exists

### J. Agriculture and local production (candidate for Dakahlia)
- Agricultural supply shops and equipment services
- Veterinary clinics and animal-care providers
- Agricultural technicians and machinery rental
- Local producers and farm products, subject to quality, traceability and applicable regulations
- Seasonal service discovery and wholesale connections as possible later capabilities

### K. Deals and discovery
- Discounts, bundles, seasonal offers and loyalty programs
- Ratings and reviews with abuse prevention
- Favorites, follow providers, recently viewed and recommendations
- Search by category, distance, availability, price and rating
- Map discovery when location access is granted
- Sponsored placements only with transparent labeling

## 3. Common platform capabilities

These are cross-cutting capabilities to evaluate across modules, not all immediate launch commitments:
- Customer accounts, profiles, addresses and preferences
- Provider onboarding, business profile and verification status
- Catalogs, service listings, pricing, availability and service areas
- Search, filtering, sorting and discovery
- Messaging/contact requests, with safety and privacy controls
- Appointment requests and booking lifecycle where relevant
- Cart, checkout, order lifecycle, cancellation and refund policies where relevant
- Delivery assignment, dispatch, tracking and proof of delivery where applicable
- Notifications and communication preferences
- Reviews, complaints, support and dispute handling
- Provider dashboard for listings, orders, bookings and performance
- Operations/admin console, moderation, audit trail and incident handling
- Localization, accessible UI, RTL/LTR, currency and regional configuration
- Analytics with privacy-conscious data collection
- Subscription/commission/promoted listing monetization, after business-model validation

## 4. Avoid forcing every service into an e-commerce order

The platform should distinguish at least these user journeys:
1. **Discover/contact:** directory listing, phone, official website or inquiry.
2. **Request a quote:** submit details and receive a provider response.
3. **Appointment/booking:** request or reserve a time slot.
4. **Product order:** cart, checkout, fulfillment and delivery/pickup.
5. **On-demand service request:** request a professional, availability and completion.
6. **Information-only public service:** verified guidance and official links.

A clinic appointment, grocery order, plumber request and government-office listing should not be forced into one identical order model. Shared platform primitives can be reused, while domain-specific rules remain explicit.

## 5. Suggested phased delivery (proposal, not approved commitment)

### Phase 0 — Product and design reconciliation
- Inventory all HTML/CSS reference files and assets.
- Group screens by journey and user role.
- Identify duplicates, inconsistencies, missing states and prototype-only data.
- Define a screen-to-domain-to-API traceability matrix.
- Close essential product and security/API design gates before production integration.

### Phase 1 — Useful local discovery
Candidate scope: customer onboarding, location/area selection, home, categories, search, provider/store profile, listing/product/service details, contact actions, favorites and basic provider onboarding/management.
Select exact categories and operational coverage before committing to launch.

### Phase 2 — One complete transactional journey
Choose one primary transactional journey for the pilot (for example, local store product order and delivery) and complete it end-to-end: availability, pricing, order creation, provider acceptance, status changes, cancellation, customer notifications and operational support.

### Phase 3 — Expand transaction types
Evaluate bookings, quote requests and home-service requests as distinct workflows. Add them only after their domain rules, provider operations and API contracts are designed.

### Phase 4 — Trust, retention and scale
Evaluate reviews, offers, loyalty, richer analytics, subscriptions/commissions, advanced discovery, additional service areas and mobile-specific capabilities.

Phases may be revised based on research, feasibility, pilot feedback and owner decisions.

## 6. Design review rules

For every supplied HTML/CSS screen, record:
- File path and screen name
- Intended actor and journey
- Keep / adapt / combine / defer / reject recommendation
- Reusable design-system components
- Missing loading, empty, error, offline, permission-denied and success states
- RTL/LTR, accessibility, responsive and mobile considerations
- Data required and its source of truth
- Candidate API/domain dependencies and unresolved contract questions
- Prototype-only content, unverified claims and security/privacy concerns

Visual fidelity alone is not acceptance. A screen is implementation-ready only when its behavior, states, data contract and authorization requirements are sufficiently defined.

## 7. Guardrails and risks

- Do not claim a provider, merchant, review, price, discount, stock count, delivery ETA, payment method or public-service integration is real merely because it appears in a mockup.
- Do not store or expose raw payment-card details. Payment integrations require a vetted provider and a separate security/compliance design.
- Health, education, finance, employment, government and location-related features may need extra privacy, safety, legal and operational review.
- Reviews, provider verification, listings and community content need moderation and abuse-reporting mechanisms.
- Location is optional; provide useful manual area selection and do not block all browsing when permission is denied.
- Keep customer, merchant, dispatcher, administrator and provider permissions server-enforced.
- Do not add every proposed domain to the first release or create empty abstractions without validated use cases.
- Do not implement production API routes based solely on a visual design; follow the existing API/security design gates.

## 8. Decisions still needed from the owner

1. Which exact categories and service areas are included in the first pilot?
2. Which journey is the first end-to-end transaction: store products and delivery, bookings, or another?
3. Will providers initially manage listings/orders themselves, or will operations staff help onboard and fulfill them?
4. Which contact/booking/payment flows are available at launch versus later?
5. What are the pilot's success measures (active providers, successful requests/orders, repeat usage, fulfillment reliability, customer satisfaction)?
6. Which verification, moderation and support processes are feasible for the pilot?

## 9. Current status

This document captures the broad product vision so the design review does not prematurely discard useful service ideas. It does not mean all categories are approved for immediate build. The next concrete artifact should be a complete inventory and screen/domain/API traceability matrix based on the actual files on `design-reference-import`.
