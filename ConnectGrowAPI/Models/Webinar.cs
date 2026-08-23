

namespace ConnectGrowAPI.Models;


//Training webinar event entity. Connected to bookings, recordings and CPD points for users
public class Webinar
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    //Educators / Parents / Professionals / General.
    public string Category { get; set; } = string.Empty;

    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }

    public decimal Price { get; set; }

    public int Capacity { get; set; }


    //count of confirmed(paid) bookings. increments when payments are confirmed
    //just used for diisplay because a pending booking also keeps a place for the user
    public int CurrentBookings { get; set; }

    public int CpdPoints { get; set; }

    public WebinarStatus Status { get; set; } = WebinarStatus.Draft;

    public string? TeamsJoinUrl { get; set; }

    // Micorosft Graph API meeting id, retained so the meeting can be updated or deleted later
    public string? TeamsMeetingId { get; set; }

    public string? FeaturedImageUrl { get; set; }
    public string? PresenterName { get; set; }
    public string? PresenterBio { get; set; }
    public string? LearningOutcomesJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public uint Version { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Recording> Recordings { get; set; } = new List<Recording>();

    public int AvailableSeats => Math.Max(0, Capacity - CurrentBookings);

    public bool IsSoldOut => CurrentBookings >= Capacity;

    public bool IsPublished => Status == WebinarStatus.Published;


    //a  webinar is bookable only while published and before it starts
    //webinar capacity is checked separately against live booking counts
    public bool IsBookable(DateTime utcNow) =>
        Status == WebinarStatus.Published && StartDateTime > utcNow;
}