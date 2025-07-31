using FileManager.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace FileManager.Domain.Entities;

public class Group : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();
}
