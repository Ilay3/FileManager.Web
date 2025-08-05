using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Entities;
using FileManager.Domain.Interfaces;
using FileManager.Domain.Enums;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;

namespace FileManager.Application.Services;

public class FileService : IFileService
{
    private readonly IFilesRepository _filesRepository;
    private readonly IFolderRepository _folderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IYandexDiskService _yandexDiskService;
    private readonly IAccessService _accessService;
    private readonly IAuditService _auditService;

    public FileService(
        IFilesRepository filesRepository,
        IFolderRepository folderRepository,
        IUserRepository userRepository,
        IYandexDiskService yandexDiskService,
        IAccessService accessService,
        IAuditService auditService)
    {
        _filesRepository = filesRepository;
        _folderRepository = folderRepository;
        _userRepository = userRepository;
        _yandexDiskService = yandexDiskService;
        _accessService = accessService;
        _auditService = auditService;
    }

    public async Task<SearchResultDto<FileDto>> GetFilesAsync(SearchRequestDto request, Guid userId, bool isAdmin = false)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return new SearchResultDto<FileDto>();

        var allFiles = isAdmin
            ? await _filesRepository.SearchAsync()
            : await _filesRepository.GetByUserIdAsync(userId);

        var accessibleFiles = new List<Files>();
        foreach (var file in allFiles)
        {
            var access = await _accessService.GetEffectiveAccessAsync(userId, file.Id);
            if (isAdmin || (access & AccessType.Read) == AccessType.Read)
            {
                accessibleFiles.Add(file);
            }
        }

        var filteredFiles = FilterFiles(accessibleFiles, request, user);
        var sortedFiles = SortFiles(filteredFiles, request);

        var totalCount = sortedFiles.Count();
        var pagedFiles = sortedFiles
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var fileDtos = pagedFiles.Select(MapToDto).ToList();

        return new SearchResultDto<FileDto>
        {
            Items = fileDtos,
            TotalCount = totalCount,
            CurrentPage = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<FileDto?> GetFileByIdAsync(Guid id, Guid userId, bool isAdmin = false)
    {
        var file = await _filesRepository.GetByIdAsync(id);
        if (file == null) return null;

        if (!isAdmin)
        {
            var access = await _accessService.GetEffectiveAccessAsync(userId, file.Id);
            if ((access & AccessType.Read) != AccessType.Read)
                return null;
        }

        return MapToDto(file);
    }

    public async Task<List<FileDto>> GetFilesByFolderAsync(Guid folderId, Guid userId, bool isAdmin = false)
    {
        var files = await _filesRepository.GetByFolderIdAsync(folderId);
        var accessible = new List<FileDto>();
        foreach (var file in files)
        {
            if (isAdmin)
            {
                accessible.Add(MapToDto(file));
                continue;
            }

            var access = await _accessService.GetEffectiveAccessAsync(userId, file.Id);
            if ((access & AccessType.Read) == AccessType.Read)
                accessible.Add(MapToDto(file));
        }
        return accessible;
    }

    public async Task<SearchResultDto<FileDto>> SearchFilesAsync(SearchRequestDto request, Guid userId, bool isAdmin = false)
    {
        var files = await _filesRepository.SearchAsync(
            request.SearchTerm,
            request.FolderId,
            request.FileType,
            request.Extension,
            request.DateFrom,
            request.DateTo,
            request.OwnerId,
            request.Tags,
            request.UpdatedFrom,
            request.UpdatedTo,
            request.MinSizeBytes,
            request.MaxSizeBytes);

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null) return new SearchResultDto<FileDto>();

        var accessible = new List<Files>();
        foreach (var file in files)
        {
            if (isAdmin)
            {
                accessible.Add(file);
                continue;
            }

            var access = await _accessService.GetEffectiveAccessAsync(userId, file.Id);
            if ((access & AccessType.Read) == AccessType.Read)
                accessible.Add(file);
        }

        var filteredFiles = FilterFiles(accessible, request, user);
        var sortedFiles = SortFiles(filteredFiles, request);

        var totalCount = sortedFiles.Count();
        var pagedFiles = sortedFiles
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var fileDtos = pagedFiles.Select(MapToDto).ToList();

        return new SearchResultDto<FileDto>
        {
            Items = fileDtos,
            TotalCount = totalCount,
            CurrentPage = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<List<FileDto>> GetRecentFilesAsync(Guid userId, int count = 10)
    {
        var files = await _filesRepository.GetByUserIdAsync(userId);
        var result = new List<Files>();
        foreach (var file in files)
        {
            var access = await _accessService.GetEffectiveAccessAsync(userId, file.Id);
            if ((access & AccessType.Read) == AccessType.Read)
                result.Add(file);
        }

        return result
            .OrderByDescending(f => f.UpdatedAt ?? f.CreatedAt)
            .Take(count)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<List<FileDto>> GetMyFilesAsync(Guid userId, int count = 50)
    {
        var files = await _filesRepository.GetByUserIdAsync(userId);
        var result = new List<Files>();
        foreach (var file in files)
        {
            var access = await _accessService.GetEffectiveAccessAsync(userId, file.Id);
            if ((access & AccessType.Read) == AccessType.Read)
                result.Add(file);
        }

        return result
            .OrderByDescending(f => f.CreatedAt)
            .Take(count)
            .Select(MapToDto)
            .ToList();
    }

    public async Task UpdateTagsAsync(Guid fileId, string tags, Guid userId, bool isAdmin = false)
    {
        var file = await _filesRepository.GetByIdAsync(fileId) ?? throw new InvalidOperationException("Файл не найден");
        if (!isAdmin)
        {
            var access = await _accessService.GetEffectiveAccessAsync(userId, file.Id);
            if ((access & AccessType.Write) != AccessType.Write)
                throw new InvalidOperationException("Недостаточно прав для изменения тегов");
        }

        file.Tags = tags;
        await _filesRepository.UpdateAsync(file);
    }

    public async Task<Stream> DownloadFilesZipAsync(IEnumerable<Guid> ids, Guid userId, bool isAdmin = false)
    {
        var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
        {
            foreach (var id in ids)
            {
                var file = await _filesRepository.GetByIdAsync(id);
                if (file == null) continue;

                if (!isAdmin)
                {
                    var access = await _accessService.GetEffectiveAccessAsync(userId, file.Id);
                    if ((access & AccessType.Read) != AccessType.Read) continue;
                }

                using var fileStream = await _yandexDiskService.DownloadFileAsync(file.YandexPath);
                var entry = archive.CreateEntry(file.OriginalName ?? file.Name);
                using var entryStream = entry.Open();
                await fileStream.CopyToAsync(entryStream);
            }
        }
        memory.Position = 0;
        return memory;
    }

    public async Task DeleteFileAsync(Guid id, Guid userId, bool isAdmin = false)
    {
        var file = await _filesRepository.GetByIdAsync(id);
        if (file == null) return;

        if (!isAdmin)
        {
            var access = await _accessService.GetEffectiveAccessAsync(userId, file.Id);
            if ((access & AccessType.Write) != AccessType.Write)
                throw new InvalidOperationException("Недостаточно прав для удаления файла");
        }

        await _yandexDiskService.DeleteFileAsync(file.YandexPath);
        await _filesRepository.DeleteAsync(id);
        await _auditService.LogAsync(AuditAction.FileDelete, userId, fileId: id, description: "Удалил файл");
    }

    private IEnumerable<Files> FilterFiles(IEnumerable<Files> files, SearchRequestDto request, User user)
    {
        var filtered = files.AsQueryable();

        if (request.FolderId.HasValue)
            filtered = filtered.Where(f => f.FolderId == request.FolderId.Value);

        if (request.FileType.HasValue)
            filtered = filtered.Where(f => f.FileType == request.FileType.Value);

        if (!string.IsNullOrEmpty(request.Extension))
            filtered = filtered.Where(f => f.Extension.Equals(request.Extension, StringComparison.OrdinalIgnoreCase));

        if (request.DateFrom.HasValue)
            filtered = filtered.Where(f => f.CreatedAt >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            filtered = filtered.Where(f => f.CreatedAt <= request.DateTo.Value);

        if (request.UpdatedFrom.HasValue)
            filtered = filtered.Where(f => (f.UpdatedAt ?? f.CreatedAt) >= request.UpdatedFrom.Value);

        if (request.UpdatedTo.HasValue)
            filtered = filtered.Where(f => (f.UpdatedAt ?? f.CreatedAt) <= request.UpdatedTo.Value);

        if (request.OnlyMyFiles)
            filtered = filtered.Where(f => f.UploadedById == user.Id);

        if (!string.IsNullOrEmpty(request.Department) && !string.IsNullOrEmpty(user.Department))
            filtered = filtered.Where(f => f.UploadedBy.Department == request.Department);

        if (request.OwnerId.HasValue)
            filtered = filtered.Where(f => f.UploadedById == request.OwnerId.Value);

        if (request.MinSizeBytes.HasValue)
            filtered = filtered.Where(f => f.SizeBytes >= request.MinSizeBytes.Value);

        if (request.MaxSizeBytes.HasValue)
            filtered = filtered.Where(f => f.SizeBytes <= request.MaxSizeBytes.Value);

        if (!string.IsNullOrEmpty(request.Tags))
        {
            var tagList = request.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var tag in tagList)
            {
                filtered = filtered.Where(f => f.Tags != null && f.Tags.Contains(tag, StringComparison.OrdinalIgnoreCase));
            }
        }

        return filtered;
    }

    private IEnumerable<Files> SortFiles(IEnumerable<Files> files, SearchRequestDto request)
    {
        var sorted = request.SortBy.ToLower() switch
        {
            "date" => request.SortDirection == "desc"
                ? files.OrderByDescending(f => f.UpdatedAt ?? f.CreatedAt)
                : files.OrderBy(f => f.UpdatedAt ?? f.CreatedAt),
            "size" => request.SortDirection == "desc"
                ? files.OrderByDescending(f => f.SizeBytes)
                : files.OrderBy(f => f.SizeBytes),
            "type" => request.SortDirection == "desc"
                ? files.OrderByDescending(f => f.Extension)
                : files.OrderBy(f => f.Extension),
            _ => request.SortDirection == "desc"
                ? files.OrderByDescending(f => f.Name)
                : files.OrderBy(f => f.Name)
        };

        return sorted;
    }

    private FileDto MapToDto(Files file)
    {
        return new FileDto
        {
            Id = file.Id,
            Name = file.Name,
            OriginalName = file.OriginalName,
            Extension = file.Extension,
            FileType = file.FileType,
            SizeBytes = file.SizeBytes,
            CreatedAt = file.CreatedAt,
            UpdatedAt = file.UpdatedAt,
            Tags = file.Tags,
            FolderId = file.FolderId,
            FolderName = file.Folder?.Name ?? "",
            UploadedById = file.UploadedById,
            UploadedByName = file.UploadedBy?.FullName ?? "",
            IsNetworkAvailable = true
        };
    }
}
