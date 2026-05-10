using System.Globalization;
using System.Text;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Coaches.Queries.GetCoaches;

public sealed class GetCoachesHandler(
    UserManager<ApplicationUser> userManager,
    ICoachRatingRepository ratingRepo,
    IUserBlockRepository userBlockRepo,
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetCoachesQuery, List<CoachResponse>>
{
    public async ValueTask<List<CoachResponse>> Handle(GetCoachesQuery request, CancellationToken cancellationToken)
    {
        var coaches = await userManager.GetUsersInRoleAsync(UserRole.Coach);

        var currentUserId = currentUser.UserId;
        var blockedIds = await userBlockRepo.GetBlockedOrBlockerIdsAsync(currentUserId, cancellationToken);
        IEnumerable<ApplicationUser> filtered = coaches
            .Where(c => c.Id != currentUserId && !blockedIds.Contains(c.Id));

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            // Normalize để hỗ trợ tìm kiếm không dấu tiếng Việt
            // Ví dụ: "binh" khớp với "Bình", "nguyen" khớp với "Nguyễn"
            var keyword = StripDiacritics(request.Name.Trim().ToLower());

            filtered = filtered.Where(c =>
            {
                var firstName  = StripDiacritics(c.FirstName.ToLower());
                var lastName   = StripDiacritics(c.LastName.ToLower());
                var fullName   = StripDiacritics($"{c.FirstName} {c.LastName}".ToLower());

                return firstName.Contains(keyword)
                    || lastName.Contains(keyword)
                    || fullName.Contains(keyword);
            });
        }

        var ordered = filtered
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .ToList();

        var coachIds = ordered.Select(c => c.Id).ToList();
        var marketplaceProfiles = await db.CoachMarketplaceProfiles
            .AsNoTracking()
            .Include(p => p.Packages)
            .Where(p => coachIds.Contains(p.CoachId))
            .ToDictionaryAsync(p => p.CoachId, cancellationToken);

        var responses = new List<CoachResponse>();
        foreach (var coach in ordered)
        {
            var summary = await ratingRepo.GetSummaryAsync(coach.Id, currentUser.UserId, cancellationToken);
            marketplaceProfiles.TryGetValue(coach.Id, out var marketplaceProfile);
            responses.Add(new CoachResponse(
                coach.Id,
                $"{coach.FirstName} {coach.LastName}",
                coach.Email!,
                coach.AvatarUrl,
                summary.AverageRating,
                summary.RatingCount,
                summary.MyRating,
                marketplaceProfile?.Headline,
                marketplaceProfile?.Specialties ?? [],
                marketplaceProfile?.ExperienceYears,
                CoachMarketplaceProfileMapper.StartingPackagePrice(marketplaceProfile?.Packages),
                marketplaceProfile?.Packages.Count ?? 0
            ));
        }

        return responses;
    }

    /// <summary>
    /// Bỏ dấu Unicode — "Nguyễn Văn Bình" → "Nguyen Van Binh"
    /// </summary>
    private static string StripDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
