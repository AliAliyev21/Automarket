using AutoMarket.Identity.Application;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Application;

// ADR-0003: iterasiya sayının aşağı həddi 210 000 options validasiyası ilə yoxlanılır
public sealed class IdentityOptionsValidatorTests
{
    [Fact]
    public void Validate_DefaultConfiguration_Succeeds() =>
        new IdentityOptionsValidator().Validate(null, TestData.Options().Value).Succeeded.ShouldBeTrue();

    [Fact]
    public void Validate_IterationCountBelowMinimum_Fails()
    {
        var options = TestData.Options().Value;
        options.Password.IterationCount = 209_999;

        new IdentityOptionsValidator().Validate(null, options).Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_MinLengthBelowTen_Fails()
    {
        var options = TestData.Options().Value;
        options.Password.MinLength = 8;

        new IdentityOptionsValidator().Validate(null, options).Failed.ShouldBeTrue();
    }
}
