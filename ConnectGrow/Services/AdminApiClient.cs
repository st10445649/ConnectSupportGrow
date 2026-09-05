using ConnectGrow.Models;

namespace ConnectGrow.Services;

/* Admin-only webinar management. Kept separate from IWebinarApiClient because these endpoints all 
require the Admin role. Splitting them means an ordinary page cannot accidentally call a
privileged endpoint.
 */
public interface IAdminClient
{
    //Every webinar including drafts and cancelled ones.
    Task<ApiResult<List<Webinars>>> GetAllAsync(CancellationToken ct = default);

    Task<ApiResult<WebinarDetailModel>> GetByIdAsync(int id, CancellationToken ct = default);

    //Always creates a Draft. Publish is a separate call.
    Task<ApiResult<WebinarDetailModel>> CreateAsync(AdminInputModel input, CancellationToken ct = default);

    Task<ApiResult<WebinarDetailModel>> UpdateAsync(AdminInputModel input, CancellationToken ct = default);

    //ails unless the webinar is a Draft and has a Teams join URL.
    Task<ApiResult> PublishAsync(int id, CancellationToken ct = default);

    Task<ApiResult> CancelAsync(int id, CancellationToken ct = default);
}

public class AdminWebinarApiClient : ApiClientBase, IAdminClient
{
    public AdminWebinarApiClient(HttpClient http, ILogger<AdminWebinarApiClient> logger)
        : base(http, logger) { }

    public Task<ApiResult<List<Webinars>>> GetAllAsync(CancellationToken ct = default) =>
        GetAsync<List<Webinars>>("/api/webinars/admin/all", ct);

    public Task<ApiResult<WebinarDetailModel>> GetByIdAsync(int id, CancellationToken ct = default) =>
        GetAsync<WebinarDetailModel>($"/api/webinars/{id}", ct);

    public Task<ApiResult<WebinarDetailModel>> CreateAsync(
        AdminInputModel input, CancellationToken ct = default) =>
        PostAsync<WebinarDetailModel>("/api/webinars", BuildPayload(input), ct);

    public Task<ApiResult<WebinarDetailModel>> UpdateAsync(
        AdminInputModel input, CancellationToken ct = default) =>
        PutAsync<WebinarDetailModel>($"/api/webinars/{input.Id}", BuildPayload(input, includeId: true), ct);

    public Task<ApiResult> PublishAsync(int id, CancellationToken ct = default) =>
        PostAsync($"/api/webinars/{id}/publish", null, ct);

    public Task<ApiResult> CancelAsync(int id, CancellationToken ct = default) =>
        PostAsync($"/api/webinars/{id}/cancel", null, ct);


    private static object BuildPayload(AdminInputModel input, bool includeId = false)
    {
        var startUtc = SouthAfricanTime.FromSastInput(input.StartDateTime);
        var endUtc = SouthAfricanTime.FromSastInput(input.EndDateTime);

        if (includeId)
        {
            return new
            {
                id = input.Id,
                title = input.Title,
                description = input.Description,
                category = input.Category,
                startDateTime = startUtc,
                endDateTime = endUtc,
                price = input.Price,
                capacity = input.Capacity,
                cpdPoints = input.CpdPoints,
                presenterName = input.PresenterName,
                presenterBio = input.PresenterBio,
                learningOutcomes = input.LearningOutcomes(),
                teamsJoinUrl = input.TeamsJoinUrl,
                featuredImageUrl = input.FeaturedImageUrl
            };
        }

        return new
        {
            title = input.Title,
            description = input.Description,
            category = input.Category,
            startDateTime = startUtc,
            endDateTime = endUtc,
            price = input.Price,
            capacity = input.Capacity,
            cpdPoints = input.CpdPoints,
            presenterName = input.PresenterName,
            presenterBio = input.PresenterBio,
            learningOutcomes = input.LearningOutcomes(),
            teamsJoinUrl = input.TeamsJoinUrl,
            featuredImageUrl = input.FeaturedImageUrl
        };
    }
}