using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Infrastructure.Persistence;

namespace Xenoh.Application.Tests.Common;

public abstract class HandlerTestBase : IDisposable
{
    // Each test class gets its own isolated in-memory database
    protected readonly string DbName = Guid.NewGuid().ToString();

    protected readonly Guid UserId = Guid.NewGuid();

    protected ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(DbName)
            .Options);

    protected ICurrentUserService CurrentUser() => new FakeCurrentUserService(UserId);

    public void Dispose() { }
}
