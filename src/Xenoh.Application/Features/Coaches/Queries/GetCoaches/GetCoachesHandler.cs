using System.Globalization;
using System.Text;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Coaches.Queries.GetCoaches;

public sealed class GetCoachesHandler(
    UserManager<ApplicationUser> userManager
) : IRequestHandler<GetCoachesQuery, List<CoachResponse>>
{
    public async ValueTask<List<CoachResponse>> Handle(GetCoachesQuery request, CancellationToken cancellationToken)
    {
        var coaches = await userManager.GetUsersInRoleAsync(UserRole.Coach);

        IEnumerable<ApplicationUser> filtered = coaches;

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            // Normalize để hỗ trợ tìm kiếm không dấu tiếng Việt
            // Ví dụ: "binh" khớp với "Bình", "nguyen" khớp với "Nguyễn"
            var keyword = StripDiacritics(request.Name.Trim().ToLower());

            filtered = coaches.Where(c =>
            {
                var firstName  = StripDiacritics(c.FirstName.ToLower());
                var lastName   = StripDiacritics(c.LastName.ToLower());
                var fullName   = StripDiacritics($"{c.FirstName} {c.LastName}".ToLower());

                return firstName.Contains(keyword)
                    || lastName.Contains(keyword)
                    || fullName.Contains(keyword);
            });
        }

        return filtered
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .Select(c => new CoachResponse(
                c.Id,
                $"{c.FirstName} {c.LastName}",
                c.Email!,
                c.AvatarUrl
            ))
            .ToList();
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
