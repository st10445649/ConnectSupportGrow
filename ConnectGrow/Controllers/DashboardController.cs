using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ConnectGrow.Models;

namespace ConnectGrow.Controllers;

public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

     public IActionResult Details()
    {
        return View();
    }

     public IActionResult WatchRecording()
    {
        return View();
    }

[HttpGet("dashboard/createevaluations")]
    public IActionResult CreateEvaluations()
    {
        return View("~/Views/Dashboard/Evaluations/Create.cshtml");
    }



}