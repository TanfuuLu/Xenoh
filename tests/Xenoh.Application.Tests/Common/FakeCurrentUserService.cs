using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Tests.Common;

public sealed class FakeCurrentUserService(Guid userId) : ICurrentUserService
{
    public Guid UserId => userId;
    public string? UserName => "testuser";
    public bool IsAuthenticated => true;
}
