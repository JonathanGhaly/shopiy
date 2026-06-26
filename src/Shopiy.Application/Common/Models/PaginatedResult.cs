namespace Shopiy.Application.Common.Models;

public class PaginatedResult<T>
{
    public IReadOnlyList<T> Data { get; init; } = [];
    public MetaData Meta { get; init; } = null!;

    public PaginatedResult()
    {
    }

    public PaginatedResult(IReadOnlyList<T> data, int count, int pageIndex, int pageSize)
    {
        Data = data;
        Meta = new MetaData
        {
            TotalItems = count,
            CurrentPage = pageIndex,
            TotalPages = (int)Math.Ceiling(count / (double)pageSize)
        };
    }
}

public class MetaData
{
    public int TotalItems { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}
