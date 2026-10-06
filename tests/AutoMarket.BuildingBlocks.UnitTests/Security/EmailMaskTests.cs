using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.BuildingBlocks.UnitTests.Security;

// SEC-LOG-01: email log-a a***@mpay.az kimi yazılır
public sealed class EmailMaskTests
{
    [Theory]
    [InlineData("ali.aliyev@mpay.az", "a***@mpay.az")]
    [InlineData("x@test.az", "x***@test.az")]
    [InlineData("no-at-sign", "***")]
    [InlineData("@test.az", "***")]
    [InlineData("", "***")]
    [InlineData(null, "***")]
    public void Mask_Email_KeepsFirstCharacterAndDomain(string? email, string expected) =>
        EmailMask.Mask(email).ShouldBe(expected);
}
