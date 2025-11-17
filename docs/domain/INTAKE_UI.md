# Intake UI Plan

This document defines the direction, structure, boundaries, and delivery expectations for the **Holmes Intake UI**. It supersedes prior PWA-oriented assumptions and reflects the final architectural decision: **the Intake experience is a lightweight, mobile-first SPA with a sub‑60‑second completion goal and no offline/PWA features**.

---

# 1. Purpose

The Intake UI exists to guide a subject through a short, tightly-scoped workflow:

1. Open invite link
2. Verify identity (OTP)
3. Review disclosures / consent
4. Provide minimal required data
5. Submit
6. Exit

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
* No saved progress
* No multi-step return journeys
* No install or re-engagement features

### ✔️ 2.3 Keep It Isolated

The Intake UI **must not** leak concepts, components, or layout patterns from the Admin SPA.

### ✔️ 2.4 Keep It Secure

* The invite link is effectively a one-time bearer token.
* All state is server-backed.
* Local caching minimized (optional safeguard only).

---

# 3. Architecture Overview

```
src/
  Holmes.App/             # Admin SPA (existing)
  Holmes.Intake/          # Intake UI (new, lightweight SPA)
  Holmes.App.Server/      # APIs, Workflow, Intake, SSE (main backend)
  Holmes.Intake.Server/   # Static file host for Intake SPA
```

### 3.1 Holmes.Intake SPA

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

---

# 4. Data Flow

```
User → Holmes.Intake → Holmes.App.Server → Intake / Workflow modules → Projections / Events
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

Submission triggers:

* `IntakeSubmitted`
* `OrderReadyForRouting` (via gateway)

The SPA then displays a final confirmation and exits.

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

## 5.3 Consent

* Show disclosure content (HTML/PDF)
* Checkbox + timestamp
* “I Agree” → Data Entry

## 5.4 Data Entry

* Minimal fields only
* Real-time validation
* Auto-format phone/email where possible

## 5.5 Review

* Display entered data
* Highlight required corrections
* “Submit”

## 5.6 Success

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

---

# 7. Technical Guardrails

### 7.1 Do Not Persist Sensitive Data Long-Term

* No IndexedDB
* No encrypted caches
* At most: temporary `localStorage` for crash-recovery of one step

### 7.2 Do Not Reuse Admin Components

The Admin SPA and Intake SPA serve different audiences and must remain logically separate.

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
2. OTP → Consent → Data → Review → Submit works on:

    * iPhone Safari
    * Android Chrome
    * Desktop (for debugging)
3. All steps match WCAG 2.1 AA expectations.
4. All backend interactions match the APIs defined in `docs/PHASE_2.md`.
5. No service worker, no caching beyond ephemeral state.
6. Build output is less than ~200 KB compressed.
7. `Holmes.Intake.Server` can serve the SPA standalone.

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
It aligns with the short-lived, single-purpose nature of the subject-facing workflow and avoids unnecessary PWA complexity.
