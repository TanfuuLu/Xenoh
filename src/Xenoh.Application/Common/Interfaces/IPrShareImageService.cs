using Xenoh.Application.Features.Share.Queries.GetPrShareData;

namespace Xenoh.Application.Common.Interfaces;

public interface IPrShareImageService
{
    Task<byte[]> GenerateAsync(PrShareData data, CancellationToken ct = default);
}
