# Holmes Guardrails

**Status:** Future phase (post–Phase 6, conceptualized for Phase 8–10)  
**Owners:** Architecture / Product / Data Science  
**Goal:** Provide ML-backed process and compliance intelligence across the full candidate lifecycle, without replacing adjudication or acting as a decision-maker.

---

## 1. Purpose & Positioning

Holmes Guardrails is a **process intelligence and anomaly detection layer** that sits on top of the existing Holmes event stream.

Guardrails analyzes the end-to-end lifecycle of a screening order:
- Intake → Workflow → Adjudication → Adverse Action → Complete

It produces:
- Consistency and drift signals (how decisions are being made)
- Timing and compliance risk predictions
- Reviewer behavior and workload patterns
- Dispute-likelihood and escalation signals
- Operational benchmarks (per tenant, per jurisdiction, per charge type)

Guardrails is explicitly **meta**:
- It does **not** replace adjudication.
- It does **not** make employment decisions.
- It evaluates **process health and consistency**, not candidate suitability.


### 1.1 Product Concept
> "Holmes Guardrails: ML-powered screening quality & compliance intelligence."

Primary value propositions:
- Help CRAs and employers detect **drift** from policy and rulesets.
- Predict **timing/compliance risks** before they become violations.
- Highlight **reviewer inconsistencies** and training opportunities.
- Provide **regulator-ready process insights** (how decisions are made over time).

---

## 2. Scope

### In Scope
- Aggregated, tenant-scoped analysis of order lifecycles.
- Model training and scoring on top of Holmes events and projections.
- Insight generation (scores, alerts, dashboards, reports).
- Anomaly detection for:
  - Reviewer decision variance
  - Adjudication outcome drift vs RuleSets
  - SLA/compliance clock risk
  - Dispute likelihood patterns
- Optional integration of external criminal data as **signal/benchmark**, not as a primary decision source.

### Out of Scope
- Acting as the system of record for any compliance decision.
- Making direct employment recommendations.
- Acting as an automated adjudication engine (that remains in Adjudication).

Guardrails always outputs **advisory signals**, not binding decisions.

---

## 3. Architecture Overview

### 3.1 Bounded Context

Guardrails is its own bounded context:
- `Holmes.Guardrails.Domain`
- `Holmes.Guardrails.Application`
- `Holmes.Guardrails.Modeling` (ML/feature pipeline)
- `Holmes.Guardrails.Infrastructure.Sql`

It writes to a dedicated schema:
- `guardrails.*` (e.g., `guardrails_insights`, `guardrails_scores`, `guardrails_models`)

### 3.2 Data Flow (High Level)

1. **Event Ingestion**
   - Subscribes to domain events from:
     - Intake (order created, data completeness)
     - Workflow (steps completed, delays)
     - Compliance (clocks, policies applied)
     - Adjudication (assessments, overrides, reason codes)
     - Adverse Action (pre/final notices, disputes)

2. **Feature Extraction**
   - Builds feature vectors for orders/reviewers/tenants.
   - Uses projections and read models to assemble features.

3. **Model Scoring**
   - Runs ML models (e.g., via ML.NET) for:
     - Anomaly detection
     - Classification (e.g., high/medium/low risk of dispute)
     - Regression (e.g., predicted SLA violation probability)

4. **Insight Persistence**
   - Writes scores and flags into `guardrails_insights` tables.
   - Updates read models for dashboards.

5. **Signals & Alerts**
   - Emits `GuardrailsInsightGenerated` events.
   - Feeds into Notifications for alerting.
   - Exposed via APIs and dashboards.

---

## 4. Data & Features

### 4.1 Primary Inputs
- **Order Timeline Events**
  - Creation, submission, completion
  - Timestamps and durations
- **Compliance Clocks**
  - SLA clocks
  - Adverse action and waiting periods
- **Adjudication Outputs**
  - Recommended outcome
  - Reason codes
  - RuleSet versions applied
  - Human overrides + justification
- **Dispute Events**
  - Dispute opened/resolved
  - Time from AA to dispute
- **Notification Outcomes** (if available)
  - Delivered / bounced / delayed

### 4.2 Optional External Inputs
- Aggregated criminal record statistics by:
  - Jurisdiction / county
  - Charge category / taxonomy
  - Disposition patterns

These are used as **contextual baselines**, not individual candidate features.


### 4.3 Example Feature Sets

**Order-level features:**
- Time from Intake to first adjudication
- Number of adjudication revisions
- Time from adverse recommendation to pre-notice
- Time from pre-notice to final notice
- Whether policy snapshots were attached
- Whether evidence packs were generated
- Whether a dispute occurred and its outcome

**Reviewer-level features:**
- Distribution of outcomes by charge taxonomy
- Deviation from RuleSet recommendations
- Rate of overrides vs peers
- Average handling time per order

**Tenant-level features:**
- Dispute rate per 100 orders
- Adverse action frequency
- SLA violation rates
- Consistency of decisions across reviewers

---

## 5. Modeling Strategy

### 5.1 Phase 1 – Foundations (Simple ML, High Value)

Use ML.NET or equivalent to implement:
- **Anomaly Detection**
  - Spot outlier orders or reviewer behaviors.
- **Drift Detection**
  - Compare decisions vs baseline distributions.
- **Risk Scoring**
  - Predict likelihood of SLA/AA timing violations.
- **Dispute Prediction (Coarse)**
  - Identify patterns correlated with disputes.

Models are trained per tenant where data volume allows; otherwise, pooled and tenant-adjusted.


### 5.2 Phase 2 – Insights & Benchmarks

- Add tenant-facing reports and dashboards:
  - Reviewer consistency
  - Adjudication outcome variance
  - Compliance risk heatmaps
- Integrate optional external criminal statistics as comparison baselines.

### 5.3 Phase 3 – Guardrail Scores & Alerts

Each order and reviewer receives:
- **Process Integrity Score**
- **Compliance Risk Score** (timing, sequence, PP/disclosure flows)
- **Decision Variance Score** (relative to peers/policy)
- **Data Completeness Score**

Threshold-based alerts:
- Emit `GuardrailsInsightGenerated` / `GuardrailsAlert` events to Notifications.

---

## 6. Interfaces & APIs

### 6.1 Internal Interfaces
- **Event Handlers** in `Holmes.Guardrails.Application` subscribing to:
  - Order/Workflow events
  - Compliance and clock events
  - Adjudication and AA events

- **Model Runner Service**
  - Periodic batch scoring (e.g., nightly)
  - Optional online scoring for high-value cases

### 6.2 External APIs

Read-only APIs:
- `GET /api/guardrails/orders/{orderId}`
  - Returns guardrail scores and flags for a specific order.
- `GET /api/guardrails/reviewers/{reviewerId}`
  - Reviewer consistency profile.
- `GET /api/guardrails/tenants/current/overview`
  - Tenant-level dashboards data.
- `GET /api/guardrails/reports/*`
  - Exportable reports for audits and compliance.

No write APIs are exposed to modify decisions; Guardrails is advisory only.

---

## 7. Integration with Notifications

Guardrails emits domain events when:
- Scores exceed configured thresholds (e.g., high compliance risk).
- Reviewer variance crosses policy limits.
- SLA/AA risk is detected early.

These events are consumed by the existing Notifications module to:
- Send alerts to CRA admins or QA leads.
- Push webhook notifications to tenant systems.

Tenants configure alerting thresholds and channels.

---

## 8. Monetization Hooks

Guardrails integrates with the existing usage & entitlements system.

### 8.1 Entitlements
- `HasGuardrailsModule`
- `HasGuardrailsDashboards`
- `HasGuardrailsReports`

These entitlements gate:
- Access to guardrail scores in the UI
- Access to dashboards and API endpoints
- Ability to configure alerts

### 8.2 Billable Events (Recorded in ComplianceUsageRecord or GuardrailsUsage)
- Guardrails scoring per order
- Monthly reviewer variance reports
- Compliance risk reports
- High-risk alert events

Example pricing (subject to PRICING.md alignment):
- Per scored order: $0.05–$0.25
- Per monthly reviewer/tenant report: $5–$50
- Enterprise Guardrails module subscription: $499–$1999/mo

Guardrails is positioned as an **enterprise add-on**, not a base-tier feature.

---

## 9. Phase Mapping (Rough)

These phases assume Guardrails is layered on after Phase 6.

- **Phase 8 – Guardrails Foundations**
  - Bounded context, schema, basic feature extraction
  - Simple anomaly detection and scoring
  - Internal-only dashboards for pilot customers

- **Phase 9 – Guardrails Dashboards & Alerts**
  - Tenant-facing UI
  - Configurable alerting
  - Reviewer and tenant-level reports

- **Phase 10 – External Signal Integration**
  - Optional criminal statistics integration
  - Cross-tenant benchmarking where legally permissible
  - Advanced modeling and long-term trend analysis

---

## 10. Compliance & Risk Considerations

- Guardrails must **never** act as the final source of truth for decisions.
- All outputs must be explainable and traceable to input events.
- External data usage (e.g., criminal statistics) must be anonymized/aggregated.
- All Guardrails features must respect tenant boundaries; no cross-tenant data leakage.
- Any inter-tenant benchmarking must be opt-in and aggregated enough to avoid deanonymization.

---

## 11. Summary

Holmes Guardrails turns the existing event-sourced architecture into a
**continuous process intelligence system**.

It:
- Uses ML to highlight risk, drift, and inconsistency.
- Helps CRAs maintain defensible, auditable processes.
- Provides high-value, enterprise-grade features that align cleanly with Holmes monetization.

Guardrails is designed as a later-phase, high-impact module that leverages existing Holmes investments in event sourcing, compliance, adjudication, and evidence storage without undermining their responsibilities or compliance boundaries.

