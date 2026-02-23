using System.ComponentModel.DataAnnotations;

namespace StreamAxis.Api.Entities;

public class Episode
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ContentId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [MaxLength(500)]
    public string? PosterUrl { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string StreamUrl { get; set; } = string.Empty;
    
    [Required]
    public int SeasonNumber { get; set; }
    
    [Required]
    public int EpisodeNumber { get; set; }
    
    [Required]
    public bool IsActive { get; set; } = true;
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
    [Required]
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property
    public virtual Content Content { get; set; } = null!;
}