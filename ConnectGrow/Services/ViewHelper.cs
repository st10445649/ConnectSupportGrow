namespace ConnectGrow.Services;
public static class ViewHelpers
{
    public static string DurationLabel(DateTime startUtc, DateTime endUtc)
    {
        var span = endUtc - startUtc;
        if (span <= TimeSpan.Zero) return string.Empty;

        var hours = (int)span.TotalHours;
        var minutes = span.Minutes;

        if (hours == 0) return $"{minutes} minutes";
        if (minutes == 0) return hours == 1 ? "1 hour" : $"{hours} hours";

        return $"{hours} hour{(hours == 1 ? "" : "s")} {minutes} minutes";
    }

    //Tailwind badge classes per category, so cards stay visually varied
    // without hard-coding a colour per card the way the static mock-up did.
    public static string CategoryBadgeClasses(string? category) =>
        (category ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "educators" or "for educators" => "bg-primary-100 text-primary-700",
            "parents" or "for parents" or "parent / guardian" => "bg-secondary-100 text-secondary-700",
            "professionals" => "bg-accent-100 text-accent-700",
            "children" or "for children" => "bg-secondary-100 text-secondary-700",
            _ => "bg-primary-100 text-primary-700"
        };

    //Falls back to a bundled image when the webinar has none, so a card never
    //renders with a broken image box.
    public static string WebinarImage(string? featuredImageUrl) =>
        string.IsNullOrWhiteSpace(featuredImageUrl)
            ? "/images/webinars/webinarautism.jpg"
            : featuredImageUrl;

    //Percentage of seats taken, for the availability progress bar
    public static int FillPercentage(int capacity, int availableSeats)
    {
        if (capacity <= 0) return 0;

        var taken = Math.Max(0, capacity - availableSeats);
        return Math.Clamp((int)Math.Round(taken * 100.0 / capacity), 0, 100);
    }
}