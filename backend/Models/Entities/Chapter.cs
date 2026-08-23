using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookIllustration_Backend.Models.Entities;

public class Chapter
{
    [Key]
    public int ChapterId { get; set; }

    public required string ChapterTitle { get; set; }

    public required string ChapterDescription { get; set; }

    public string? ChapterIllustrationPath { get; set; }

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public required Project Project { get; set; }

    public ICollection<Character> Characters { get; } = [];
}
