namespace ProcessManager.Api.DTOs;

/// <summary>
/// Standard paginated response wrapper for list endpoints.
/// </summary>
public record PaginatedResponse<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

/// <summary>
/// Standard error response following RFC 7807 ProblemDetails-like structure.
/// </summary>
public record ApiError(
    string Title,
    string Detail,
    int Status,
    Dictionary<string, string[]>? Errors = null);
