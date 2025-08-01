using FileManager.Domain.Entities;
using FileManager.Domain.Interfaces;
using FileManager.Domain.Enums;
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
            .Include(f => f.Folder)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Files>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Files
            .Where(f => f.UploadedById == userId && !f.IsDeleted)
            .Include(f => f.Folder)
            .Include(f => f.UploadedBy)
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

    public async Task<IEnumerable<Files>> SearchAsync(string? searchTerm = null, Guid? folderId = null,
        FileType? fileType = null, string? extension = null, DateTime? dateFrom = null,
        DateTime? dateTo = null, Guid? userId = null)
    {
        var query = _context.Files
            .Where(f => !f.IsDeleted)
            .Include(f => f.UploadedBy)
            .Include(f => f.Folder)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(f => f.Name.Contains(searchTerm) ||
                                   f.OriginalName.Contains(searchTerm) ||
                                   (f.Tags != null && f.Tags.Contains(searchTerm)));
        }

        if (folderId.HasValue)
        {
            query = query.Where(f => f.FolderId == folderId.Value);
        }

        if (fileType.HasValue)
        {
            query = query.Where(f => f.FileType == fileType.Value);
        }

        if (!string.IsNullOrEmpty(extension))
        {
            query = query.Where(f => f.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(f => f.CreatedAt >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(f => f.CreatedAt <= dateTo.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(f => f.UploadedById == userId.Value);
        }

        return await query
            .OrderByDescending(f => f.UpdatedAt ?? f.CreatedAt)
            .Take(200)
            .ToListAsync();
    }

    public async Task<IEnumerable<Files>> GetRecentFilesAsync(Guid userId, int count = 10)
    {
        return await _context.Files
            .Where(f => f.UploadedById == userId && !f.IsDeleted)
            .Include(f => f.Folder)
            .Include(f => f.UploadedBy)
            .OrderByDescending(f => f.UpdatedAt ?? f.CreatedAt)
            .Take(count)
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