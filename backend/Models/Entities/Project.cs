using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookIllustration_Backend.Models.Entities;

public class Project
{
    [Key]
    public int ProjectId { get; set; }

    [MaxLength(255)]
    public required string ProjectTitle { get; set; }

    [MaxLength(500)]
    public required string BookTextPath { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Style { get; set; }

    [MaxLength(255)]
    public required string UserEmail { get; set; }

    [ForeignKey(nameof(UserEmail))]
    public required User User { get; set; }

    public ICollection<PipelineStep> PipelineSteps { get; } = [];

    public ICollection<Character> Characters { get; } = [];

    public ICollection<Chapter> Chapters { get; } = [];
}
