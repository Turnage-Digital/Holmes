# Holmes Pricing Model

This document defines the commercial packaging and pricing structure for Holmes. It is aligned with the Phase 3–6 roadmap, the Compliance Suite architecture, and the multi-tenant/white‑label design. All pricing values are reference anchors; actual billing contracts may vary per CRA or enterprise deployment.

---

# 1. Overview
Holmes is delivered as a modular, multi‑tenant SaaS platform. Each tenant subscribes to a **tier**, and may add optional **usage‑based components**. The pricing model is structured around:

- **Compliance automation** (high value)
- **Adjudication decisioning** (enterprise value)
- **Notifications and evidence services** (usage‑based)
- **Branding and private deployments** (white‑label)
- **Support SLAs** (enterprise)

Holmes monetizes through:
- Monthly subscription tiers
- Per‑event billing (adverse action, evidence packs, notifications)
- Premium engines (adjudication, simulation)
- Optional dedicated deployments

---

# 2. Pricing Tiers
Holmes supports four primary subscription tiers. All modules and events are enforced via entitlements.

## 2.1 Essential Tier
**Target:** Small CRAs, simple use cases.  
**Monthly:** Free – $99/mo

**Includes:**
- Users, Customers, Subjects
- Order Intake → Workflow lifecycle
- Basic reports & timeline
- Integration with tenant‑provided IdP (OIDC)
- Access to Notifications (usage‑billed)

**Not included:**
- SLA/Compliance features
- Adverse Action automation
- Evidence Packs
- Adjudication Engine
- White‑label branding

---

## 2.2 Compliance Tier (Phase 3)
**Target:** CRAs requiring FCRA‑aligned workflows.  
**Monthly:** $99–$399/mo

**Includes:**
- SLA Clocks & Business Calendar
- Compliance Policies
- Permissible Purpose Guardrails
- DisclosurePack evidence storage
- Clock enforcement (regulatory timers)
- Baseline Notifications
- Identity Broker support (Holmes-managed or tenant IdP)

**Add‑ons:**
- Evidence Pack generation (usage)
- Adverse Action automation (upgrade to Compliance Suite)

---

## 2.3 Compliance Suite (Phase 4)
**Target:** CRAs needing full adverse action automation, evidence, and dispute readiness.  
**Monthly:** $199–$799/mo

**Includes everything in Compliance Tier, plus:**
- Adverse Action Engine (pre/final notice state machine)
- Waiting‑period calculations
- Pause/resume behavior for disputes
- Dispute case lifecycle primitives
- WORM Artifact Storage (consent, notices, policy snapshots)
- Evidence Pack Bundler (admin & auto‑generated)
- Regulator API endpoints (evidence, clocks)

**Usage fees:**
- Adverse Action events: $0.50–$2.00 per case
- Evidence Pack exports: $5–$20 per bundle
- Notification delivery: $0.01–$0.05 per message

---

## 2.4 Adjudication Suite (Phase 5)
**Target:** Mid‑to‑large CRAs with reviewer teams and compliance QA pipelines.  
**Monthly:** $499–$1999/mo

**Includes:**
- RuleSet authoring & publish workflow
- Deterministic assessment engine (reason codes)
- Reviewer queue + workload routing
- Assessment summary read model
- Human override flow (with justification)
- Advanced Notifications integration (assessment change events)
- Simulator API (policy what‑ifs)

**Usage fees:**
- Simulator runs: $0.10–$1.00 per simulation
- Large workloads may negotiate volume pricing

---

## 2.5 Enterprise / White‑Label Tier (Phase 6)
**Target:** Enterprise CRAs, private‑label partners, or large employers.  
**Monthly:** $2000–$10,000+/mo

**Includes:**
- Full white‑label theming (logo, colors, templates)
- Tenant branding + localized templates
- Dedicated support SLAs
- Observability dashboards (SLA, adverse, adjudication metrics)
- Priority enhancement requests
- Custom IdP federation setups

**Optional:**
- Private tenant deployments (AKS, private DB): $5000–$50,000/mo
- Long‑term WORM retention (3/5/7 year plans)

---

# 3. Usage‑Based Billing
Holmes records billable events in `ComplianceUsageRecord`.

| Event Type | Description | Rate |
|------------|-------------|-------|
| AdverseActionCaseCreated | Pre/final AA workflow initiated | $0.50–$2.00 per case |
| EvidencePackGenerated | Evidence bundle export | $5–$20 per pack |
| NotificationRequested | Email/SMS/webhook fired | $0.01–$0.05 per event |
| DisputeOpened | Candidate dispute initiation | Included or +$0.25/event depending on tier |
| SimulationRun | Policy what‑if request | $0.10–$1.00 per run |

---

# 4. Add‑On Modules
These modules can be attached to any tier.

### Notifications Enhancements
- Priority delivery queues
- Multi-provider routing
- Starts at +$49/mo

### Evidence Retention Plans
- 3-year retention: +$49/mo
- 5-year retention: +$99/mo
- 7-year retention: +$199/mo

### Analytics Pack
- SLA dashboards
- Adverse Action throughput
- Adjudication performance
- $49–$199/mo

---

# 5. White‑Label Deployment Options
Holmes supports:
- Dedicated subdomain or custom domain
- Full tenant branding
- Tenant-specific IdP config
- Multi-environment separation (sandbox, prod)

**Private-hosted deployments:** $5k–$50k/mo depending on:
- Dedicated AKS cluster
- Per-tenant database isolation
- HA + DR requirements
- Compliance obligations (SOC2, ISO)

---

# 6. Support Packages
| Level | Description | Monthly |
|-------|-------------|----------|
| Basic | Email support, next-business-day response | Included |
| Enhanced | Business-hours NBD, phone support | $499/mo |
| Premium | 24/7, 4-hour SLA | $1999/mo |
| Enterprise | Contract-defined, priority access | Negotiated |

---

# 7. Summary
Holmes monetization aligns directly with the phased architecture:
- Phase 3 → Compliance Tier
- Phase 4 → Compliance Suite (Adverse Action & Evidence Packs)
- Phase 5 → Adjudication Suite
- Phase 6 → Enterprise / White‑Label

This PRICING.md is a living document and intended to guide product packaging, sales conversations, and entitlement design.

