# Holmes.App UI Architecture

**Audience:** Frontend developers, UX partners (Luis Mendoza + Rebecca Wirfs-Brock’s team), and reviewers planning Phase
2 SPA work across the **Internal** (ops/admin) and **Intake** (subject-facing) apps.

This document captures the architecture decisions from the Phase 1.9 workshop so future slices land consistently.

---

## 1. Stack & Principles

- **Framework:** React 19 + Vite (ESM). Keep dependencies minimal: React Router 6, MUI 7 (+ Emotion),
  `@tanstack/react-query`.
- **Philosophy:** Domain-first UI. Screens mirror bounded contexts (Users/Customers/Subjects) and reuse shared
  primitives (SLA badges, audit panels, action rails).
- **State/data:** React Query orchestrates server calls; local component state only for ephemeral UI (dialogs/forms). No
  global state libraries.

---

## 2. Design Tokens & Theme

- Shared theme lives in `@holmes/ui-core` (exported `createTheme` + tokens).
- Palette: primary (slate), secondary (amber/brown), gray ramp, success/warning/error scales; action/hover/selected
  defined via `alpha`.
- Typography: Inter stack, detailed h1–h6, subtitle, body, caption, overline weights/sizing.
- Shape/spacing/shadows: border radius 3px, spacing 8px, custom shadow scale. Component overrides for buttons, app bar,
  paper, inputs, chips, toggles, etc.

---

## 3. Shared UI Core (Internal + Intake)

- Thin shared package (`src/Holmes.Core`, npm name `@holmes/ui-core`) holds:
    - Theme factory + tokens (used by both apps).
    - Shared API helper (`apiFetch`, `toQueryString`, `ApiError`, `createEventSource`).
- Internal and Intake keep separate layout shells and routes. Only theme + API helper are shared to avoid bleed-over of
  navigation or role concepts.

---

## 4. Layout & Navigation

- **Internal app:** `AppLayout` (`src/components/layout/AppLayout.tsx`) owns the chrome: AppBar, brand, and primary
  navigation. `PrimaryNav` (`src/components/navigation/PrimaryNav.tsx`) renders the context tabs and syncs with React
  Router’s location. `PageHeader` standardizes page titles, subtitles, status/meta badges, and action buttons.
  `Container maxWidth="xl"` keeps the main surface consistent; children stay oblivious to nav semantics.
- **Intake app:** chrome-free, single-route wizard using `WizardShell` and `PageSection` wrappers sized for mobile; no
  side nav.

---

## 5. Data Fetching

- `src/lib/queryClient.ts` exports the singleton with Holmes defaults:
    - `staleTime = 30s`, no refetch-on-focus, two retry attempts for GETs, zero retries for mutations.
- Shared HTTP utilities come from `@holmes/ui-core` (`apiFetch`, `ApiError`, `toQueryString`, `createEventSource`).
- Queries must live close to their page component, with typed DTOs imported from `src/types/api.ts`.
- When invalidating caches, scope by key segments (e.g., `{ queryKey: ["users"] }`) to avoid full cache nukes.
- **Intake fetch profile:** invite-token auth only (no Internal/admin headers); per-session caches wiped on
  submit/abandon. Submit returns a synchronous result; no SSE/poll needed for completion.

---

## 6. Shared Patterns

- `SlaBadge` (`src/components/patterns/SlaBadge.tsx`) provides consistent badge semantics (`on_track`, `at_risk`,
  `breached`).
- `SectionCard` wraps forms/tables in a consistent shell (header + divider + body).
- `AuditPanel` and `TimelineCard` standardize dashboard-style insights and event history cards.
- `DataGridNoRowsOverlay`/`EmptyState` give us reusable “no data” messaging.
- Future primitives:
    - **ActionRail** for contextual operations with policy gating.
- Place new primitives under `src/components/patterns/` or `src/components/layout/` depending on scope.

---

## 7. Page Structure Guidelines

Each feature view follows this stack:

1. `<PageHeader />` with subtitle + status badges.
2. Optional global alerts (error/success).
3. Primary card(s) for actions (forms, dialogs).
4. Data surface (DataGrid/List) tied to React Query results.

This order keeps the “action -> data -> detail” hierarchy consistent and aligns with Rebecca’s UX guidelines.

- Intake uses a single-page wizard: Welcome → OTP → Consent → Data → Review → Success. Each step renders its own
  section with clear CTAs, inline validation, and mobile-friendly tap targets.

---

## 8. Testing & Linting

- `npm run lint:fix` formats + lints all TS/TSX via the Shopify ESLint config (Internal + Intake workspaces).
- `npm run build` runs `tsc` type-checking and Vite production build; must stay green before merging.
- Accessibility: run axe-core checks on OTP, consent, review/success steps; ensure semantic status tokens meet contrast.
- Visual regression: when/if Playwright lands later, add baselines for Intake wizard states and high-value Internal
  screens; for now, lean on existing test tooling only.
- For interactive flows (dialogs, table filtering), add React Testing Library specs under `src/__tests__` when feasible
  (phase 2 stretch).

---

## 9. Roadmap Hooks

- **Design system expansion:** Tokenize spacing/typography variants for timeline, drawer, and empty states.
- **Responsive polish:** Tablet breakpoint mockups are pending from Rebecca’s team; update `PageHeader` and nav when
  they arrive.
- **Accessibility:** Audit the new primitives (SLA badge, nav tabs, Intake wizard controls) with axe-core in Phase 2 to
  guarantee color contrast + keyboard support.

With this baseline in place, future modules (Intake workflow, SLA dashboards) can slot into the same structure without
rethinking layout or design primitives.
