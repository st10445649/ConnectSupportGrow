using Microsoft.AspNetCore.Identity;

namespace ConnectGrowAPI.Models;


public class ApplicationUser : IdentityUser<Guid>
{
    
    public ApplicationUser() => Id = Guid.NewGuid();

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    //optional employer or company for professionals
    public string? Organisation { get; set; }

    //soft-delete option
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
    public ICollection<RecordingAccess> RecordingAccesses { get; set; } = new List<RecordingAccess>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}

public class ApplicationRole : IdentityRole<Guid>
{
    
    public ApplicationRole() => Id = Guid.NewGuid();

    public ApplicationRole(string roleName) : base(roleName) => Id = Guid.NewGuid();
}


public static class RoleNames
{
    public const string Admin = "Admin";
    public const string User = "User";
}