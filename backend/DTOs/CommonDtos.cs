namespace GiaPhuWaterDelivery.Api.DTOs;

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages)
{
    public static PagedResult<T> Create(IReadOnlyCollection<T> items, int totalCount, int page, int pageSize) =>
        new(items, totalCount, page, pageSize, (int)Math.Ceiling(totalCount / (double)pageSize));
}

public sealed record MessageResponse(string Message);
