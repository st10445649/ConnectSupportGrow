
using ConnectGrowAPI.Dtos;
using ConnectGrowAPI.Models;
using ConnectGrowAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectGrowAPI.Controllers;

// Webinar catalogue (public) and management (admin). The read endpoints are
// deliberately anonymous so the catalogue works for guests and can be cached by
// the service worker for offline browsing. 

[Route("api/webinars")]
public class WebinarsController : ApiControllerBase 
{
    private readonly IWebinarService _webinars;

    public WebinarsController(IWebinarService webinars) => _webinars = webinars;

    //Published, upcoming webinars, optionally filtered by category
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<WebinarListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WebinarListDto>>> GetCatalogue(
        [FromQuery] string? category, CancellationToken ct)
    {
        var result = await _webinars.GetCatalogueAsync(category, ct);
        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    //Full detail for one webinar. Drafts are visible to admins only
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WebinarDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WebinarDetailDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _webinars.GetDetailAsync(id, IsAdmin, ct);
        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }


    // Feed for the integrated calendar view sp the ca]lient can add stright to calendar
    [HttpGet("calendar")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<object>>> GetCalendarFeed(CancellationToken ct)
    {
        var result = await _webinars.GetCatalogueAsync(null, ct);
        if (result.IsFailure) return Problem(result);

        var events = result.Value!.Select(w => new
        {
            id = w.Id,
            title = w.Title,
            start = w.StartDateTime,
            end = w.EndDateTime,
            url = $"/webinars/{w.Id}",
            extendedProps = new { w.Category, w.Price, w.AvailableSeats, w.IsSoldOut }
        });

        return Ok(events);
    }



    [HttpGet("admin/all")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<ActionResult<IReadOnlyList<WebinarListDto>>> GetAllForAdmin(CancellationToken ct)
    {
        var result = await _webinars.GetAllForAdminAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(WebinarDetailDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WebinarDetailDto>> Create(
        [FromBody] CreateWebinarRequest request, CancellationToken ct)
    {
        var result = await _webinars.CreateAsync(request, ct);
        if (result.IsFailure) return Problem(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<ActionResult<WebinarDetailDto>> Update(
        int id, [FromBody] UpdateWebinarRequest request, CancellationToken ct)
    {
        if (id != request.Id)
            return BadRequest(new ApiError("The id in the URL does not match the request body."));

        var result = await _webinars.UpdateAsync(request, ct);
        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    [HttpPost("{id:int}/publish")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Publish(int id, CancellationToken ct)
    {
        var result = await _webinars.PublishAsync(id, ct);
        return result.IsSuccess ? NoContent() : Problem(result);
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var result = await _webinars.CancelAsync(id, ct);
        return result.IsSuccess ? NoContent() : Problem(result);
    }
}