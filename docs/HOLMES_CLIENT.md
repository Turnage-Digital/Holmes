# Holmes.Client UI Architecture

**Audience:** Frontend developers, UX partners (Luis Mendoza + Rebecca Wirfs-Brock’s team), and reviewers planning Phase
2 SPA work.

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

- Tokens live in `src/theme/tokens.ts` (brand colors, spacing, typography).
- `src/theme/index.ts` composes a MUI theme:
    - Primary/secondary palettes match Holmes brand.
    - Buttons default to `contained`, rounded corners, bold typography.
    - Tabs use a thicker indicator + gutter padding to feel like rails.
- Future adjustments (dark mode, tenant branding) happen via tokens so downstream components stay untouched.

---

## 3. Layout & Navigation

- `AppLayout` (`src/components/layout/AppLayout.tsx`) owns the chrome: AppBar, brand, and primary navigation.
- `PrimaryNav` (`src/components/navigation/PrimaryNav.tsx`) renders the context tabs and syncs with React Router’s
  location.
- `PageHeader` standardizes page titles, subtitles, status/meta badges, and action buttons.
- `Container maxWidth="xl"` keeps the main surface consistent; children stay oblivious to nav semantics.

---

## 4. Data Fetching

- `src/lib/queryClient.ts` exports the singleton with Holmes defaults:
    - `staleTime = 30s`, no refetch-on-focus, two retry attempts for GETs, zero retries for mutations.
- Helpers (`apiFetch`, `toQueryString`) stay in `src/lib/api.ts`.
- Queries must live close to their page component, with typed DTOs imported from `src/types/api.ts`.
- When invalidating caches, scope by key segments (e.g., `{ queryKey: ["users"] }`) to avoid full cache nukes.

---

## 5. Shared Patterns

- `SlaBadge` (`src/components/patterns/SlaBadge.tsx`) provides consistent badge semantics (`on_track`, `at_risk`,
  `breached`).
- `SectionCard` wraps forms/tables in a consistent shell (header + divider + body).
- `AuditPanel` and `TimelineCard` standardize dashboard-style insights and event history cards.
- `DataGridNoRowsOverlay`/`EmptyState` give us reusable “no data” messaging.
- Future primitives:
    - **ActionRail** for contextual operations with policy gating.
- Place new primitives under `src/components/patterns/` or `src/components/layout/` depending on scope.

---

## 6. Page Structure Guidelines

Each feature view follows this stack:

1. `<PageHeader />` with subtitle + status badges.
2. Optional global alerts (error/success).
3. Primary card(s) for actions (forms, dialogs).
4. Data surface (DataGrid/List) tied to React Query results.

This order keeps the “action -> data -> detail” hierarchy consistent and aligns with Rebecca’s UX guidelines.

---

## 7. Testing & Linting

- `npm run lint:fix` formats + lints all TS/TSX via the Shopify ESLint config.
- `npm run build` runs `tsc` type-checking and Vite production build; must stay green before merging.
- For interactive flows (dialogs, table filtering), add React Testing Library specs under `src/__tests__` (phase 2
  stretch).

---

## 8. Roadmap Hooks

- **Design system expansion:** Tokenize spacing/typography variants for timeline, drawer, and empty states.
- **Responsive polish:** Tablet breakpoint mockups are pending from Rebecca’s team; update `PageHeader` and nav when
  they arrive.
- **Accessibility:** Audit the new primitives (SLA badge, nav tabs) with axe-core in Phase 2 to guarantee color
  contrast + keyboard support.

With this baseline in place, future modules (Intake workflow, SLA dashboards) can slot into the same structure without
rethinking layout or design primitives.
