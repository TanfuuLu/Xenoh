using System.Text.RegularExpressions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Admin;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Promotions.Admin;

public sealed record AdminPromotionCodeResponse(
    Guid Id,
    string Code,
    string Description,
    PromotionDiscountType DiscountType,
    decimal DiscountValue,
    PlanTier? AppliesToTier,
    int? MaxRedemptions,
    int MaxRedemptionsPerUser,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    bool IsActive,
    DateTime CreatedAt,
    int CompletedRedemptions,
    int PendingUses,
    decimal TotalDiscountGranted
);

public sealed record PromotionCodePayload(
    string Code,
    string? Description,
    PromotionDiscountType DiscountType,
    decimal DiscountValue,
    PlanTier? AppliesToTier,
    int? MaxRedemptions,
    int MaxRedemptionsPerUser,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    bool IsActive
);

public sealed record GetAdminPromotionCodesQuery : IRequest<List<AdminPromotionCodeResponse>>;
public sealed record CreatePromotionCodeCommand(PromotionCodePayload Payload) : IRequest<AdminPromotionCodeResponse>;
public sealed record UpdatePromotionCodeCommand(Guid Id, PromotionCodePayload Payload) : IRequest<AdminPromotionCodeResponse>;
public sealed record DeletePromotionCodeCommand(Guid Id) : IRequest<Unit>;

internal static class PromotionCodeRules
{
    private static readonly Regex CodePattern = new("^[A-Z0-9]{3,40}$", RegexOptions.Compiled);

    /// <summary>Normalizes and validates a payload; throws InvalidOperationException with a user-readable message.</summary>
    public static PromotionCodePayload Validate(PromotionCodePayload payload)
    {
        var code = PromotionEvaluation.NormalizeCode(payload.Code ?? string.Empty);
        if (!CodePattern.IsMatch(code))
            throw new InvalidOperationException("Code must be 3-40 characters, letters and digits only.");

        switch (payload.DiscountType)
        {
            case PromotionDiscountType.Percent when payload.DiscountValue is < 1 or > 100:
                throw new InvalidOperationException("Percent discount must be between 1 and 100.");
            case PromotionDiscountType.FixedAmount when payload.DiscountValue < 1_000:
                throw new InvalidOperationException("Fixed discount must be at least 1,000 VND.");
        }

        if (payload.AppliesToTier == PlanTier.Free)
            throw new InvalidOperationException("Promotion codes can only target paid tiers.");
        if (payload.MaxRedemptions is < 1)
            throw new InvalidOperationException("Max redemptions must be at least 1 (or empty for unlimited).");
        if (payload.MaxRedemptionsPerUser < 1)
            throw new InvalidOperationException("Max redemptions per user must be at least 1.");
        if (payload.StartsAt.HasValue && payload.ExpiresAt.HasValue && payload.StartsAt >= payload.ExpiresAt)
            throw new InvalidOperationException("Start date must be before expiry date.");

        return payload with
        {
            Code = code,
            Description = payload.Description?.Trim() ?? string.Empty,
            StartsAt = AsUtc(payload.StartsAt),
            ExpiresAt = AsUtc(payload.ExpiresAt)
        };
    }

    public static string Describe(PromotionCode p)
    {
        var discount = p.DiscountType == PromotionDiscountType.Percent
            ? $"{p.DiscountValue:0.##}%"
            : $"{p.DiscountValue:N0} VND";
        return $"{p.Code}: {discount} off {(p.AppliesToTier?.ToString() ?? "any paid tier")}, " +
               $"max {(p.MaxRedemptions?.ToString() ?? "unlimited")} uses ({p.MaxRedemptionsPerUser}/user), " +
               $"{(p.IsActive ? "active" : "inactive")}";
    }

    private static DateTime? AsUtc(DateTime? value) => value?.ToUniversalTime();
}

public sealed class GetAdminPromotionCodesHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminPromotionCodesQuery, List<AdminPromotionCodeResponse>>
{
    public async ValueTask<List<AdminPromotionCodeResponse>> Handle(
        GetAdminPromotionCodesQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await db.PromotionCodes.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new AdminPromotionCodeResponse(
                p.Id, p.Code, p.Description, p.DiscountType, p.DiscountValue,
                p.AppliesToTier,
                p.MaxRedemptions, p.MaxRedemptionsPerUser,
                p.StartsAt, p.ExpiresAt, p.IsActive, p.CreatedAt,
                p.PaymentOrders.Count(o => o.Status == PaymentStatus.Completed),
                p.PaymentOrders.Count(o => o.Status == PaymentStatus.Pending && o.ExpiresAt > now),
                p.PaymentOrders.Where(o => o.Status == PaymentStatus.Completed).Sum(o => o.DiscountAmount)))
            .ToListAsync(ct);
    }
}

public sealed class CreatePromotionCodeHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreatePromotionCodeCommand, AdminPromotionCodeResponse>
{
    public async ValueTask<AdminPromotionCodeResponse> Handle(
        CreatePromotionCodeCommand request, CancellationToken ct)
    {
        var payload = PromotionCodeRules.Validate(request.Payload);

        var exists = await db.PromotionCodes.AnyAsync(p => p.Code == payload.Code, ct);
        if (exists)
            throw new InvalidOperationException($"Promotion code '{payload.Code}' already exists.");

        var promo = new PromotionCode
        {
            Code = payload.Code,
            Description = payload.Description ?? string.Empty,
            DiscountType = payload.DiscountType,
            DiscountValue = payload.DiscountValue,
            AppliesToTier = payload.AppliesToTier,
            MaxRedemptions = payload.MaxRedemptions,
            MaxRedemptionsPerUser = payload.MaxRedemptionsPerUser,
            StartsAt = payload.StartsAt,
            ExpiresAt = payload.ExpiresAt,
            IsActive = payload.IsActive
        };
        db.PromotionCodes.Add(promo);

        AdminAudit.Add(
            db, currentUser.UserId, AdminAudit.CreatePromotionCode, "PromotionCode", promo.Id, null,
            $"Created promotion code {promo.Code}", "-", PromotionCodeRules.Describe(promo));

        await db.SaveChangesAsync(ct);
        return ToResponse(promo);
    }

    internal static AdminPromotionCodeResponse ToResponse(PromotionCode p, int completed = 0, int pending = 0, decimal granted = 0) =>
        new(p.Id, p.Code, p.Description, p.DiscountType, p.DiscountValue,
            p.AppliesToTier, p.MaxRedemptions, p.MaxRedemptionsPerUser,
            p.StartsAt, p.ExpiresAt, p.IsActive, p.CreatedAt, completed, pending, granted);
}

public sealed class UpdatePromotionCodeHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdatePromotionCodeCommand, AdminPromotionCodeResponse>
{
    public async ValueTask<AdminPromotionCodeResponse> Handle(
        UpdatePromotionCodeCommand request, CancellationToken ct)
    {
        var payload = PromotionCodeRules.Validate(request.Payload);

        var promo = await db.PromotionCodes.FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new InvalidOperationException("Promotion code not found.");

        var duplicate = await db.PromotionCodes.AnyAsync(p => p.Code == payload.Code && p.Id != promo.Id, ct);
        if (duplicate)
            throw new InvalidOperationException($"Promotion code '{payload.Code}' already exists.");

        var before = PromotionCodeRules.Describe(promo);

        promo.Code = payload.Code;
        promo.Description = payload.Description ?? string.Empty;
        promo.DiscountType = payload.DiscountType;
        promo.DiscountValue = payload.DiscountValue;
        promo.AppliesToTier = payload.AppliesToTier;
        promo.MaxRedemptions = payload.MaxRedemptions;
        promo.MaxRedemptionsPerUser = payload.MaxRedemptionsPerUser;
        promo.StartsAt = payload.StartsAt;
        promo.ExpiresAt = payload.ExpiresAt;
        promo.IsActive = payload.IsActive;
        promo.UpdatedAt = DateTime.UtcNow;

        AdminAudit.Add(
            db, currentUser.UserId, AdminAudit.UpdatePromotionCode, "PromotionCode", promo.Id, null,
            $"Updated promotion code {promo.Code}", before, PromotionCodeRules.Describe(promo));

        await db.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;
        var stats = await db.PaymentOrders.AsNoTracking()
            .Where(o => o.PromotionCodeId == promo.Id)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Completed = g.Count(o => o.Status == PaymentStatus.Completed),
                Pending = g.Count(o => o.Status == PaymentStatus.Pending && o.ExpiresAt > now),
                Granted = g.Where(o => o.Status == PaymentStatus.Completed).Sum(o => o.DiscountAmount)
            })
            .FirstOrDefaultAsync(ct);

        return CreatePromotionCodeHandler.ToResponse(
            promo, stats?.Completed ?? 0, stats?.Pending ?? 0, stats?.Granted ?? 0);
    }
}

public sealed class DeletePromotionCodeHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<DeletePromotionCodeCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeletePromotionCodeCommand request, CancellationToken ct)
    {
        var promo = await db.PromotionCodes.FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new InvalidOperationException("Promotion code not found.");

        var used = await db.PaymentOrders.AnyAsync(o => o.PromotionCodeId == promo.Id, ct);
        if (used)
            throw new InvalidOperationException(
                "This code has been used on payment orders and cannot be deleted. Deactivate it instead.");

        db.PromotionCodes.Remove(promo);

        AdminAudit.Add(
            db, currentUser.UserId, AdminAudit.DeletePromotionCode, "PromotionCode", promo.Id, null,
            $"Deleted promotion code {promo.Code}", PromotionCodeRules.Describe(promo), "-");

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
