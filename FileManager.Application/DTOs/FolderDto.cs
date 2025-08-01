namespace FileManager.Application.DTOs;

public class FolderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Parent folder info
    public Guid? ParentFolderId { get; set; }
    public string? ParentFolderName { get; set; }

    // Creator info
    public Guid CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;

    // Counts
    public int FilesCount { get; set; }
    public int SubFoldersCount { get; set; }

    // For tree view
    public List<FolderDto> SubFolders { get; set; } = new();
    public List<FileDto> Files { get; set; } = new();
    public bool IsExpanded { get; set; } = false;
    public int Level { get; set; } = 0;

    public string FolderIcon => "📁";
}

public class TreeNodeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "folder" or "file"
    public string Icon { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // For files
    public long? SizeBytes { get; set; }
    public string? Extension { get; set; }
    public string? UploadedByName { get; set; }

    // For folders
    public int? ItemsCount { get; set; }

    // Tree structure
    public Guid? ParentId { get; set; }
    public List<TreeNodeDto> Children { get; set; } = new();
    public bool HasChildren { get; set; }
    public bool IsExpanded { get; set; } = false;
    public int Level { get; set; } = 0;
}