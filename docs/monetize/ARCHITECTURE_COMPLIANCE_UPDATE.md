# Holmes Architecture – Updated for Phase 3+ Compliance, Monetization, and Identity Broker Model

This document extends the existing Holmes architectural outline with:
- The Phase 3+ Compliance Suite and monetization architecture
- White‑label and multi‑tenant considerations
- Updated Identity Provider (IdP) model (Holmes as identity broker)
- Modular usage metering and entitlements

## 1. Architectural Positioning
Holmes remains a tenant‑isolated, event‑sourced system built around bounded contexts. The system now explicitly supports commercial packaging and compliance automation as first‑class architectural concerns.

Holmes is designed to operate as:
- A **multi‑tenant background screening platform**
- A **white‑label substrate** for CRAs
- A **compliance engine** with evidence integrity and auditability
- A **federated identity broker**, not a single-purpose dev IdP

## 2. Compliance Suite (Phase 3–4 Architecture)
Compliance becomes its own bounded context:
- `Holmes.Compliance.Domain`
- `Holmes.Compliance.Application`
- `Holmes.Compliance.Infrastructure.Sql`

This context uses its own `compliance.*` schema and houses:
- **SLA & compliance clocks** (regulatory timers)
- **Compliance policies**
- **Permissible Purpose evaluations**
- **Disclosure/authorization evidence references**
- **Adverse Action case management** (Phase 4)
- **Dispute threads & case linkage**

Compliance integrates with Intake, Workflow, and Adjudication exclusively via domain events and read models.

## 3. WORM Artifacts & Evidence Packs
The existing `IConsentArtifactStore` abstraction expands into a **WORM artifact service**:
- Write‑once, immutable storage for:
  - Policy snapshots
  - Consent signatures
  - Adverse notices
  - Dispute submissions
  - Evidence bundles
- Hash verification for regulator integrity requirements
- Backed initially by encrypted MySQL BLOBs, swappable to Azure Blob Storage

The system generates deterministic **Evidence Packs** (ZIP or equivalent) containing:
- Full event history relevant to an order
- All artifacts pertaining to adverse action or disputes
- JSON manifest for reproducibility

## 4. Adverse Action State Machine (Phase 4)
A dedicated aggregate governs the legally required process:
- Pre‑notice → waiting period → final notice
- Pause/resume on dispute
- Clock enforcement using the compliance clock engine
- Notice generation requests via Notifications (Holmes doesn’t send mail directly)

State transitions rely entirely on:
- Domain events from Workflow
- Policy overlays
- Tenant configuration

## 5. Notifications as a Provider‑Abstracted Module
Holmes does not directly send messages; it **requests** them.

Notifications layer:
- Tenant provides provider keys (SendGrid, Twilio, webhook targets)
- Holmes emits `NotificationRequested` events
- Provider adapters handle actual delivery

This avoids FCRA liability while enabling automation.

## 6. Usage Metering & Entitlements (Monetization Framework)
A dedicated module records billable events:
- `ComplianceUsageRecord`
- `UsageType` (AdverseActionCaseCreated, EvidencePackGenerated, DisputeOpened, NoticeRequested, etc.)

Entitlements gate features:
- Compliance Suite
- Adverse Action Automation
- Evidence Packs
- Dispute Portal
- Adjudication Engine

The entitlements layer allows Holmes to run multiple SaaS tiers in one deployment.

## 7. Identity Architecture (Broker Model)
Holmes.Identity.Server evolves into a **federated identity broker**, not only a dev stub.

### 7.1 Modes of Authentication
- **Mode A: Tenant uses external IdP** (Azure AD, Okta, generic OIDC)
  - Holmes.Identity redirects to upstream IdP
  - Normalizes external identity into Holmes User Registry

- **Mode B: Holmes-managed credentials** (for smaller tenants)
  - Local password-based authentication in Holmes.Identity
  - Still issues the same Holmes-standard tokens

### 7.2 Why a Broker?
- Uniform claims model across all tenants
- Holmes enforces TenantId, roles, and policies centrally
- Clean multi-tenant isolation
- White‑label friendly authentication UX
- Supports mixed environments (some tenants bring IdP, others don’t)

Holmes.Identity.Server thus becomes the **single IdP for Holmes.App and Holmes.Client**, delegating upstream only as needed.

## 8. White-Label Architecture
Holmes supports private-label deployments with:
- Tenant‑specific branding metadata
- Themed login flows via Holmes.Identity
- Themed Holmes.Client UI (logo, colors, email templates, PDF templates)
- Tenant-scoped IdP configuration (BYO IdP)
- Per-tenant features and billing

## 9. Phase Alignment
### Phase 3
- SLA clocks
- Compliance policies
- Baseline notifications
- Permissible purpose, disclosure acceptance, policy overlays

### Phase 4
- Adverse action engine
- Dispute case threading
- WORM storage
- Evidence packs
- Regulator endpoints

### Phase 5
- Adjudication engine
- Rulesets, overrides, reason codes
- Reviewer queue
- SSE events & notifications expansion

### Phase 6
- Hardening
- White-label readiness
- Dashboards for SLA, adverse action, adjudication throughput
- Performance tuning
- Deployment & operational maturity

## 10. Multi-Tenant & Deployment Considerations
The architecture continues to support:
- Row‑level tenant isolation via `TenantId`
- Separate schemas per bounded context
- Azure deployments via AKS
- Scaling via horizontal pods and projection runners

Optional enterprise deployments may use per‑tenant databases.

## 11. Summary
Holmes is now architecturally positioned as:
- A white-label, multi‑tenant background screening platform
- A compliance automation engine
- A federated identity broker
- A modular SaaS product with monetizable tiers aligned to Phase 3–6

