using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using ApplicationUser = Xenoh.Domain.Entities.ApplicationUser;

namespace Xenoh.Application.Features.Auth.Commands.Register;

public sealed class RegisterHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext context
) : IRequestHandler<RegisterCommand, RegisterResponse>
{
    public async ValueTask<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var allowedRoles = new[] { UserRole.Individual };
        if (!allowedRoles.Contains(request.Role))
            throw new InvalidOperationException($"Role '{request.Role}' is not allowed. Registration is open to Individual accounts only.");

        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            throw new InvalidOperationException("Email is already registered.");

        if (request.Gender is null)
            throw new InvalidOperationException("Gender is required.");

        if (request.DateOfBirth is null)
            throw new InvalidOperationException("Date of birth is required.");

        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Gender = request.Gender.Value,
            DateOfBirth = request.DateOfBirth.Value,
            Height = request.Height,
            // No default avatar — the UI renders initials when AvatarUrl is null.
            AvatarUrl = null,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        await userManager.AddToRoleAsync(user, request.Role);

        if (request.Bodyweight is > 0)
        {
            context.BodyweightLogs.Add(new BodyweightLog
            {
                UserId = user.Id,
                Weight = request.Bodyweight.Value,
                Date = DateOnly.FromDateTime(DateTime.UtcNow)
            });
            await context.SaveChangesAsync(cancellationToken);
        }

        return new RegisterResponse(user.Id, user.Email!);
    }
}
