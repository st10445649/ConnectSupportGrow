using System.ComponentModel.DataAnnotations;

namespace ConnectGrow.Models;
public class Webinars
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
 
    public bool IsFree => Price <= 0;

     public bool IsDraft => string.Equals(Status, "Draft", StringComparison.OrdinalIgnoreCase);
    public bool IsPublished => string.Equals(Status, "Published", StringComparison.OrdinalIgnoreCase);
    public bool IsCancelled => string.Equals(Status, "Cancelled", StringComparison.OrdinalIgnoreCase);
    public bool CanEdit => StartDateTime > DateTime.UtcNow && !IsCancelled;
 
    public string ScarcityLabel => IsSoldOut
        ? "Sold out"
        : AvailableSeats <= 5 ? $"{AvailableSeats} seats left" : string.Empty;
}
 
public class WebinarDetailModel : Webinars
{
    public string Description { get; set; } = string.Empty;
    public string? PresenterBio { get; set; }
    public List<string> LearningOutcomes { get; set; } = new();
    
}

public class AdminInputModel
{
    public int Id { get; set; }
 
    [Required(ErrorMessage = "Enter a title.")]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
 
    [Required(ErrorMessage = "Enter a description.")]
    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;
 
    [Required(ErrorMessage = "Choose a category.")]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;
 
    [Required(ErrorMessage = "Enter a start date and time.")]
    [Display(Name = "Start date & time")]
    [DataType(DataType.DateTime)]
    public DateTime StartDateTime { get; set; }
 
    [Required(ErrorMessage = "Enter an end date and time.")]
    [Display(Name = "End date & time")]
    [DataType(DataType.DateTime)]
    public DateTime EndDateTime { get; set; }
 
    [Range(0, 100000, ErrorMessage = "Price must be zero or more.")]
    public decimal Price { get; set; }
 
    [Range(1, 10000, ErrorMessage = "Capacity must be at least 1.")]
    public int Capacity { get; set; } = 30;
 
    [Range(0, 100, ErrorMessage = "CPD points must be between 0 and 100.")]
    [Display(Name = "CPD points")]
    public int CpdPoints { get; set; }
 
    [MaxLength(200)]
    [Display(Name = "Presenter name")]
    public string? PresenterName { get; set; }
 
    [MaxLength(4000)]
    [Display(Name = "Presenter biography")]
    public string? PresenterBio { get; set; }
 
    [Display(Name = "Learning outcomes")]
    public string? LearningOutcomesText { get; set; }
 
    [Url(ErrorMessage = "Enter a valid URL.")]
    [MaxLength(2000)]
    [Display(Name = "Teams join link")]
    public string? TeamsJoinUrl { get; set; }
 
    [MaxLength(2000)]
    [Display(Name = "Featured image URL")]
    public string? FeaturedImageUrl { get; set; }
 
    //"draft" or "published". The API always creates a Draft, so choosing 
    // published triggers a second publish call after a successful create.
    public string Status { get; set; } = "draft";
 
    public List<string> LearningOutcomes() =>
        (LearningOutcomesText ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
}
 
 