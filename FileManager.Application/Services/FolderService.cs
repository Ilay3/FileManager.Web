using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Entities;
using FileManager.Domain.Interfaces;
using FileManager.Domain.Enums;

namespace FileManager.Application.Services;

public class FolderService : IFolderService
{
    private readonly IFolderRepository _folderRepository;
    private readonly IFilesRepository _filesRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditService _auditService;
    private readonly IYandexDiskService _yandexDiskService;
    private readonly IAccessService _accessService;

    public FolderService(IFolderRepository folderRepository, IFilesRepository filesRepository, IUserRepository userRepository,
        IAuditService auditService, IYandexDiskService yandexDiskService, IAccessService accessService)
    {
        _folderRepository = folderRepository;
        _filesRepository = filesRepository;
        _userRepository = userRepository;
        _auditService = auditService;
        _yandexDiskService = yandexDiskService;
        _accessService = accessService;
    }

    public async Task<List<TreeNodeDto>> GetTreeStructureAsync(Guid userId, bool isAdmin = false)
    {
        var rootFolders = isAdmin
            ? await _folderRepository.GetRootFoldersAsync()
            : await _folderRepository.GetUserAccessibleFoldersAsync(userId);
        var treeNodes = new List<TreeNodeDto>();

        foreach (var folder in rootFolders)
        {
            var node = await BuildTreeNodeAsync(folder, userId, 0, isAdmin);
            treeNodes.Add(node);
        }

        return treeNodes;
    }

    public async Task<FolderDto?> GetFolderByIdAsync(Guid id, Guid userId)
    {
        var folder = await _folderRepository.GetByIdAsync(id);
        if (folder == null) return null;

        return await MapToFolderDtoAsync(folder);
    }

    public async Task<List<FolderDto>> GetRootFoldersAsync(Guid userId)
    {
        var folders = await _folderRepository.GetUserAccessibleFoldersAsync(userId);
        var folderDtos = new List<FolderDto>();

        foreach (var folder in folders)
        {
            var dto = await MapToFolderDtoAsync(folder);
            folderDtos.Add(dto);
        }

        return folderDtos;
    }

    public async Task<List<FolderDto>> GetSubFoldersAsync(Guid parentId, Guid userId)
    {
        var folders = await _folderRepository.GetSubFoldersAsync(parentId);
        var folderDtos = new List<FolderDto>();

        foreach (var folder in folders)
        {
            var dto = await MapToFolderDtoAsync(folder);
            folderDtos.Add(dto);
        }

        return folderDtos;
    }

    public async Task<List<BreadcrumbDto>> GetBreadcrumbsAsync(Guid folderId)
    {
        var breadcrumbs = new List<BreadcrumbDto>();
        var currentFolder = await _folderRepository.GetByIdAsync(folderId);

        while (currentFolder != null)
        {
            breadcrumbs.Insert(0, new BreadcrumbDto
            {
                Id = currentFolder.Id,
                Name = currentFolder.Name,
                Icon = "bi bi-folder"
            });

            if (currentFolder.ParentFolderId.HasValue)
                currentFolder = await _folderRepository.GetByIdAsync(currentFolder.ParentFolderId.Value);
            else
                break;
        }

        // Add root
        if (breadcrumbs.Count == 0 || breadcrumbs[0].Name != "Корневая папка")
        {
            breadcrumbs.Insert(0, new BreadcrumbDto
            {
                Id = Guid.Empty,
                Name = "Корневая папка",
                Icon = "bi bi-folder"
            });
        }

        return breadcrumbs;
    }

    public async Task<TreeNodeDto?> GetFolderContentsAsync(Guid folderId, Guid userId, SearchRequestDto? searchRequest = null, bool isAdmin = false)
    {
        Folder? folder = null;

        if (folderId != Guid.Empty)
        {
            folder = await _folderRepository.GetByIdAsync(folderId);
            if (folder == null) return null;
        }

        var node = new TreeNodeDto
        {
            Id = folderId,
            Name = folder?.Name ?? "Корневая папка",
            Type = "folder",
            IconClass = "bi bi-folder",
            IsNetworkAvailable = true,
            CreatedAt = folder?.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = folder?.UpdatedAt,
            Children = new List<TreeNodeDto>()
        };

        // Get subfolders
        var subFolders = folderId == Guid.Empty
            ? (isAdmin ? await _folderRepository.GetRootFoldersAsync() : await _folderRepository.GetUserAccessibleFoldersAsync(userId))
            : await _folderRepository.GetSubFoldersAsync(folderId);

        if (!isAdmin)
        {
            var accessibleSubFolders = new List<Folder>();
            foreach (var sf in subFolders)
            {
                var access = await _accessService.GetEffectiveFolderAccessAsync(userId, sf.Id);
                if ((access & AccessType.Read) == AccessType.Read)
                    accessibleSubFolders.Add(sf);
            }
            subFolders = accessibleSubFolders;
        }

        if (!string.IsNullOrEmpty(searchRequest?.SearchTerm))
        {
            subFolders = subFolders
                .Where(f => f.Name.Contains(searchRequest.SearchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        foreach (var subFolder in subFolders)
        {
            var filesInSub = await _filesRepository.GetByFolderIdAsync(subFolder.Id);
            if (!isAdmin)
            {
                var accessibleFiles = new List<Files>();
                foreach (var f in filesInSub)
                {
                    var access = await _accessService.GetEffectiveAccessAsync(userId, f.Id);
                    if ((access & AccessType.Read) == AccessType.Read)
                        accessibleFiles.Add(f);
                }
                filesInSub = accessibleFiles;
            }
            var filesCount = filesInSub.Count();

            var subSubFolders = await _folderRepository.GetSubFoldersAsync(subFolder.Id);
            if (!isAdmin)
            {
                var accessible = new List<Folder>();
                foreach (var ssf in subSubFolders)
                {
                    var access = await _accessService.GetEffectiveFolderAccessAsync(userId, ssf.Id);
                    if ((access & AccessType.Read) == AccessType.Read)
                        accessible.Add(ssf);
                }
                subSubFolders = accessible;
            }
            var subFoldersCount = subSubFolders.Count();

            node.Children.Add(new TreeNodeDto
            {
                Id = subFolder.Id,
                Name = subFolder.Name,
                Type = "folder",
                IconClass = "bi bi-folder",
                IsNetworkAvailable = true,
                CreatedAt = subFolder.CreatedAt,
                UpdatedAt = subFolder.UpdatedAt,
                ItemsCount = filesCount + subFoldersCount,
                ParentId = folderId == Guid.Empty ? null : folderId,
                HasChildren = subFoldersCount > 0
            });
        }

        // Get files
        var files = folderId == Guid.Empty ? new List<Files>() : await _filesRepository.GetByFolderIdAsync(folderId);

        if (!isAdmin)
        {
            var accessibleFiles = new List<Files>();
            foreach (var f in files)
            {
                var access = await _accessService.GetEffectiveAccessAsync(userId, f.Id);
                if ((access & AccessType.Read) == AccessType.Read)
                    accessibleFiles.Add(f);
            }
            files = accessibleFiles;
        }

        if (!string.IsNullOrEmpty(searchRequest?.SearchTerm))
        {
            files = files
                .Where(f => f.Name.Contains(searchRequest.SearchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        foreach (var file in files)
        {
            node.Children.Add(new TreeNodeDto
            {
                Id = file.Id,
                Name = file.Name,
                Type = "file",
                IconClass = GetFileIconClass(file.FileType),
                IsNetworkAvailable = true,
                CreatedAt = file.CreatedAt,
                UpdatedAt = file.UpdatedAt,
                SizeBytes = file.SizeBytes,
                Extension = file.Extension,
                UploadedByName = file.UploadedBy?.FullName,
                ParentId = folderId == Guid.Empty ? null : folderId,
                HasChildren = false
            });
        }

        return node;
    }

    public async Task<FolderDto> CreateFolderAsync(string name, Guid? parentId, Guid userId)
    {
        var siblings = parentId.HasValue
            ? await _folderRepository.GetSubFoldersAsync(parentId.Value)
            : await _folderRepository.GetRootFoldersAsync();

        if (siblings.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Имя папки должно быть уникальным");

        string yandexPath;
        if (parentId.HasValue)
        {
            var parent = await _folderRepository.GetByIdAsync(parentId.Value)
                ?? throw new InvalidOperationException("Родительская папка не найдена");
            yandexPath = $"{parent.YandexPath}/{name}";
        }
        else
        {
            yandexPath = $"/FileManager/{name}";
        }

        await _yandexDiskService.CreateFolderAsync(yandexPath);

        var folder = new Folder
        {
            Name = name,
            ParentFolderId = parentId,
            CreatedById = userId,
            YandexPath = yandexPath
        };

        var created = await _folderRepository.CreateAsync(folder);

        await _accessService.GrantAccessAsync(null, created.Id, userId, null,
            AccessType.FullAccess, userId);

        await _auditService.LogAsync(AuditAction.FolderCreate, userId, folderId: created.Id,
            description: $"Создал папку {name}");

        return await MapToFolderDtoAsync(created);
    }

    public async Task<FolderDto?> RenameFolderAsync(Guid id, string newName, Guid userId)
    {
        var folder = await _folderRepository.GetByIdAsync(id);
        if (folder == null) return null;

        var siblings = folder.ParentFolderId.HasValue
            ? await _folderRepository.GetSubFoldersAsync(folder.ParentFolderId.Value)
            : await _folderRepository.GetRootFoldersAsync();

        if (siblings.Any(f => f.Id != id && f.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Имя папки должно быть уникальным");

        folder.Name = newName;
        var parentPath = folder.ParentFolder?.YandexPath ?? "/FileManager";
        folder.YandexPath = $"{parentPath}/{newName}";

        await _folderRepository.UpdateAsync(folder);
        await _auditService.LogAsync(AuditAction.FolderRename, userId, folderId: folder.Id,
            description: $"Переименовал папку в {newName}");

        return await MapToFolderDtoAsync(folder);
    }

    public async Task<bool> DeleteFolderAsync(Guid id, Guid userId)
    {
        var filesCount = await _folderRepository.GetFilesCountInFolderAsync(id);
        var subFoldersCount = await _folderRepository.GetSubFoldersCountAsync(id);
        if (filesCount > 0 || subFoldersCount > 0)
            return false;

        await _folderRepository.DeleteAsync(id);
        await _auditService.LogAsync(AuditAction.FolderDelete, userId, folderId: id, description: "Удалил папку");
        return true;
    }

    public async Task<bool> MoveFolderAsync(Guid id, Guid? newParentId, Guid userId)
    {
        var folder = await _folderRepository.GetByIdAsync(id);
        if (folder == null) return false;

        if (newParentId == id) return false;

        IEnumerable<Folder> siblings;
        string newPath;
        if (newParentId.HasValue)
        {
            var parent = await _folderRepository.GetByIdAsync(newParentId.Value);
            if (parent == null) return false;
            siblings = await _folderRepository.GetSubFoldersAsync(newParentId.Value);
            newPath = $"{parent.YandexPath}/{folder.Name}";
            folder.ParentFolderId = newParentId.Value;
        }
        else
        {
            siblings = await _folderRepository.GetRootFoldersAsync();
            newPath = $"/FileManager/{folder.Name}";
            folder.ParentFolderId = null;
        }

        if (siblings.Any(f => f.Id != id && f.Name.Equals(folder.Name, StringComparison.OrdinalIgnoreCase)))
            return false;

        folder.YandexPath = newPath;
        await _folderRepository.UpdateAsync(folder);
        await _auditService.LogAsync(AuditAction.FolderMove, userId, folderId: folder.Id, description: "Переместил папку");
        return true;
    }

    private async Task<TreeNodeDto> BuildTreeNodeAsync(Folder folder, Guid userId, int level, bool isAdmin)
    {
        var files = await _filesRepository.GetByFolderIdAsync(folder.Id);
        if (!isAdmin)
        {
            var accessibleFiles = new List<Files>();
            foreach (var f in files)
            {
                var access = await _accessService.GetEffectiveAccessAsync(userId, f.Id);
                if ((access & AccessType.Read) == AccessType.Read)
                    accessibleFiles.Add(f);
            }
            files = accessibleFiles;
        }

        var subFolders = await _folderRepository.GetSubFoldersAsync(folder.Id);
        if (!isAdmin)
        {
            var accessibleFolders = new List<Folder>();
            foreach (var sf in subFolders)
            {
                var access = await _accessService.GetEffectiveFolderAccessAsync(userId, sf.Id);
                if ((access & AccessType.Read) == AccessType.Read)
                    accessibleFolders.Add(sf);
            }
            subFolders = accessibleFolders;
        }

        var node = new TreeNodeDto
        {
            Id = folder.Id,
            Name = folder.Name,
            Type = "folder",
            IconClass = "bi bi-folder",
            IsNetworkAvailable = true,
            CreatedAt = folder.CreatedAt,
            UpdatedAt = folder.UpdatedAt,
            ItemsCount = files.Count() + subFolders.Count(),
            Level = level,
            HasChildren = subFolders.Any(),
            Children = new List<TreeNodeDto>()
        };

        // Don't load children for performance reasons - load on demand
        return node;
    }

    private async Task<FolderDto> MapToFolderDtoAsync(Folder folder)
    {
        var filesCount = (await _filesRepository.GetByFolderIdAsync(folder.Id)).Count();
        var subFoldersCount = (await _folderRepository.GetSubFoldersAsync(folder.Id)).Count();

        return new FolderDto
        {
            Id = folder.Id,
            Name = folder.Name,
            CreatedAt = folder.CreatedAt,
            UpdatedAt = folder.UpdatedAt,
            ParentFolderId = folder.ParentFolderId,
            ParentFolderName = folder.ParentFolder?.Name,
            CreatedById = folder.CreatedById,
            CreatedByName = folder.CreatedBy?.FullName ?? "",
            FilesCount = filesCount,
            SubFoldersCount = subFoldersCount
        };
    }

    private string GetFileIconClass(Domain.Enums.FileType fileType)
    {
        return fileType switch
        {
            Domain.Enums.FileType.Document => "bi bi-file-earmark-text",
            Domain.Enums.FileType.Spreadsheet => "bi bi-file-earmark-spreadsheet",
            Domain.Enums.FileType.Presentation => "bi bi-file-earmark-slides",
            Domain.Enums.FileType.Pdf => "bi bi-file-earmark-pdf",
            Domain.Enums.FileType.Image => "bi bi-file-earmark-image",
            Domain.Enums.FileType.Text => "bi bi-file-earmark-richtext",
            Domain.Enums.FileType.Archive => "bi bi-file-earmark-zip",
            _ => "bi bi-file-earmark"
        };
    }
}
