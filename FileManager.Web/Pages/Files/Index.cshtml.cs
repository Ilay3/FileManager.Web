using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Linq;

namespace FileManager.Web.Pages.Files;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IFileService _fileService;
    private readonly IFolderService _folderService;

    public IndexModel(IFileService fileService, IFolderService folderService)
    {
        _fileService = fileService;
        _folderService = folderService;
    }

    [BindProperty(SupportsGet = true)]
    public SearchRequestDto SearchRequest { get; set; } = new();

    public SearchResultDto<FileDto> FilesResult { get; set; } = new();
    public List<BreadcrumbDto> Breadcrumbs { get; set; } = new();
    public List<TreeNodeDto> GridItems { get; set; } = new();

    public Guid CurrentFolderId { get; set; } = Guid.Empty;

    public async Task OnGetAsync(Guid? folderId = null)
    {
        CurrentFolderId = folderId ?? Guid.Empty;

        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";

        // Set folder context
        SearchRequest.FolderId = CurrentFolderId;
        Breadcrumbs = await _folderService.GetBreadcrumbsAsync(CurrentFolderId);

        FilesResult = await _fileService.SearchFilesAsync(SearchRequest, userId, isAdmin);
        var folderContents = await _folderService.GetFolderContentsAsync(CurrentFolderId, userId, SearchRequest, isAdmin);
        if (folderContents != null)
        {
            GridItems = folderContents.Children
                .Where(c => c.Type == "folder")
                .ToList();
        }

        GridItems.AddRange(FilesResult.Items.Select(f => new TreeNodeDto
        {
            Id = f.Id,
            Name = f.Name,
            Type = "file",
            IconClass = f.FileIconClass,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt,
            SizeBytes = f.SizeBytes,
            UploadedByName = f.UploadedByName,
            IsNetworkAvailable = f.IsNetworkAvailable
        }));

        if (!string.IsNullOrEmpty(SearchRequest.SortBy))
        {
            var desc = SearchRequest.SortDirection?.ToLower() == "desc";
            GridItems = SearchRequest.SortBy.ToLower() switch
            {
                "name" => desc
                    ? GridItems.OrderByDescending(i => i.Name).ToList()
                    : GridItems.OrderBy(i => i.Name).ToList(),
                "date" => desc
                    ? GridItems.OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt).ToList()
                    : GridItems.OrderBy(i => i.UpdatedAt ?? i.CreatedAt).ToList(),
                "size" => desc
                    ? GridItems.OrderByDescending(i => i.SizeBytes).ToList()
                    : GridItems.OrderBy(i => i.SizeBytes).ToList(),
                "type" => desc
                    ? GridItems.OrderByDescending(i => i.Type).ToList()
                    : GridItems.OrderBy(i => i.Type).ToList(),
                _ => GridItems
            };
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}