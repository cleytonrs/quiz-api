using FluentValidation.TestHelper;
using QuizApp.DTOs;
using QuizApp.Validators;

namespace QuizApp.Tests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid_Request()
    {
        var request = new RegisterRequest { Email = "test@example.com", Password = "password123" };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Email_Is_Empty()
    {
        var request = new RegisterRequest { Email = "", Password = "password123" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Fail_When_Email_Is_Invalid()
    {
        var request = new RegisterRequest { Email = "not-an-email", Password = "password123" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Fail_When_Password_Is_Empty()
    {
        var request = new RegisterRequest { Email = "test@example.com", Password = "" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Fail_When_Password_Is_Too_Short()
    {
        var request = new RegisterRequest { Email = "test@example.com", Password = "1234567" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters long.");
    }

    [Fact]
    public void Should_Pass_When_Password_Is_Exactly_8_Characters()
    {
        var request = new RegisterRequest { Email = "test@example.com", Password = "12345678" };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
