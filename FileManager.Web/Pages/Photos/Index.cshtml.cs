using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Collections.Generic;

namespace FileManager.Web.Pages.Photos;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IFileService _fileService;

    public IndexModel(IFileService fileService)
    {
        _fileService = fileService;
    }

    public List<FileDto> Photos { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid.TryParse(userIdClaim, out var userId);
        var request = new SearchRequestDto
        {
            FileType = FileType.Image,
            PageSize = 100
        };
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
        var result = await _fileService.SearchFilesAsync(request, userId, isAdmin);
        Photos = result.Items;
    }
}
