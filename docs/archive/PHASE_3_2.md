# Phase 3.2 Delivery Plan — Frontend & Subject Data Expansion

**Phase Window:** Immediate (following Phase 3.1 backend completion)
**Outcome Target:** Complete intake captures rich subject data (employment, education, addresses), Internal UI supports
ordering, customer setup with services/tiers, and fulfillment visibility.

**Commercial Alignment:** Without comprehensive data capture and internal tooling, Holmes is a backend skeleton. This
phase delivers the **usable product** — what ops teams and subjects actually interact with.

## 1. The Gap: Backend vs Frontend Reality

### What Backend Supports vs What Frontend Captures

| Data Domain                   | Backend Model                 | Intake UI         | Internal UI | Gap            |
|-------------------------------|-------------------------------|-------------------|-------------|----------------|
| **Subject Identity**          | Name, DOB, Email, Aliases     | Name, DOB, Email  | View only   | MATCHED        |
| **Subject SSN**               | Not stored                    | Captured (last 4) | Not shown   | INTAKE ONLY    |
| **Subject Phone**             | Not stored                    | Captured          | Not shown   | NO PERSISTENCE |
| **Subject Address (Current)** | Not modeled                   | Captured          | Not shown   | NO PERSISTENCE |
| **Subject Address History**   | Not modeled                   | Not captured      | Not shown   | TOTAL GAP      |
| **Employment History**        | Not modeled                   | Not captured      | Not shown   | TOTAL GAP      |
| **Education History**         | Not modeled                   | Not captured      | Not shown   | TOTAL GAP      |
| **References**                | Not modeled                   | Not captured      | Not shown   | TOTAL GAP      |
| **Customer Services**         | ServiceCatalog backend exists | N/A               | Not built   | TOTAL GAP      |
| **Customer Tiers**            | Tiering backend exists        | N/A               | Not built   | TOTAL GAP      |
| **Order Services**            | ServiceRequest backend exists | N/A               | Not built   | TOTAL GAP      |
| **Fulfillment Dashboard**     | Events exist                  | N/A               | Not built   | TOTAL GAP      |

### Current State Summary

**Holmes.Intake (React SPA):**

- 5-step flow: Verify → OTP → Consent → Data → Review → Success
- Captures: name, DOB, email, phone, SSN (last 4), single address, consent
- Missing: employment history, education history, previous addresses, references

**Holmes.Internal (React SPA):**

- Pages: Dashboard, Orders, Order Detail, Subjects, Customers, Users
- Can: create orders, view order timeline, list subjects/customers
- Missing: service configuration, tier management, fulfillment visibility, customer setup

## 2. Scope Breakdown

### Track A: Subject Domain Expansion

| Deliverable            | Description                              | Backend           | Frontend                  |
|------------------------|------------------------------------------|-------------------|---------------------------|
| **Address History**    | Collection of addresses with date ranges | Subject aggregate | Intake multi-address form |
| **Employment History** | Collection of employment records         | Subject aggregate | Intake employment form    |
| **Education History**  | Collection of education records          | Subject aggregate | Intake education form     |
| **Phone Numbers**      | Collection of phone numbers              | Subject aggregate | Intake phone capture      |
| **SSN Storage**        | Encrypted SSN storage                    | Subject aggregate | Already captured          |
| **References**         | Personal/professional references         | Subject aggregate | Intake reference form     |

### Track B: Intake UI Expansion

| Deliverable                 | Description                                    | Components              |
|-----------------------------|------------------------------------------------|-------------------------|
| **Dynamic Form Sections**   | Show/hide sections based on policy             | Policy-driven rendering |
| **Address History Section** | Multi-address entry with date ranges           | AddressHistoryForm      |
| **Employment Section**      | Multi-employer entry with verification consent | EmploymentHistoryForm   |
| **Education Section**       | Multi-institution entry                        | EducationHistoryForm    |
| **Reference Section**       | Reference collection                           | ReferenceForm           |
| **Progress Persistence**    | Save all sections to encrypted snapshot        | IntakeAnswersSnapshot   |

### Track C: Internal UI — Customer Setup

| Deliverable                | Description                          | Components              |
|----------------------------|--------------------------------------|-------------------------|
| **Customer Detail Page**   | Full customer view with tabs         | CustomerDetailPage      |
| **Service Catalog Tab**    | Enable/disable services for customer | ServiceCatalogEditor    |
| **Tier Configuration Tab** | Define execution tiers               | TierConfigurationEditor |
| **Stop Conditions Tab**    | Configure tier stop conditions       | StopConditionEditor     |
| **Vendor Mappings Tab**    | Assign vendors per service type      | VendorMappingEditor     |
| **Policy Snapshot View**   | View customer's policy configuration | PolicySnapshotView      |

### Track D: Internal UI — Order Services & Fulfillment

| Deliverable               | Description                    | Components           |
|---------------------------|--------------------------------|----------------------|
| **Order Services Tab**    | View services for order        | OrderServicesPanel   |
| **Service Status Cards**  | Show status per service        | ServiceStatusCard    |
| **Service Timeline**      | Events per service             | ServiceTimeline      |
| **Fulfillment Dashboard** | Cross-order service visibility | FulfillmentDashboard |
| **Tier Progress View**    | Show tier completion status    | TierProgressView     |
| **Retry/Cancel Actions**  | Ops actions on services        | ServiceActionButtons |

## 3. Subject Domain Model Expansion

### 3.1 Address Collection

```csharp
// Add to Holmes.Subjects.Domain

public sealed class SubjectAddress
{
    public UlidId Id { get; private set; }
    public string Street1 { get; private set; }
    public string? Street2 { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }
    public string? CountyFips { get; private set; }
    public DateOnly FromDate { get; private set; }
    public DateOnly? ToDate { get; private set; }
    public bool IsCurrent => ToDate is null;
    public AddressType Type { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}

public enum AddressType
{
    Residential,
    Mailing,
    Business
}
```

### 3.2 Employment Collection

```csharp
public sealed class SubjectEmployment
{
    public UlidId Id { get; private set; }
    public string EmployerName { get; private set; }
    public string? EmployerPhone { get; private set; }
    public string? EmployerAddress { get; private set; }
    public string? JobTitle { get; private set; }
    public string? SupervisorName { get; private set; }
    public string? SupervisorPhone { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsCurrent => EndDate is null;
    public string? ReasonForLeaving { get; private set; }
    public bool CanContact { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
```

### 3.3 Education Collection

```csharp
public sealed class SubjectEducation
{
    public UlidId Id { get; private set; }
    public string InstitutionName { get; private set; }
    public string? InstitutionAddress { get; private set; }
    public string? Degree { get; private set; }
    public string? Major { get; private set; }
    public DateOnly? AttendedFrom { get; private set; }
    public DateOnly? AttendedTo { get; private set; }
    public DateOnly? GraduationDate { get; private set; }
    public bool Graduated { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
```

### 3.4 Reference Collection

```csharp
public sealed class SubjectReference
{
    public UlidId Id { get; private set; }
    public string Name { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Relationship { get; private set; }
    public int YearsKnown { get; private set; }
    public ReferenceType Type { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}

public enum ReferenceType
{
    Personal,
    Professional
}
```

### 3.5 Enhanced Subject Aggregate

```csharp
public class Subject : AggregateRoot
{
    // Existing
    public UlidId Id { get; }
    public string GivenName { get; }
    public string FamilyName { get; }
    public DateOnly? DateOfBirth { get; }
    public string? Email { get; }
    public IReadOnlyCollection<SubjectAlias> Aliases { get; }

    // NEW: Enhanced data collections
    private readonly List<SubjectAddress> _addresses = new();
    private readonly List<SubjectEmployment> _employments = new();
    private readonly List<SubjectEducation> _educations = new();
    private readonly List<SubjectReference> _references = new();
    private readonly List<SubjectPhone> _phones = new();

    public IReadOnlyCollection<SubjectAddress> Addresses => _addresses.AsReadOnly();
    public IReadOnlyCollection<SubjectEmployment> Employments => _employments.AsReadOnly();
    public IReadOnlyCollection<SubjectEducation> Educations => _educations.AsReadOnly();
    public IReadOnlyCollection<SubjectReference> References => _references.AsReadOnly();
    public IReadOnlyCollection<SubjectPhone> Phones => _phones.AsReadOnly();

    // NEW: Encrypted SSN
    public string? EncryptedSsn { get; private set; }
    public string? SsnLast4 { get; private set; }

    // Methods
    public void AddAddress(SubjectAddress address) { ... }
    public void AddEmployment(SubjectEmployment employment) { ... }
    public void AddEducation(SubjectEducation education) { ... }
    public void AddReference(SubjectReference reference) { ... }
    public void AddPhone(SubjectPhone phone) { ... }
    public void SetSsn(string encryptedSsn, string last4) { ... }
}
```

## 4. Intake UI Expansion

### 4.1 Dynamic Form Schema

The intake form should be policy-driven. The policy snapshot defines what sections are required:

```typescript
// IntakeFormSchema derived from policy
interface IntakeFormSchema {
  sections: {
    identity: { required: true; fields: string[] };
    contact: { required: true; fields: string[] };
    consent: { required: true };
    addresses: {
      required: boolean;
      minCount: number;
      yearsRequired: number;  // e.g., 7 years
    };
    employment: {
      required: boolean;
      minCount: number;
      yearsRequired: number;  // e.g., 10 years
    };
    education: {
      required: boolean;
      minCount: number;
    };
    references: {
      required: boolean;
      minCount: number;
      types: ReferenceType[];  // Personal, Professional
    };
  };
}
```

### 4.2 New Intake Components

```
src/Holmes.Intake/src/components/
├── forms/
│   ├── AddressHistoryForm.tsx      # Multi-address with date ranges
│   ├── EmploymentHistoryForm.tsx   # Multi-employer with contact info
│   ├── EducationHistoryForm.tsx    # Multi-institution
│   ├── ReferenceForm.tsx           # Reference collection
│   └── DynamicFormSection.tsx      # Policy-driven section wrapper
├── steps/
│   ├── DataStep.tsx                # ENHANCED: orchestrates all data sections
│   ├── AddressesStep.tsx           # NEW: dedicated addresses step
│   ├── EmploymentStep.tsx          # NEW: dedicated employment step
│   ├── EducationStep.tsx           # NEW: dedicated education step
│   └── ReferencesStep.tsx          # NEW: dedicated references step
```

### 4.3 Intake Step Flow (Enhanced)

```
1. Verify     - Invite validation, device fingerprint
2. OTP        - SMS verification
3. Consent    - Disclosure agreement
4. Identity   - Name, DOB, SSN (last 4), email, phone
5. Addresses  - Current + previous (7 years)
6. Employment - Current + previous (10 years) [if policy requires]
7. Education  - Degrees/certifications [if policy requires]
8. References - Personal/professional [if policy requires]
9. Review     - Summary of all entered data
10. Success   - Confirmation
```

## 5. Internal UI — Customer Setup

### 5.1 Customer Detail Page

```typescript
// CustomerDetailPage.tsx
const CustomerDetailPage = () => {
  const tabs = [
    { id: 'overview', label: 'Overview', component: CustomerOverview },
    { id: 'admins', label: 'Administrators', component: CustomerAdmins },
    { id: 'services', label: 'Services', component: ServiceCatalogEditor },
    { id: 'tiers', label: 'Tiers', component: TierConfigurationEditor },
    { id: 'policy', label: 'Policy', component: PolicySnapshotView },
    { id: 'orders', label: 'Orders', component: CustomerOrders },
  ];

  return <TabbedDetailPage tabs={tabs} />;
};
```

### 5.2 Service Catalog Editor

```typescript
interface ServiceCatalogEditorProps {
  customerId: string;
}

// Shows all available services with toggles
// Allows setting tier assignment per service
// Allows setting vendor preference per service
// Saves to ServiceCatalog aggregate
```

### 5.3 Tier Configuration Editor

```typescript
interface TierConfig {
  tier: number;
  services: string[];          // Service type codes in this tier
  stopConditions: string[];    // What halts downstream tiers
  parallelWithPrevious: boolean; // Can run with previous tier
}

// Visual tier builder
// Drag-drop services between tiers
// Configure stop conditions per tier
```

## 6. Internal UI — Order Services & Fulfillment

### 6.1 Order Detail Enhancement

Add Services tab to Order Detail Page:

```typescript
// OrderServicesPanel.tsx
const OrderServicesPanel = ({ orderId }: { orderId: string }) => {
  const { data: services } = useOrderServices(orderId);

  return (
    <Box>
      <TierProgressView services={services} />
      <ServiceGrid>
        {services.map(service => (
          <ServiceStatusCard
            key={service.id}
            service={service}
            onRetry={() => retryService(service.id)}
            onCancel={() => cancelService(service.id)}
          />
        ))}
      </ServiceGrid>
    </Box>
  );
};
```

### 6.2 Service Status Card

```typescript
interface ServiceStatusCardProps {
  service: {
    id: string;
    serviceType: string;
    tier: number;
    status: ServiceStatus;
    vendorCode: string | null;
    vendorReferenceId: string | null;
    dispatchedAt: string | null;
    completedAt: string | null;
    failedAt: string | null;
    attemptCount: number;
    lastError: string | null;
    result: ServiceResult | null;
  };
  onRetry: () => void;
  onCancel: () => void;
}

// Shows:
// - Service type with icon
// - Current status with color indicator
// - Tier badge
// - Vendor assignment
// - Timing info (dispatched, completed, duration)
// - Result summary (Clear/Hit/Error)
// - Action buttons (Retry for Failed, Cancel for Pending)
```

### 6.3 Fulfillment Dashboard

New top-level page for ops to monitor all in-flight services:

```typescript
// FulfillmentDashboard.tsx
const FulfillmentDashboard = () => {
  return (
    <Box>
      {/* Summary cards */}
      <Grid container spacing={2}>
        <SummaryCard title="Pending" count={pending} />
        <SummaryCard title="In Progress" count={inProgress} />
        <SummaryCard title="At Risk" count={atRisk} color="warning" />
        <SummaryCard title="Failed" count={failed} color="error" />
      </Grid>

      {/* Filters */}
      <ServiceFilters
        vendors={vendors}
        categories={categories}
        statuses={statuses}
      />

      {/* Service list */}
      <ServiceDataGrid services={filteredServices} />
    </Box>
  );
};
```

## 7. API Endpoints

### 7.1 Subject Data Endpoints (for Intake)

```
# Intake submission (enhanced)
POST /intake/sessions/{sessionId}/submit
{
  "identity": { ... },
  "addresses": [ { street1, city, state, postalCode, fromDate, toDate }, ... ],
  "employments": [ { employerName, jobTitle, startDate, endDate, ... }, ... ],
  "educations": [ { institutionName, degree, graduationDate, ... }, ... ],
  "references": [ { name, phone, relationship, ... }, ... ]
}

# Subject data (internal read)
GET /api/subjects/{id}/addresses
GET /api/subjects/{id}/employments
GET /api/subjects/{id}/educations
GET /api/subjects/{id}/references
```

### 7.2 Customer Configuration Endpoints

```
# Service catalog
GET  /api/customers/{id}/service-catalog
PUT  /api/customers/{id}/service-catalog
POST /api/customers/{id}/service-catalog/snapshot

# Tier configuration
GET  /api/customers/{id}/tiers
PUT  /api/customers/{id}/tiers
```

### 7.3 Order Services Endpoints

```
GET  /api/orders/{orderId}/services
GET  /api/orders/{orderId}/services/{serviceId}
POST /api/orders/{orderId}/services/{serviceId}/retry
POST /api/orders/{orderId}/services/{serviceId}/cancel
```

### 7.4 Fulfillment Dashboard Endpoints

```
GET /api/fulfillment/summary
GET /api/fulfillment/services?status=...&vendor=...&category=...
GET /api/fulfillment/at-risk
```

## 8. Database Schema Additions

### 8.1 Subject Collections

```sql
-- Subject addresses
CREATE TABLE subjects.subject_addresses (
    id CHAR(26) PRIMARY KEY,
    subject_id CHAR(26) NOT NULL,
    street1 VARCHAR(256) NOT NULL,
    street2 VARCHAR(256),
    city VARCHAR(128) NOT NULL,
    state VARCHAR(8) NOT NULL,
    postal_code VARCHAR(16) NOT NULL,
    country VARCHAR(3) NOT NULL DEFAULT 'USA',
    county_fips VARCHAR(5),
    from_date DATE NOT NULL,
    to_date DATE,
    address_type INT NOT NULL DEFAULT 0,
    created_at DATETIME(6) NOT NULL,

    FOREIGN KEY (subject_id) REFERENCES subjects(id),
    INDEX idx_subject (subject_id)
);

-- Subject employments
CREATE TABLE subjects.subject_employments (
    id CHAR(26) PRIMARY KEY,
    subject_id CHAR(26) NOT NULL,
    employer_name VARCHAR(256) NOT NULL,
    employer_phone VARCHAR(32),
    employer_address VARCHAR(512),
    job_title VARCHAR(128),
    supervisor_name VARCHAR(128),
    supervisor_phone VARCHAR(32),
    start_date DATE NOT NULL,
    end_date DATE,
    reason_for_leaving VARCHAR(256),
    can_contact TINYINT(1) NOT NULL DEFAULT 1,
    created_at DATETIME(6) NOT NULL,

    FOREIGN KEY (subject_id) REFERENCES subjects(id),
    INDEX idx_subject (subject_id)
);

-- Subject educations
CREATE TABLE subjects.subject_educations (
    id CHAR(26) PRIMARY KEY,
    subject_id CHAR(26) NOT NULL,
    institution_name VARCHAR(256) NOT NULL,
    institution_address VARCHAR(512),
    degree VARCHAR(128),
    major VARCHAR(128),
    attended_from DATE,
    attended_to DATE,
    graduation_date DATE,
    graduated TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME(6) NOT NULL,

    FOREIGN KEY (subject_id) REFERENCES subjects(id),
    INDEX idx_subject (subject_id)
);

-- Subject references
CREATE TABLE subjects.subject_references (
    id CHAR(26) PRIMARY KEY,
    subject_id CHAR(26) NOT NULL,
    name VARCHAR(128) NOT NULL,
    phone VARCHAR(32),
    email VARCHAR(256),
    relationship VARCHAR(64),
    years_known INT,
    reference_type INT NOT NULL DEFAULT 0,
    created_at DATETIME(6) NOT NULL,

    FOREIGN KEY (subject_id) REFERENCES subjects(id),
    INDEX idx_subject (subject_id)
);

-- Subject phones
CREATE TABLE subjects.subject_phones (
    id CHAR(26) PRIMARY KEY,
    subject_id CHAR(26) NOT NULL,
    phone_number VARCHAR(32) NOT NULL,
    phone_type INT NOT NULL DEFAULT 0,
    is_primary TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME(6) NOT NULL,

    FOREIGN KEY (subject_id) REFERENCES subjects(id),
    INDEX idx_subject (subject_id)
);

-- SSN storage (encrypted)
ALTER TABLE subjects.subjects
    ADD COLUMN encrypted_ssn VARBINARY(256),
    ADD COLUMN ssn_last4 CHAR(4);
```

## 9. Implementation Order

### Phase 3.2.1 — Subject Domain Expansion (Backend)

1. Add SubjectAddress, SubjectEmployment, SubjectEducation, SubjectReference, SubjectPhone to Subject aggregate
2. Create EF Core configurations and migrations
3. Update Subject repository with collection loading
4. Add commands: AddSubjectAddress, AddSubjectEmployment, etc.
5. Update IntakeAnswersSnapshot to include rich data
6. Update SubmitIntakeCommand to persist all collections

### Phase 3.2.2 — Intake UI Enhancement

1. Create AddressHistoryForm component
2. Create EmploymentHistoryForm component
3. Create EducationHistoryForm component
4. Create ReferenceForm component
5. Update DataStep to use dynamic sections based on policy
6. Add new steps to intake flow
7. Update progress persistence to include all sections
8. Update review step to show all collected data

### Phase 3.2.3 — Internal UI — Customer Setup

1. Create CustomerDetailPage with tabs
2. Create ServiceCatalogEditor component
3. Create TierConfigurationEditor component
4. Add customer service/tier API endpoints
5. Wire up to ServiceCatalog aggregate

### Phase 3.2.4 — Internal UI — Order Services

1. Add Services tab to OrderDetailPage
2. Create ServiceStatusCard component
3. Create TierProgressView component
4. Add retry/cancel actions
5. Add SSE streaming for service status updates

### Phase 3.2.5 — Internal UI — Fulfillment Dashboard

1. Create FulfillmentDashboard page
2. Add to navigation
3. Create summary cards
4. Create service filters
5. Create service data grid
6. Add real-time updates

## 10. Acceptance Criteria

### Subject Data

- [ ] Subject aggregate supports addresses, employments, educations, references, phones
- [ ] SSN stored encrypted with last 4 accessible
- [ ] All collections have proper EF Core mappings
- [ ] Intake submission persists all collections to Subject

### Intake UI

- [ ] Address history form captures 7+ years based on policy
- [ ] Employment history form captures 10+ years based on policy
- [ ] Education form captures degrees/certifications
- [ ] Reference form captures required references
- [ ] Form sections show/hide based on policy configuration
- [ ] All data persists to encrypted progress snapshot
- [ ] Review step shows all entered data

### Customer Setup UI

- [ ] Customer detail page with tabbed navigation
- [ ] Service catalog shows all available services
- [ ] Services can be enabled/disabled per customer
- [ ] Tier assignments configurable
- [ ] Stop conditions configurable
- [ ] Changes save to ServiceCatalog aggregate

### Order Services UI

- [ ] Order detail shows Services tab
- [ ] Each service shows status, vendor, timing
- [ ] Tier progress visible
- [ ] Retry button works for failed services
- [ ] Cancel button works for pending services
- [ ] Real-time updates via SSE

### Fulfillment Dashboard

- [ ] Dashboard accessible from nav
- [ ] Summary cards show correct counts
- [ ] Filters work (vendor, category, status)
- [ ] Grid shows service details
- [ ] Drill-down to order works

## 11. Risks & Mitigations

| Risk                                  | Impact | Mitigation                                                                      |
|---------------------------------------|--------|---------------------------------------------------------------------------------|
| Form complexity overwhelming subjects | High   | Progressive disclosure; show sections based on policy; save progress frequently |
| SSN encryption key management         | High   | Use existing encryption infrastructure; never log SSN                           |
| Policy schema changes breaking intake | Medium | Version policy schema; migration path for in-flight sessions                    |
| Large subject data slowing queries    | Medium | Eager loading strategies; read model projections                                |

## 12. Dependencies

- Phase 3.1 Services backend (completed)
- Existing intake infrastructure
- Existing customer/order infrastructure
- SSE streaming infrastructure (exists)

---

This document is the authoritative reference for Phase 3.2 delivery. It should be updated at each checkpoint to reflect
decisions, scope changes, and readiness status.
