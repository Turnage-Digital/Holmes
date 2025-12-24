using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Holmes.Core.Application.Abstractions.Security;
using Holmes.IntakeSessions.Application.Abstractions.Services;
using Holmes.IntakeSessions.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Holmes.IntakeSessions.Infrastructure.Sql.Services;

public sealed class IntakeAnswersDecryptor(
    IAeadEncryptor encryptor,
    ILogger<IntakeAnswersDecryptor> logger
) : IIntakeAnswersDecryptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<DecryptedIntakeAnswers?> DecryptAsync(
        IntakeAnswersSnapshot snapshot,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var cipherBytes = Convert.FromBase64String(snapshot.PayloadCipherText);
            var plainBytes = await encryptor.DecryptAsync(cipherBytes, cancellationToken: cancellationToken);
            var json = Encoding.UTF8.GetString(plainBytes);

            var payload = JsonSerializer.Deserialize<IntakeAnswersPayload>(json, JsonOptions);
            if (payload is null)
            {
                logger.LogWarning("Failed to deserialize intake answers payload");
                return null;
            }

            return new DecryptedIntakeAnswers(
                payload.MiddleName,
                payload.Ssn,
                payload.Addresses?.Select(MapAddress).ToList() ?? [],
                payload.Employments?.Select(MapEmployment).ToList() ?? [],
                payload.Educations?.Select(MapEducation).ToList() ?? [],
                payload.References?.Select(MapReference).ToList() ?? [],
                payload.Phones?.Select(MapPhone).ToList() ?? []
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to decrypt intake answers snapshot");
            return null;
        }
    }

    private static DecryptedAddress MapAddress(IntakeAddressPayload a)
    {
        return new DecryptedAddress(
            a.Street1 ?? string.Empty,
            a.Street2,
            a.City ?? string.Empty,
            a.State ?? string.Empty,
            a.PostalCode ?? string.Empty,
            a.Country ?? "USA",
            a.CountyFips,
            a.FromDate ?? DateOnly.FromDateTime(DateTime.Today),
            a.ToDate,
            a.Type ?? 0
        );
    }

    private static DecryptedEmployment MapEmployment(IntakeEmploymentPayload e)
    {
        return new DecryptedEmployment(
            e.EmployerName ?? string.Empty,
            e.EmployerPhone,
            e.EmployerAddress,
            e.JobTitle,
            e.SupervisorName,
            e.SupervisorPhone,
            e.StartDate ?? DateOnly.FromDateTime(DateTime.Today),
            e.EndDate,
            e.ReasonForLeaving,
            e.CanContact ?? false
        );
    }

    private static DecryptedEducation MapEducation(IntakeEducationPayload e)
    {
        return new DecryptedEducation(
            e.InstitutionName ?? string.Empty,
            e.InstitutionAddress,
            e.Degree,
            e.Major,
            e.AttendedFrom,
            e.AttendedTo,
            e.GraduationDate,
            e.Graduated ?? false
        );
    }

    private static DecryptedReference MapReference(IntakeReferencePayload r)
    {
        return new DecryptedReference(
            r.Name ?? string.Empty,
            r.Phone,
            r.Email,
            r.Relationship,
            r.YearsKnown,
            r.Type ?? 0
        );
    }

    private static DecryptedPhone MapPhone(IntakePhonePayload p)
    {
        return new DecryptedPhone(
            p.PhoneNumber ?? string.Empty,
            p.Type ?? 0,
            p.IsPrimary ?? false
        );
    }

    // Internal payload models matching JSON structure from frontend
    private sealed record IntakeAnswersPayload
    {
        public string? MiddleName { get; init; }
        public string? Ssn { get; init; }
        public List<IntakeAddressPayload>? Addresses { get; init; }
        public List<IntakeEmploymentPayload>? Employments { get; init; }
        public List<IntakeEducationPayload>? Educations { get; init; }
        public List<IntakeReferencePayload>? References { get; init; }
        public List<IntakePhonePayload>? Phones { get; init; }
    }

    private sealed record IntakeAddressPayload
    {
        public string? Street1 { get; init; }
        public string? Street2 { get; init; }
        public string? City { get; init; }
        public string? State { get; init; }
        public string? PostalCode { get; init; }
        public string? Country { get; init; }
        public string? CountyFips { get; init; }
        public DateOnly? FromDate { get; init; }
        public DateOnly? ToDate { get; init; }
        public int? Type { get; init; }
    }

    private sealed record IntakeEmploymentPayload
    {
        public string? EmployerName { get; init; }
        public string? EmployerPhone { get; init; }
        public string? EmployerAddress { get; init; }
        public string? JobTitle { get; init; }
        public string? SupervisorName { get; init; }
        public string? SupervisorPhone { get; init; }
        public DateOnly? StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
        public string? ReasonForLeaving { get; init; }
        public bool? CanContact { get; init; }
    }

    private sealed record IntakeEducationPayload
    {
        public string? InstitutionName { get; init; }
        public string? InstitutionAddress { get; init; }
        public string? Degree { get; init; }
        public string? Major { get; init; }
        public DateOnly? AttendedFrom { get; init; }
        public DateOnly? AttendedTo { get; init; }
        public DateOnly? GraduationDate { get; init; }
        public bool? Graduated { get; init; }
    }

    private sealed record IntakeReferencePayload
    {
        public string? Name { get; init; }
        public string? Phone { get; init; }
        public string? Email { get; init; }
        public string? Relationship { get; init; }
        public int? YearsKnown { get; init; }
        public int? Type { get; init; }
    }

    private sealed record IntakePhonePayload
    {
        public string? PhoneNumber { get; init; }
        public int? Type { get; init; }
        public bool? IsPrimary { get; init; }
    }
}