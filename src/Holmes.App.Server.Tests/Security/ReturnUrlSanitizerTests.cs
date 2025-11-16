using Holmes.App.Server.Security;
using Microsoft.AspNetCore.Http;

namespace Holmes.App.Server.Tests.Security;

[TestFixture]
public class ReturnUrlSanitizerTests
{
    [TestCase(null, "/")]
    [TestCase("", "/")]
    [TestCase("   ", "/")]
    [TestCase("/dashboard", "/dashboard")]
    [TestCase("dashboard", "/")]
    public void Sanitize_Handles_Null_Empty_And_Relative(string? input, string expected)
    {
        var request = new DefaultHttpContext().Request;
        request.Host = new HostString("app.example");

        var sanitized = ReturnUrlSanitizer.Sanitize(input, request);

        Assert.That(sanitized, Is.EqualTo(expected));
    }

    [Test]
    public void Sanitize_Allows_Absolute_With_Same_Host()
    {
        var request = new DefaultHttpContext().Request;
        request.Host = new HostString("app.example");

        var sanitized = ReturnUrlSanitizer.Sanitize("https://app.example/orders?id=1", request);

        Assert.That(sanitized, Is.EqualTo("/orders?id=1"));
    }

    [Test]
    public void Sanitize_Drops_Mismatched_Host()
    {
        var request = new DefaultHttpContext().Request;
        request.Host = new HostString("app.example");

        var sanitized = ReturnUrlSanitizer.Sanitize("https://evil.example/phish", request);

        Assert.That(sanitized, Is.EqualTo("/"));
    }
}
