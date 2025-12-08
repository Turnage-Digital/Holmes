using System.Text.Json;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;
using Holmes.Services.Infrastructure.Sql.Entities;

namespace Holmes.Services.Infrastructure.Sql.Mappers;

public static class ServiceRequestMapper
{
    public static ServiceRequest ToDomain(ServiceRequestDb db)
    {
        ServiceResult? result = null;
        if (db.Result is not null)
        {
            result = ToResultDomain(db.Result);
        }

        return ServiceRequest.Rehydrate(
            id: UlidId.Parse(db.Id),
            orderId: UlidId.Parse(db.OrderId),
            customerId: UlidId.Parse(db.CustomerId),
            catalogSnapshotId: db.CatalogSnapshotId is not null ? UlidId.Parse(db.CatalogSnapshotId) : null,
            serviceTypeCode: db.ServiceTypeCode,
            category: db.Category,
            tier: db.Tier,
            scopeType: db.ScopeType,
            scopeValue: db.ScopeValue,
            status: db.Status,
            vendorCode: db.VendorCode,
            vendorReferenceId: db.VendorReferenceId,
            attemptCount: db.AttemptCount,
            maxAttempts: db.MaxAttempts,
            lastError: db.LastError,
            createdAt: DateTime.SpecifyKind(db.CreatedAt, DateTimeKind.Utc),
            updatedAt: DateTime.SpecifyKind(db.UpdatedAt, DateTimeKind.Utc),
            dispatchedAt: db.DispatchedAt.HasValue ? DateTime.SpecifyKind(db.DispatchedAt.Value, DateTimeKind.Utc) : null,
            completedAt: db.CompletedAt.HasValue ? DateTime.SpecifyKind(db.CompletedAt.Value, DateTimeKind.Utc) : null,
            failedAt: db.FailedAt.HasValue ? DateTime.SpecifyKind(db.FailedAt.Value, DateTimeKind.Utc) : null,
            canceledAt: db.CanceledAt.HasValue ? DateTime.SpecifyKind(db.CanceledAt.Value, DateTimeKind.Utc) : null,
            result: result);
    }

    public static ServiceRequestDb ToDb(ServiceRequest domain)
    {
        var db = new ServiceRequestDb
        {
            Id = domain.Id.ToString(),
            OrderId = domain.OrderId.ToString(),
            CustomerId = domain.CustomerId.ToString(),
            CatalogSnapshotId = domain.CatalogSnapshotId?.ToString(),
            ServiceTypeCode = domain.ServiceTypeCode,
            Category = domain.Category,
            Tier = domain.Tier,
            ScopeType = domain.ScopeType,
            ScopeValue = domain.ScopeValue,
            Status = domain.Status,
            VendorCode = domain.VendorCode,
            VendorReferenceId = domain.VendorReferenceId,
            AttemptCount = domain.AttemptCount,
            MaxAttempts = domain.MaxAttempts,
            LastError = domain.LastError,
            CreatedAt = domain.CreatedAt.UtcDateTime,
            UpdatedAt = domain.UpdatedAt.UtcDateTime,
            DispatchedAt = domain.DispatchedAt?.UtcDateTime,
            CompletedAt = domain.CompletedAt?.UtcDateTime,
            FailedAt = domain.FailedAt?.UtcDateTime,
            CanceledAt = domain.CanceledAt?.UtcDateTime
        };

        if (domain.Result is not null)
        {
            db.Result = ToResultDb(domain.Result, domain.Id.ToString());
        }

        return db;
    }

    public static void UpdateDb(ServiceRequestDb db, ServiceRequest domain)
    {
        db.CatalogSnapshotId = domain.CatalogSnapshotId?.ToString();
        db.Status = domain.Status;
        db.VendorCode = domain.VendorCode;
        db.VendorReferenceId = domain.VendorReferenceId;
        db.AttemptCount = domain.AttemptCount;
        db.LastError = domain.LastError;
        db.UpdatedAt = domain.UpdatedAt.UtcDateTime;
        db.DispatchedAt = domain.DispatchedAt?.UtcDateTime;
        db.CompletedAt = domain.CompletedAt?.UtcDateTime;
        db.FailedAt = domain.FailedAt?.UtcDateTime;
        db.CanceledAt = domain.CanceledAt?.UtcDateTime;

        if (domain.Result is not null && db.Result is null)
        {
            db.Result = ToResultDb(domain.Result, domain.Id.ToString());
        }
        else if (domain.Result is not null && db.Result is not null)
        {
            UpdateResultDb(db.Result, domain.Result);
        }
    }

    private static ServiceResult ToResultDomain(ServiceResultDb db)
    {
        var records = DeserializeRecords(db.RecordsJson);

        return ServiceResult.Rehydrate(
            id: UlidId.Parse(db.Id),
            status: db.Status,
            records: records,
            rawResponseHash: db.RawResponseHash,
            vendorReferenceId: db.VendorReferenceId,
            receivedAt: DateTime.SpecifyKind(db.ReceivedAt, DateTimeKind.Utc),
            normalizedAt: db.NormalizedAt.HasValue ? DateTime.SpecifyKind(db.NormalizedAt.Value, DateTimeKind.Utc) : null);
    }

    private static ServiceResultDb ToResultDb(ServiceResult domain, string serviceRequestId)
    {
        return new ServiceResultDb
        {
            Id = domain.Id.ToString(),
            ServiceRequestId = serviceRequestId,
            Status = domain.Status,
            RecordsJson = SerializeRecords(domain.Records),
            RawResponseHash = domain.RawResponseHash,
            VendorReferenceId = domain.VendorReferenceId,
            ReceivedAt = domain.ReceivedAt.UtcDateTime,
            NormalizedAt = domain.NormalizedAt?.UtcDateTime
        };
    }

    private static void UpdateResultDb(ServiceResultDb db, ServiceResult domain)
    {
        db.Status = domain.Status;
        db.RecordsJson = SerializeRecords(domain.Records);
        db.RawResponseHash = domain.RawResponseHash;
        db.VendorReferenceId = domain.VendorReferenceId;
        db.ReceivedAt = domain.ReceivedAt.UtcDateTime;
        db.NormalizedAt = domain.NormalizedAt?.UtcDateTime;
    }

    private static string? SerializeRecords(IReadOnlyList<NormalizedRecord> records)
    {
        if (records.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(records, JsonSerializerOptions);
    }

    private static IReadOnlyList<NormalizedRecord> DeserializeRecords(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

        // Deserialize as a list of JsonElements first, then deserialize each by type
        var elements = JsonSerializer.Deserialize<List<JsonElement>>(json, JsonSerializerOptions);
        if (elements is null)
        {
            return [];
        }

        var records = new List<NormalizedRecord>();
        foreach (var element in elements)
        {
            if (!element.TryGetProperty("RecordType", out var typeProperty))
            {
                continue;
            }

            var recordType = typeProperty.GetString();
            NormalizedRecord? record = recordType switch
            {
                nameof(CriminalRecord) => element.Deserialize<CriminalRecord>(JsonSerializerOptions),
                nameof(EmploymentRecord) => element.Deserialize<EmploymentRecord>(JsonSerializerOptions),
                nameof(EducationRecord) => element.Deserialize<EducationRecord>(JsonSerializerOptions),
                nameof(IdentityRecord) => element.Deserialize<IdentityRecord>(JsonSerializerOptions),
                nameof(AddressRecord) => element.Deserialize<AddressRecord>(JsonSerializerOptions),
                nameof(SanctionsRecord) => element.Deserialize<SanctionsRecord>(JsonSerializerOptions),
                nameof(DrivingRecord) => element.Deserialize<DrivingRecord>(JsonSerializerOptions),
                nameof(DrugTestRecord) => element.Deserialize<DrugTestRecord>(JsonSerializerOptions),
                _ => null
            };

            if (record is not null)
            {
                records.Add(record);
            }
        }

        return records;
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
