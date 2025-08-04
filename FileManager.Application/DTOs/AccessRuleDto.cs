using FileManager.Domain.Enums;

namespace FileManager.Application.DTOs;

public class AccessRuleDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public AccessType AccessType { get; set; }
    public bool InheritFromParent { get; set; }
}
