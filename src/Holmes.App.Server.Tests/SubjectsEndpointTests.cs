using System.Net;
using System.Net.Http.Json;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Subjects.Infrastructure.Sql.Entities;
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
        client.DefaultRequestHeaders.Add("X-Auth-Email", "tester@holmes.dev");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "tester@holmes.dev");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "tester@holmes.dev");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "tester@holmes.dev");

        var request = new RegisterSubjectRequest("Jane", "Doe", new DateOnly(1985, 6, 1), "jane.doe@example.com");
        var response = await client.PostAsJsonAsync("/api/subjects", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var summary = await response.Content.ReadFromJsonAsync<SubjectSummaryResponse>();
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

        var response = await client.GetAsync($"/api/subjects/{Ulid.NewUlid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetSubjectById_Returns_Summary()
    {
        await using var factory = new HolmesWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubjectsDbContext>();
        var subjectId = Ulid.NewUlid().ToString();
        db.SubjectDirectory.Add(new SubjectDirectoryDb
        {
            SubjectId = subjectId,
            GivenName = "John",
            FamilyName = "Smith",
            CreatedAt = DateTimeOffset.UtcNow,
            AliasCount = 0,
            IsMerged = false
        });
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "subject-tester");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "tester@holmes.dev");

        var response = await client.GetAsync($"/api/subjects/{subjectId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var summary = await response.Content.ReadFromJsonAsync<SubjectSummaryResponse>();
        Assert.That(summary, Is.Not.Null);
        Assert.That(summary!.GivenName, Is.EqualTo("John"));
    }

    private sealed record RegisterSubjectRequest(
        string GivenName,
        string FamilyName,
        DateOnly? DateOfBirth,
        string? Email
    );

    private sealed record SubjectSummaryResponse(
        string SubjectId,
        string GivenName,
        string FamilyName,
        DateOnly? DateOfBirth,
        string? Email,
        bool IsMerged,
        int AliasCount,
        DateTimeOffset CreatedAt
    );
}