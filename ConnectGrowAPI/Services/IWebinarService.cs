using System.Text.Json;
using ConnectGrowAPI.Dtos;
using ConnectGrowAPI.Interfaces;
using ConnectGrowAPI.Models;

namespace ConnectGrowAPI.Services;

public interface IWebinarService
{
    Task<Result<IReadOnlyList<WebinarListDto>>> GetCatalogueAsync(string? category, CancellationToken ct = default);
    Task<Result<WebinarDetailDto>> GetDetailAsync(int id, bool includeUnpublished, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WebinarListDto>>> GetAllForAdminAsync(CancellationToken ct = default);

    Task<Result<WebinarDetailDto>> CreateAsync(CreateWebinarRequest request, CancellationToken ct = default);
    Task<Result<WebinarDetailDto>> UpdateAsync(UpdateWebinarRequest request, CancellationToken ct = default);
    Task<Result> PublishAsync(int id, CancellationToken ct = default);
    Task<Result> CancelAsync(int id, CancellationToken ct = default);
}

public class WebinarService : IWebinarService
{
    private readonly IWebinarRepository _webinars;
    private readonly IBookingRepository _bookings;
    private readonly ILogger<WebinarService> _logger;

    public WebinarService(
        IWebinarRepository webinars,
        IBookingRepository bookings,
        ILogger<WebinarService> logger)
    {
        _webinars = webinars;
        _bookings = bookings;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<WebinarListDto>>> GetCatalogueAsync(
        string? category, CancellationToken ct = default)
    {
        var webinars = await _webinars.GetPublishedUpcomingAsync(category, ct);
        IReadOnlyList<WebinarListDto> dtos = webinars.Select(MapToList).ToList();
        return Result<IReadOnlyList<WebinarListDto>>.Success(dtos);
    }

    public async Task<Result<WebinarDetailDto>> GetDetailAsync(
        int id, bool includeUnpublished, CancellationToken ct = default)
    {
        var webinar = await _webinars.GetDetailAsync(id, ct);
        if (webinar is null)
            return Result<WebinarDetailDto>.NotFound("Webinar not found.");

        // Drafts are admin-only. Return 404 rather than 403 so an anonymous
        // visitor cannot enumerate which draft ids exist.
        if (!includeUnpublished && webinar.Status == WebinarStatus.Draft)
            return Result<WebinarDetailDto>.NotFound("Webinar not found.");

        return Result<WebinarDetailDto>.Success(MapToDetail(webinar));
    }

    public async Task<Result<IReadOnlyList<WebinarListDto>>> GetAllForAdminAsync(
        CancellationToken ct = default)
    {
        var all = await _webinars.GetAllAsync(ct);
        IReadOnlyList<WebinarListDto> dtos = all
            .OrderByDescending(w => w.StartDateTime)
            .Select(MapToList)
            .ToList();

        return Result<IReadOnlyList<WebinarListDto>>.Success(dtos);
    }

    public async Task<Result<WebinarDetailDto>> CreateAsync(
        CreateWebinarRequest request, CancellationToken ct = default)
    {
        var validation = Validate(request);
        if (validation.IsFailure)
            return Result<WebinarDetailDto>.Failure(validation.ErrorType, validation.Error!);

        var webinar = new Webinar
        {
            Title = request.Title.Trim(),
            Description = request.Description,
            Category = request.Category.Trim(),
            StartDateTime = request.StartDateTime.ToUniversalTime(),
            EndDateTime = request.EndDateTime.ToUniversalTime(),
            Price = request.Price,
            Capacity = request.Capacity,
            CpdPoints = request.CpdPoints,
            PresenterName = request.PresenterName,
            PresenterBio = request.PresenterBio,
            TeamsJoinUrl = request.TeamsJoinUrl,
            FeaturedImageUrl = request.FeaturedImageUrl,
            LearningOutcomesJson = JsonSerializer.Serialize(request.LearningOutcomes),
            Status = WebinarStatus.Draft,   // always starts hidden
            CurrentBookings = 0
        };

        await _webinars.AddAsync(webinar, ct);
        await _webinars.SaveChangesAsync(ct);

        _logger.LogInformation("Webinar {Id} created as draft: {Title}", webinar.Id, webinar.Title);
        return Result<WebinarDetailDto>.Success(MapToDetail(webinar));
    }

    public async Task<Result<WebinarDetailDto>> UpdateAsync(
        UpdateWebinarRequest request, CancellationToken ct = default)
    {
        var webinar = await _webinars.GetByIdAsync(request.Id, ct);
        if (webinar is null)
            return Result<WebinarDetailDto>.NotFound("Webinar not found.");

        if (webinar.StartDateTime <= DateTime.UtcNow)
            return Result<WebinarDetailDto>.Invalid(
                "A webinar cannot be edited once it has started.");

        var validation = Validate(request);
        if (validation.IsFailure)
            return Result<WebinarDetailDto>.Failure(validation.ErrorType, validation.Error!);

        // Capacity must not be cut below seats already sold.
        if (request.Capacity < webinar.CurrentBookings)
            return Result<WebinarDetailDto>.Invalid(
                $"Capacity cannot be reduced below the {webinar.CurrentBookings} seats already booked.");

        webinar.Title = request.Title.Trim();
        webinar.Description = request.Description;
        webinar.Category = request.Category.Trim();
        webinar.StartDateTime = request.StartDateTime.ToUniversalTime();
        webinar.EndDateTime = request.EndDateTime.ToUniversalTime();
        webinar.Price = request.Price;
        webinar.Capacity = request.Capacity;
        webinar.CpdPoints = request.CpdPoints;
        webinar.PresenterName = request.PresenterName;
        webinar.PresenterBio = request.PresenterBio;
        webinar.TeamsJoinUrl = request.TeamsJoinUrl;
        webinar.FeaturedImageUrl = request.FeaturedImageUrl;
        webinar.LearningOutcomesJson = JsonSerializer.Serialize(request.LearningOutcomes);

        _webinars.Update(webinar);
        await _webinars.SaveChangesAsync(ct);

        return Result<WebinarDetailDto>.Success(MapToDetail(webinar));
    }

    public async Task<Result> PublishAsync(int id, CancellationToken ct = default)
    {
        var webinar = await _webinars.GetByIdAsync(id, ct);
        if (webinar is null) return Result.NotFound("Webinar not found.");

        if (webinar.Status != WebinarStatus.Draft)
            return Result.Invalid($"Only draft webinars can be published (current status: {webinar.Status}).");

        // A published webinar must have somewhere for attendees to go. The Graph
        // integration will populate this automatically; until then the admin
        // supplies it manually.
        if (string.IsNullOrWhiteSpace(webinar.TeamsJoinUrl))
            return Result.Invalid("A Teams join URL is required before publishing.");

        webinar.Status = WebinarStatus.Published;
        _webinars.Update(webinar);
        await _webinars.SaveChangesAsync(ct);

        _logger.LogInformation("Webinar {Id} published.", id);
        return Result.Success();
    }

    public async Task<Result> CancelAsync(int id, CancellationToken ct = default)
    {
        var webinar = await _webinars.GetByIdAsync(id, ct);
        if (webinar is null) return Result.NotFound("Webinar not found.");

        if (webinar.Status == WebinarStatus.Completed)
            return Result.Invalid("A completed webinar cannot be cancelled.");

        webinar.Status = WebinarStatus.Cancelled;
        _webinars.Update(webinar);
        await _webinars.SaveChangesAsync(ct);

        // Attendee cancellation emails and refunds are handled by the event
        // handlers once the dispatcher is wired up.
        _logger.LogWarning(
            "Webinar {Id} cancelled. Attendees must be notified and refunded.", id);

        return Result.Success();
    }

    // -----------------------------------------------------------------------

    private static Result Validate(CreateWebinarRequest r)
    {
        if (r.EndDateTime <= r.StartDateTime)
            return Result.Invalid("The end time must be after the start time.");

        if (r.StartDateTime <= DateTime.UtcNow)
            return Result.Invalid("The start time must be in the future.");

        if (r.Capacity < 1)
            return Result.Invalid("Capacity must be at least 1.");

        if (r.Price < 0)
            return Result.Invalid("Price cannot be negative.");

        return Result.Success();
    }

     private static WebinarListDto MapToList(Webinar w) => new()
    {
        Id = w.Id,
        Title = w.Title,
        Category = w.Category,
        StartDateTime = w.StartDateTime,
        EndDateTime = w.EndDateTime,
        Price = w.Price,
        CpdPoints = w.CpdPoints,
        Capacity = w.Capacity,
        AvailableSeats = w.AvailableSeats,
        IsSoldOut = w.IsSoldOut,
        FeaturedImageUrl = w.FeaturedImageUrl,
        PresenterName = w.PresenterName
    };

    private static WebinarDetailDto MapToDetail(Webinar w) => new()
    {
        Id = w.Id,
        Title = w.Title,
        Category = w.Category,
        StartDateTime = w.StartDateTime,
        EndDateTime = w.EndDateTime,
        Price = w.Price,
        CpdPoints = w.CpdPoints,
        Capacity = w.Capacity,
        AvailableSeats = w.AvailableSeats,
        IsSoldOut = w.IsSoldOut,
        FeaturedImageUrl = w.FeaturedImageUrl,
        PresenterName = w.PresenterName,
        Description = w.Description,
        PresenterBio = w.PresenterBio,
        Status = w.Status.ToString(),
        LearningOutcomes = DeserialiseOutcomes(w.LearningOutcomesJson)
    };

    private static List<string> DeserialiseOutcomes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}