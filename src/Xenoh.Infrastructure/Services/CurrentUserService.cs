using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var id = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
        }
    }

    public string? UserName => User?.FindFirstValue(ClaimTypes.Name);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
