using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ConnectGrow.Models;

namespace ConnectGrow.Controllers;

public class AccountController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

     public IActionResult Login()
    {
        return View();
    }

     public IActionResult Register()
    {
        return View();
    }

     public IActionResult ResetPassword()
    {
        return View();
    }

     public IActionResult Settings()
    {
        return View();
    }

     public IActionResult ForgotPassword()
    {
        return View();
    }

}
