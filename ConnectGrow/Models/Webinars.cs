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
 
    public bool IsFree => Price <= 0;
 
    public string ScarcityLabel => IsSoldOut
        ? "Sold out"
        : AvailableSeats <= 5 ? $"{AvailableSeats} seats left" : string.Empty;
}
 
public class WebinarDetailModel : Webinars
{
    public string Description { get; set; } = string.Empty;
    public string? PresenterBio { get; set; }
    public List<string> LearningOutcomes { get; set; } = new();
    public string Status { get; set; } = string.Empty;
}
 