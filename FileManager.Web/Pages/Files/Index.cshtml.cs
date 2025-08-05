using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

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
    public TreeNodeDto? CurrentFolderContents { get; set; }

    // View state
    public string ViewMode { get; set; } = "list"; // list, grid
    public Guid CurrentFolderId { get; set; } = Guid.Empty;

    public async Task OnGetAsync(Guid? folderId = null, string? view = null)
    {
        CurrentFolderId = folderId ?? Guid.Empty;
        ViewMode = view ?? "list";

        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";

        // Set folder context
        SearchRequest.FolderId = CurrentFolderId;
        Breadcrumbs = await _folderService.GetBreadcrumbsAsync(CurrentFolderId);

        // Load data based on view mode
        if (ViewMode.ToLower() == "grid")
        {
            FilesResult = await _fileService.GetFilesAsync(SearchRequest, userId, isAdmin);
        }
        else
        {
            CurrentFolderContents = await _folderService.GetFolderContentsAsync(CurrentFolderId, userId, SearchRequest);
        }
    }

    public async Task<IActionResult> OnGetSearchAsync()
    {
        var userId = GetCurrentUserId();
        FilesResult = await _fileService.SearchFilesAsync(SearchRequest, userId);

        return Partial("_FilesList", FilesResult);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}