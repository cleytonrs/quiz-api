using FluentValidation;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using QuizApp.DTOs;
using QuizApp.Validators;

namespace QuizApp.Tests.PropertyTests;

/// <summary>
/// Property 10: Password Length Validation
/// For any string shorter than 8 characters used as a password in a registration request,
/// the system SHALL reject the registration with a validation error.
/// For any string of 8 or more characters, the password length validation SHALL pass.
///
/// Validates: Requirements 7.5
/// </summary>
[Trait("Feature", "quiz-app")]
[Trait("Property", "10: Password Length Validation")]
public class PasswordValidationProperties
{
    private readonly RegisterRequestValidator _validator = new();

    /// <summary>
    /// **Validates: Requirements 7.5**
    ///
    /// Any password shorter than 8 characters must fail the password length validation rule.
    /// </summary>
    [Property(Arbitrary = new[] { typeof(PasswordArbitraries) })]
    public bool Password_ShorterThan8Characters_FailsLengthValidation(ShortPasswordInput input)
    {
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = input.Password
        };

        var result = _validator.Validate(request);

        // Should have a validation error on the Password field related to length
        return result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterRequest.Password) &&
            e.ErrorMessage.Contains("8"));
    }

    /// <summary>
    /// **Validates: Requirements 7.5**
    ///
    /// Any password of 8 or more characters must pass the password length validation rule.
    /// </summary>
    [Property(Arbitrary = new[] { typeof(PasswordArbitraries) })]
    public bool Password_8OrMoreCharacters_PassesLengthValidation(ValidLengthPasswordInput input)
    {
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = input.Password
        };

        var result = _validator.Validate(request);

        // Should NOT have any validation error about minimum length on the Password field
        return !result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterRequest.Password) &&
            e.ErrorMessage.Contains("8"));
    }
}

/// <summary>
/// Input type for passwords shorter than 8 characters.
/// </summary>
public record ShortPasswordInput(string Password);

/// <summary>
/// Input type for passwords of 8 or more characters.
/// </summary>
public record ValidLengthPasswordInput(string Password);

/// <summary>
/// Custom Arbitrary instances for password validation property tests.
/// </summary>
public static class PasswordArbitraries
{
    public static Arbitrary<ShortPasswordInput> ShortPasswordInput()
    {
        // Generate non-empty strings with length 1-7
        var charGen = Gen.Choose(33, 126).Select(c => (char)c);
        var gen = Gen.Choose(1, 7)
            .SelectMany(length =>
                Gen.ArrayOf(charGen, length)
                    .Select(chars => new string(chars)))
            .Select(password => new ShortPasswordInput(password));

        return gen.ToArbitrary();
    }

    public static Arbitrary<ValidLengthPasswordInput> ValidLengthPasswordInput()
    {
        // Generate strings with length 8-50
        var charGen = Gen.Choose(33, 126).Select(c => (char)c);
        var gen = Gen.Choose(8, 50)
            .SelectMany(length =>
                Gen.ArrayOf(charGen, length)
                    .Select(chars => new string(chars)))
            .Select(password => new ValidLengthPasswordInput(password));

        return gen.ToArbitrary();
    }
}
