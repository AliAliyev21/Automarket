using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Domain.Users;

// SEC-AUTH-03: 15 dəq × 2^(level−1), ən çox 24 saat
public sealed class LockoutPolicyTests
{
    [Theory]
    [InlineData(1, 15)]
    [InlineData(2, 30)]
    [InlineData(3, 60)]
    [InlineData(4, 120)]
    [InlineData(5, 240)]
    [InlineData(6, 480)]
    [InlineData(7, 960)]
    [InlineData(8, 1440)]
    [InlineData(9, 1440)]
    [InlineData(1000, 1440)]
    public void DurationForLevel_Level_DoublesUpToMax(int level, int expectedMinutes) =>
        TestData.Lockout.DurationForLevel(level).ShouldBe(TimeSpan.FromMinutes(expectedMinutes));
}
