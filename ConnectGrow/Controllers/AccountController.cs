using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ConnectGrow.Models;
using ConnectGrow.Services;
using Microsoft.AspNetCore.Authorization;

namespace ConnectGrow.Controllers;

public class AccountController : Controller
{

    private readonly IAuthApiClient _authApi;
    private readonly ISignInService _signIn;
 
    public AccountController(IAuthApiClient authApi, ISignInService signIn)
    {
        _authApi = authApi;
        _signIn = signIn;
    }
    
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocalOr(returnUrl, nameof(Dashboard));
 
        return View(new LoginInputModel { ReturnUrl = returnUrl });
    }
 
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(input);
 
        var result = await _authApi.LoginAsync(input.Email, input.Password, ct);
 
        if (result.IsFailure || result.Value is null)
        {
            // Surfaced verbatim: the API deliberately returns one message for
            // wrong password and unknown email, so passing it through does not
            // leak whether the account exists.
            ModelState.AddModelError(string.Empty, result.Error ?? "Sign in failed.");
            return View(input);
        }
 
        await _signIn.SignInAsync(HttpContext, result.Value);
 
        return RedirectToLocalOr(input.ReturnUrl, nameof(Dashboard));
    }
 
    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocalOr(returnUrl, nameof(Dashboard));
 
        return View(new RegisterInputModel { ReturnUrl = returnUrl });
    }
 
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(input);
 
        var result = await _authApi.RegisterAsync(input, ct);
 
        if (result.IsFailure || result.Value is null)
        {
            // A 409 means the email is taken — attach it to the email field so
            // it renders next to the input rather than in a summary at the top.
            if (result.StatusCode == 409)
                ModelState.AddModelError(nameof(input.Email), result.Error ?? "That email is already registered.");
            else
                ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
 
            return View(input);
        }
 
        // Registration signs the user straight in — asking someone to log in
        // with credentials they just typed is friction for no benefit.
        await _signIn.SignInAsync(HttpContext, result.Value);
 
        return RedirectToLocalOr(input.ReturnUrl, nameof(Dashboard));
    }
 
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        // Revoke the refresh token server-side first, while the bearer token is
        // still attached. Clearing the cookie first would leave the refresh
        // token live in the API's database.
        await _authApi.LogoutAsync(ct);
        await _signIn.SignOutAsync(HttpContext);
 
        return RedirectToAction("Index", "Home");
    }
 
    [HttpGet]
    [Authorize]
    public IActionResult Dashboard() => RedirectToAction("Index", "Bookings");
 
    [HttpGet]
    public IActionResult AccessDenied() => View();
 
    /// <summary>
    /// Only redirects to URLs within this application. Without the
    /// <see cref="Url.IsLocalUrl"/> check, a crafted returnUrl would turn the
    /// sign-in page into an open redirect.
    /// </summary>
    private IActionResult RedirectToLocalOr(string? returnUrl, string fallbackAction)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
 
        return RedirectToAction(fallbackAction);
    }
}