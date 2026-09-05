using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ConnectGrow.Models;
using ConnectGrow.Services;
using Microsoft.AspNetCore.Authorization;

namespace ConnectGrow.Controllers;
[Authorize]
public class DashboardController : Controller
{
    private readonly IBookingApiClient _bookings;
    private readonly ISignInService _signIn;
 
    public DashboardController(IBookingApiClient bookings, ISignInService signIn)
    {
        _bookings = bookings;
        _signIn = signIn;
    }
 
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _bookings.GetMineAsync(ct);
 
        if (result.IsUnauthorised) return await SignOutAndRedirect();
 
        if (result.IsFailure)
        {
            ViewBag.Error = result.Error;
            return View(new DashboardViewModel());
        }
 
        var bookings = result.Value ?? new List<Bookings>();
        var now = DateTime.UtcNow;
 
        var active = bookings
            .Where(b => !string.Equals(b.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            .ToList();
 
        var model = new DashboardViewModel
        {
            Upcoming = active
                .Where(b => b.StartDateTime > now)
                .OrderBy(b => b.StartDateTime)
                .ToList(),
 
            Past = active
                .Where(b => b.StartDateTime <= now)
                .OrderByDescending(b => b.StartDateTime)
                .ToList(),
 
            TotalCpdPoints = active
                .Where(b => b.AttendanceConfirmed)
                .Sum(b => b.CpdPoints)
        };
 
        return View(model);
    }
 
    [HttpGet]
    public async Task<IActionResult> WatchRecording(int id, CancellationToken ct)
    {
        await Task.CompletedTask;
 
        ViewBag.BookingId = id;
        return View();
    }
 
    [HttpGet("dashboard/createevaluations")]
    public IActionResult CreateEvaluations(int? bookingId)
    {
        ViewBag.BookingId = bookingId;
        return View("~/Views/Dashboard/Evaluations/Create.cshtml");
    }
 
    private async Task<IActionResult> SignOutAndRedirect()
    {
        await _signIn.SignOutAsync(HttpContext);
 
        return RedirectToAction("Login", "Account",
            new { returnUrl = Request.Path + Request.QueryString });
    }

}

public class DashboardViewModel
{
    public List<Bookings> Upcoming { get; set; } = new();
    public List<Bookings> Past { get; set; } = new();
 
    public int TotalCpdPoints { get; set; }
 
    public int UpcomingCount => Upcoming.Count;
    public int PastCount => Past.Count;
 
    public List<Bookings> AwaitingEvaluation =>
        Past.Where(b => b.CanSubmitEvaluation).ToList();
}
 