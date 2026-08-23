using System.ComponentModel.DataAnnotations;

namespace BookIllustration_Backend.Models.Entities;

public class User
{
    [Key]
    [MaxLength(255)]
    public required string Email { get; set; }

    [MaxLength(255)]
    public required string FullName { get; set; }

    public ICollection<Project> Projects { get; } = [];
}
