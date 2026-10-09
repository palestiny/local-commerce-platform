using LocalCommerce.Application.Errors;
using Xunit;

namespace LocalCommerce.Application.Tests.Errors;

public sealed class ApplicationFailureExceptionTests
{
    [Fact]
    public void Stable_code_is_exposed_independently_of_human_readable_message()
    {
        var failure = new ApplicationFailureException(
            ApplicationErrorCodes.OrderInvalidState,
            "The Order cannot be cancelled in its current state.");

        Assert.Equal("order.invalid_state", failure.Code);
        Assert.Equal("The Order cannot be cancelled in its current state.", failure.Message);
    }

    [Fact]
    public void Inner_exception_is_preserved_for_server_side_diagnostics()
    {
        var inner = new InvalidOperationException("private diagnostic");
        var failure = new ApplicationFailureException(
            ApplicationErrorCodes.InternalUnexpected,
            "An unexpected application failure occurred.",
            inner);

        Assert.Equal(ApplicationErrorCodes.InternalUnexpected, failure.Code);
        Assert.Same(inner, failure.InnerException);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_error_code_is_rejected(string? code)
    {
        Assert.Throws<ArgumentException>(() =>
            new ApplicationFailureException(code!, "A message."));
    }
}
