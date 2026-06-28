namespace Xenoh.Application.Features.Files.Dtos;

/// <summary>One person a file is shared with (coach → client).</summary>
public sealed record FileShareTargetDto(
    Guid ShareId,
    Guid SharedWithUserId,
    string SharedWithName
);

/// <summary>A file owned by the current user, with its share targets.</summary>
public sealed record StoredFileDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime CreatedAt,
    IReadOnlyList<FileShareTargetDto> SharedWith
);

/// <summary>The current user's files plus their quota usage.</summary>
public sealed record MyFilesResponse(
    long UsedBytes,
    long QuotaBytes,
    long MaxFileSizeBytes,
    IReadOnlyList<StoredFileDto> Files
);

/// <summary>A file shared TO the current user by a coach.</summary>
public sealed record SharedFileDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime CreatedAt,
    string OwnerName
);

/// <summary>A short-lived presigned download URL.</summary>
public sealed record FileDownloadUrlResponse(string Url);
