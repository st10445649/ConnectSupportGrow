using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ConnectGrow.Models;
using Microsoft.AspNetCore.Authorization;
using ConnectGrow.Services;

namespace ConnectGrow.Areas.Admin.Controllers;
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class WebinarsController : Controller
{
    private readonly IAdminClient _admin;
    private readonly ISignInService _signIn;

    public WebinarsController(IAdminClient admin, ISignInService signIn)
    {
        _admin = admin;
        _signIn = signIn;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status, CancellationToken ct)
    {
        var result = await _admin.GetAllAsync(ct);

        if (result.IsUnauthorised) return await SignOutAndRedirect();

        if (result.IsFailure)
        {
            ViewBag.Error = result.Error;
            return View(new List<Webinars>());
        }

        var webinars = result.Value ?? new List<Webinars>();

        if (!string.IsNullOrWhiteSpace(status))
        {
            webinars = webinars
                .Where(w => string.Equals(w.Status, status, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        ViewBag.SelectedStatus = status;

        return View(webinars);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var startSast = DateTime.UtcNow.AddDays(14).ToSast();

        return View(new AdminInputModel
        {
            StartDateTime = new DateTime(startSast.Year, startSast.Month, startSast.Day, 10, 0, 0),
            EndDateTime = new DateTime(startSast.Year, startSast.Month, startSast.Day, 11, 30, 0),
            Capacity = 30,
            Price = 150,
            CpdPoints = 3
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminInputModel input, CancellationToken ct)
    {
        var wantsPublish = string.Equals(input.Status, "published", StringComparison.OrdinalIgnoreCase);

        // Caught here rather than after creating a draft that then fails to
        // publish, which would leave a half-finished webinar behind.
        if (wantsPublish && string.IsNullOrWhiteSpace(input.TeamsJoinUrl))
        {
            ModelState.AddModelError(nameof(input.TeamsJoinUrl),
                "A Teams join link is required to publish. Save as a draft to add it later.");
        }

        if (input.EndDateTime <= input.StartDateTime)
        {
            ModelState.AddModelError(nameof(input.EndDateTime),
                "The end time must be after the start time.");
        }

        if (!ModelState.IsValid) return View(input);

        var created = await _admin.CreateAsync(input, ct);

        if (created.IsUnauthorised) return await SignOutAndRedirect();

        if (created.IsFailure || created.Value is null)
        {
            ModelState.AddModelError(string.Empty, created.Error ?? "Could not create the webinar.");
            return View(input);
        }

        if (wantsPublish)
        {
            var published = await _admin.PublishAsync(created.Value.Id, ct);

            TempData[published.IsSuccess ? "Message" : "Error"] = published.IsSuccess
                ? $"\"{created.Value.Title}\" was created and published."
                : $"The webinar was saved as a draft, but publishing failed: {published.Error}";
        }
        else
        {
            TempData["Message"] = $"\"{created.Value.Title}\" was saved as a draft.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var result = await _admin.GetByIdAsync(id, ct);

        if (result.IsUnauthorised) return await SignOutAndRedirect();
        if (result.IsNotFound) return View("Error404");

        if (result.IsFailure || result.Value is null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        var webinar = result.Value;

        if (!webinar.CanEdit)
        {
            // The API rejects edits after the start time, so say so here rather
            // than letting the admin fill in a form that cannot be saved.
            TempData["Error"] = "This webinar has already started and can no longer be edited.";
            return RedirectToAction(nameof(Index));
        }

        return View(new AdminInputModel
        {
            Id = webinar.Id,
            Title = webinar.Title,
            Description = webinar.Description,
            Category = webinar.Category,

            StartDateTime = webinar.StartDateTime.ToSast(),
            EndDateTime = webinar.EndDateTime.ToSast(),

            Price = webinar.Price,
            Capacity = webinar.Capacity,
            CpdPoints = webinar.CpdPoints,
            PresenterName = webinar.PresenterName,
            PresenterBio = webinar.PresenterBio,
            LearningOutcomesText = string.Join('\n', webinar.LearningOutcomes),
            FeaturedImageUrl = webinar.FeaturedImageUrl,
            Status = webinar.Status
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminInputModel input, CancellationToken ct)
    {
        if (input.EndDateTime <= input.StartDateTime)
        {
            ModelState.AddModelError(nameof(input.EndDateTime),
                "The end time must be after the start time.");
        }

        if (!ModelState.IsValid) return View(input);

        var result = await _admin.UpdateAsync(input, ct);

        if (result.IsUnauthorised) return await SignOutAndRedirect();

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not save the webinar.");
            return View(input);
        }

        TempData["Message"] = "Your changes have been saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, CancellationToken ct)
    {
        var result = await _admin.PublishAsync(id, ct);

        if (result.IsUnauthorised) return await SignOutAndRedirect();

        TempData[result.IsSuccess ? "Message" : "Error"] = result.IsSuccess
            ? "The webinar is now live in the public catalogue."
            : result.Error;

        return RedirectToAction(nameof(Index));
    }

    //Cancel, not delete. The API has no delete endpoint by design: a webinar
    // with paid bookings against it cannot be removed without destroying
    // financial history. Cancelling hides it and triggers attendee notification.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var result = await _admin.CancelAsync(id, ct);

        if (result.IsUnauthorised) return await SignOutAndRedirect();

        TempData[result.IsSuccess ? "Message" : "Error"] = result.IsSuccess
            ? "The webinar has been cancelled. Registered attendees should be notified."
            : result.Error;

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SignOutAndRedirect()
    {
        await _signIn.SignOutAsync(HttpContext);

        return RedirectToAction("Login", "Account",
            new { area = "", returnUrl = Request.Path + Request.QueryString });
    }
}