using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Subjects.Infrastructure.Sql.Repositories;

public class SqlSubjectRepository(SubjectsDbContext dbContext) : ISubjectRepository
{
    public Task AddAsync(Subject subject, CancellationToken cancellationToken)
    {
        var entity = ToDb(subject);
        dbContext.Subjects.Add(entity);
        UpsertDirectory(subject, entity);
        return Task.CompletedTask;
    }

    public async Task<Subject?> GetByIdAsync(UlidId id, CancellationToken cancellationToken)
    {
        var subjectId = id.ToString();
        var entity = await dbContext.Subjects
            .Include(s => s.Aliases)
            .Include(s => s.Addresses)
            .Include(s => s.Employments)
            .Include(s => s.Educations)
            .Include(s => s.References)
            .Include(s => s.Phones)
            .FirstOrDefaultAsync(s => s.SubjectId == subjectId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return Rehydrate(entity);
    }

    public async Task UpdateAsync(Subject subject, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Subjects
            .Include(s => s.Aliases)
            .Include(s => s.Addresses)
            .Include(s => s.Employments)
            .Include(s => s.Educations)
            .Include(s => s.References)
            .Include(s => s.Phones)
            .FirstOrDefaultAsync(s => s.SubjectId == subject.Id.ToString(), cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException($"Subject '{subject.Id}' not found.");
        }

        ApplyState(subject, entity);
        UpsertDirectory(subject, entity);
    }

    private static Subject Rehydrate(SubjectDb entity)
    {
        var aliases = entity.Aliases
            .Select(a => new SubjectAlias(a.GivenName, a.FamilyName, a.DateOfBirth));

        var addresses = entity.Addresses
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

        var employments = entity.Employments
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

        var educations = entity.Educations
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

        var references = entity.References
            .Select(r => SubjectReference.Rehydrate(
                UlidId.Parse(r.Id),
                r.Name,
                r.Phone,
                r.Email,
                r.Relationship,
                r.YearsKnown,
                (ReferenceType)r.ReferenceType,
                r.CreatedAt));

        var phones = entity.Phones
            .Select(p => SubjectPhone.Rehydrate(
                UlidId.Parse(p.Id),
                p.PhoneNumber,
                (PhoneType)p.PhoneType,
                p.IsPrimary,
                p.CreatedAt));

        return Subject.Rehydrate(
            UlidId.Parse(entity.SubjectId),
            entity.GivenName,
            entity.FamilyName,
            entity.MiddleName,
            entity.DateOfBirth,
            entity.Email,
            entity.CreatedAt,
            aliases,
            addresses,
            employments,
            educations,
            references,
            phones,
            entity.EncryptedSsn,
            entity.SsnLast4,
            entity.MergedIntoSubjectId is null ? null : UlidId.Parse(entity.MergedIntoSubjectId),
            entity.MergedBy,
            entity.MergedAt);
    }

    private static void ApplyState(Subject subject, SubjectDb entity)
    {
        entity.GivenName = subject.GivenName;
        entity.FamilyName = subject.FamilyName;
        entity.MiddleName = subject.MiddleName;
        entity.DateOfBirth = subject.DateOfBirth;
        entity.Email = subject.Email;
        entity.EncryptedSsn = subject.EncryptedSsn;
        entity.SsnLast4 = subject.SsnLast4;
        entity.MergedIntoSubjectId = subject.MergedIntoSubjectId?.ToString();
        entity.MergedBy = subject.MergedBy;
        entity.MergedAt = subject.MergedAt;

        SyncAliases(subject, entity);
        SyncAddresses(subject, entity);
        SyncEmployments(subject, entity);
        SyncEducations(subject, entity);
        SyncReferences(subject, entity);
        SyncPhones(subject, entity);
    }

    private static void SyncAliases(Subject subject, SubjectDb entity)
    {
        var desired = subject.Aliases
            .ToDictionary(a => (a.GivenName, a.FamilyName, a.DateOfBirth), a => a);

        var existing = entity.Aliases
            .ToDictionary(a => (a.GivenName, a.FamilyName, a.DateOfBirth), a => a);

        foreach (var key in existing.Keys.Except(desired.Keys).ToList())
        {
            var alias = existing[key];
            entity.Aliases.Remove(alias);
        }

        foreach (var desiredAlias in desired)
        {
            if (!existing.TryGetValue(desiredAlias.Key, out var alias))
            {
                entity.Aliases.Add(new SubjectAliasDb
                {
                    SubjectId = entity.SubjectId,
                    GivenName = desiredAlias.Value.GivenName,
                    FamilyName = desiredAlias.Value.FamilyName,
                    DateOfBirth = desiredAlias.Value.DateOfBirth
                });
            }
        }
    }

    private static void SyncAddresses(Subject subject, SubjectDb entity)
    {
        var desired = subject.Addresses.ToDictionary(a => a.Id.ToString(), a => a);
        var existing = entity.Addresses.ToDictionary(a => a.Id, a => a);

        foreach (var key in existing.Keys.Except(desired.Keys).ToList())
        {
            entity.Addresses.Remove(existing[key]);
        }

        foreach (var addr in desired)
        {
            if (!existing.ContainsKey(addr.Key))
            {
                entity.Addresses.Add(new SubjectAddressDb
                {
                    Id = addr.Key,
                    SubjectId = entity.SubjectId,
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

    private static void SyncEmployments(Subject subject, SubjectDb entity)
    {
        var desired = subject.Employments.ToDictionary(e => e.Id.ToString(), e => e);
        var existing = entity.Employments.ToDictionary(e => e.Id, e => e);

        foreach (var key in existing.Keys.Except(desired.Keys).ToList())
        {
            entity.Employments.Remove(existing[key]);
        }

        foreach (var emp in desired)
        {
            if (!existing.ContainsKey(emp.Key))
            {
                entity.Employments.Add(new SubjectEmploymentDb
                {
                    Id = emp.Key,
                    SubjectId = entity.SubjectId,
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

    private static void SyncEducations(Subject subject, SubjectDb entity)
    {
        var desired = subject.Educations.ToDictionary(e => e.Id.ToString(), e => e);
        var existing = entity.Educations.ToDictionary(e => e.Id, e => e);

        foreach (var key in existing.Keys.Except(desired.Keys).ToList())
        {
            entity.Educations.Remove(existing[key]);
        }

        foreach (var edu in desired)
        {
            if (!existing.ContainsKey(edu.Key))
            {
                entity.Educations.Add(new SubjectEducationDb
                {
                    Id = edu.Key,
                    SubjectId = entity.SubjectId,
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

    private static void SyncReferences(Subject subject, SubjectDb entity)
    {
        var desired = subject.References.ToDictionary(r => r.Id.ToString(), r => r);
        var existing = entity.References.ToDictionary(r => r.Id, r => r);

        foreach (var key in existing.Keys.Except(desired.Keys).ToList())
        {
            entity.References.Remove(existing[key]);
        }

        foreach (var reference in desired)
        {
            if (!existing.ContainsKey(reference.Key))
            {
                entity.References.Add(new SubjectReferenceDb
                {
                    Id = reference.Key,
                    SubjectId = entity.SubjectId,
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

    private static void SyncPhones(Subject subject, SubjectDb entity)
    {
        var desired = subject.Phones.ToDictionary(p => p.Id.ToString(), p => p);
        var existing = entity.Phones.ToDictionary(p => p.Id, p => p);

        foreach (var key in existing.Keys.Except(desired.Keys).ToList())
        {
            entity.Phones.Remove(existing[key]);
        }

        foreach (var phone in desired)
        {
            if (!existing.ContainsKey(phone.Key))
            {
                entity.Phones.Add(new SubjectPhoneDb
                {
                    Id = phone.Key,
                    SubjectId = entity.SubjectId,
                    PhoneNumber = phone.Value.PhoneNumber,
                    PhoneType = (int)phone.Value.Type,
                    IsPrimary = phone.Value.IsPrimary,
                    CreatedAt = phone.Value.CreatedAt
                });
            }
        }
    }

    private static SubjectDb ToDb(Subject subject)
    {
        var entity = new SubjectDb
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
            entity.Aliases.Add(new SubjectAliasDb
            {
                SubjectId = entity.SubjectId,
                GivenName = alias.GivenName,
                FamilyName = alias.FamilyName,
                DateOfBirth = alias.DateOfBirth
            });
        }

        foreach (var addr in subject.Addresses)
        {
            entity.Addresses.Add(new SubjectAddressDb
            {
                Id = addr.Id.ToString(),
                SubjectId = entity.SubjectId,
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
            entity.Employments.Add(new SubjectEmploymentDb
            {
                Id = emp.Id.ToString(),
                SubjectId = entity.SubjectId,
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
            entity.Educations.Add(new SubjectEducationDb
            {
                Id = edu.Id.ToString(),
                SubjectId = entity.SubjectId,
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
            entity.References.Add(new SubjectReferenceDb
            {
                Id = reference.Id.ToString(),
                SubjectId = entity.SubjectId,
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
            entity.Phones.Add(new SubjectPhoneDb
            {
                Id = phone.Id.ToString(),
                SubjectId = entity.SubjectId,
                PhoneNumber = phone.PhoneNumber,
                PhoneType = (int)phone.Type,
                IsPrimary = phone.IsPrimary,
                CreatedAt = phone.CreatedAt
            });
        }

        return entity;
    }

    private void UpsertDirectory(Subject subject, SubjectDb entity)
    {
        var record = dbContext.SubjectDirectory.SingleOrDefault(x => x.SubjectId == entity.SubjectId);
        if (record is null)
        {
            record = new SubjectDirectoryDb
            {
                SubjectId = entity.SubjectId,
                GivenName = entity.GivenName,
                FamilyName = entity.FamilyName,
                DateOfBirth = entity.DateOfBirth,
                Email = entity.Email,
                CreatedAt = entity.CreatedAt,
                IsMerged = subject.IsMerged,
                AliasCount = entity.Aliases.Count
            };
            dbContext.SubjectDirectory.Add(record);
        }
        else
        {
            record.GivenName = entity.GivenName;
            record.FamilyName = entity.FamilyName;
            record.DateOfBirth = entity.DateOfBirth;
            record.Email = entity.Email;
            record.IsMerged = subject.IsMerged;
            record.AliasCount = entity.Aliases.Count;
        }
    }
}
