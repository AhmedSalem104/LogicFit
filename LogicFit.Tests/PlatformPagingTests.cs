using LogicFit.API.Features.Platform.Common;
using Xunit;

namespace LogicFit.Tests;

public class PlatformPagingTests
{
    [Fact]
    public void Create_returns_a_one_based_bounded_page_contract()
    {
        var page = PlatformPaging.Create(Enumerable.Range(1, 45).ToList(), page: 2, pageSize: 20);

        Assert.Equal(45, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(20, page.PageSize);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
        Assert.Equal(Enumerable.Range(21, 20), page.Items);
    }

    [Fact]
    public void Create_normalizes_invalid_page_values_and_handles_an_empty_result()
    {
        var page = PlatformPaging.Create(Array.Empty<int>(), page: 0, pageSize: 10_000);

        Assert.Equal(1, page.Page);
        Assert.Equal(PlatformPaging.MaximumPageSize, page.PageSize);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.TotalPages);
        Assert.False(page.HasPreviousPage);
        Assert.False(page.HasNextPage);
    }
}
