# Phase 2 Delivery Plan — Intake & Workflow Launch

**Phase Window:** Weeks 4–7  
**Outcome Target:** First customer order moves from invite → intake → `ready_for_routing`, with resumable SSE streams
and audit-ready evidence.

## 1. Stakeholders & Working Cadence

- **Domain Steward (Eric Evans):** Facilitates event-storming sessions, curates ubiquitous language, guards aggregate
  boundaries.
- **Product & UX Lead (Intake Experience Designer):** Owns intake journey research, wireframes, interaction states, and
  accessibility standards.
- **Tech Leads (Backend & Client):** Own aggregate/handler implementation, API contracts, projections, SSE
  infrastructure, and PWA delivery.
- **Compliance & Ops Partner:** Confirms policy snapshot, consent, and audit artifacts meet regulatory requirements for
  first-order go-live.

Standing ceremonies:

1. **Event Storm (2 × 2 hrs):** Map invite→submit narrative, enumerate commands/events, surface external systems.
2. **Experience Studio (weekly):** Review UX prototypes, copy, error states with engineering + compliance feedback.
3. **Build Checkpoint (twice weekly):** Track aggregate progress, API readiness, projection health, and client
   integration blockers.
4. **Runbook Review (weekly):** Dry-run DB reset + first-order scenario to keep environments reproducible.

## 2. Scope Breakdown

| Track                         | Deliverables                                                                                                                                                                     | Definition of Done                                                                                                         |
|-------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------|
| **Domain & Application**      | `IntakeSession` + `Order` aggregates, state transitions (`invited`, `in_progress`, `submitted`, `ready_for_routing`), policy snapshot enforcement, optimistic concurrency guards | Command handlers emit canonical events, invariants covered by unit + integration tests, documentation of state diagrams    |
| **Read Models & Projections** | `order_summary`, `order_timeline_events`, `intake_sessions` projections with EF contexts + checkpointing                                                                         | Replayable via projection runner, surfaced in runbooks with verification queries                                           |
| **API & Streaming**           | REST endpoints (invite, resume, submit, state transitions), SSE `/changes` with tenant filters, Last-Event-ID resume, heartbeat contracts                                        | OpenAPI/contract tests published; SSE resilience test proves resume after simulated disconnect                             |
| **Client Experience (PWA)**   | Mobile-first intake shell covering OTP verification, consent capture, data entry wizard, review/submit screen, success state with next steps                                     | UX specs signed, React implementation responsive + accessible, connects to live APIs with optimistic UI + failure recovery |
| **Compliance & Evidence**     | Policy snapshot persisted per order, consent proof attached via `DatabaseConsentArtifactStore`, audit timeline events created                                                    | Evidence pack for first order includes policy version, consent timestamp, subject metadata + artifact hash                 |
| **Observability & Ops**       | Metrics for intake latency, SSE reconnects, projection lag; trace spans around UnitOfWork + order transitions; runbooks updated                                                  | `docs/RUNBOOKS.md` includes Phase 2 flow, Grafana dashboard links captured, alert thresholds defined                       |

## 3. Detailed Workstreams

### 3.1 Domain Modeling (Eric Evans)

- Capture bounded contexts for Intake vs Workflow, including hand-off rules and aggregate responsibilities.
- Enumerate commands/events: `InviteIssued`, `IntakeSessionStarted`, `ConsentCaptured`, `IntakeSubmitted`,
  `OrderReadyForRouting`, `OrderStateChanged`.
- Define policy snapshot contract (customer settings, permissible purpose, disclosure versions) and how it binds to an
  order immutably.
- Produce state diagrams and aggregate design notes stored under `docs/domain/`.

### 3.2 APIs & Application Services

- Implement REST endpoints in `Holmes.App.Server` for invite issuance, session resumption, save-as-you-go, submission,
  and order transitions.
- Ensure idempotency via ULID correlation headers; enforce tenant context middleware on every endpoint.
- Add SSE `/changes` route exposing filtered event frames (tenant + subject/order filters) with heartbeat pulses every
  10s and Last-Event-ID resume tokens persisted per client.
- Author integration tests simulating invite→submit, SSE reconnect, and concurrent updates (409 handling).

### 3.3 Read Models & Projections

- Create projection runners for `order_summary`, `intake_sessions`, `order_timeline_events`, each with MySQL schemas and
  checkpoint tables.
- Extend runbooks with replay/reset steps and SQL snippets for verification.
- Instrument projection lag metrics and expose health endpoints so Ops can monitor readiness.

### 3.4 Client Experience & UX

- UX designer delivers journey map, high-fidelity mockups, content strategy (plain language instructions, error copy,
  SMS/email templates).
- React PWA implements modular wizard with offline-friendly persistence (local draft cache), OTP verification screen,
  consent viewer (PDF or HTML), and summary/submit confirmation.
- Accessibility: WCAG 2.1 AA, keyboard navigation, screen reader labels; usability tests with at least 3 participants
  validating time-to-complete goals (P50 sub-minute).
- Integrate analytics hooks (page timing, drop-off) and feature flags for staged rollout.

### 3.5 Compliance & Ops

- Validate disclosure + authorization flows with compliance counsel; ensure evidence artifacts (consent signature, IP,
  timestamp) stored immutably alongside order events.
- Implement `IConsentArtifactStore` abstraction with Phase 2 concrete `DatabaseConsentArtifactStore` (encrypted byte
  arrays in MySQL) so future Azure Blob/File providers can replace it by changing DI wiring.
- Update `docs/RUNBOOKS.md` with first-order walkthrough, including: invite issuance, subject login, data entry,
  submission, order inspection.
- Capture incident response checklist for intake outages (SSE failure, projection lag, consent storage).

## 4. Acceptance Checklist

1. **Functional:** End-to-end invite→submit completes in staging, order state becomes `ready_for_routing`, SSE client
   receives streamed events and resumes after simulated disconnect.
2. **Quality:** All new handlers, projections, and React flows covered by automated tests; manual QA sign-off on
   usability + accessibility.
3. **Compliance:** Policy snapshot + consent evidence attached to order record; audit timeline exportable for the first
   order run.
4. **Observability:** Metrics dashboards live; runbooks updated with screenshots/links; alerts configured for intake
   latency & SSE error rates.
5. **Documentation:** API contracts published, UX artifacts checked into repo, domain diagrams available, Phase 2 retro
   scheduled with stakeholders.

## 5. Timeline & Milestones

- **Week 4:** Event storm + UX research complete; aggregate skeletons + API stubs checked in.
- **Week 5:** Projections + SSE infrastructure ready; UX high-fidelity designs approved; client app MVP wired to APIs.
- **Week 6:** Full invite→submit happy path automated; consent/policy evidence stored; observability hooks live.
- **Week 7:** Hardening + readiness review; first customer order executed end-to-end; documentation/runbooks finalized.

## 6. Risks & Mitigations

- **Scope Creep:** Freeze backlog items (MCP sidecar, SLA watchdog) per Phase 2 charter; treat any new requirement as
  change request.
- **Concurrency & Idempotency Bugs:** Leverage event sourcing tests, enforce ULID command IDs, add chaos tests for retry
  scenarios.
- **UX Debt:** UX lead embedded with dev squad; gating milestone requires signed designs before implementation.
- **Projection Lag:** Add synthetic load tests + alerts; fallback dashboards to detect lag before it affects order
  readiness.

## 7. Next Actions

1. Schedule event storm + UX kickoff (calendar invites within 24h).
2. Create `docs/domain/` for diagrams and copy template from `docs/MODULE_TEMPLATE.md`.
3. Update `docs/RUNBOOKS.md` with Phase 2 rehearsal steps once first dry-run completes.

This document is the authoritative reference for Phase 2 delivery; update it at each checkpoint to reflect decisions,
scope changes, and readiness status.
