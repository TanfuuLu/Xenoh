using Mediator;
using Xenoh.Application.Features.ProgressLibrary.Dtos;

namespace Xenoh.Application.Features.ProgressLibrary.Queries.GetProgressPhotoPreviewUrl;

public sealed record GetProgressPhotoPreviewUrlQuery(Guid PhotoId) : IRequest<ProgressPhotoPreviewUrlDto>;
