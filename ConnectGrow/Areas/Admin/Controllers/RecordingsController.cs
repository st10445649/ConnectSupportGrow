using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ConnectGrow.Models;

namespace ConnectGrow.Areas.Admin.Controllers;
[Area("Admin")]
public class RecordingsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    

}
