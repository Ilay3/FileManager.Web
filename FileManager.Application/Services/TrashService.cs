using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Interfaces;
using FileManager.Domain.Enums;

namespace FileManager.Application.Services;

public class TrashService : ITrashService
{
    private readonly IFilesRepository _filesRepository;
    private readonly IFolderRepository _folderRepository;
    private readonly IYandexDiskService _yandexDiskService;
    private readonly IAuditService _auditService;

    public TrashService(IFilesRepository filesRepository, IFolderRepository folderRepository, IYandexDiskService yandexDiskService, IAuditService auditService)
    {
        _filesRepository = filesRepository;
        _folderRepository = folderRepository;
        _yandexDiskService = yandexDiskService;
        _auditService = auditService;
    }

    public async Task<List<TrashItemDto>> GetTrashAsync(Guid userId, bool isAdmin = false)
    {
        var items = new List<TrashItemDto>();
        var files = isAdmin ? await _filesRepository.GetDeletedAsync() : await _filesRepository.GetDeletedAsync(userId);
        var folders = isAdmin ? await _folderRepository.GetDeletedAsync() : await _folderRepository.GetDeletedAsync(userId);
        var limit = DateTime.UtcNow.AddDays(-30);

        foreach (var f in files)
        {
            if (isAdmin || f.DeletedAt == null || f.DeletedAt >= limit)
            {
                items.Add(new TrashItemDto { Id = f.Id, Name = f.Name, Type = "file", DeletedAt = f.DeletedAt });
            }
        }

        foreach (var f in folders)
        {
            if (isAdmin || f.DeletedAt == null || f.DeletedAt >= limit)
            {
                items.Add(new TrashItemDto { Id = f.Id, Name = f.Name, Type = "folder", DeletedAt = f.DeletedAt });
            }
        }

        return items.OrderByDescending(i => i.DeletedAt).ToList();
    }

    public async Task<bool> RestoreFileAsync(Guid fileId, Guid userId, bool isAdmin = false)
    {
        var file = await _filesRepository.GetDeletedByIdAsync(fileId);
        if (file == null) return false;
        if (!isAdmin)
        {
            if (file.UploadedById != userId) return false;
            if (file.DeletedAt < DateTime.UtcNow.AddDays(-30)) return false;
        }

        file.IsDeleted = false;
        file.DeletedAt = null;
        await _filesRepository.UpdateAsync(file);
        await _auditService.LogAsync(AuditAction.FileRestore, userId, fileId: file.Id, description: "Восстановил файл");
        return true;
    }

    public async Task<bool> RestoreFolderAsync(Guid folderId, Guid userId, bool isAdmin = false)
    {
        var folder = await _folderRepository.GetDeletedByIdAsync(folderId);
        if (folder == null) return false;
        if (!isAdmin)
        {
            if (folder.CreatedById != userId) return false;
            if (folder.DeletedAt < DateTime.UtcNow.AddDays(-30)) return false;
        }

        folder.IsDeleted = false;
        folder.DeletedAt = null;
        await _folderRepository.UpdateAsync(folder);
        await _auditService.LogAsync(AuditAction.FolderRestore, userId, folderId: folder.Id, description: "Восстановил папку");
        return true;
    }

    public async Task<bool> DeleteFilePermanentAsync(Guid fileId, Guid userId, bool isAdmin = false)
    {
        var file = await _filesRepository.GetDeletedByIdAsync(fileId);
        if (file == null) return false;
        if (!isAdmin && file.UploadedById != userId) return false;

        await _yandexDiskService.DeleteFileAsync(file.YandexPath, true);
        await _filesRepository.HardDeleteAsync(fileId);
        await _auditService.LogAsync(AuditAction.FileDelete, userId, fileId: fileId, description: "Удалил файл окончательно");
        return true;
    }

    public async Task<bool> DeleteFolderPermanentAsync(Guid folderId, Guid userId, bool isAdmin = false)
    {
        var folder = await _folderRepository.GetDeletedByIdAsync(folderId);
        if (folder == null) return false;
        if (!isAdmin && folder.CreatedById != userId) return false;

        await _folderRepository.HardDeleteAsync(folderId);
        await _auditService.LogAsync(AuditAction.FolderDelete, userId, folderId: folderId, description: "Удалил папку окончательно");
        return true;
    }
}
