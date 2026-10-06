using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Auth.Register;
using AutoMarket.Identity.Infrastructure.Passwords;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Application.Auth.Register;

// FR-AUTH-01 AC1/AC3: hər qaydanın sərhəd dəyərləri (CONVENTIONS §11.6)
public sealed class RegisterRequestValidatorTests
{
    private static readonly RegisterRequestValidator Validator = new(new PasswordPolicy(TestData.Options()));

    private static readonly RegisterRequest Valid = new("leyla@automarket.az", "x7!kq2#z9-strong", "Leyla", "+994501234567", true);

    [Fact]
    public void Validate_ValidRequest_NoErrors() => Validator.Validate(Valid).IsValid.ShouldBeTrue();

    [Fact]
    public void Validate_WithoutPhone_NoErrors() => Validator.Validate(Valid with { Phone = null }).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData(null, ValidationCodes.Required)]
    [InlineData("", ValidationCodes.Required)]
    [InlineData("not-an-email", ValidationCodes.InvalidFormat)]
    public void Validate_InvalidEmail_HasCode(string? email, string code) =>
        Codes(Valid with { Email = email }, nameof(RegisterRequest.Email)).ShouldContain(code);

    [Fact]
    public void Validate_Email255Characters_TooLong() =>
        Codes(Valid with { Email = new string('a', 246) + "@test.az1" }, nameof(RegisterRequest.Email)).ShouldContain(ValidationCodes.TooLong);

    [Theory]
    [InlineData(1, ValidationCodes.TooShort)]
    [InlineData(51, ValidationCodes.TooLong)]
    public void Validate_NameOutOfRange_HasCode(int length, string code) =>
        Codes(Valid with { Name = new string('ə', length) }, nameof(RegisterRequest.Name)).ShouldContain(code);

    [Theory]
    [InlineData(2)]
    [InlineData(50)]
    public void Validate_NameAtBoundary_Valid(int length) =>
        Codes(Valid with { Name = new string('ə', length) }, nameof(RegisterRequest.Name)).ShouldBeEmpty();

    [Theory]
    [InlineData("0501234567")]
    [InlineData("+99450123456")]
    [InlineData("+9945012345678")]
    [InlineData("+994 50 123 45 67")]
    public void Validate_PhoneNotAzerbaijaniFormat_InvalidFormat(string phone) =>
        Codes(Valid with { Phone = phone }, nameof(RegisterRequest.Phone)).ShouldContain(ValidationCodes.InvalidFormat);

    [Theory]
    [InlineData(null, ValidationCodes.Required)]
    [InlineData(false, ValidationCodes.MustBeTrue)]
    public void Validate_TermsNotAccepted_HasCode(bool? accepted, string code) =>
        Codes(Valid with { TermsAccepted = accepted }, nameof(RegisterRequest.TermsAccepted)).ShouldContain(code);

    [Fact]
    public void Validate_CommonPassword_PasswordTooCommon() =>
        Codes(Valid with { Password = "1234567890" }, nameof(RegisterRequest.Password)).ShouldContain(ValidationCodes.PasswordTooCommon);

    [Fact]
    public void Validate_PasswordContainsName_PersonalInfo() =>
        Codes(Valid with { Name = "Leyla Xanım", Password = "leyla xanım 2026!" }, nameof(RegisterRequest.Password))
            .ShouldContain(ValidationCodes.PasswordContainsPersonalInfo);

    [Fact]
    public void Sanitize_TextFields_TrimsAndRemovesControlCharacters()
    {
        var sanitized = (Valid with { Email = "  leyla@automarket.az ", Name = " Ley\u0000la​ " }).Sanitize();

        sanitized.Email.ShouldBe("leyla@automarket.az");
        sanitized.Name.ShouldBe("Leyla");
    }

    private static string[] Codes(RegisterRequest request, string property) =>
        [.. Validator.Validate(request).Errors.Where(error => error.PropertyName == property).Select(error => error.ErrorCode)];
}
