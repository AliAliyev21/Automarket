using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.BuildingBlocks.UnitTests.Security;

// SEC-SEC-03
public sealed class SecretValueTests
{
    [Theory]
    [InlineData("change-me-postgres", true)]
    [InlineData("CHANGEME", true)]
    [InlineData("<password>", true)]
    [InlineData("s3cr3t-Value", false)]
    public void IsPlaceholder_Value_Detected(string value, bool expected) =>
        SecretValue.IsPlaceholder(value).ShouldBe(expected);
}
