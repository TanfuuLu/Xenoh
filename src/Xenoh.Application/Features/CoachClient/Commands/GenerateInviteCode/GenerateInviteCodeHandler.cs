using System.Security.Cryptography;
using Mapster;
using Xenoh.Application.Features.CoachClient.Agreements;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.CoachClient.Commands.GenerateInviteCode;

public sealed class GenerateInviteCodeHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GenerateInviteCodeCommand, CoachInviteCodeDto>
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0/O/I/1 to avoid confusion
    private const int CodeLength = 8;

    public async ValueTask<CoachInviteCodeDto> Handle(
        GenerateInviteCodeCommand request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;
        var today = Xenoh.Domain.Rules.CoachingPolicy.LocalDate(DateTime.UtcNow);
        if (!request.Acknowledged || request.Terms is null)
            throw new InvalidOperationException("Review and acknowledge the coaching agreement before publishing.");
        var agreement = request.Terms.Publish(coachId, request.CoachingStartDate, request.CoachingEndDate, coachId);

        if (request.CoachingStartDate < today)
            throw new InvalidOperationException("Coaching start date cannot be in the past.");
        if (request.CoachingEndDate <= request.CoachingStartDate)
            throw new InvalidOperationException("Coaching end date must be after start date.");

        // Generate a unique 8-character code, retry on collision
        string code;
        do
        {
            code = GenerateCode();
        }
        while (await db.CoachInviteCodes.AnyAsync(c => c.Code == code, cancellationToken));

        var inviteCode = new CoachInviteCode
        {
            CoachId = coachId,
            Agreement = agreement,
            AgreementId = agreement.Id,
            Code = code,
            CoachingStartDate = request.CoachingStartDate,
            CoachingEndDate = request.CoachingEndDate,
            IsUsed = false
        };

        db.CoachInviteCodes.Add(inviteCode);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(inviteCode);
    }

    private static string GenerateCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(CodeLength);
        var chars = new char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        return new string(chars);
    }

    internal static CoachInviteCodeDto ToDto(CoachInviteCode c) =>
        new(c.Id, c.Code, c.CoachingStartDate, c.CoachingEndDate,
            c.IsUsed, c.UsedByClientId, c.UsedAt, c.CreatedAt, c.Agreement?.Adapt<AgreementDto>(), c.Agreement?.RelationshipId);
}
