using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Entities;
using FileManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FileManager.Application.Services;

public class FavoriteService : IFavoriteService
{
    private readonly IAppDbContext _context;
    private readonly IAccessService _accessService;

    public FavoriteService(IAppDbContext context, IAccessService accessService)
    {
        _context = context;
        _accessService = accessService;
    }

    public async Task<bool> AddFileAsync(Guid userId, Guid fileId)
    {
        var access = await _accessService.GetEffectiveAccessAsync(userId, fileId);
        if (access == AccessType.None)
            return false;

        var exists = await _context.Favorites.AnyAsync(f => f.UserId == userId && f.FileId == fileId);
        if (exists)
            return true;

        _context.Favorites.Add(new Favorite { UserId = userId, FileId = fileId });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddFolderAsync(Guid userId, Guid folderId)
    {
        var access = await _accessService.GetEffectiveFolderAccessAsync(userId, folderId);
        if (access == AccessType.None)
            return false;

        var exists = await _context.Favorites.AnyAsync(f => f.UserId == userId && f.FolderId == folderId);
        if (exists)
            return true;

        _context.Favorites.Add(new Favorite { UserId = userId, FolderId = folderId });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveFileAsync(Guid userId, Guid fileId)
    {
        var favorite = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.FileId == fileId);
        if (favorite == null)
            return false;

        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveFolderAsync(Guid userId, Guid folderId)
    {
        var favorite = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.FolderId == folderId);
        if (favorite == null)
            return false;

        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<FavoriteItemDto>> GetFavoritesAsync(Guid userId)
    {
        var favorites = await _context.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.File)
            .Include(f => f.Folder)
            .ToListAsync();

        var result = new List<FavoriteItemDto>();
        var removed = false;

        foreach (var fav in favorites)
        {
            if (fav.FileId.HasValue)
            {
                var access = await _accessService.GetEffectiveAccessAsync(userId, fav.FileId.Value);
                if (access == AccessType.None || fav.File == null)
                {
                    _context.Favorites.Remove(fav);
                    removed = true;
                    continue;
                }

                result.Add(new FavoriteItemDto
                {
                    Id = fav.FileId.Value,
                    Name = fav.File.Name,
                    Type = "file"
                });
            }
            else if (fav.FolderId.HasValue)
            {
                var access = await _accessService.GetEffectiveFolderAccessAsync(userId, fav.FolderId.Value);
                if (access == AccessType.None || fav.Folder == null)
                {
                    _context.Favorites.Remove(fav);
                    removed = true;
                    continue;
                }

                result.Add(new FavoriteItemDto
                {
                    Id = fav.FolderId.Value,
                    Name = fav.Folder.Name,
                    Type = "folder"
                });
            }
        }

        if (removed)
            await _context.SaveChangesAsync();

        return result;
    }
}
