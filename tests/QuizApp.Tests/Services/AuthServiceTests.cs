using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using QuizApp.DTOs;
using QuizApp.Exceptions;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var configData = new Dictionary<string, string?>
        {
            { "Jwt:Key", "TestKeyThatIsLongEnoughForHmacSha256Algorithm123!" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
            { "Jwt:ExpirationInMinutes", "60" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _authService = new AuthService(_userManagerMock.Object, _configuration);
    }

    [Fact]
    public async Task RegisterAsync_Should_Return_Token_And_Email_On_Success()
    {
        var request = new RegisterRequest { Email = "test@example.com", Password = "password123" };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _authService.RegisterAsync(request);

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_ConflictException_When_Email_Exists()
    {
        var request = new RegisterRequest { Email = "existing@example.com", Password = "password123" };
        var existingUser = new ApplicationUser { Email = "existing@example.com", UserName = "existing@example.com" };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _authService.RegisterAsync(request));

        Assert.Equal("Email is already in use", exception.Message);
        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_ValidationException_When_Identity_Fails()
    {
        var request = new RegisterRequest { Email = "test@example.com", Password = "password123" };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Some error" }));

        var exception = await Assert.ThrowsAsync<Exceptions.ValidationException>(
            () => _authService.RegisterAsync(request));

        Assert.Equal("Registration failed", exception.Message);
        Assert.Contains("Some error", exception.Details);
    }

    [Fact]
    public async Task LoginAsync_Should_Return_Token_And_Email_On_Success()
    {
        var request = new LoginRequest { Email = "test@example.com", Password = "password123" };
        var user = new ApplicationUser { Id = "user-id-1", Email = "test@example.com", UserName = "test@example.com" };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);

        var result = await _authService.LoginAsync(request);

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_UnauthorizedException_When_User_Not_Found()
    {
        var request = new LoginRequest { Email = "nonexistent@example.com", Password = "password123" };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _authService.LoginAsync(request));

        Assert.Equal("Invalid email or password", exception.Message);
        Assert.Equal(401, exception.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_UnauthorizedException_When_Password_Is_Wrong()
    {
        var request = new LoginRequest { Email = "test@example.com", Password = "wrongpassword" };
        var user = new ApplicationUser { Id = "user-id-1", Email = "test@example.com", UserName = "test@example.com" };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _authService.LoginAsync(request));

        Assert.Equal("Invalid email or password", exception.Message);
        Assert.Equal(401, exception.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_Should_Return_Same_Generic_Error_For_Wrong_Email_And_Wrong_Password()
    {
        // Requirement 7.4: Do not reveal which field is wrong
        var wrongEmailRequest = new LoginRequest { Email = "wrong@example.com", Password = "password123" };
        var wrongPasswordRequest = new LoginRequest { Email = "test@example.com", Password = "wrongpassword" };
        var user = new ApplicationUser { Id = "user-id-1", Email = "test@example.com", UserName = "test@example.com" };

        _userManagerMock.Setup(x => x.FindByEmailAsync("wrong@example.com"))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "wrongpassword"))
            .ReturnsAsync(false);

        var wrongEmailException = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _authService.LoginAsync(wrongEmailRequest));
        var wrongPasswordException = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _authService.LoginAsync(wrongPasswordRequest));

        // Both should return the same generic message
        Assert.Equal(wrongEmailException.Message, wrongPasswordException.Message);
        Assert.Equal("Invalid email or password", wrongEmailException.Message);
    }
}
