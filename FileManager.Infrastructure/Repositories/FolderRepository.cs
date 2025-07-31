using FileManager.Domain.Entities;
using FileManager.Domain.Interfaces;
using FileManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FileManager.Infrastructure.Repositories;

public class FolderRepository : IFolderRepository
{
    private readonly AppDbContext _context;

    public FolderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Folder?> GetByIdAsync(Guid id)
    {
        return await _context.Folders
            .Include(f => f.CreatedBy)
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
    }

    public async Task<IEnumerable<Folder>> GetRootFoldersAsync()
    {
        return await _context.Folders
            .Where(f => f.ParentFolderId == null && !f.IsDeleted)
            .Include(f => f.CreatedBy)
            .ToListAsync();
    }

    public async Task<IEnumerable<Folder>> GetSubFoldersAsync(Guid parentId)
    {
        return await _context.Folders
            .Where(f => f.ParentFolderId == parentId && !f.IsDeleted)
            .Include(f => f.CreatedBy)
            .ToListAsync();
    }

    public async Task<Folder> CreateAsync(Folder folder)
    {
        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();
        return folder;
    }

    public async Task<Folder> UpdateAsync(Folder folder)
    {
        folder.UpdatedAt = DateTime.UtcNow;
        _context.Folders.Update(folder);
        await _context.SaveChangesAsync();
        return folder;
    }

    public async Task DeleteAsync(Guid id)
    {
        var folder = await GetByIdAsync(id);
        if (folder != null)
        {
            folder.IsDeleted = true;
            folder.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Folder?> GetByYandexPathAsync(string yandexPath)
    {
        return await _context.Folders
            .FirstOrDefaultAsync(f => f.YandexPath == yandexPath && !f.IsDeleted);
    }

    public async Task<int> CountAsync()
    {
        return await _context.Folders.CountAsync(f => !f.IsDeleted);
    }
}
