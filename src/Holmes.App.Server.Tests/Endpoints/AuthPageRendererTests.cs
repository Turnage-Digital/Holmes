using Holmes.App.Server.Endpoints;

namespace Holmes.App.Server.Tests.Endpoints;

[TestFixture]
public class AuthPageRendererTests
{
    [Test]
    public void RenderAuthOptionsPage_Embeds_Destination()
    {
        var html = AuthPageRenderer.RenderOptionsPage("/home");

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("Sign in to Holmes"));
            Assert.That(html, Does.Contain("/auth/login?returnUrl=%2Fhome"));
        });
    }

    [Test]
    public void RenderAccessDeniedPage_Customizes_By_Reason()
    {
        var html = AuthPageRenderer.RenderAccessDeniedPage("uninvited");

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("Invitation Required"));
            Assert.That(html, Does.Contain("Return to sign in"));
        });
    }
}
