using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Holmes.Identity.Server.Data;
using Holmes.Identity.Server.Pages.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Holmes.Identity.Server.Tests.Pages;

public class LoginModelTests
{
    private Mock<IIdentityServerInteractionService> _interactionMock = null!;
    private Mock<ILogger<LoginModel>> _loggerMock = null!;
    private LoginModel _pageModel = null!;
    private Mock<SignInManager<ApplicationUser>> _signInManagerMock = null!;
    private FakeTimeProvider _timeProvider = null!;
    private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;

    [SetUp]
    public void SetUp()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);

        _interactionMock = new Mock<IIdentityServerInteractionService>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero));
        _loggerMock = new Mock<ILogger<LoginModel>>();

        _pageModel = new LoginModel(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _interactionMock.Object,
            _timeProvider,
            _loggerMock.Object);

        SetupPageContext(_pageModel);
    }

    [Test]
    public async Task OnPostAsync_WithValidCredentials_RedirectsToReturnUrl()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@example.com",
            PasswordExpires = _timeProvider.GetUtcNow().AddDays(30)
        };

        _pageModel.Input = new LoginModel.InputModel
        {
            Email = "test@example.com",
            Password = "password123"
        };

        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync("test@example.com", "password123", false, true))
            .ReturnsAsync(SignInResult.Success);
        _userManagerMock
            .Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        _interactionMock
            .Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>()))
            .ReturnsAsync((AuthorizationRequest?)null);

        var result = await _pageModel.OnPostAsync("/dashboard");

        Assert.That(result, Is.TypeOf<RedirectResult>());
        var redirect = (RedirectResult)result;
        Assert.That(redirect.Url, Is.EqualTo("/dashboard"));
    }

    [Test]
    public async Task OnPostAsync_WithExpiredPassword_RedirectsToChangePassword()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@example.com",
            PasswordExpires = _timeProvider.GetUtcNow().AddDays(-1)
        };

        _pageModel.Input = new LoginModel.InputModel
        {
            Email = "test@example.com",
            Password = "password123"
        };

        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync("test@example.com", "password123", false, true))
            .ReturnsAsync(SignInResult.Success);
        _userManagerMock
            .Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);

        var result = await _pageModel.OnPostAsync("/dashboard");

        Assert.That(result, Is.TypeOf<RedirectToPageResult>());
        var redirect = (RedirectToPageResult)result;
        Assert.That(redirect.PageName, Is.EqualTo("./ChangePassword"));
        Assert.That(redirect.RouteValues!["expired"], Is.True);
        _signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
    }

    [Test]
    public async Task OnPostAsync_WithInvalidCredentials_ReturnsPageWithError()
    {
        _pageModel.Input = new LoginModel.InputModel
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync("test@example.com", "wrongpassword", false, true))
            .ReturnsAsync(SignInResult.Failed);

        var result = await _pageModel.OnPostAsync();

        Assert.That(result, Is.TypeOf<PageResult>());
        Assert.That(_pageModel.ModelState.IsValid, Is.False);
        Assert.That(_pageModel.ModelState[string.Empty]?.Errors[0].ErrorMessage,
            Is.EqualTo("Invalid email or password."));
    }

    [Test]
    public async Task OnPostAsync_WithLockedOutAccount_ReturnsPageWithLockoutError()
    {
        _pageModel.Input = new LoginModel.InputModel
        {
            Email = "test@example.com",
            Password = "password123"
        };

        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync("test@example.com", "password123", false, true))
            .ReturnsAsync(SignInResult.LockedOut);

        var result = await _pageModel.OnPostAsync();

        Assert.That(result, Is.TypeOf<PageResult>());
        Assert.That(_pageModel.ModelState[string.Empty]?.Errors[0].ErrorMessage,
            Does.Contain("locked out"));
    }

    [Test]
    public async Task OnPostAsync_WithUnconfirmedEmail_ReturnsPageWithConfirmationError()
    {
        _pageModel.Input = new LoginModel.InputModel
        {
            Email = "test@example.com",
            Password = "password123"
        };

        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync("test@example.com", "password123", false, true))
            .ReturnsAsync(SignInResult.NotAllowed);

        var result = await _pageModel.OnPostAsync();

        Assert.That(result, Is.TypeOf<PageResult>());
        Assert.That(_pageModel.ModelState[string.Empty]?.Errors[0].ErrorMessage,
            Does.Contain("confirm your email"));
    }

    [Test]
    public async Task OnPostAsync_WithIdentityServerContext_RedirectsToAuthorizationEndpoint()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@example.com",
            PasswordExpires = _timeProvider.GetUtcNow().AddDays(30)
        };

        _pageModel.Input = new LoginModel.InputModel
        {
            Email = "test@example.com",
            Password = "password123"
        };

        var authContext = new AuthorizationRequest
        {
            Client = new Client { ClientId = "test-client" }
        };

        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync("test@example.com", "password123", false, true))
            .ReturnsAsync(SignInResult.Success);
        _userManagerMock
            .Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        _interactionMock
            .Setup(x => x.GetAuthorizationContextAsync("/connect/authorize?client_id=test"))
            .ReturnsAsync(authContext);

        var result = await _pageModel.OnPostAsync("/connect/authorize?client_id=test");

        Assert.That(result, Is.TypeOf<RedirectResult>());
        var redirect = (RedirectResult)result;
        Assert.That(redirect.Url, Is.EqualTo("/connect/authorize?client_id=test"));
    }

    private static void SetupPageContext(PageModel pageModel)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new PageActionDescriptor());

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns<string>(s => s);
        urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns(true);

        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock
            .Setup(x => x.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(urlHelperMock.Object);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IUrlHelperFactory)))
            .Returns(urlHelperFactoryMock.Object);

        httpContext.RequestServices = serviceProviderMock.Object;

        pageModel.PageContext = new PageContext(actionContext);
        pageModel.Url = urlHelperMock.Object;
    }
}