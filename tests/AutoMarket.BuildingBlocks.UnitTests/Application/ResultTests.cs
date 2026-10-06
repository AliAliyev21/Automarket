using AutoMarket.BuildingBlocks.Application;

namespace AutoMarket.BuildingBlocks.UnitTests.Application;

public sealed class ResultTests
{
    [Fact]
    public void ImplicitValue_IsSuccess()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void ImplicitError_IsFailureAndValueThrows()
    {
        Result<int> result = CommonErrors.Forbidden;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(CommonErrors.Forbidden);
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void RateLimitedFor_Delay_KeepsCodeAndCarriesRetryAfter()
    {
        var error = CommonErrors.RateLimitedFor(TimeSpan.FromSeconds(30));

        error.Code.ShouldBe(ErrorCodes.RateLimited);
        error.HttpStatus.ShouldBe(429);
        error.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
    }
}
