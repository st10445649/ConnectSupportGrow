
using Microsoft.AspNetCore.Mvc;
using ConnectGrow.Models;
using ConnectGrow.Services;
using Microsoft.AspNetCore.Authorization;

namespace ConnectGrow.Areas.Admin.Controllers;

public class AdminDashboardViewModel
{
    public int TotalWebinars { get; set; }
    public int PublishedCount { get; set; }
    public int DraftCount { get; set; }
    public int UpcomingCount { get; set; }

    //Seats sold across upcoming published webinars
    public int SeatsBooked { get; set; }

    public int SeatsAvailable { get; set; }

    public List<Webinars> NextUp { get; set; } = new();

    //Published webinars at or near capacity — the ones worth scheduling a repeat of.
    public List<Webinars> NearlyFull { get; set; } = new();

    public int FillPercentage => SeatsBooked + SeatsAvailable == 0
        ? 0
        : (int)Math.Round(SeatsBooked * 100.0 / (SeatsBooked + SeatsAvailable));
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IAdminClient _admin;
    private readonly ISignInService _signIn;

    public DashboardController(IAdminClient admin, ISignInService signIn)
    {
        _admin = admin;
        _signIn = signIn;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _admin.GetAllAsync(ct);

        if (result.IsUnauthorised)
        {
            await _signIn.SignOutAsync(HttpContext);
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        if (result.IsFailure)
        {
            ViewBag.Error = result.Error;
            return View(new AdminDashboardViewModel());
        }

        var all = result.Value ?? new List<Webinars>();
        var now = DateTime.UtcNow;

        var upcomingPublished = all
            .Where(w => w.IsPublished && w.StartDateTime > now)
            .ToList();

        var model = new AdminDashboardViewModel
        {
            TotalWebinars = all.Count,
            PublishedCount = all.Count(w => w.IsPublished),
            DraftCount = all.Count(w => w.IsDraft),
            UpcomingCount = upcomingPublished.Count,

            SeatsBooked = upcomingPublished.Sum(w => w.Capacity - w.AvailableSeats),
            SeatsAvailable = upcomingPublished.Sum(w => w.AvailableSeats),

            NextUp = upcomingPublished
                .OrderBy(w => w.StartDateTime)
                .Take(5)
                .ToList(),

            NearlyFull = upcomingPublished
                .Where(w => w.Capacity > 0 && w.AvailableSeats <= Math.Max(1, w.Capacity / 10))
                .OrderBy(w => w.AvailableSeats)
                .Take(5)
                .ToList()
        };

        return View(model);
    }
}