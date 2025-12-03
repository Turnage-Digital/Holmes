using Holmes.Identity.Server.Data;
using Holmes.Identity.Server.Pages.Account;
using Holmes.Identity.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Holmes.Identity.Server.Tests.Pages;

public class ConfirmEmailModelTests
{
    private Mock<ILogger<ConfirmEmailModel>> _loggerMock = null!;
    private ConfirmEmailModel _pageModel = null!;
    private Mock<IPasswordHistoryService> _passwordHistoryServiceMock = null!;
    private IOptions<PasswordPolicyOptions> _passwordOptions = null!;
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

        _passwordHistoryServiceMock = new Mock<IPasswordHistoryService>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero));
        _passwordOptions = Options.Create(new PasswordPolicyOptions
        {
            ExpirationDays = 90,
            PreviousPasswordCount = 10
        });
        _loggerMock = new Mock<ILogger<ConfirmEmailModel>>();

        _pageModel = new ConfirmEmailModel(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _passwordHistoryServiceMock.Object,
            _timeProvider,
            _passwordOptions,
            _loggerMock.Object);

        SetupPageContext(_pageModel);
    }

    [Test]
    public async Task OnGetAsync_WithValidToken_ConfirmsEmail()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@example.com",
            EmailConfirmed = false,
            PasswordHash = "existingHash"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);
        _userManagerMock
            .Setup(x => x.ConfirmEmailAsync(user, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _pageModel.OnGetAsync("user1", "dGVzdC10b2tlbg==", "/dashboard");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<PageResult>());
            Assert.That(_pageModel.EmailConfirmed, Is.True);
            Assert.That(_pageModel.RequiresPassword, Is.False);
        });
    }

    [Test]
    public async Task OnGetAsync_WhenUserHasNoPassword_ShowsPasswordForm()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@example.com",
            EmailConfirmed = false,
            PasswordHash = null
        };

        _userManagerMock.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);
        _userManagerMock
            .Setup(x => x.ConfirmEmailAsync(user, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _pageModel.OnGetAsync("user1", "dGVzdC10b2tlbg==");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<PageResult>());
            Assert.That(_pageModel.EmailConfirmed, Is.True);
            Assert.That(_pageModel.RequiresPassword, Is.True);
            Assert.That(_pageModel.StatusMessage, Does.Contain("set your password"));
        });
    }

    [Test]
    public async Task OnGetAsync_WithInvalidToken_ShowsError()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@example.com",
            EmailConfirmed = false
        };

        _userManagerMock.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);
        _userManagerMock
            .Setup(x => x.ConfirmEmailAsync(user, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        var result = await _pageModel.OnGetAsync("user1", "aW52YWxpZA==");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<PageResult>());
            Assert.That(_pageModel.EmailConfirmed, Is.False);
            Assert.That(_pageModel.StatusMessage, Does.Contain("Error"));
        });
    }

    [Test]
    public async Task OnGetAsync_WithMissingUserId_RedirectsToIndex()
    {
        var result = await _pageModel.OnGetAsync(null, "dGVzdC10b2tlbg==");

        Assert.That(result, Is.TypeOf<RedirectToPageResult>());
    }

    [Test]
    public async Task OnPostAsync_WithValidPassword_SetsPasswordAndSignsIn()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@example.com",
            PasswordHash = null
        };

        _pageModel.Input = new ConfirmEmailModel.InputModel
        {
            Password = "NewSecurePass123!",
            ConfirmPassword = "NewSecurePass123!"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);
        _userManagerMock
            .Setup(x => x.AddPasswordAsync(user, "NewSecurePass123!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _pageModel.OnPostAsync("user1", "dGVzdC10b2tlbg==", "/dashboard");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<RedirectResult>());
            Assert.That(user.LastPasswordChangedAt, Is.EqualTo(_timeProvider.GetUtcNow()));
            Assert.That(user.PasswordExpires, Is.EqualTo(_timeProvider.GetUtcNow().AddDays(90)));
        });

        _signInManagerMock.Verify(x => x.SignInAsync(user, false, null), Times.Once);
    }

    [Test]
    public async Task OnPostAsync_WithWeakPassword_ReturnsPageWithErrors()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@example.com",
            PasswordHash = null
        };

        _pageModel.Input = new ConfirmEmailModel.InputModel
        {
            Password = "weak",
            ConfirmPassword = "weak"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);
        _userManagerMock
            .Setup(x => x.AddPasswordAsync(user, "weak"))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "PasswordTooShort", Description = "Password too short" }));

        var result = await _pageModel.OnPostAsync("user1", "dGVzdC10b2tlbg==");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<PageResult>());
            Assert.That(_pageModel.ModelState.IsValid, Is.False);
            Assert.That(_pageModel.RequiresPassword, Is.True);
        });
    }

    [Test]
    public async Task OnPostAsync_WithExistingPassword_RecordsInHistory()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@example.com",
            PasswordHash = "oldHash"
        };

        _pageModel.Input = new ConfirmEmailModel.InputModel
        {
            Password = "NewSecurePass123!",
            ConfirmPassword = "NewSecurePass123!"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);
        _userManagerMock
            .Setup(x => x.AddPasswordAsync(user, "NewSecurePass123!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        await _pageModel.OnPostAsync("user1", "dGVzdC10b2tlbg==");

        _passwordHistoryServiceMock.Verify(
            x => x.RecordPasswordChangeAsync("user1", "oldHash", It.IsAny<CancellationToken>()),
            Times.Once);
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

        pageModel.PageContext = new PageContext(actionContext);
        pageModel.Url = urlHelperMock.Object;
    }
}