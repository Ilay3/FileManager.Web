using FileManager.Domain.Enums;

namespace FileManager.Application.DTOs;

public class SearchRequestDto
{
    public string? SearchTerm { get; set; }
    public Guid? FolderId { get; set; }
    public FileType? FileType { get; set; }
    public string? Extension { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Tags { get; set; }
    public bool OnlyMyFiles { get; set; } = false;
    public string? Department { get; set; }

    // Sorting
    public string SortBy { get; set; } = "name"; // name, date, size, type
    public string SortDirection { get; set; } = "asc"; // asc, desc

    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class SearchResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

public class BreadcrumbDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "📁";
}