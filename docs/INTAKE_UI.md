# Intake UI Plan

This document defines the direction, structure, boundaries, and delivery expectations for the **Holmes Intake UI**. It
supersedes prior PWA-oriented assumptions and reflects the final architectural decision: **the Intake experience is a
lightweight, mobile-first SPA with a sub‑60‑second completion goal and no offline/PWA features**.

---

# 1. Purpose

The Intake UI exists to guide a subject through a short, tightly-scoped workflow:

1. Open invite link
2. Verify identity (OTP)
3. Review disclosure
4. Provide authorization
5. Provide minimal required data
6. Submit
7. Exit

The experience must be:

* fast
* mobile-first
* distraction-free
* ephemeral
* secure

**The user does not return to this application.** No onboarding, no dashboards, no sessions longer than a minute.

---

# 2. High-Level Principles

### ✔️ 2.1 Keep It Fast

Every decision prioritizes friction reduction:

* zero navigation complexity
* minimal fields
* instant transitions
* predictable behavior on slow connections

### ✔️ 2.2 Keep It Disposable

The Intake UI is not an ongoing touchpoint.

* No account creation
* No long-lived saved progress (only crash-recovery drafts with TTL)
* No multi-step return journeys
* No install or re-engagement features

### ✔️ 2.3 Keep It Isolated

The Intake UI **must not** leak concepts, components, or layout patterns from the Internal SPA.

### ✔️ 2.4 Keep It Secure

* The invite link is effectively a one-time bearer token.
* All state is server-backed.
* Local caching minimized (optional safeguard only).

---

# 3. Architecture Overview

```
src/
  Holmes.Internal/        # Internal SPA (existing)
  Holmes.Intake/          # Intake UI (new, lightweight SPA)
  Holmes.App.Server/      # APIs, Workflow, Intake, SSE (main backend)
  Holmes.Intake.Server/   # Static file host for Intake SPA
```

### 3.1 Holmes.IntakeSessions SPA

* React + Vite
* Mobile-first layout
* Very small component surface
* One primary route with internal steps (wizard-style state machine)
* No service worker
* No manifest.json
* No PWA prompts
* Minimal asset footprint

### 3.2 Holmes.Intake.Server (optional)

* Simple ASP.NET Core static host (or Nginx/static bucket if preferred later)
* Should **not** expose OIDC or any admin infrastructure
* Handles fallback route for SPA (`index.html`)

### 3.3 Shared UI Core

* Intake and Internal share a thin `ui-core` layer (tokens, typography, form primitives, OTP control, disclosure viewer,
  wizard shell, fetch/error helpers).
* Layout shells, navigation, and role concepts remain isolated to each app.

---

# 4. Data Flow

```
User → Holmes.IntakeSessions → Holmes.App.Server → Intake / Workflow modules → Projections / Events
```

### 4.1 Authentication / Identification

The client authenticates by **presenting the invite token**.

* Token validated server-side
* Session created implicitly
* All additional frontend state short-lived

### 4.2 Reads

* GET /api/intake/session/{id}
* GET /api/policy/snapshot/{id}
* GET /api/subjects/{id} (lightweight subset)

### 4.3 Writes

* POST /api/intake/verify-otp
* POST /api/intake/update
* POST /api/intake/submit

### 4.4 Completion

Submission path:

* Client posts submit payload with invite token.
* Server returns **synchronous success** with final status + evidence hashes (or error payload on failure).
* SPA renders Success on the response and wipes local draft; no SSE/poll required for completion (SSE remains available
  for order/timeline updates elsewhere).

Events emitted:

* `IntakeSubmitted`
* `OrderReadyForRouting` (via gateway)

---

# 5. Screens & Micro-Flows

## 5.1 Welcome

* Minimal branding
* Explain purpose in one sentence
* “Continue” → OTP screen

## 5.2 OTP Verification

* Input for code
* Error states: expired, wrong, too many attempts
* On success load subject + policy data

## 5.3 Disclosure

* Show disclosure content only (HTML/PDF)
* No checkbox or authorization language
* “Continue” → Authorization

## 5.4 Authorization

* Short authorization statement
* Unchecked checkbox + timestamp
* “Authorize & continue” → Data Entry

## 5.5 Data Entry

* Minimal fields only
* Real-time validation
* Auto-format phone/email where possible

## 5.6 Review

* Display entered data
* Highlight required corrections
* “Submit”

## 5.7 Success

* “You’re all set. You may close this screen.”
* No next steps, no navigation

---

# 6. UI/UX Priorities

### 6.1 Mobile First

* Single column layout
* Large tap targets
* Native-like field behavior
* Safe-area padding for iOS

### 6.2 Fast Visual Feedback

* Inline validation
* Instant transitions
* Skeleton screens for async loads

### 6.3 Accessibility

* WCAG 2.1 AA
* Aria-labels
* Focus management
* Keyboard-friendly (for compliance)

### 6.4 Performance

* No large libraries
* Aggressive tree-shaking
* Lazy-load steps where possible

### 6.5 Evidence Capture

* Authorization step must record checkbox, timestamp, IP/UA context, and the disclosure version/hash surfaced by the server.
* Review step surfaces what will be stored; submission wipes local draft data.

---

# 7. Technical Guardrails

### 7.1 Do Not Persist Sensitive Data Long-Term

* No IndexedDB; no service worker caches.
* Allow **short-lived `localStorage` drafts** per invite/session for crash-recovery only:
    - Minimal footprint (only fields the user typed).
    - Per-invite key; wipe on submit, abandon, or expiry (e.g., 2h TTL).
    - Prefer encryption-at-rest if we add a lightweight crypto helper; otherwise keep data minimal and documented.

### 7.2 Do Not Reuse Internal Components

The Internal SPA and Intake SPA serve different audiences and must remain logically separate. Only shared `ui-core`
primitives are reused.

### 7.3 No Routing Beyond the Wizard

Keep it linear:

* `/` → internal step machine
* No deep navigation

### 7.4 Maintain Strict Isolation

The Intake SPA must:

* not load admin APIs
* not require admin auth
* not expose internal concepts (roles, customers, ACL, etc.)

---

# 8. Future Extensions (Phase 2+ Options)

These are deliberately **not** part of the initial build, but good to anticipate:

* Add optional resume tokens if abandonment becomes a real problem
* Add optional “document upload” step (requires WORM artifact store)
* Integrate analytics (page timing, drop-off)
* Implement language toggles or localization
* Expand validation rules using customer policy overlays

---

# 9. Definition of Done

The Intake UI is considered complete when:

1. A subject can complete the flow in under **60 seconds**.
2. OTP → Disclosure → Authorization → Data → Review → Submit works on:

    * iPhone Safari
    * Android Chrome
    * Desktop (for debugging)
3. All steps match WCAG 2.1 AA expectations.
4. All backend interactions match the APIs defined in `docs/PHASE_2.md`; submit returns synchronous success/error
   without
   hanging the UI.
5. Draft persistence follows guardrails (per-invite key, TTL, wiped on submit/abandon; minimal PII).
6. Authorization step captures evidence (checkbox, timestamp, IP/UA, disclosure version/hash) surfaced by the server.
7. No service worker, no caching beyond the guarded draft storage.
8. Build output is less than ~200 KB compressed.
9. `Holmes.Intake.Server` can serve the SPA standalone.

---

# 10. Next Steps

1. Scaffold the Vite project under `src/Holmes.Intake/`.
2. Add a minimal wizard-state machine.
3. Implement the 5–6 screens.
4. Hook calls to Intake/Workflow endpoints.
5. Add mobile layout tuning.
6. Run through end-to-end staging test.

---

This plan is intentionally concise, low-risk, and optimized for the Intake experience described in Phase 2.
It aligns with the short-lived, single-purpose nature of the subject-facing workflow and avoids unnecessary PWA
complexity.
