using GiaPhuWaterDelivery.Api.DTOs;

namespace GiaPhuWaterDelivery.Api.Tests.DTOs;

public sealed class PagedResultTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(25, 10, 3)]
    [InlineData(100, 100, 1)]
    public void Create_rounds_total_pages_up(
        int totalCount,
        int pageSize,
        int expectedTotalPages)
    {
        var result = PagedResult<int>.Create(
            Array.Empty<int>(),
            totalCount,
            page: 1,
            pageSize);

        Assert.Equal(expectedTotalPages, result.TotalPages);
    }

    [Fact]
    public void Create_preserves_items_and_paging_metadata()
    {
        int[] items = [21, 22];

        var result = PagedResult<int>.Create(
            items,
            totalCount: 22,
            page: 3,
            pageSize: 10);

        Assert.Same(items, result.Items);
        Assert.Equal(22, result.TotalCount);
        Assert.Equal(3, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalPages);
    }
}
