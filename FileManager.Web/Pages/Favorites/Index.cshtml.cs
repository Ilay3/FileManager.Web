using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace FileManager.Web.Pages.Favorites;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IFavoriteService _favoriteService;

    public IndexModel(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    public List<FavoriteItemDto> Favorites { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid.TryParse(userIdClaim, out var userId);
        Favorites = await _favoriteService.GetFavoritesAsync(userId);
    }
}
