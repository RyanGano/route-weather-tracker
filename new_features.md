# New Features Plan — Route Weather Tracker

Date: 2026-03-21

Goal: add five achievable, travel-useful features (mix of obvious and non-obvious) that increase user value and create monetization paths where appropriate. Track implementation progress in this file and in the repo's issue tracker.

Overview of features

1. In-App Advertising & Affiliate Links — ✅ IMPLEMENTED (2026-03-21)

**Strategy:** Two-tier system — contextual affiliate card wins when a pass/route signal fires; Google AdSense fills when no match; nothing renders when disabled.

**Ad services to sign up for (your side):**

- Amazon Associates — https://affiliate-program.amazon.com (earn 3–8% on gear purchases)
- Booking.com Affiliate — https://www.booking.com/affiliate-program (earn ~4% on hotel bookings)
- Google AdSense — https://adsense.google.com (CPM/CPC fallback fill)

**Contextual targeting triggers (already live):**

- Chains required at any pass → Amazon tire chains link
- Below freezing at any pass → Amazon cold-weather gear link
- Snow in 3-day forecast → Amazon winter driving kit link
- High-elevation pass + snow → Amazon ski/outdoor gear link
- Destination city known → Booking.com hotel search for that city
- Trip > 3 hours → Booking.com lodging link
- No match → AdSense fallback (when unit ID configured)

**Files created:**

- `src/types/adTypes.ts` — `AffiliateOffer`, `AdConfig` types
- `src/utils/adContextUtils.ts` — `getContextualOffer()` and `getPassOffer()` targeting logic
- `src/contexts/AdContext.tsx` — `AdProvider` / `useAdConfig()` reading `VITE_*` env vars
- `src/components/ContextualOfferCard.tsx` — native affiliate card with dismissal and Amazon disclosure
- `src/components/AdSlot.tsx` — AdSense `<ins>` wrapper
- `src/components/AdBanner.tsx` — orchestrator component
- `.env.local.example` — documents all required env vars

**Files edited:**

- `App.tsx` — wrapped in `<AdProvider>`, `<AdBanner>` placed after `<RouteStatus>`
- `PassCard.tsx` — per-pass micro-offer link in travel restriction alert
- `.github/workflows/azure-dev.yml` — `VITE_*` vars now injected at build step (also fixes pre-existing `VITE_API_URL` bug)

**Remaining tasks (your side):**

- [ ] Add GitHub Actions Repository Variables: `VITE_ADS_ENABLED`, `VITE_AMAZON_ASSOCIATE_TAG`, `VITE_BOOKING_AFFILIATE_ID`, `VITE_ADSENSE_PUBLISHER_ID` (see `.env.local.example`)
- [ ] Add `VITE_ADSENSE_AD_UNIT_ID` once AdSense approves your site
- [ ] Add Amazon Associate disclosure and Privacy Policy page before going live

2. Premium Notifications & Route Alerts (Subscription)

- Summary: let users subscribe to route/pass alerts (closures, severe forecast changes) with simple preferences and delivery methods (in-app, email, push placeholder).
- Why: increases retention and creates subscription revenue potential.
- Implementation notes:
  - Frontend: `AlertsContext` for preferences and an Alerts UI (subscribe button on route page).
  - Backend: `AlertsController` + background worker to evaluate pass forecasts vs thresholds and enqueue notifications; simple subscription model flag in user profile.
  - Entry points: [route-weather-tracker-app/src/contexts](route-weather-tracker-app/src/contexts#L1), [route-weather-tracker-service/Program.cs](route-weather-tracker-service/Program.cs#L1)

3. Offline Route Pack & Weather Cache (Premium)

- Summary: generate downloadable route packs containing geometry, pass summaries, camera snapshot URLs, and short-term weather snapshots for offline viewing.
- Why: useful for travelers with limited connectivity and premium upsell.
- Implementation notes:
  - Backend: `/api/offline-pack?routeId=...` to produce a ZIP or JSON bundle built from `PassAggregatorService` outputs.
  - Frontend: UI to request download and local storage (IndexedDB); offline viewer page.
  - Entry points: [route-weather-tracker-service/Controllers/RoutesController.cs](route-weather-tracker-service/Controllers/RoutesController.cs#L1), [route-weather-tracker-app/src/components/RouteHeader.tsx](route-weather-tracker-app/src/components/RouteHeader.tsx#L1)

4. Community Photo Reports & Camera Uploads

- Summary: allow travelers to submit pass photos and short reports, show a moderated gallery per pass, enable upvotes and pin popular reports.
- Why: adds fresh user content, drives engagement, and enables promoted reports or sponsored listings.
- Implementation notes:
  - Backend: `ReportsController` for CRUD, storage adapter (local dev folder → S3 in prod), moderation flags.
  - Frontend: upload UI in `WebcamViewer` and `PassCard`, gallery component.
  - Entry points: [route-weather-tracker-app/src/components/WebcamViewer.tsx](route-weather-tracker-app/src/components/WebcamViewer.tsx#L1), [route-weather-tracker-service/Controllers/PassesController.cs](route-weather-tracker-service/Controllers/PassesController.cs#L1)

5. Predictive Weather-based ETA & Risk Score

- Summary: compute a pass-level Risk Score and adjusted ETA using forecasted conditions (precipitation, visibility), elevation, and travel restriction severity.
- Why: directly useful to travelers (safety), differentiator for premium analytics, and a gated premium PDF/export product.
- Implementation notes:
  - Backend: `RiskScoreService` that consumes `PassWeatherForecast`, `PassCondition`, and historical closure heuristics; endpoint `/api/routes/{id}/risk` or embed in `ComputedRoute` responses.
  - Frontend: show `RiskScore` on `PassCard` and `RouteStatus`, allow users to toggle detailed analysis (premium).
  - Entry points: [route-weather-tracker-service/Services/PassAggregatorService.cs](route-weather-tracker-service/Services/PassAggregatorService.cs#L1), [route-weather-tracker-app/src/components/PassCard.tsx](route-weather-tracker-app/src/components/PassCard.tsx#L1)

Implementation phasing & estimates (MVP scope)

- Phase 0 (1–2 days): Add this `new_features.md` to repo, create feature-flagging hook, and add a `todo` plan (done).
- Phase 1 (3–7 days): Implement Ads skeleton + `/api/offers` + feature flag; test in frontend behind flag.
- Phase 2 (5–10 days): Implement Reports API and upload UI; local storage for dev images.
- Phase 3 (7–14 days): Implement RiskScoreService and show on frontend; basic heuristics only.
- Phase 4 (7–14 days): Implement Offline Pack generator and basic Alerts subscription (email stub). Integrate payment provider for ad-free/premium (Stripe) as optional.

Security, privacy, and cost notes

- Ads and analytics must respect user privacy (consent and opt-out). Add a privacy notice before enabling ads.
- Photo storage needs quotas and moderation to avoid abuse; schedule TTL and storage lifecycle.
- Offline packs and images increase storage/bandwidth costs — design lifecycle policies.

Next actions (short):

1. (In-progress) Add `new_features.md` to repo (this file).
2. Create a feature-flag helper in frontend and toggle for `ads`.
3. Scaffold `AdSlot.tsx` and `/api/offers` endpoint.

If you want, I can proceed now and scaffold the frontend `AdSlot` component + backend `OffersController` (small API) as an initial implementation. Proceed with scaffold?
