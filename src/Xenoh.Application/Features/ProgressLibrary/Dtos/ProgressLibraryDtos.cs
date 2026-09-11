using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.ProgressLibrary.Dtos;

public sealed record ProgressPhotoDto(
    Guid Id,
    string FileName,
    ProgressPhotoAngle Angle,
    int SortOrder);

public sealed record ProgressCheckInDto(
    Guid Id,
    DateOnly CheckInDate,
    string? Title,
    string? Notes,
    decimal? BodyweightKg,
    bool IsMilestone,
    bool IsSharedWithCoach,
    DateTime CreatedAt,
    IReadOnlyList<ProgressPhotoDto> Photos);

public sealed record ProgressPhotoUpload(
    string FileName,
    string ContentType,
    long Length,
    ProgressPhotoAngle Angle,
    Stream Content);

public sealed record ProgressPhotoPreviewUrlDto(string Url);
