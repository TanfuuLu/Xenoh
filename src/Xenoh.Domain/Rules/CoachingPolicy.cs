using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Rules;

/// <summary>Agreement v1 uses Asia/Bangkok (UTC+07, no daylight-saving changes).</summary>
public static class CoachingPolicy
{
    public const string TimeZone = "Asia/Bangkok";
    public static DateOnly LocalDate(DateTime utc) => DateOnly.FromDateTime(utc.AddHours(7));
    public static DateTime StartBoundary(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc);
    public static DateTime TermBoundary(DateOnly inclusiveEnd) => StartBoundary(inclusiveEnd.AddDays(1));

    public static bool HasAccess(CoachClientRelationship relationship, DateTime now) =>
        (relationship.Status is RelationshipStatus.Active or RelationshipStatus.PendingTermination or RelationshipStatus.PendingRenewal)
        && relationship.StartDate <= LocalDate(now)
        && (relationship.EndDate == null || now < TermBoundary(relationship.EndDate.Value))
        && (relationship.NoticeEndsAtUtc == null || now < relationship.NoticeEndsAtUtc);

    public static DateTime EndingAt(CoachClientRelationship relationship, DateTime now, bool immediate)
    {
        if (immediate || relationship.AgreementId == null || relationship.StartDate > LocalDate(now))
            return now;
        if (relationship.NoticeEndsAtUtc is { } existing) return existing;
        var notice = now.AddDays(relationship.NoticeDays);
        return relationship.EndDate is { } end && TermBoundary(end) < notice ? TermBoundary(end) : notice;
    }

    public static string DisplayStatus(CoachClientRelationship relationship, DateTime now)
    {
        if (relationship.Status is RelationshipStatus.Ended or RelationshipStatus.Expired or RelationshipStatus.Pending)
            return relationship.Status.ToString();
        if (relationship.NoticeEndsAtUtc is { } notice)
            return now >= notice ? "Ended" : "Ending";
        if (relationship.EndDate is { } end && now >= TermBoundary(end)) return "Expired";
        return relationship.StartDate > LocalDate(now) ? "Scheduled" : "Active";
    }
}
