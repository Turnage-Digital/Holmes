# IntakeSession Domain Notes

**Context:** Phase 2 Intake & Workflow launch. IntakeSession captures the full subject experience from invite to
submission while anchoring consent, policy snapshots, and partial progress.

## 1. Purpose

- Represent a **single subject’s intake attempt** for a specific customer order.
- Provide a **stateful workspace** where OTP verification, identity info, disclosures, and data entry occur before an
  order transitions.
- Persist **policy snapshots** (permissible purpose, disclosure versions, Fair-Chance overlays) so downstream
  adjudication references immutable context.
- Emit **timeline events** that power projections (`intake_sessions`, `order_timeline_events`) and SSE feeds.

## 2. Lifecycle & States

| State             | Description                                                                   | Entry Event                                       | Exit Trigger                                      |
|-------------------|-------------------------------------------------------------------------------|---------------------------------------------------|---------------------------------------------------|
| `invited`         | Session created, invite sent with resume token; no subject interaction yet    | `IntakeSessionInvited`                            | Subject authenticates via OTP                     |
| `in_progress`     | Subject verified, may save partial answers; consent pending                   | `IntakeSessionStarted`                            | Consent captured + all required sections complete |
| `awaiting_review` | Subject submitted data but order validation still running (IDV, fraud checks) | `IntakeSubmissionReceived`                        | All validations pass (or manual override)         |
| `submitted`       | Intake locked, data attached to pending order transition                      | `IntakeSubmissionAccepted`                        | Order workflow moves to `ready_for_routing`       |
| `abandoned`       | Invite expired or subject withdrew; session archived                          | `IntakeSessionExpired` / `IntakeSessionWithdrawn` | n/a                                               |

Rules:

- Transitions are **forward-only** except `in_progress → abandoned` via expiration/withdrawal.
- Each order can reference **one active session**; retries create a new session with linkage to the superseded one.
- Sessions carry `ExpiresAt` to enforce compliance-driven lifetimes (e.g., 7 days).

## 3. Commands & Events

### Commands

- `IssueIntakeInviteCommand` — creates session, sends communication payloads.
- `StartIntakeSessionCommand` — verifies OTP token, records device metadata.
- `SaveIntakeProgressCommand` — idempotent partial updates; enforces schema validation.
- `CaptureConsentCommand` — stores disclosure acceptance, signature artifacts.
- `SubmitIntakeCommand` — finalizes answers, locks session, raises submission events.
- `ExpireIntakeSessionCommand` / `WithdrawIntakeSessionCommand` — handles abandonment flows.

### Domain Events

- `IntakeSessionInvited`
- `IntakeSessionStarted`
- `IntakeProgressSaved`
- `ConsentCaptured`
- `IntakeSubmissionReceived`
- `IntakeSubmissionAccepted`
- `IntakeSessionExpired`
- `IntakeSessionSuperseded`

Events feed projections and ensure audit completeness; each includes `OrderId`, `SubjectId`, `CustomerId`, and
`PolicySnapshotId`.

## 4. Data Model & Invariants

Key fields:

- `IntakeSessionId` (ULID), `OrderId`, `SubjectId`, `CustomerId`
- `Status`, `ExpiresAt`, `LastTouchedAt`
- `PolicySnapshot` (embedded JSON / reference ID)
- `ConsentArtifacts` (hash pointer to immutable storage)
- `Answers` (encrypted payload or reference to normalized tables)
- `ResumeToken` (one-time + rotating option for security)

Invariants:

1. `OrderId` + `SubjectId` combination must be unique for active sessions.
2. `ConsentCaptured` required before `SubmitIntakeCommand` succeeds.
3. `Answers` schema version must match `PolicySnapshot.SchemaVersion`.
4. Once `submitted`, no mutable fields other than metadata (e.g., `SubmittedAt` timestamp).
5. Superseding a session emits `IntakeSessionSuperseded` and marks prior session as read-only.

### Consent Artifact Storage (Phase 2)

- Expose an abstraction `IConsentArtifactStore` (`SaveAsync`, `GetAsync`, `ExistsAsync`) so the aggregate/application
  layer only depends on metadata (`ConsentArtifactPointer`) rather than storage details.
- Phase 2 ships with `DatabaseConsentArtifactStore` implemented inside `Holmes.Intake.Infrastructure.Sql`, persisting
  encrypted byte arrays plus metadata columns (`Hash`, `MimeType`, `Length`, `SchemaVersion`) in MySQL.
- Application handlers call the store, receive a pointer (ULID/URI) that is persisted on the `IntakeSession`. Swapping
  to Azure Blob/File storage later only changes DI wiring and the concrete implementation.
- Keep the payload size bounded (e.g., <2 MB) and record hashes so future migrations to blob storage can verify
  integrity during backfill.

**Interface & DTO sketch:**

```csharp
public sealed record ConsentArtifactDescriptor(
    Ulid ArtifactId,
    string MimeType,
    long Length,
    string Hash,
    string HashAlgorithm,
    string SchemaVersion,
    DateTimeOffset CreatedAt,
    string? StorageHint = null);

public interface IConsentArtifactStore
{
    Task<ConsentArtifactDescriptor> SaveAsync(
        ConsentArtifactWriteRequest request,
        Stream payload,
        CancellationToken cancellationToken);

    Task<ConsentArtifactStream> GetAsync(
        Ulid artifactId,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Ulid artifactId, CancellationToken cancellationToken);
}

public sealed record ConsentArtifactWriteRequest(
    Ulid ArtifactId,
    Ulid OrderId,
    Ulid SubjectId,
    string MimeType,
    string SchemaVersion,
    IDictionary<string, string> Metadata);

public sealed record ConsentArtifactStream(
    ConsentArtifactDescriptor Descriptor,
    Stream Content);
```

`DatabaseConsentArtifactStore` implements the interface, encrypts content before writing to MySQL, and returns the
descriptor so aggregates persist only the pointer + hash.

## 5. Interactions with Order Aggregate

- Upon `IntakeSubmissionAccepted`, an `OrderIntakeCompleted` integration event triggers `Order` aggregate to evaluate
  policy gates (required docs, identity matches) before moving to `ready_for_routing`.
- `Order` subscribes to session events to update summary projection (subject status, outstanding items).
- Failures in order validation push the session back to `in_progress` via a compensating command only if fields remain
  editable (requires explicit policy).

## 6. UX & Operational Considerations

- UX must surface session status, expiration timer, and last saved timestamp; auto-save should call
  `SaveIntakeProgressCommand`.
- SSE channel should broadcast `IntakeSessionStarted`, `ConsentCaptured`, and `IntakeSubmissionAccepted` so ops
  dashboards reflect live progress.
- Runbooks need procedures for resending invites, force-expiring sessions, and verifying consent artifacts.

## 7. Open Questions

1. Do we allow multiple active sessions per subject if different customers request background checks simultaneously? (
   Default: yes, but per-order uniqueness enforced.)
2. Should consent artifacts live in object storage or SQL blob for Phase 2? Decision impacts `ConsentCaptured` handling.
3. What is the default expiration window, and can customers override it?

Track these decisions in the weekly Domain & Experience checkpoints and update this doc as the answers solidify.
