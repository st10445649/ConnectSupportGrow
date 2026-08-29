
using ConnectGrowAPI.Models;

namespace ConnectGrowAPI.Models;

public class Recording
{
    public int Id { get; set; }

    public int WebinarId { get; set; }
    public Webinar Webinar { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    /// api.video video id //doesnt get sent to client
    public string ApiVideoId { get; set; } = string.Empty;

    public long? FileSize { get; set; }
    public int? DurationSeconds { get; set; }

    // true while api.video transcodes. access cannot be granted until false
    public bool IsProcessing { get; set; } = true;


    public bool AllowOfflineDownload { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    public ICollection<RecordingAccess> AccessGrants { get; set; } = new List<RecordingAccess>();

    public bool IsReady => !IsProcessing && !string.IsNullOrWhiteSpace(ApiVideoId);
}


public class RecordingAccess
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int RecordingId { get; set; }
    public Recording Recording { get; set; } = null!;

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? RevokedAt { get; set; }

    public int AccessCount { get; set; }
    public DateTime? LastAccessedAt { get; set; }

    public bool IsValid(DateTime utcNow) => IsActive && ExpiresAt > utcNow;

    public int RemainingDays(DateTime utcNow) =>
        Math.Max(0, (int)Math.Ceiling((ExpiresAt - utcNow).TotalDays));
}


