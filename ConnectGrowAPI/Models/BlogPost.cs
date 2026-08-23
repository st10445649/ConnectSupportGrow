namespace ConnectGrowAPI.Models;

// blog entity for posts 
public class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? FeaturedImageUrl { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }

    public Guid? AuthorId { get; set; }

    public BlogPostStatus Status { get; set; } = BlogPostStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public int ViewsCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}