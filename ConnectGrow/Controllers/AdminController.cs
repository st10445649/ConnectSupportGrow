using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ConnectGrow.Models;

namespace ConnectGrow.Controllers;

public class AdminController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    

}
