using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ConnectGrow.Models;

namespace ConnectGrow.Controllers;

public class WebinarsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

     public IActionResult Detail()
    {
        return View();
    }

}