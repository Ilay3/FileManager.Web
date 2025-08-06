namespace FileManager.Application.DTOs;

public class TrashItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // file or folder
    public DateTime? DeletedAt { get; set; }
}
