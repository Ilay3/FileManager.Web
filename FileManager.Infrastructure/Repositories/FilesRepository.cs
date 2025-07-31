using FileManager.Domain.Entities;
using FileManager.Domain.Interfaces;
using FileManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FileManager.Infrastructure.Repositories;

public class FilesRepository : IFilesRepository
{
    private readonly AppDbContext _context;

    public FilesRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Files?> GetByIdAsync(Guid id)
    {
        return await _context.Files
            .Include(f => f.UploadedBy)
            .Include(f => f.Folder)
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
    }

    public async Task<IEnumerable<Files>> GetByFolderIdAsync(Guid folderId)
    {
        return await _context.Files
            .Where(f => f.FolderId == folderId && !f.IsDeleted)
            .Include(f => f.UploadedBy)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Files>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Files
            .Where(f => f.UploadedById == userId && !f.IsDeleted)
            .Include(f => f.Folder)
            .OrderByDescending(f => f.CreatedAt)
            .Take(50)
            .ToListAsync();
    }

    public async Task<Files> CreateAsync(Files file)
    {
        _context.Files.Add(file);
        await _context.SaveChangesAsync();
        return file;
    }

    public async Task<Files> UpdateAsync(Files file)
    {
        file.UpdatedAt = DateTime.UtcNow;
        _context.Files.Update(file);
        await _context.SaveChangesAsync();
        return file;
    }

    public async Task DeleteAsync(Guid id)
    {
        var file = await GetByIdAsync(id);
        if (file != null)
        {
            file.IsDeleted = true;
            file.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Files>> SearchByNameAsync(string searchTerm)
    {
        return await _context.Files
            .Where(f => f.Name.Contains(searchTerm) && !f.IsDeleted)
            .Include(f => f.UploadedBy)
            .Include(f => f.Folder)
            .OrderByDescending(f => f.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        return await _context.Files.CountAsync(f => !f.IsDeleted);
    }

    public async Task<long> GetTotalSizeAsync()
    {
        return await _context.Files
            .Where(f => !f.IsDeleted)
            .SumAsync(f => (long?)f.SizeBytes) ?? 0;
    }
}
