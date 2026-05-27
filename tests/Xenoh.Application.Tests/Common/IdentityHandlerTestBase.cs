using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;

namespace Xenoh.Application.Tests.Common;

public abstract class IdentityHandlerTestBase : HandlerTestBase
{
    private IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection()
            .SetApplicationName($"test-{DbName}")
            .UseEphemeralDataProtectionProvider();
        services.AddDbContext<ApplicationDbContext>(opts =>
            opts.UseInMemoryDatabase(DbName));
        services.AddIdentityCore<ApplicationUser>(opts =>
        {
            opts.Password.RequireDigit = false;
            opts.Password.RequireLowercase = false;
            opts.Password.RequireUppercase = false;
            opts.Password.RequireNonAlphanumeric = false;
            opts.Password.RequiredLength = 6;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
        return services.BuildServiceProvider();
    }

    protected UserManager<ApplicationUser> CreateUserManager() =>
        BuildServiceProvider().GetRequiredService<UserManager<ApplicationUser>>();

    protected RoleManager<IdentityRole<Guid>> CreateRoleManager() =>
        BuildServiceProvider().GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    protected async Task SeedRolesAsync()
    {
        var roleManager = CreateRoleManager();
        foreach (var roleName in new[] { UserRole.Individual, UserRole.Coach, UserRole.Admin })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        }
    }

    protected async Task<ApplicationUser> SeedUserAsync(string email, string password, string firstName = "Test", string lastName = "User")
        => await SeedUserAsync(UserId, email, password, firstName, lastName);

    protected async Task<ApplicationUser> SeedUserAsync(Guid userId, string email, string password, string firstName = "Test", string lastName = "User")
    {
        var userManager = CreateUserManager();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName
        };
        await userManager.CreateAsync(user, password);
        return user;
    }
}
