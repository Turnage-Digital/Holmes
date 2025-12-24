# Holmes Delivery Plan – Updated for Phase 3–6 Alignment

This updated PLAN.md clarifies the work for Phases 3–6, aligns roadmap items with the Compliance Suite and Identity
Broker architecture, and modernizes the language around notifications, evidence packs, adjudication, and white-label
readiness.

---

# Phase 3 — SLA, Compliance & Notifications

**Modules delivered:** SlaClocks, Compliance, Notifications (baseline)

**Outcomes**

- Business calendar service + EF models for calendars/holidays.
- Aggregates delivered:
    - `SlaClock`
    - `CompliancePolicy`
    - `PermissiblePurposeGrant`
    - `DisclosurePack`
- Order workflow protections:
    - Permissible Purpose guardrails
    - Disclosure acceptance evidence
    - Customer policy overlays applied during Intake → Workflow lifecycle
- Initial Compliance bounded context foundations (Phase 3 of Compliance Suite)
- Baseline Notifications:
    - Tenant-configured provider abstractions (email/SMS/webhook)
    - Holmes emits `NotificationCreated` events; providers deliver
- Identity Broker readiness:
    - Holmes.Identity can federate with tenant IdPs via OIDC
    - Tenant-scoped identity mapping and role assignment

---

# Phase 4 — Adverse Action & Evidence Packs

**Modules delivered:** AdverseAction, Artifacts (Infrastructure), Compliance extensions

**Outcomes**

- **Adverse Action State Machine:**
    - Pre-adverse → waiting period → final notice workflow
    - Pause/resume on disputes
    - Policy-driven wait period calculations
    - Regulatory-compliant clock enforcement
- **Evidence Packs:**
    - Deterministic bundler (ZIP of PDFs + JSON manifest)
    - Containing consent, policy snapshots, notices, artifacts, dispute thread, timeline events
- **WORM Artifact Store:**
    - Write-once model with hash validation
    - Backed by encrypted MySQL BLOBs (swappable to Azure Blob Storage)
- **Dispute Case Integration:**
    - Dispute lifecycle primitives ready for Phase 5 integration
- **Regulator/Operations APIs:**
    - Read models: `adverse_action_clocks`, `adverse_action_cases`
    - Clock pause/resume
    - Evidence pack retrieval endpoints
- **Tests:**
    - Clock boundary conditions
    - Policy overlays
    - Artifact integrity
    - State transitions

---

# Phase 5 — Adjudication Engine

**Modules delivered:** Adjudication, ChargeTaxonomy, Notifications enhancements

**Outcomes**

- **RuleSet Authoring + Publish Workflow:**
    - Tenant-scoped rule definitions and versioning
    - Persisted snapshot per order for auditability
- **Deterministic Assessment Engine:**
    - Generates reason codes and recommended outcomes
    - Deterministic re-evaluation from snapshots
- **Reviewer Queue:**
    - `adjudication_queue` projection
    - Workload routing and escalation
- **Assessment Summary Read Model:**
    - Order-level classification, reviewer notes, override history
- **Human Override Flow:**
    - Required justification text
    - Optional attachments stored in WORM artifacts
- **Notifications Upgraded:**
    - Assessment change triggers
    - Delay notifications and escalation events
- **Simulation API:**
    - What-if runs for policy validation and tuning

---

# Phase 6 — Hardening & Pilot Readiness

**Modules matured:** All

**Outcomes**

- **Branding & White-Label Readiness:**
    - Tenant branding (logos, colors, templates)
    - Policy snapshot UI contract for tenant-managed policies
    - Theming support across Holmes.Client
- **Observability:**
    - Dashboards for SLA health, adverse-action throughput, adjudication performance
    - Metrics + tracing coverage (OpenTelemetry)
- **Chaos & Property Testing:**
    - Duplicate event ingestion
    - Out-of-order delivery
    - SSE reconnect storms
- **Performance Tuning:**
    - Projection replay efficiency
    - Index optimization for MySQL
    - SSE scalability tuning
- **Deployment & Ops Maturity:**
    - Automated container image builds
    - Migration automation
    - Runbooks completed and validated

---

# Phase Summary (High-Level)

- **Phase 3:** Compliance foundations (SLA, policy, PP, notifications, IdP federation)
- **Phase 4:** Full Adverse Action + Evidence Packs + WORM storage
- **Phase 5:** Adjudication Engine with rulesets, overrides, and simulation
- **Phase 6:** Hardening, white-label readiness, observability, and pilot launch

This updated PLAN.md aligns the roadmap with the Compliance Suite architecture, monetizable boundaries, and the new
Identity Broker model.

