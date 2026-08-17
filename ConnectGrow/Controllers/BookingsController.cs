using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ConnectGrow.Models;

namespace ConnectGrow.Controllers;

public class BookingsController : Controller
{
    public IActionResult Confirmation()
    {
        return View();
    }

}
