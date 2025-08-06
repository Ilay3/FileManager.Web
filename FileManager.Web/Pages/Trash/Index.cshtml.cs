using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace FileManager.Web.Pages.Trash;

[Microsoft.AspNetCore.Authorization.Authorize]
public class IndexModel : PageModel
{
    private readonly ITrashService _trashService;
    public List<TrashItemDto> Items { get; set; } = new();

    public IndexModel(ITrashService trashService)
    {
        _trashService = trashService;
    }

    public async Task OnGet()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
        Items = await _trashService.GetTrashAsync(userId, isAdmin);
    }
}
