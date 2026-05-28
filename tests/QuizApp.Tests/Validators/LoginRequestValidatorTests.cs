using FluentValidation.TestHelper;
using QuizApp.DTOs;
using QuizApp.Validators;

namespace QuizApp.Tests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid_Request()
    {
        var request = new LoginRequest { Email = "test@example.com", Password = "password123" };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Email_Is_Empty()
    {
        var request = new LoginRequest { Email = "", Password = "password123" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Fail_When_Password_Is_Empty()
    {
        var request = new LoginRequest { Email = "test@example.com", Password = "" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
