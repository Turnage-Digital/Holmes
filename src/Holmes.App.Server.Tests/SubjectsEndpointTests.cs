using System.Net;
using System.Net.Http.Json;
using Holmes.Subjects.Contracts.Dtos;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Subjects.Infrastructure.Sql.Entities;
using Holmes.Users.Application.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.App.Server.Tests;

[TestFixture]
public class SubjectsEndpointTests
{
    [Test]
    public async Task RegisterSubject_Returns_Created_Summary()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "subject-tester");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "tester@holmes.dev");

        await EnsureTestUserAsync(factory);
        var request = new RegisterSubjectRequest("Jane", "Doe", new DateOnly(1985, 6, 1), "jane.doe@example.com");
        var response = await client.PostAsJsonAsync("/api/subjects", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var summary = await response.Content.ReadFromJsonAsync<SubjectSummaryDto>();
        Assert.That(summary, Is.Not.Null);
        Assert.That(summary!.GivenName, Is.EqualTo("Jane"));
        Assert.That(summary.AliasCount, Is.EqualTo(0));
    }

    [Test]
    public async Task GetSubjectById_Returns_NotFound_For_Missing_Subject()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "subject-tester");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "tester@holmes.dev");

        await EnsureTestUserAsync(factory);
        var response = await client.GetAsync($"/api/subjects/{Ulid.NewUlid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetSubjectById_Returns_Detail()
    {
        await using var factory = new HolmesWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubjectsDbContext>();
        var subjectId = Ulid.NewUlid().ToString();
        db.Subjects.Add(new SubjectDb
        {
            SubjectId = subjectId,
            GivenName = "John",
            FamilyName = "Smith",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "subject-tester");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "tester@holmes.dev");

        await EnsureTestUserAsync(factory);
        var response = await client.GetAsync($"/api/subjects/{subjectId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var detail = await response.Content.ReadFromJsonAsync<SubjectDetailDto>();
        Assert.That(detail, Is.Not.Null);
        Assert.That(detail!.FirstName, Is.EqualTo("John"));
        Assert.That(detail.LastName, Is.EqualTo("Smith"));
        Assert.That(detail.Addresses, Is.Empty);
        Assert.That(detail.Employments, Is.Empty);
    }

    [Test]
    public async Task GetAddresses_Returns_Empty_For_Subject_Without_Addresses()
    {
        await using var factory = new HolmesWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubjectsDbContext>();
        var subjectId = Ulid.NewUlid().ToString();
        db.Subjects.Add(new SubjectDb
        {
            SubjectId = subjectId,
            GivenName = "Jane",
            FamilyName = "Doe",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "subject-tester");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "addr-empty@holmes.dev");

        await EnsureTestUserAsync(factory, "addr-empty@holmes.dev");
        var response = await client.GetAsync($"/api/subjects/{subjectId}/addresses");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var addresses = await response.Content.ReadFromJsonAsync<List<SubjectAddressDto>>();
        Assert.That(addresses, Is.Not.Null);
        Assert.That(addresses, Is.Empty);
    }

    [Test]
    public async Task GetAddresses_Returns_Address_History()
    {
        await using var factory = new HolmesWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubjectsDbContext>();
        var subjectId = Ulid.NewUlid().ToString();
        db.Subjects.Add(new SubjectDb
        {
            SubjectId = subjectId,
            GivenName = "Bob",
            FamilyName = "Builder",
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SubjectAddresses.Add(new SubjectAddressDb
        {
            Id = Ulid.NewUlid().ToString(),
            SubjectId = subjectId,
            Street1 = "123 Main St",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            Country = "US",
            CountyFips = "17167",
            FromDate = new DateOnly(2020, 1, 1),
            ToDate = new DateOnly(2022, 12, 31),
            AddressType = 0, // Residential
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SubjectAddresses.Add(new SubjectAddressDb
        {
            Id = Ulid.NewUlid().ToString(),
            SubjectId = subjectId,
            Street1 = "456 Oak Ave",
            City = "Chicago",
            State = "IL",
            PostalCode = "60601",
            Country = "US",
            FromDate = new DateOnly(2023, 1, 1),
            ToDate = null, // Current address
            AddressType = 0,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "subject-tester");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "addr-hist@holmes.dev");

        await EnsureTestUserAsync(factory, "addr-hist@holmes.dev");
        var response = await client.GetAsync($"/api/subjects/{subjectId}/addresses");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var addresses = await response.Content.ReadFromJsonAsync<List<SubjectAddressDto>>();
        Assert.That(addresses, Is.Not.Null);
        Assert.That(addresses, Has.Count.EqualTo(2));
        // Current address should come first (sorted by IsCurrent desc, then FromDate desc)
        Assert.That(addresses![0].IsCurrent, Is.True);
        Assert.That(addresses[0].City, Is.EqualTo("Chicago"));
        Assert.That(addresses[1].IsCurrent, Is.False);
        Assert.That(addresses[1].City, Is.EqualTo("Springfield"));
        Assert.That(addresses[1].CountyFips, Is.EqualTo("17167"));
    }

    [Test]
    public async Task GetAddresses_Returns_BadRequest_For_Invalid_Id()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "subject-tester");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "addr-invalid@holmes.dev");

        await EnsureTestUserAsync(factory, "addr-invalid@holmes.dev");
        var response = await client.GetAsync("/api/subjects/not-a-valid-ulid/addresses");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetEmployments_Returns_Empty_For_Subject_Without_Employments()
    {
        await using var factory = new HolmesWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubjectsDbContext>();
        var subjectId = Ulid.NewUlid().ToString();
        db.Subjects.Add(new SubjectDb
        {
            SubjectId = subjectId,
            GivenName = "Alice",
            FamilyName = "Wonder",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "subject-tester");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "emp-empty@holmes.dev");

        await EnsureTestUserAsync(factory, "emp-empty@holmes.dev");
        var response = await client.GetAsync($"/api/subjects/{subjectId}/employments");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var employments = await response.Content.ReadFromJsonAsync<List<SubjectEmploymentDto>>();
        Assert.That(employments, Is.Not.Null);
        Assert.That(employments, Is.Empty);
    }

    [Test]
    public async Task GetEmployments_Returns_Employment_History()
    {
        await using var factory = new HolmesWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubjectsDbContext>();
        var subjectId = Ulid.NewUlid().ToString();
        db.Subjects.Add(new SubjectDb
        {
            SubjectId = subjectId,
            GivenName = "Charlie",
            FamilyName = "Brown",
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SubjectEmployments.Add(new SubjectEmploymentDb
        {
            Id = Ulid.NewUlid().ToString(),
            SubjectId = subjectId,
            EmployerName = "Acme Corp",
            EmployerPhone = "555-123-4567",
            EmployerAddress = "100 Industrial Way, Springfield, IL 62701",
            JobTitle = "Software Engineer",
            SupervisorName = "Lucy Van Pelt",
            SupervisorPhone = "555-987-6543",
            StartDate = new DateOnly(2018, 6, 1),
            EndDate = new DateOnly(2022, 5, 31),
            ReasonForLeaving = "Career advancement",
            CanContact = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SubjectEmployments.Add(new SubjectEmploymentDb
        {
            Id = Ulid.NewUlid().ToString(),
            SubjectId = subjectId,
            EmployerName = "Tech Giants Inc",
            JobTitle = "Senior Developer",
            StartDate = new DateOnly(2022, 6, 15),
            EndDate = null, // Current employment
            CanContact = false,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "subject-tester");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "emp-hist@holmes.dev");

        await EnsureTestUserAsync(factory, "emp-hist@holmes.dev");
        var response = await client.GetAsync($"/api/subjects/{subjectId}/employments");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var employments = await response.Content.ReadFromJsonAsync<List<SubjectEmploymentDto>>();
        Assert.That(employments, Is.Not.Null);
        Assert.That(employments, Has.Count.EqualTo(2));
        // Current employment should come first (sorted by IsCurrent desc, then StartDate desc)
        Assert.That(employments![0].IsCurrent, Is.True);
        Assert.That(employments[0].EmployerName, Is.EqualTo("Tech Giants Inc"));
        Assert.That(employments[0].CanContact, Is.False);
        Assert.That(employments[1].IsCurrent, Is.False);
        Assert.That(employments[1].EmployerName, Is.EqualTo("Acme Corp"));
        Assert.That(employments[1].ReasonForLeaving, Is.EqualTo("Career advancement"));
        Assert.That(employments[1].SupervisorName, Is.EqualTo("Lucy Van Pelt"));
    }

    [Test]
    public async Task GetEmployments_Returns_BadRequest_For_Invalid_Id()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "subject-tester");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "emp-invalid@holmes.dev");

        await EnsureTestUserAsync(factory, "emp-invalid@holmes.dev");
        var response = await client.GetAsync("/api/subjects/not-a-valid-ulid/employments");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private sealed record RegisterSubjectRequest(
        string GivenName,
        string FamilyName,
        DateOnly? DateOfBirth,
        string? Email
    );

    private static async Task EnsureTestUserAsync(
        HolmesWebApplicationFactory factory,
        string email = "tester@holmes.dev"
    )
    {
        using var scope = factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new RegisterExternalUserCommand(
            "https://issuer.holmes.test",
            "subject-tester",
            email,
            "Subject Tester",
            "pwd",
            DateTimeOffset.UtcNow,
            true));
    }
}