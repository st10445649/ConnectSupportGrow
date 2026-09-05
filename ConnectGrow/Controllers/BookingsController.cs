using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ConnectGrow.Models;
using ConnectGrow.Services;
using Microsoft.AspNetCore.Authorization;

namespace ConnectGrow.Controllers;


[Authorize]
public class BookingsController : Controller
{
    private readonly IBookingApiClient _bookings;
    private readonly ISignInService _signIn;
 
    public BookingsController(IBookingApiClient bookings, ISignInService signIn)
    {
        _bookings = bookings;
        _signIn = signIn;
    }

    /* Starts the checkout process */
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int webinarId, CancellationToken ct)
    {
        var result = await _bookings.CreateAsync(webinarId, ct);
 
        if (result.IsUnauthorised) return await SignOutAndRedirect();
 
        if (result.IsFailure || result.Value is null)
        {
            // Covers "already booked" (409) and "fully booked" (409) as well as
            // transport failures. Shown on the detail page the user came from.
            TempData["Error"] = result.Error;
            return RedirectToAction("Detail", "Webinars", new { id = webinarId });
        }
 
        // Paid webinars leave for Payfast's hosted page. Free ones are already
        // confirmed by the API and sends straight to the confirmation.
        if (result.Value.RequiresPayment &&
            !string.IsNullOrWhiteSpace(result.Value.PaymentRedirectUrl))
        {
            return Redirect(result.Value.PaymentRedirectUrl);
        }
 
        return RedirectToAction(nameof(Confirmation), new { id = result.Value.Booking.Id });
    }
 
    /* Where Payfast returns the buyer.
    This deliberately does not confirm payment. Confirmation happens only on
    the verified ITN callback to the API, so a booking can legitimately still
    read Pending here if the notification has not sent yet — which is why
    the view phrases it as confirming isntead of paid. */
   
    [HttpGet]
    public async Task<IActionResult> Confirmation(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
 
        var result = await _bookings.GetByIdAsync(id, ct);
 
        if (result.IsUnauthorised) return await SignOutAndRedirect();
 
        // The API returns 404 rather than 403 for someone else's booking, so
        // this covers both does not exist and not your booking.
        if (result.IsNotFound) return View("Error404");
 
        if (result.IsFailure || result.Value is null)
        {
            ViewBag.Error = result.Error;
            return View("Error500");
        }
 
        return View(result.Value);
    }
 
    /* Payfast's cancel_url. No booking is destroyed here , the Pending record
     releases its seat on its own once the hold window passes. */

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Cancelled(int? webinarId)
    {
        TempData["Error"] = "Your payment was cancelled, so the booking was not completed.";
 
        return webinarId.HasValue
            ? RedirectToAction("Detail", "Webinars", new { id = webinarId.Value })
            : RedirectToAction("Index", "Webinars");
    }
 
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var result = await _bookings.CancelAsync(id, ct);
 
        if (result.IsUnauthorised) return await SignOutAndRedirect();
 
        TempData[result.IsSuccess ? "Message" : "Error"] = result.IsSuccess
            ? "Your booking has been cancelled."
            : result.Error;
 
        return RedirectToAction("Index", "Dashboard");
    }
    private async Task<IActionResult> SignOutAndRedirect()
    {
        await _signIn.SignOutAsync(HttpContext);
 
        return RedirectToAction("Login", "Account",
            new { returnUrl = Request.Path + Request.QueryString });
    }
}
 