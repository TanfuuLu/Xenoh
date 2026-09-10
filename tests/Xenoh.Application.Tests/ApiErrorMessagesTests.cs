using FluentAssertions;
using Xenoh.API.Security;
using Xunit;

namespace Xenoh.Application.Tests;

public sealed class ApiErrorMessagesTests
{
    [Fact]
    public void Safe_preserves_bounded_business_messages()
    {
        ApiErrorMessages.Safe("Registration is not open.", "Request failed.")
            .Should().Be("Registration is not open.");
    }

    [Fact]
    public void Safe_replaces_technical_exception_details()
    {
        ApiErrorMessages.Safe("Npgsql.PostgresException: SELECT * FROM users at C:\\app\\db.cs:42", "Something went wrong.")
            .Should().Be("Something went wrong.");
    }

    [Fact]
    public void Safe_replaces_empty_and_oversized_messages()
    {
        ApiErrorMessages.Safe(" ", "Request failed.").Should().Be("Request failed.");
        ApiErrorMessages.Safe(new string('x', 241), "Request failed.").Should().Be("Request failed.");
    }
}
