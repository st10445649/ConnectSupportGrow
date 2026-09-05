using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ConnectGrow.Models;
using ConnectGrow.Services;
using Microsoft.AspNetCore.Authorization;

namespace ConnectGrow.Controllers;

[AllowAnonymous]
public class WebinarsController : Controller
{
    private readonly IWebinarApiClient _webinars;
 
    public WebinarsController(IWebinarApiClient webinars) => _webinars = webinars;
 
    /* Public catalogue. Gguests  are able to browse before registering,
    and the booking button prompts sign-in only after wanting to book. */

    [HttpGet]
    public async Task<IActionResult> Index(string? category, string? sort, CancellationToken ct)
    {
        var result = await _webinars.GetCatalogueAsync(category, ct);
 
        if (result.IsFailure)
        {
            ViewBag.Error = result.Error;
            return View(new List<Webinars>());
        }
 
        var webinars = result.Value ?? new List<Webinars>();
 
        webinars = sort switch
        {
            "price-low" => webinars.OrderBy(w => w.Price).ToList(),
            "price-high" => webinars.OrderByDescending(w => w.Price).ToList(),
            "cpd" => webinars.OrderByDescending(w => w.CpdPoints).ToList(),
            _ => webinars.OrderBy(w => w.StartDateTime).ToList()
        };
 
        ViewBag.SelectedCategory = category;
        ViewBag.SelectedSort = sort;
 
        return View(webinars);
    }
 
    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
 
        var result = await _webinars.GetByIdAsync(id, ct);
 
        if (result.IsNotFound) return View("Error404");
 
        if (result.IsFailure || result.Value is null)
        {
            ViewBag.Error = result.Error;
            return View("Error500");
        }
 
        return View(result.Value);
    }
}
 