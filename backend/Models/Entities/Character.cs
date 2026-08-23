using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookIllustration_Backend.Models.Entities;

public class Character
{
    [Key]
    public int CharacterId { get; set; }

    [MaxLength(255)]
    public required string CharacterName { get; set; }

    public required string CharacterDescription { get; set; }

    public string? CharacterIllustrationPath { get; set; }

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public required Project Project { get; set; }

    public ICollection<Chapter> Chapters { get; } = [];
}
