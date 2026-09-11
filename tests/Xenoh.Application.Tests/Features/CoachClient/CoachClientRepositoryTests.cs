using FluentAssertions;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Rules;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Xenoh.Application.Tests.Features.CoachClient;

public sealed class CoachClientRepositoryTests : HandlerTestBase
{
    [Fact]
    public async Task Get_all_by_coach_returns_an_active_client_with_display_status()
    {
        var coachId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var today = CoachingPolicy.LocalDate(DateTime.UtcNow);
        await using var db = CreateContext();
        db.ApplicationUsers.AddRange(
            new ApplicationUser { Id = coachId, Email = "coach@example.test", UserName = "coach@example.test", FirstName = "Ada", LastName = "Coach" },
            new ApplicationUser { Id = clientId, Email = "client@example.test", UserName = "client@example.test", FirstName = "Sam", LastName = "Client" });
        db.CoachClientRelationships.Add(new CoachClientRelationship
        {
            CoachId = coachId,
            ClientId = clientId,
            Status = RelationshipStatus.Active,
            StartDate = today.AddDays(-7),
            EndDate = today.AddDays(30),
        });
        await db.SaveChangesAsync();

        var clients = await new CoachClientRepository(db).GetAllByCoachAsync(coachId, default);

        clients.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            ClientId = clientId,
            FullName = "Sam Client",
            Email = "client@example.test",
            Status = "Active",
        });
    }
}
