using System.ComponentModel.DataAnnotations;
using ConnectGrowAPI.Models;

namespace ConnectGrowAPI.Dtos;
   
  public class WebinarListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public decimal Price { get; set; }
    public int CpdPoints { get; set; }
    public int Capacity { get; set; }
    public int AvailableSeats { get; set; }
    public bool IsSoldOut { get; set; }
    public string? FeaturedImageUrl { get; set; }
    public string? PresenterName { get; set; }

    public string Status { get; set; } = string.Empty;
}
 
public class WebinarDetailDto : WebinarListDto
{
    public string Description { get; set; } = string.Empty;
    public string? PresenterBio { get; set; }
    public List<string> LearningOutcomes { get; set; } = new();
}
 
public class CreateWebinarRequest
{
    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;
 
    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;
 
    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;
 
    [Required]
    public DateTime StartDateTime { get; set; }
 
    [Required]
    public DateTime EndDateTime { get; set; }
 
    [Range(0, 100000, ErrorMessage = "Price must be zero or greater.")]
    public decimal Price { get; set; }
 
    [Range(1, 10000, ErrorMessage = "Capacity must be at least 1.")]
    public int Capacity { get; set; }
 
    [Range(0, 100)]
    public int CpdPoints { get; set; }
 
    [MaxLength(200)]
    public string? PresenterName { get; set; }
 
    [MaxLength(4000)]
    public string? PresenterBio { get; set; }
 
    public List<string> LearningOutcomes { get; set; } = new();
 

    [MaxLength(2000)]
    public string? TeamsJoinUrl { get; set; }
 
    [MaxLength(2000)]
    public string? FeaturedImageUrl { get; set; }
}
 
public class UpdateWebinarRequest : CreateWebinarRequest
{
    public int Id { get; set; }
}