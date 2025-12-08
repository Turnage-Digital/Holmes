using System.Globalization;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Infrastructure.Sql.Entities;

namespace Holmes.Subjects.Infrastructure.Sql.Mappers;

public static class SubjectMapper
{
    public static SubjectSummaryDto ToSummary(SubjectDirectoryDb directory)
    {
        return new SubjectSummaryDto(
            directory.SubjectId,
            directory.GivenName,
            directory.FamilyName,
            directory.DateOfBirth,
            directory.Email,
            directory.IsMerged,
            directory.AliasCount,
            directory.CreatedAt);
    }

    public static SubjectListItemDto ToListItem(SubjectDb subject)
    {
        var status = subject.MergedIntoSubjectId is null ? "Active" : "Merged";
        var aliases = subject.Aliases
            .OrderBy(a => a.FamilyName)
            .ThenBy(a => a.GivenName)
            .Select(a => new SubjectAliasDto(
                a.Id.ToString(CultureInfo.InvariantCulture),
                a.GivenName,
                a.FamilyName,
                a.DateOfBirth,
                subject.CreatedAt))
            .ToList();

        return new SubjectListItemDto(
            subject.SubjectId,
            subject.GivenName,
            subject.MiddleName,
            subject.FamilyName,
            subject.DateOfBirth,
            subject.Email,
            status,
            subject.MergedIntoSubjectId,
            aliases,
            subject.CreatedAt,
            subject.MergedAt ?? subject.CreatedAt);
    }

    public static SubjectDetailDto ToDetail(SubjectDb subject)
    {
        var status = subject.MergedIntoSubjectId is null ? "Active" : "Merged";

        var aliases = subject.Aliases
            .OrderBy(a => a.FamilyName)
            .ThenBy(a => a.GivenName)
            .Select(a => new SubjectAliasDto(
                a.Id.ToString(CultureInfo.InvariantCulture),
                a.GivenName,
                a.FamilyName,
                a.DateOfBirth,
                subject.CreatedAt))
            .ToList();

        var addresses = subject.Addresses
            .OrderByDescending(a => a.ToDate == null)
            .ThenByDescending(a => a.FromDate)
            .Select(ToAddressDto)
            .ToList();

        var employments = subject.Employments
            .OrderByDescending(e => e.EndDate == null)
            .ThenByDescending(e => e.StartDate)
            .Select(ToEmploymentDto)
            .ToList();

        var educations = subject.Educations
            .OrderByDescending(e => e.GraduationDate ?? e.AttendedTo)
            .Select(ToEducationDto)
            .ToList();

        var references = subject.References
            .OrderByDescending(r => r.CreatedAt)
            .Select(ToReferenceDto)
            .ToList();

        var phones = subject.Phones
            .OrderByDescending(p => p.IsPrimary)
            .ThenByDescending(p => p.CreatedAt)
            .Select(ToPhoneDto)
            .ToList();

        return new SubjectDetailDto(
            subject.SubjectId,
            subject.GivenName,
            subject.MiddleName,
            subject.FamilyName,
            subject.DateOfBirth,
            subject.Email,
            subject.SsnLast4,
            status,
            subject.MergedIntoSubjectId,
            aliases,
            addresses,
            employments,
            educations,
            references,
            phones,
            subject.CreatedAt,
            subject.MergedAt ?? subject.CreatedAt);
    }

    public static SubjectAddressDto ToAddressDto(SubjectAddressDb address)
    {
        var addressTypeLabel = address.AddressType switch
        {
            0 => "Residential",
            1 => "Mailing",
            2 => "Business",
            _ => "Unknown"
        };

        return new SubjectAddressDto(
            address.Id,
            address.Street1,
            address.Street2,
            address.City,
            address.State,
            address.PostalCode,
            address.Country,
            address.CountyFips,
            address.FromDate,
            address.ToDate,
            address.ToDate is null,
            addressTypeLabel,
            address.CreatedAt);
    }

    public static SubjectEmploymentDto ToEmploymentDto(SubjectEmploymentDb employment)
    {
        return new SubjectEmploymentDto(
            employment.Id,
            employment.EmployerName,
            employment.EmployerPhone,
            employment.EmployerAddress,
            employment.JobTitle,
            employment.SupervisorName,
            employment.SupervisorPhone,
            employment.StartDate,
            employment.EndDate,
            employment.EndDate is null,
            employment.ReasonForLeaving,
            employment.CanContact,
            employment.CreatedAt);
    }

    public static SubjectEducationDto ToEducationDto(SubjectEducationDb education)
    {
        return new SubjectEducationDto(
            education.Id,
            education.InstitutionName,
            education.InstitutionAddress,
            education.Degree,
            education.Major,
            education.AttendedFrom,
            education.AttendedTo,
            education.GraduationDate,
            education.Graduated,
            education.CreatedAt);
    }

    public static SubjectReferenceDto ToReferenceDto(SubjectReferenceDb reference)
    {
        var referenceTypeLabel = reference.ReferenceType switch
        {
            0 => "Personal",
            1 => "Professional",
            _ => "Unknown"
        };

        return new SubjectReferenceDto(
            reference.Id,
            reference.Name,
            reference.Phone,
            reference.Email,
            reference.Relationship,
            reference.YearsKnown,
            referenceTypeLabel,
            reference.CreatedAt);
    }

    public static SubjectPhoneDto ToPhoneDto(SubjectPhoneDb phone)
    {
        var phoneTypeLabel = phone.PhoneType switch
        {
            0 => "Mobile",
            1 => "Home",
            2 => "Work",
            _ => "Unknown"
        };

        return new SubjectPhoneDto(
            phone.Id,
            phone.PhoneNumber,
            phoneTypeLabel,
            phone.IsPrimary,
            phone.CreatedAt);
    }
}