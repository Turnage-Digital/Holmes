using System.Globalization;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Infrastructure.Sql.Entities;

namespace Holmes.Subjects.Infrastructure.Sql.Mappers;

public static class SubjectMapper
{
    public static Subject ToDomain(SubjectDb db)
    {
        var aliases = db.Aliases
            .Select(a => new SubjectAlias(a.GivenName, a.FamilyName, a.DateOfBirth));

        var addresses = db.Addresses
            .Select(a => SubjectAddress.Rehydrate(
                UlidId.Parse(a.Id),
                a.Street1,
                a.Street2,
                a.City,
                a.State,
                a.PostalCode,
                a.Country,
                a.CountyFips,
                a.FromDate,
                a.ToDate,
                (AddressType)a.AddressType,
                a.CreatedAt));

        var employments = db.Employments
            .Select(e => SubjectEmployment.Rehydrate(
                UlidId.Parse(e.Id),
                e.EmployerName,
                e.EmployerPhone,
                e.EmployerAddress,
                e.JobTitle,
                e.SupervisorName,
                e.SupervisorPhone,
                e.StartDate,
                e.EndDate,
                e.ReasonForLeaving,
                e.CanContact,
                e.CreatedAt));

        var educations = db.Educations
            .Select(e => SubjectEducation.Rehydrate(
                UlidId.Parse(e.Id),
                e.InstitutionName,
                e.InstitutionAddress,
                e.Degree,
                e.Major,
                e.AttendedFrom,
                e.AttendedTo,
                e.GraduationDate,
                e.Graduated,
                e.CreatedAt));

        var references = db.References
            .Select(r => SubjectReference.Rehydrate(
                UlidId.Parse(r.Id),
                r.Name,
                r.Phone,
                r.Email,
                r.Relationship,
                r.YearsKnown,
                (ReferenceType)r.ReferenceType,
                r.CreatedAt));

        var phones = db.Phones
            .Select(p => SubjectPhone.Rehydrate(
                UlidId.Parse(p.Id),
                p.PhoneNumber,
                (PhoneType)p.PhoneType,
                p.IsPrimary,
                p.CreatedAt));

        return Subject.Rehydrate(
            UlidId.Parse(db.SubjectId),
            db.GivenName,
            db.FamilyName,
            db.MiddleName,
            db.DateOfBirth,
            db.Email,
            db.CreatedAt,
            aliases,
            addresses,
            employments,
            educations,
            references,
            phones,
            db.EncryptedSsn,
            db.SsnLast4,
            db.MergedIntoSubjectId is null ? null : UlidId.Parse(db.MergedIntoSubjectId),
            db.MergedBy,
            db.MergedAt);
    }

    public static SubjectDb ToDb(Subject subject)
    {
        var db = new SubjectDb
        {
            SubjectId = subject.Id.ToString(),
            GivenName = subject.GivenName,
            FamilyName = subject.FamilyName,
            MiddleName = subject.MiddleName,
            DateOfBirth = subject.DateOfBirth,
            Email = subject.Email,
            EncryptedSsn = subject.EncryptedSsn,
            SsnLast4 = subject.SsnLast4,
            CreatedAt = subject.CreatedAt,
            MergedIntoSubjectId = subject.MergedIntoSubjectId?.ToString(),
            MergedBy = subject.MergedBy,
            MergedAt = subject.MergedAt
        };

        foreach (var alias in subject.Aliases)
        {
            db.Aliases.Add(new SubjectAliasDb
            {
                SubjectId = db.SubjectId,
                GivenName = alias.GivenName,
                FamilyName = alias.FamilyName,
                DateOfBirth = alias.DateOfBirth
            });
        }

        foreach (var addr in subject.Addresses)
        {
            db.Addresses.Add(new SubjectAddressDb
            {
                Id = addr.Id.ToString(),
                SubjectId = db.SubjectId,
                Street1 = addr.Street1,
                Street2 = addr.Street2,
                City = addr.City,
                State = addr.State,
                PostalCode = addr.PostalCode,
                Country = addr.Country,
                CountyFips = addr.CountyFips,
                FromDate = addr.FromDate,
                ToDate = addr.ToDate,
                AddressType = (int)addr.Type,
                CreatedAt = addr.CreatedAt
            });
        }

        foreach (var emp in subject.Employments)
        {
            db.Employments.Add(new SubjectEmploymentDb
            {
                Id = emp.Id.ToString(),
                SubjectId = db.SubjectId,
                EmployerName = emp.EmployerName,
                EmployerPhone = emp.EmployerPhone,
                EmployerAddress = emp.EmployerAddress,
                JobTitle = emp.JobTitle,
                SupervisorName = emp.SupervisorName,
                SupervisorPhone = emp.SupervisorPhone,
                StartDate = emp.StartDate,
                EndDate = emp.EndDate,
                ReasonForLeaving = emp.ReasonForLeaving,
                CanContact = emp.CanContact,
                CreatedAt = emp.CreatedAt
            });
        }

        foreach (var edu in subject.Educations)
        {
            db.Educations.Add(new SubjectEducationDb
            {
                Id = edu.Id.ToString(),
                SubjectId = db.SubjectId,
                InstitutionName = edu.InstitutionName,
                InstitutionAddress = edu.InstitutionAddress,
                Degree = edu.Degree,
                Major = edu.Major,
                AttendedFrom = edu.AttendedFrom,
                AttendedTo = edu.AttendedTo,
                GraduationDate = edu.GraduationDate,
                Graduated = edu.Graduated,
                CreatedAt = edu.CreatedAt
            });
        }

        foreach (var reference in subject.References)
        {
            db.References.Add(new SubjectReferenceDb
            {
                Id = reference.Id.ToString(),
                SubjectId = db.SubjectId,
                Name = reference.Name,
                Phone = reference.Phone,
                Email = reference.Email,
                Relationship = reference.Relationship,
                YearsKnown = reference.YearsKnown,
                ReferenceType = (int)reference.Type,
                CreatedAt = reference.CreatedAt
            });
        }

        foreach (var phone in subject.Phones)
        {
            db.Phones.Add(new SubjectPhoneDb
            {
                Id = phone.Id.ToString(),
                SubjectId = db.SubjectId,
                PhoneNumber = phone.PhoneNumber,
                PhoneType = (int)phone.Type,
                IsPrimary = phone.IsPrimary,
                CreatedAt = phone.CreatedAt
            });
        }

        return db;
    }

    public static void UpdateDb(SubjectDb db, Subject subject)
    {
        db.GivenName = subject.GivenName;
        db.FamilyName = subject.FamilyName;
        db.MiddleName = subject.MiddleName;
        db.DateOfBirth = subject.DateOfBirth;
        db.Email = subject.Email;
        db.EncryptedSsn = subject.EncryptedSsn;
        db.SsnLast4 = subject.SsnLast4;
        db.MergedIntoSubjectId = subject.MergedIntoSubjectId?.ToString();
        db.MergedBy = subject.MergedBy;
        db.MergedAt = subject.MergedAt;

        SyncAliases(db, subject);
        SyncAddresses(db, subject);
        SyncEmployments(db, subject);
        SyncEducations(db, subject);
        SyncReferences(db, subject);
        SyncPhones(db, subject);
    }

    // DTO mapping methods for API responses
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

    // Private sync methods for collection updates
    private static void SyncAliases(SubjectDb db, Subject subject)
    {
        var desired = subject.Aliases
            .ToDictionary(a => (a.GivenName, a.FamilyName, a.DateOfBirth), a => a);

        var existing = db.Aliases
            .ToDictionary(a => (a.GivenName, a.FamilyName, a.DateOfBirth), a => a);

        foreach (var key in existing.Keys.Except(desired.Keys).ToList())
        {
            var alias = existing[key];
            db.Aliases.Remove(alias);
        }

        foreach (var desiredAlias in desired)
        {
            if (!existing.TryGetValue(desiredAlias.Key, out _))
            {
                db.Aliases.Add(new SubjectAliasDb
                {
                    SubjectId = db.SubjectId,
                    GivenName = desiredAlias.Value.GivenName,
                    FamilyName = desiredAlias.Value.FamilyName,
                    DateOfBirth = desiredAlias.Value.DateOfBirth
                });
            }
        }
    }

    private static void SyncAddresses(SubjectDb db, Subject subject)
    {
        var desired = subject.Addresses.ToDictionary(a => a.Id.ToString(), a => a);
        var existing = db.Addresses.ToDictionary(a => a.Id, a => a);

        foreach (var key in existing.Keys.Except(desired.Keys).ToList())
        {
            db.Addresses.Remove(existing[key]);
        }

        foreach (var addr in desired)
        {
            if (!existing.ContainsKey(addr.Key))
            {
                db.Addresses.Add(new SubjectAddressDb
                {
                    Id = addr.Key,
                    SubjectId = db.SubjectId,
                    Street1 = addr.Value.Street1,
                    Street2 = addr.Value.Street2,
                    City = addr.Value.City,
                    State = addr.Value.State,
                    PostalCode = addr.Value.PostalCode,
                    Country = addr.Value.Country,
                    CountyFips = addr.Value.CountyFips,
                    FromDate = addr.Value.FromDate,
                    ToDate = addr.Value.ToDate,
                    AddressType = (int)addr.Value.Type,
                    CreatedAt = addr.Value.CreatedAt
                });
            }
        }
    }

    private static void SyncEmployments(SubjectDb db, Subject subject)
    {
        var desired = subject.Employments.ToDictionary(e => e.Id.ToString(), e => e);
        var existing = db.Employments.ToDictionary(e => e.Id, e => e);

        foreach (var key in existing.Keys.Except(desired.Keys).ToList())
        {
            db.Employments.Remove(existing[key]);
        }

        foreach (var emp in desired)
        {
            if (!existing.ContainsKey(emp.Key))
            {
                db.Employments.Add(new SubjectEmploymentDb
                {
                    Id = emp.Key,
                    SubjectId = db.SubjectId,
                    EmployerName = emp.Value.EmployerName,
                    EmployerPhone = emp.Value.EmployerPhone,
                    EmployerAddress = emp.Value.EmployerAddress,
                    JobTitle = emp.Value.JobTitle,
                    SupervisorName = emp.Value.SupervisorName,
                    SupervisorPhone = emp.Value.SupervisorPhone,
                    StartDate = emp.Value.StartDate,
                    EndDate = emp.Value.EndDate,
                    ReasonForLeaving = emp.Value.ReasonForLeaving,
                    CanContact = emp.Value.CanContact,
                    CreatedAt = emp.Value.CreatedAt
                });
            }
        }
    }

    private static void SyncEducations(SubjectDb db, Subject subject)
    {
        var desired = subject.Educations.ToDictionary(e => e.Id.ToString(), e => e);
        var existing = db.Educations.ToDictionary(e => e.Id, e => e);

        foreach (var key in existing.Keys.Except(desired.Keys).ToList())
        {
            db.Educations.Remove(existing[key]);
        }

        foreach (var edu in desired)
        {
            if (!existing.ContainsKey(edu.Key))
            {
                db.Educations.Add(new SubjectEducationDb
                {
                    Id = edu.Key,
                    SubjectId = db.SubjectId,
                    InstitutionName = edu.Value.InstitutionName,
                    InstitutionAddress = edu.Value.InstitutionAddress,
                    Degree = edu.Value.Degree,
                    Major = edu.Value.Major,
                    AttendedFrom = edu.Value.AttendedFrom,
                    AttendedTo = edu.Value.AttendedTo,
                    GraduationDate = edu.Value.GraduationDate,
                    Graduated = edu.Value.Graduated,
                    CreatedAt = edu.Value.CreatedAt
                });
            }
        }
    }

    private static void SyncReferences(SubjectDb db, Subject subject)
    {
        var desired = subject.References.ToDictionary(r => r.Id.ToString(), r => r);
        var existing = db.References.ToDictionary(r => r.Id, r => r);

        foreach (var key in existing.Keys.Except(desired.Keys).ToList())
        {
            db.References.Remove(existing[key]);
        }

        foreach (var reference in desired)
        {
            if (!existing.ContainsKey(reference.Key))
            {
                db.References.Add(new SubjectReferenceDb
                {
                    Id = reference.Key,
                    SubjectId = db.SubjectId,
                    Name = reference.Value.Name,
                    Phone = reference.Value.Phone,
                    Email = reference.Value.Email,
                    Relationship = reference.Value.Relationship,
                    YearsKnown = reference.Value.YearsKnown,
                    ReferenceType = (int)reference.Value.Type,
                    CreatedAt = reference.Value.CreatedAt
                });
            }
        }
    }

    private static void SyncPhones(SubjectDb db, Subject subject)
    {
        var desired = subject.Phones.ToDictionary(p => p.Id.ToString(), p => p);
        var existing = db.Phones.ToDictionary(p => p.Id, p => p);

        foreach (var key in existing.Keys.Except(desired.Keys).ToList())
        {
            db.Phones.Remove(existing[key]);
        }

        foreach (var phone in desired)
        {
            if (!existing.ContainsKey(phone.Key))
            {
                db.Phones.Add(new SubjectPhoneDb
                {
                    Id = phone.Key,
                    SubjectId = db.SubjectId,
                    PhoneNumber = phone.Value.PhoneNumber,
                    PhoneType = (int)phone.Value.Type,
                    IsPrimary = phone.Value.IsPrimary,
                    CreatedAt = phone.Value.CreatedAt
                });
            }
        }
    }
}