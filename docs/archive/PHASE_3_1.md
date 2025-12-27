# Phase 3.1 Delivery Plan — Services & Fulfillment

**Phase Window:** Weeks 8–10
**Outcome Target:** Orders route to background check services, vendor integrations execute via anti-corruption layer,
results normalize into canonical schema, and Orders advance based on service completion.

**Commercial Alignment:** This phase delivers the **Fulfillment Engine** — the heart of background screening where
actual work happens. Without this, Holmes is just intake and SLAs; with it, Holmes orchestrates real background checks.

## 1. The Big Picture: Why Services Matter

Background screening is fundamentally about **services**: discrete units of work that external vendors perform.

- A "Criminal Background Check" isn't one thing — it's potentially dozens of county searches, state repositories,
  federal databases, and watch lists, each with different vendors, SLAs, and result formats.
- An "Employment Verification" might go to The Work Number (TWN), a direct verification vendor, or require manual
  outreach — each path has different costs and turnaround times.
- The variety is vast: Criminal, Employment, Education, MVR, Drug, Credit, Identity, Reference, Civil, Healthcare —
  and each category has subtypes.

**The DDD Question:** Where do Services live?

**Answer:** Services are a **separate bounded context** (`Holmes.Services`) with `Service` as an aggregate root.

**Why not child entities of Order?**

- Services have independent lifecycles (async vendor callbacks, retries at different times)
- Order aggregate would become massive with N service types
- Contention: updates to one service shouldn't lock the entire Order
- Services need their own SLA tracking independent of Order-level SLAs
- Anti-corruption layer is cleaner as a separate concern

**Relationship to Order:**

- Order *references* Services by ID
- `OrderRoutingService` creates Services when Order reaches `ReadyForRouting`
- Order subscribes to `ServiceCompleted` events to know when to advance
- Order transitions to `ReadyForReport` when all required services complete

## 2. Stakeholders & Working Cadence

- **Domain Steward (Eric Evans):** Facilitates event-storming for Services bounded context, validates aggregate
  boundaries and vendor integration patterns.
- **Product Lead:** Owns service type taxonomy, package composition rules, vendor prioritization.
- **Tech Leads (Backend & Infrastructure):** Own aggregate implementation, anti-corruption layer, vendor adapter
  contracts.
- **Ops Partner:** Validates fulfillment dashboards, service-level SLA alerts, vendor health monitoring.

Standing ceremonies:

1. **Event Storm (2 × 2 hrs):** Map service lifecycle, vendor integration patterns, result normalization.
2. **Vendor Workshop (once):** Define adapter contract, callback handling, credential management.
3. **Build Checkpoint (twice weekly):** Track aggregate progress, adapter implementation, routing logic.
4. **Integration Review (weekly):** Test end-to-end flow from Order routing through service completion.

## 3. Scope Breakdown

| Track                     | Deliverables                                                                     | Definition of Done                                                                                     |
|---------------------------|----------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------|
| **Service Aggregate**     | `Service` with state machine, tier assignment, vendor assignment, result storage | State transitions fire domain events; results stored in normalized schema; aggregate replayable        |
| **Service Tiering**       | Customer-defined execution tiers with stop conditions                            | Tier 1 completes before Tier 2 dispatches; stop conditions halt downstream tiers; parallel mode option |
| **Service Type Taxonomy** | `ServiceType` enum/value object with categories and specific types               | All common service types defined; extensible for custom types                                          |
| **Service Catalog**       | `ServiceCatalog` aggregate for customer-specific service configurations          | Customer can enable/disable service types; tier assignments; vendor mappings per customer              |
| **Vendor Adapter Layer**  | `IVendorAdapter` interface, `IVendorCredentialStore`, `StubVendorAdapter`        | Adapters translate vendor protocols; credentials fetched from secure store; stub returns fixture data  |
| **Order Routing**         | `OrderRoutingService` that determines services from package + policy             | Package code maps to service list; services created with tier assignments when Order ready for routing |
| **Service SLA Clocks**    | Optional service-level SLA tracking                                              | Clock starts on dispatch; at-risk/breach detection; customer-configurable targets                      |
| **Address History**       | Subject enhancement with address collection                                      | Addresses captured during intake; county FIPS derivation for criminal searches                         |
| **Read Models**           | `service_requests`, `service_results`, `fulfillment_dashboard`                   | Projections replayable; dashboard shows in-flight services by tier                                     |

## 4. Domain Model

### 4.1 Service Aggregate

**Bounded Context:** `Holmes.Services`

**Location:** `src/Modules/Services/`

```
Holmes.Services.Domain/
  Service.cs           # Aggregate root
  ServiceCatalog.cs           # Aggregate root (customer service config)
  ServiceType.cs              # Value object / enum
  ServiceScope.cs             # Geographic scope
  ServiceStatus.cs            # State enum
  ServiceResult.cs            # Normalized result
  VendorAssignment.cs         # Vendor binding (value object)
  Events/
    ServiceCreated.cs
    ServiceDispatched.cs
    ServiceResultReceived.cs
    ServiceCompleted.cs
    ServiceFailed.cs
    ServiceCanceled.cs
    ServiceRetried.cs
  IServiceRepository.cs
  IServicesUnitOfWork.cs

Holmes.Services.Infrastructure.Sql/
  IVendorCredentialStore.cs   # Infrastructure port
  VendorCredentialStore.cs    # Retrieves credentials from vault/encrypted store
```

> **Note:** `VendorCredential` is NOT a domain aggregate. Credentials are operational infrastructure
> (API keys, account IDs) retrieved at dispatch time via `IVendorCredentialStore`. This keeps
> secrets management out of the domain layer and allows swapping implementations (Key Vault,
> Secrets Manager, encrypted DB) without domain changes.

**States:**

```
Pending → Dispatched → InProgress → Completed
                   ↘            ↗
                     → Failed → (Retry) →
                   ↘
                     → Canceled
```

**Invariants:**

- Must have OrderId and ServiceType
- Cannot dispatch without VendorAssignment
- Cannot complete without ServiceResult
- Failed requests can retry up to max attempts
- Canceled requests cannot be reactivated
- Tier N services cannot dispatch until Tier N-1 completes (unless customer opts out)

### 4.2 Service Tiering

Services execute in **customer-defined tiers** to optimize cost and enable fail-fast behavior.

**Why Tiering Matters:**

- **Cost optimization:** Don't pay $50 for a drug test if the $2 SSN trace returns no-match
- **Fail-fast:** Disqualifying results in early tiers can halt expensive downstream work
- **Compliance gates:** Some jurisdictions require identity verification before criminal checks
- **Customer control:** Each customer defines their own tier assignments and stop conditions

**Default Tier Structure (customer-overridable):**

| Tier  | Services                                                          | Rationale                             |
|-------|-------------------------------------------------------------------|---------------------------------------|
| **1** | SSN Trace, SSN Verification, Address Verification, OFAC/Sanctions | Cheap, fast, identity validation      |
| **2** | Federal Criminal, Statewide, County Searches, Sex Offender        | Core criminal screening               |
| **3** | Employment Verification, Education Verification                   | Slower, requires outreach             |
| **4** | Drug Test, Physical, MVR, Credit Check                            | Expensive, only if earlier tiers pass |

**Tier Execution Logic:**

```csharp
public sealed class TierExecutionService
{
    public async Task ExecuteNextTierAsync(UlidId orderId)
    {
        var services = await _repo.GetByOrderIdAsync(orderId);
        var currentTier = GetLowestIncompleteTier(services);

        // Check if current tier is complete
        var tierServices = services.Where(s => s.Tier == currentTier);
        if (tierServices.All(s => s.Status == ServiceStatus.Completed))
        {
            // Check for stop conditions
            if (HasStopCondition(tierServices))
            {
                await HandleStopCondition(orderId, currentTier);
                return;
            }

            // Dispatch next tier
            var nextTier = currentTier + 1;
            var nextServices = services.Where(s => s.Tier == nextTier && s.Status == ServiceStatus.Pending);
            foreach (var service in nextServices)
            {
                await _sender.Send(new DispatchServiceCommand(service.Id));
            }
        }
    }
}
```

**Stop Conditions (customer-defined):**

- `SSNMismatch` — SSN doesn't match provided info
- `DeceasedRecord` — Subject appears on Death Master File
- `SanctionsHit` — OFAC/watchlist match requires manual review
- `IdentityNotVerified` — Cannot confirm identity

**ServiceCatalog Configuration:**

```yaml
# Customer service catalog (stored as JSON in DB)
customer_id: cust_acme_01
tiers:
  1:
    services: [SSNTrace, AddressVerification, OFAC]
    stop_on: [SSNMismatch, DeceasedRecord, SanctionsHit]
  2:
    services: [FederalCriminal, StatewideSearch, CountySearch]
    stop_on: []  # No auto-stop, proceed to adjudication
  3:
    services: [EmploymentVerification, EducationVerification]
    stop_on: []
  4:
    services: [DrugTest, MVR]
    stop_on: []
parallel_mode: false  # true = ignore tiers, run everything at once
```

### 4.2.1 ServiceCatalog Snapshotting

**Why Snapshot?** Customer configuration changes over time — adding services, reordering tiers, adjusting stop
conditions. Like `PolicySnapshotId` on Order, we snapshot the ServiceCatalog at routing time so:

- **In-flight orders are immutable** — config changes don't affect orders already routing
- **Audit trail** — reconstruct exactly which services/tiers were configured for any order
- **No mid-order surprises** — adding a new service type doesn't retroactively insert it into existing orders

**Pattern:**

```csharp
// When Order reaches ReadyForRouting:
public async Task Handle(OrderStatusChanged notification, ...)
{
    if (notification.NewStatus != OrderStatus.ReadyForRouting)
        return;

    var order = await _orderRepo.GetByIdAsync(notification.OrderId);

    // Snapshot the customer's current service catalog
    var catalogSnapshot = await _catalogService.CreateSnapshotAsync(order.CustomerId);

    // Store snapshot reference on the routing context
    var services = _routingService.DetermineServices(
        order.PackageCode,
        catalogSnapshot,  // Uses snapshotted config
        subject,
        policy);

    // Services reference the snapshot
    foreach (var service in services)
    {
        await _sender.Send(new CreateServiceCommand(
            order.Id,
            catalogSnapshot.Id,  // Link to snapshot
            service.Type,
            service.Tier,
            service.Scope,
            service.VendorAssignment));
    }
}
```

**Schema:**

```sql
CREATE TABLE services.service_catalog_snapshots (
    id CHAR(26) PRIMARY KEY,
    customer_id CHAR(26) NOT NULL,
    version INT NOT NULL,
    config_json JSON NOT NULL,           -- Full tiering/service config
    created_at DATETIME(6) NOT NULL,
    created_by CHAR(26),                 -- System or user who triggered

    UNIQUE KEY idx_customer_version (customer_id, version),
    INDEX idx_customer (customer_id)
);

-- Service references the snapshot
ALTER TABLE services.service_requests
    ADD COLUMN catalog_snapshot_id CHAR(26) AFTER customer_id,
    ADD FOREIGN KEY (catalog_snapshot_id) REFERENCES service_catalog_snapshots(id);
```

**Analogy to PolicySnapshotId:**

| Concept          | Policy                                              | ServiceCatalog                                            |
|------------------|-----------------------------------------------------|-----------------------------------------------------------|
| What it captures | SLA rules, intake requirements, adjudication matrix | Tiers, stop conditions, vendor mappings, enabled services |
| When snapshotted | Order creation                                      | Order routing                                             |
| Stored on        | `Order.PolicySnapshotId`                            | `Service.CatalogSnapshotId`                               |
| Ensures          | Policy changes don't affect in-flight orders        | Service config changes don't affect in-flight orders      |

### 4.3 Service Type Taxonomy

```csharp
public enum ServiceCategory
{
    Criminal,
    Identity,
    Employment,
    Education,
    Driving,
    Credit,
    Drug,
    Civil,
    Reference,
    Healthcare,
    Custom
}

public enum CriminalServiceType
{
    FederalCriminal,
    StatewideSearch,
    CountySearch,
    MunicipalSearch,
    SexOffenderRegistry,
    GlobalWatchlist,
    OFACSanctions
}

public enum IdentityServiceType
{
    SSNVerification,
    SSNTrace,
    AddressVerification,
    IdentityVerification,
    DeathMasterFile
}

public enum EmploymentServiceType
{
    TWNEmploymentVerification,
    DirectEmploymentVerification,
    IncomeVerification,
    I9Verification
}

public enum EducationServiceType
{
    EducationVerification,
    ProfessionalLicense,
    Certification
}

public enum DrivingServiceType
{
    MVR,
    CDLVerification,
    PSPReport
}

// ... additional categories
```

### 4.4 ServiceResult (Normalized)

```csharp
public sealed class ServiceResult
{
    public UlidId Id { get; init; }
    public ServiceResultStatus Status { get; init; }  // Clear, Hit, UnableToVerify, Error
    public IReadOnlyList<NormalizedRecord> Records { get; init; }
    public string? RawResponseHash { get; init; }     // SHA-256 of vendor response
    public DateTimeOffset ReceivedAt { get; init; }
    public DateTimeOffset? NormalizedAt { get; init; }
    public string? VendorReferenceId { get; init; }
}

public enum ServiceResultStatus
{
    Clear,           // No records found
    Hit,             // Records found (see Records collection)
    UnableToVerify,  // Source unavailable or data insufficient
    Error            // Vendor error
}
```

### 4.5 NormalizedRecord (polymorphic)

```csharp
public abstract record NormalizedRecord
{
    public UlidId Id { get; init; }
    public string RecordType { get; init; }  // Discriminator
    public string? SourceJurisdiction { get; init; }
    public DateTimeOffset? RecordDate { get; init; }
    public string? RawRecordHash { get; init; }
}

public sealed record CriminalRecord : NormalizedRecord
{
    public string? CaseNumber { get; init; }
    public string? Court { get; init; }
    public string? ChargeDescription { get; init; }
    public string? ChargeCategory { get; init; }     // From taxonomy
    public ChargeSeverity? Severity { get; init; }   // Felony, Misdemeanor, Infraction
    public string? Disposition { get; init; }        // Convicted, Dismissed, etc.
    public DateOnly? DispositionDate { get; init; }
    public DateOnly? OffenseDate { get; init; }
    public string? Sentence { get; init; }
}

public sealed record EmploymentRecord : NormalizedRecord
{
    public string? EmployerName { get; init; }
    public string? JobTitle { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool? CurrentlyEmployed { get; init; }
    public string? VerificationSource { get; init; }  // TWN, Direct, etc.
}

public sealed record EducationRecord : NormalizedRecord
{
    public string? InstitutionName { get; init; }
    public string? Degree { get; init; }
    public string? Major { get; init; }
    public DateOnly? GraduationDate { get; init; }
    public bool? Verified { get; init; }
}

// ... additional record types
```

## 5. Anti-Corruption Layer

### 5.1 Vendor Adapter Interface

```csharp
public interface IVendorAdapter
{
    string VendorCode { get; }
    IEnumerable<ServiceCategory> SupportedCategories { get; }

    Task<DispatchResult> DispatchAsync(
        Service request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> ParseCallbackAsync(
        string callbackPayload,
        CancellationToken cancellationToken = default);

    Task<ServiceStatusResult> GetStatusAsync(
        string vendorReferenceId,
        CancellationToken cancellationToken = default);
}

// Credentials fetched internally by adapter via IVendorCredentialStore
public interface IVendorCredentialStore
{
    Task<VendorCredential?> GetAsync(
        UlidId customerId,
        string vendorCode,
        CancellationToken cancellationToken = default);
}

public record VendorCredential(
    string ApiKey,           // Encrypted at rest
    string? AccountId,
    string? SecretKey,
    Dictionary<string, string>? Metadata);
}

public sealed record DispatchResult(
    bool Success,
    string? VendorReferenceId,
    string? ErrorMessage,
    TimeSpan? EstimatedTurnaround);

public sealed record ServiceStatusResult(
    ServiceResultStatus Status,
    bool IsComplete,
    string? StatusMessage);
```

### 5.2 Stub Vendor Adapter (Phase 3.1)

```csharp
public sealed class StubVendorAdapter : IVendorAdapter
{
    public string VendorCode => "STUB";
    public IEnumerable<ServiceCategory> SupportedCategories =>
        Enum.GetValues<ServiceCategory>();

    public async Task<DispatchResult> DispatchAsync(...)
    {
        // Simulate network delay
        await Task.Delay(TimeSpan.FromMilliseconds(100));

        return new DispatchResult(
            Success: true,
            VendorReferenceId: $"STUB-{Guid.NewGuid():N}",
            ErrorMessage: null,
            EstimatedTurnaround: TimeSpan.FromSeconds(5));
    }

    public async Task<ServiceResult> ParseCallbackAsync(...)
    {
        // Return fixture data based on service type
        return LoadFixtureResult(callbackPayload);
    }
}
```

### 5.3 Callback Webhook Endpoint

```
POST /api/vendors/{vendorCode}/callback
```

- Receives vendor callbacks with raw payloads
- Routes to appropriate adapter's `ParseCallbackAsync`
- Emits `RecordServiceResultCommand`
- Returns 200 OK to vendor (idempotent)

### 5.4 Service Dispatcher Background Service

`ServiceDispatcherService` polls for `Pending` requests and dispatches them:

```csharp
public sealed class ServiceDispatcherService : BackgroundService
{
    // Poll for pending requests
    // Dispatch via appropriate adapter
    // Update request to Dispatched or Failed
    // Handle rate limiting per vendor
}
```

## 6. Order Integration

### 6.1 Order Routing Service

When Order emits `OrderStatusChanged` to `ReadyForRouting`:

1. `OrderRoutingHandler` receives event
2. Loads Package definition (e.g., "EMP_STD_US")
3. Loads Subject's address history for county determination
4. Applies Policy rules (address_years, excluded jurisdictions)
5. Creates `Service` for each required service
6. Transitions Order to `RoutingInProgress`

```csharp
public sealed class OrderRoutingHandler : INotificationHandler<OrderStatusChanged>
{
    public async Task Handle(OrderStatusChanged notification, ...)
    {
        if (notification.NewStatus != OrderStatus.ReadyForRouting)
            return;

        var order = await _orderRepo.GetByIdAsync(notification.OrderId);
        var package = await _packageService.GetPackageAsync(order.PackageCode);
        var subject = await _subjectRepo.GetByIdAsync(order.SubjectId);
        var policy = await _policyService.GetPolicyAsync(order.PolicySnapshotId);

        var services = _routingService.DetermineServices(package, subject, policy);

        foreach (var service in services)
        {
            await _sender.Send(new CreateServiceCommand(
                order.Id,
                service.Type,
                service.Scope,
                service.VendorAssignment));
        }

        await _sender.Send(new BeginOrderRoutingCommand(order.Id));
    }
}
```

### 6.2 Service Completion Handler

When all services complete, Order can advance:

```csharp
public sealed class ServiceCompletedOrderHandler : INotificationHandler<ServiceCompleted>
{
    public async Task Handle(ServiceCompleted notification, ...)
    {
        var allServices = await _serviceRepo.GetByOrderIdAsync(notification.OrderId);

        var allComplete = allServices.All(s =>
            s.Status == ServiceStatus.Completed ||
            s.Status == ServiceStatus.Canceled);

        if (allComplete)
        {
            await _sender.Send(new MarkOrderReadyForReportCommand(notification.OrderId));
        }
    }
}
```

## 7. Subject Address History

### 7.1 Domain Enhancement

Add to `Holmes.Subjects.Domain`:

```csharp
public sealed class Address
{
    public UlidId Id { get; init; }
    public string Street1 { get; init; }
    public string? Street2 { get; init; }
    public string City { get; init; }
    public string State { get; init; }       // ISO 3166-2
    public string PostalCode { get; init; }
    public string Country { get; init; }     // ISO 3166-1
    public DateOnly FromDate { get; init; }
    public DateOnly? ToDate { get; init; }   // null = current
    public bool IsCurrent => ToDate is null;
    public string? CountyFips { get; init; } // For US addresses
}
```

### 7.2 County FIPS Resolution

`ICountyResolutionService`:

```csharp
public interface ICountyResolutionService
{
    Task<string?> GetCountyFipsAsync(
        string street,
        string city,
        string state,
        string postalCode,
        CancellationToken cancellationToken = default);
}
```

Phase 3.1: Use ZIP-to-County lookup table (covers ~95% of cases).
Future: Integrate Census Geocoder API for precise resolution.

### 7.3 Address Collection During Intake

Intake session collects addresses based on Policy `address_years`:

```yaml
# From Policy
intake:
  address_years: 7
```

UI presents address history form; `SubmitIntakeCommand` includes addresses.

## 8. Service-Level SLA Clocks

### 8.1 Integration with SlaClocks Module

When `ServiceDispatched` fires:

```csharp
public sealed class ServiceDispatchedSlaHandler : INotificationHandler<ServiceDispatched>
{
    public async Task Handle(ServiceDispatched notification, ...)
    {
        var service = await _serviceRepo.GetByIdAsync(notification.ServiceId);
        var config = await _catalogService.GetServiceConfigAsync(
            service.CustomerId, service.ServiceType);

        if (config.SlaBusinessDays.HasValue)
        {
            await _sender.Send(new StartSlaClockCommand(
                orderId: service.OrderId,
                kind: ClockKind.Service,
                serviceRequestId: service.Id,
                targetBusinessDays: config.SlaBusinessDays.Value));
        }
    }
}
```

### 8.2 ClockKind Extension

```csharp
public enum ClockKind
{
    Intake,
    Fulfillment,
    Overall,
    Service,     // NEW: individual service SLA
    Custom
}
```

## 9. Database Schema

### 9.1 service_requests

```sql
CREATE TABLE services.service_requests (
    id CHAR(26) PRIMARY KEY,
    order_id CHAR(26) NOT NULL,
    customer_id CHAR(26) NOT NULL,
    service_category INT NOT NULL,
    service_type VARCHAR(64) NOT NULL,
    scope_type VARCHAR(32),
    scope_value VARCHAR(128),          -- FIPS code, state, etc.
    status INT NOT NULL,
    vendor_code VARCHAR(32),
    vendor_reference_id VARCHAR(256),
    dispatched_at DATETIME(6),
    completed_at DATETIME(6),
    failed_at DATETIME(6),
    canceled_at DATETIME(6),
    attempt_count INT DEFAULT 0,
    max_attempts INT DEFAULT 3,
    last_error VARCHAR(1024),
    created_at DATETIME(6) NOT NULL,
    updated_at DATETIME(6) NOT NULL,

    INDEX idx_order (order_id),
    INDEX idx_customer_status (customer_id, status),
    INDEX idx_status_created (status, created_at),
    INDEX idx_vendor_ref (vendor_code, vendor_reference_id)
);
```

### 9.2 service_results

```sql
CREATE TABLE services.service_results (
    id CHAR(26) PRIMARY KEY,
    service_request_id CHAR(26) NOT NULL,
    result_status INT NOT NULL,
    records_json JSON,
    raw_response_hash VARCHAR(64),
    vendor_reference_id VARCHAR(256),
    received_at DATETIME(6) NOT NULL,
    normalized_at DATETIME(6),

    FOREIGN KEY (service_request_id) REFERENCES service_requests(id),
    INDEX idx_service_request (service_request_id)
);
```

### 9.3 subject_addresses

```sql
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
    created_at DATETIME(6) NOT NULL,

    FOREIGN KEY (subject_id) REFERENCES subjects(id),
    INDEX idx_subject (subject_id),
    INDEX idx_subject_dates (subject_id, from_date, to_date)
);
```

## 10. API Surface

### 10.1 Services

```
GET  /api/orders/{orderId}/services           # List services for order
GET  /api/services/{serviceId}                 # Get service details
GET  /api/services/{serviceId}/result          # Get normalized result
POST /api/services/{serviceId}/retry           # Retry failed service (ops)
POST /api/services/{serviceId}/cancel          # Cancel pending service (ops)
```

### 10.2 Vendor Callbacks

```
POST /api/vendors/{vendorCode}/callback        # Receive vendor callback
GET  /api/vendors/{vendorCode}/status/{ref}    # Poll vendor status (fallback)
```

### 10.3 Service Catalog (Admin)

```
GET  /api/admin/service-catalog                # List available services
GET  /api/admin/customers/{id}/services        # Customer service config
PUT  /api/admin/customers/{id}/services/{type} # Update service config
```

## 11. Acceptance Checklist

1. **Service Aggregate:**
    - [ ] State machine enforces valid transitions
    - [ ] Domain events fire on state changes
    - [ ] Results stored in normalized schema
    - [ ] Retry logic respects max attempts
    - [ ] Tier assignment stored and queryable

2. **Service Tiering:**
    - [ ] Tier 1 services dispatch immediately on routing
    - [ ] Tier N waits for Tier N-1 completion
    - [ ] Stop conditions halt downstream tiers
    - [ ] Parallel mode bypasses tier ordering
    - [ ] Customer-specific tier configuration honored
    - [ ] ServiceCatalog snapshotted at routing time
    - [ ] Services reference catalog snapshot ID

3. **Service Type Taxonomy:**
    - [ ] All common service types defined
    - [ ] Category/type hierarchy queryable
    - [ ] Extensible for custom types

4. **Vendor Adapter Layer:**
    - [ ] `IVendorAdapter` contract supports real vendors
    - [ ] `StubVendorAdapter` returns fixture data
    - [ ] Callback webhook receives and routes payloads
    - [ ] Dispatch service polls and dispatches

5. **Order Integration:**
    - [ ] Package routes to correct services with tier assignments
    - [ ] Services created on `ReadyForRouting`
    - [ ] Order advances when all services complete
    - [ ] Canceled services don't block Order

6. **Address History:**
    - [ ] Subject stores address collection
    - [ ] Intake captures addresses per policy
    - [ ] County FIPS derived for criminal searches
    - [ ] Address years lookback respected

7. **Service SLA Clocks:**
    - [ ] Clock starts on dispatch (if configured)
    - [ ] At-risk/breach detection works
    - [ ] Clock completes when service completes

8. **Read Models:**
    - [ ] `service_requests` projection accurate (includes tier)
    - [ ] `service_results` stores normalized data
    - [ ] `fulfillment_dashboard` shows in-flight services by tier

9. **Observability:**
    - [ ] Service status metrics exposed
    - [ ] Vendor health dashboard
    - [ ] Alerts for stuck/failed services

## 12. Risks & Mitigations

- **Vendor Contract Complexity:** Start with StubVendorAdapter; defer real vendor integrations to Phase 4+.
- **County Resolution Accuracy:** ZIP-to-County covers 95%; add geocoding later.
- **Service Explosion:** Limit initial taxonomy to most common types; add as needed.
- **Tier Stop Conditions:** Carefully define what constitutes a "stop" vs "review required" to avoid blocking orders
  unnecessarily.

## 13. Dependencies

- **From Phase 2:** Order aggregate, `ReadyForRouting` state, Intake completion
- **From Phase 3:** SLA Clocks module (for service-level SLAs)
- **Vendor Contracts:** Define for real vendors in Phase 4+

## 14. Non-Goals (Phase 3.1)

- Real vendor integrations (use stubs)
- Service dependencies/sequencing (all parallel for now)
- Cost/pricing per service (Phase 5+)
- Vendor failover/fallback chains (Phase 4+)
- Manual/out-of-system service handling (Phase 4+)

## 15. Current Status Snapshot

*[Updated 2025-12-07]*

- **Services Module:** Not started
- **Subject Address History:** Not implemented
- **County Resolution:** Not implemented
- **Order Routing:** Basic state machine exists; no package→service mapping

## 16. Next Actions

1. Create `Holmes.Services.Domain` project scaffolding
2. Define `ServiceType` taxonomy (enums + value objects)
3. Implement `Service` aggregate with state machine
4. Create `StubVendorAdapter` with fixture data
5. Add address history to Subject aggregate
6. Implement `OrderRoutingHandler` for package→services

---

This document is the authoritative reference for Phase 3.1 delivery; update it at each checkpoint to reflect decisions,
scope changes, and readiness status.
