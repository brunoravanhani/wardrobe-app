using VirtualWardrobe.Application.Common;

namespace VirtualWardrobe.UnitTests.Common;

public sealed class ResultPatternTests
{
    [Fact]
    public void SuccessResultShouldExposeNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Equal(ResultError.None, result.Error);
    }

    [Fact]
    public void FailureResultShouldExposeError()
    {
        var error = ResultError.Validation("Invalid input");
        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error.Code);
    }

    [Fact]
    public void FailureResultOfTAccessingValueShouldThrow()
    {
        var result = Result.Failure<string>(ResultError.NotFound("x"));

        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }
}
