using Holmes.Identity.Server.Data;
using Holmes.Identity.Server.Pages.Account;
using Holmes.Identity.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Holmes.Identity.Server.Tests.Pages;

public class ChangePasswordModelTests
{
    private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
    private Mock<SignInManager<ApplicationUser>> _signInManagerMock = null!;
    private Mock<IPasswordHistoryService> _passwordHistoryServiceMock = null!;
    private FakeTimeProvider _timeProvider = null!;
    private IOptions<PasswordPolicyOptions> _passwordOptions = null!;
    private Mock<ILogger<ChangePasswordModel>> _loggerMock = null!;
    private ChangePasswordModel _pageModel = null!;

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
        _loggerMock = new Mock<ILogger<ChangePasswordModel>>();

        _pageModel = new ChangePasswordModel(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _passwordHistoryServiceMock.Object,
            _timeProvider,
            _passwordOptions,
            _loggerMock.Object);

        SetupPageContext(_pageModel);
    }

    [Test]
    public void OnGet_WithExpiredFlag_SetsIsExpired()
    {
        _pageModel.OnGet(expired: true, returnUrl: "/dashboard");

        Assert.Multiple(() =>
        {
            Assert.That(_pageModel.IsExpired, Is.True);
            Assert.That(_pageModel.ReturnUrl, Is.EqualTo("/dashboard"));
        });
    }

    [Test]
    public async Task OnPostAsync_WithValidCurrentPassword_ChangesPassword()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@example.com",
            PasswordHash = "oldHash"
        };

        _pageModel.Input = new ChangePasswordModel.InputModel
        {
            Email = "test@example.com",
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmPassword = "NewPassword456!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "OldPassword123!")).ReturnsAsync(true);
        _userManagerMock
            .Setup(x => x.ChangePasswordAsync(user, "OldPassword123!", "NewPassword456!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _pageModel.OnPostAsync("/dashboard");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<PageResult>());
            Assert.That(_pageModel.ChangeComplete, Is.True);
            Assert.That(user.LastPasswordChangedAt, Is.EqualTo(_timeProvider.GetUtcNow()));
            Assert.That(user.PasswordExpires, Is.EqualTo(_timeProvider.GetUtcNow().AddDays(90)));
        });

        _signInManagerMock.Verify(x => x.SignInAsync(user, false, null), Times.Once);
        _passwordHistoryServiceMock.Verify(
            x => x.RecordPasswordChangeAsync("user1", "oldHash", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task OnPostAsync_WithInvalidCurrentPassword_ReturnsError()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@example.com"
        };

        _pageModel.Input = new ChangePasswordModel.InputModel
        {
            Email = "test@example.com",
            CurrentPassword = "WrongPassword!",
            NewPassword = "NewPassword456!",
            ConfirmPassword = "NewPassword456!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "WrongPassword!")).ReturnsAsync(false);

        var result = await _pageModel.OnPostAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<PageResult>());
            Assert.That(_pageModel.ModelState.IsValid, Is.False);
            Assert.That(_pageModel.ModelState[string.Empty]?.Errors[0].ErrorMessage,
                Does.Contain("Current password is incorrect"));
        });
    }

    [Test]
    public async Task OnPostAsync_WithUserNotFound_ReturnsError()
    {
        _pageModel.Input = new ChangePasswordModel.InputModel
        {
            Email = "nonexistent@example.com",
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmPassword = "NewPassword456!"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync("nonexistent@example.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _pageModel.OnPostAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<PageResult>());
            Assert.That(_pageModel.ModelState.IsValid, Is.False);
        });
    }

    [Test]
    public async Task OnPostAsync_WithReusedPassword_ReturnsValidationError()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@example.com",
            PasswordHash = "oldHash"
        };

        _pageModel.Input = new ChangePasswordModel.InputModel
        {
            Email = "test@example.com",
            CurrentPassword = "OldPassword123!",
            NewPassword = "OldPassword123!",
            ConfirmPassword = "OldPassword123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "OldPassword123!")).ReturnsAsync(true);
        _userManagerMock
            .Setup(x => x.ChangePasswordAsync(user, "OldPassword123!", "OldPassword123!"))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "PasswordReused", Description = "Cannot reuse password" }));

        var result = await _pageModel.OnPostAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<PageResult>());
            Assert.That(_pageModel.ModelState.IsValid, Is.False);
            Assert.That(_pageModel.ChangeComplete, Is.False);
        });
    }

    [Test]
    public async Task OnPostAsync_DoesNotRecordHistoryWhenNoOldHash()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@example.com",
            PasswordHash = null
        };

        _pageModel.Input = new ChangePasswordModel.InputModel
        {
            Email = "test@example.com",
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmPassword = "NewPassword456!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "OldPassword123!")).ReturnsAsync(true);
        _userManagerMock
            .Setup(x => x.ChangePasswordAsync(user, "OldPassword123!", "NewPassword456!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        await _pageModel.OnPostAsync();

        _passwordHistoryServiceMock.Verify(
            x => x.RecordPasswordChangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static void SetupPageContext(PageModel pageModel)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            new PageActionDescriptor());

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns<string>(s => s);
        urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns(true);

        pageModel.PageContext = new PageContext(actionContext);
        pageModel.Url = urlHelperMock.Object;
    }
}
