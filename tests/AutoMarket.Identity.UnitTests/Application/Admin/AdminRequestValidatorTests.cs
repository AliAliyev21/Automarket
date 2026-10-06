using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Admin;
using AutoMarket.Identity.Application.Admin.Users.BlockUser;
using AutoMarket.Identity.Application.Admin.Users.GrantRole;

namespace AutoMarket.Identity.UnitTests.Application.Admin;

// FR-ADM-01 AC1 (səbəb 1–500), FR-ADM-02 AC1 (yalnız Moderator/Admin), SEC-INP-01
public sealed class AdminRequestValidatorTests
{
    [Theory]
    [InlineData(null, ValidationCodes.Required)]
    [InlineData("", ValidationCodes.Required)]
    public void BlockUser_MissingReason_Required(string? reason, string code) =>
        new BlockUserRequestValidator().Validate(new BlockUserRequest(reason)).Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe(code);

    [Fact]
    public void BlockUser_ReasonOver500_TooLong() =>
        new BlockUserRequestValidator().Validate(new BlockUserRequest(new string('a', 501))).Errors
            .ShouldHaveSingleItem().ErrorCode.ShouldBe(ValidationCodes.TooLong);

    [Fact]
    public void BlockUser_Reason500_Valid() =>
        new BlockUserRequestValidator().Validate(new BlockUserRequest(new string('a', 500))).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData("Moderator")]
    [InlineData("admin")]
    public void GrantRole_AssignableRole_Valid(string role) =>
        new GrantRoleRequestValidator().Validate(new GrantRoleRequest(role)).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData("User")]
    [InlineData("SuperAdmin")]
    public void GrantRole_NotAssignableRole_InvalidFormat(string role) =>
        new GrantRoleRequestValidator().Validate(new GrantRoleRequest(role)).Errors
            .ShouldHaveSingleItem().ErrorCode.ShouldBe(ValidationCodes.InvalidFormat);

    [Fact]
    public void AssignableRoles_Normalize_ReturnsCanonicalName() =>
        AssignableRoles.Normalize("moderator").ShouldBe("Moderator");
}
