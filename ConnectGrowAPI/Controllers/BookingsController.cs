
using ConnectGrowAPI.Controllers;
using ConnectGrowAPI.Dtos;
using ConnectGrowAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSG.Api.Controllers;

// Booking endpoints. Every action requires authentication; ownership is checked inside the service rather than trusted from the request.

[Route("api/bookings")]
[Authorize]
public class BookingsController : ApiControllerBase
{
    private readonly IBookingService _bookings;

    public BookingsController(IBookingService bookings) => _bookings = bookings;

    //Bookings belonging to the signed-in user, newest first
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BookingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookingDto>>> GetMine(CancellationToken ct)
    {
        var result = await _bookings.GetForUserAsync(CurrentUserId, ct);
        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    //A single booking. Returns 404 for someone else's booking
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _bookings.GetByIdAsync(id, CurrentUserId, IsAdmin, ct);
        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    //Reserves a seat and starts checkout. Returns the gateway redirect URL for
    // paid webinars, or a confirmed booking for free ones.
    [HttpPost]
    [ProducesResponseType(typeof(BookingCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingCreatedDto>> Create(
        [FromBody] CreateBookingRequest request, CancellationToken ct)
    {
        var result = await _bookings.CreateAsync(CurrentUserId, request.WebinarId, ct);

        if (result.IsFailure) return Problem(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Booking.Id },
            result.Value);
    }

    //Cancels a Pending or Paid booking. Paid cancellations are logged for refund review.
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var result = await _bookings.CancelAsync(id, CurrentUserId, IsAdmin, ct);
        return result.IsSuccess ? NoContent() : Problem(result);
    }
}