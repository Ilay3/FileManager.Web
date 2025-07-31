using FileManager.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace FileManager.Web.Pages.Files;

[Authorize]
public class IndexModel : PageModel
{
    private readonly FilesService _filesService;

    public IndexModel(FilesService filesService)
    {
        _filesService = filesService;
    }

    // Используем полное имя класса, чтобы избежать конфликта с namespace
    public List<FileManager.Domain.Entities.Files> Files { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            Files = (await _filesService.GetUserFilesAsync(userId)).ToList();
        }
    }
}
