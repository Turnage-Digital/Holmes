using System.Text;
using Holmes.Core.Application.Abstractions.Security;
using Holmes.Core.Domain;
using Holmes.IntakeSessions.Application.Abstractions.Services;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed class SubmitIntakeCommandHandler(
    IIntakeAnswersDecryptor answersDecryptor,
    IAeadEncryptor ssnEncryptor,
    IIntakeSessionsUnitOfWork unitOfWork,
    ILogger<SubmitIntakeCommandHandler> logger
)
    : IRequestHandler<SubmitIntakeCommand, Result>
{
    public async Task<Result> Handle(SubmitIntakeCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.IntakeSessions;
        var session = await repository.GetByIdAsync(request.IntakeSessionId, cancellationToken);
        if (session is null)
        {
            return Result.Fail($"Intake session '{request.IntakeSessionId}' not found.");
        }

        if (session.ConsentArtifact is null || session.AnswersSnapshot is null)
        {
            return Result.Fail("Intake session is missing consent or answers data.");
        }

        // Decrypt and persist subject data from intake answers
        var decryptedAnswers = await answersDecryptor.DecryptAsync(session.AnswersSnapshot, cancellationToken);
        if (decryptedAnswers is not null)
        {
            await CaptureSubjectDataAsync(session, decryptedAnswers, request.SubmittedAt, cancellationToken);
        }
        else
        {
            logger.LogWarning(
                "Could not decrypt intake answers for session {SessionId}, subject data will not be updated",
                request.IntakeSessionId);
        }

        session.Submit(request.SubmittedAt);
        await repository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task CaptureSubjectDataAsync(
        IntakeSession session,
        DecryptedIntakeAnswers answers,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken
    )
    {
        byte[]? encryptedSsn = null;
        string? ssnLast4 = null;

        // Re-encrypt SSN for storage if present
        if (!string.IsNullOrWhiteSpace(answers.Ssn) && answers.Ssn.Length >= 4)
        {
            var ssnBytes = Encoding.UTF8.GetBytes(answers.Ssn);
            encryptedSsn = await ssnEncryptor.EncryptAsync(ssnBytes, cancellationToken: cancellationToken);
            ssnLast4 = answers.Ssn[^4..];
        }

        session.CaptureSubjectData(
            answers.MiddleName,
            encryptedSsn,
            ssnLast4,
            answers.Addresses.Select(a => new SubjectIntakeAddressData(
                    a.Street1,
                    a.Street2,
                    a.City,
                    a.State,
                    a.PostalCode,
                    a.Country,
                    a.CountyFips,
                    a.FromDate,
                    a.ToDate,
                    a.Type
                ))
                .ToList(),
            answers.Employments.Select(e => new SubjectIntakeEmploymentData(
                    e.EmployerName,
                    e.EmployerPhone,
                    e.EmployerAddress,
                    e.JobTitle,
                    e.SupervisorName,
                    e.SupervisorPhone,
                    e.StartDate,
                    e.EndDate,
                    e.ReasonForLeaving,
                    e.CanContact
                ))
                .ToList(),
            answers.Educations.Select(e => new SubjectIntakeEducationData(
                    e.InstitutionName,
                    e.InstitutionAddress,
                    e.Degree,
                    e.Major,
                    e.AttendedFrom,
                    e.AttendedTo,
                    e.GraduationDate,
                    e.Graduated
                ))
                .ToList(),
            answers.References.Select(r => new SubjectIntakeReferenceData(
                    r.Name,
                    r.Phone,
                    r.Email,
                    r.Relationship,
                    r.YearsKnown,
                    r.Type
                ))
                .ToList(),
            answers.Phones.Select(p => new SubjectIntakePhoneData(
                    p.PhoneNumber,
                    p.Type,
                    p.IsPrimary
                ))
                .ToList(),
            timestamp);

        logger.LogInformation(
            "Persisted intake data for Subject {SubjectId}: {AddressCount} addresses, {EmploymentCount} employments, {EducationCount} educations",
            session.SubjectId,
            answers.Addresses.Count,
            answers.Employments.Count,
            answers.Educations.Count);
    }
}