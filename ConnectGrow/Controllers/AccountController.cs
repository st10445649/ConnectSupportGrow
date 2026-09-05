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
            if (result.StatusCode == 409)
                ModelState.AddModelError(nameof(input.Email), result.Error ?? "That email is already registered.");
            else
                ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
 
            return View(input);
        }
 
        await _signIn.SignInAsync(HttpContext, result.Value);
 
        return RedirectToLocalOr(input.ReturnUrl, nameof(Dashboard));
    }
 
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _authApi.LogoutAsync(ct);
        await _signIn.SignOutAsync(HttpContext);
 
        return RedirectToAction("Index", "Home");
    }
 
    [HttpGet]
    [Authorize]

    //sends user to dashboard
    public IActionResult Dashboard() => RedirectToAction("Index", "Dashboard");
 
    [HttpGet]
    public IActionResult AccessDenied() => View();


     [HttpGet]
    [Authorize]
    public async Task<IActionResult> Settings(CancellationToken ct)
    {
        var result = await _authApi.GetProfileAsync(ct);
 
        if (result.IsUnauthorised) return await SignOutAndRedirect();
 
        if (result.IsFailure || result.Value is null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction("Index", "Dashboard");
        }
 
        return View(BuildSettingsModel(result.Value));
    }
 
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(UpdateProfileInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(nameof(Settings), await RebuildSettingsAsync(input, null, "profile", ct));
 
        var result = await _authApi.UpdateProfileAsync(input, ct);
 
        if (result.IsUnauthorised) return await SignOutAndRedirect();
 
        if (result.IsFailure || result.Value is null)
        {
            if (result.StatusCode == 409)
                ModelState.AddModelError("Profile.Email", result.Error ?? "That email is already in use.");
            else
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not save your profile.");
 
            return View(nameof(Settings), await RebuildSettingsAsync(input, null, "profile", ct));
        }
 
        await RefreshClaimsAsync(result.Value, ct);
 
        TempData["Message"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Settings));
    }
 
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(nameof(Settings), await RebuildSettingsAsync(null, input, "password", ct));
 
        var result = await _authApi.ChangePasswordAsync(input, ct);
 
        if (result.IsUnauthorised) return await SignOutAndRedirect();
 
        if (result.IsFailure)
        {
            ModelState.AddModelError("Password.CurrentPassword",
                result.Error ?? "Could not change your password.");
 
            return View(nameof(Settings), await RebuildSettingsAsync(null, input, "password", ct));
        }
 
        await _signIn.SignOutAsync(HttpContext);
 
        TempData["Message"] = "Your password has been changed. Please sign in again.";
        return RedirectToAction(nameof(Login));
    }
 
 
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordInputModel());
 
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(input);
 
        var result = await _authApi.ForgotPasswordAsync(input.Email, ct);
 
        // The same confirmation is shown whatever happened, including a
        // transport failure. Varying the message would turn this page into a
        // way of testing which email addresses have accounts.
        ViewBag.SuccessMessage =
            "If an account exists for that address, a reset link is on its way.";
 

        /* if (_env.IsDevelopment() && !string.IsNullOrWhiteSpace(result.Value?.DevResetToken))
        {
            ViewBag.DevResetLink = Url.Action(nameof(ResetPassword), "Account",
                new { email = input.Email, token = result.Value.DevResetToken },
                Request.Scheme);
        } */
 
        return View(new ForgotPasswordInputModel());
    }
 
    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            TempData["Error"] = "That reset link is incomplete. Please request a new one.";
            return RedirectToAction(nameof(ForgotPassword));
        }
 
        return View(new ResetPasswordInputModel { Email = email, Token = token });
    }
 
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(input);
 
        var result = await _authApi.ResetPasswordAsync(input, ct);
 
        if (result.IsFailure)
        {
            // The API returns one generic message for an expired token
            ModelState.AddModelError(string.Empty,
                result.Error ?? "This reset link is invalid or has expired.");
            return View(input);
        }
 
        TempData["Message"] = "Your password has been reset. You can now sign in.";
        return RedirectToAction(nameof(Login));
    }
 
 
    private static SettingsViewModel BuildSettingsModel(Users user, string activeTab = "profile") => new()
    {
        Profile = new UpdateProfileInputModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Organisation = user.Organisation
        },
        Roles = user.Roles,
        MemberSince = user.CreatedAt,
        ActiveTab = activeTab
    };
 
 
    private async Task<SettingsViewModel> RebuildSettingsAsync(
        UpdateProfileInputModel? profile,
        ChangePasswordInputModel? password,
        string activeTab,
        CancellationToken ct)
    {
        var current = await _authApi.GetProfileAsync(ct);
 
        var model = current.IsSuccess && current.Value is not null
            ? BuildSettingsModel(current.Value, activeTab)
            : new SettingsViewModel { ActiveTab = activeTab };
 
        if (profile is not null) model.Profile = profile;
        if (password is not null) model.Password = password;
 
        return model;
    }
 

    private async Task RefreshClaimsAsync(Users user, CancellationToken ct)
    {
        var accessToken = User.FindFirst(TokenClaims.AccessToken)?.Value;
        var refreshToken = User.FindFirst(TokenClaims.RefreshToken)?.Value;
        var expiresRaw = User.FindFirst(TokenClaims.AccessTokenExpiresAt)?.Value;
 
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
            return;
 
        var expiresAt = long.TryParse(expiresRaw, out var unix)
            ? DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime
            : DateTime.UtcNow.AddMinutes(5);
 
        await _signIn.SignInAsync(HttpContext, new AuthResponseModel
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = expiresAt,
            User = user
        });
    }
 
    private async Task<IActionResult> SignOutAndRedirect()
    {
        await _signIn.SignOutAsync(HttpContext);
 
        return RedirectToAction(nameof(Login),
            new { returnUrl = Request.Path + Request.QueryString });
    }

    private IActionResult RedirectToLocalOr(string? returnUrl, string fallbackAction)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
 
        return RedirectToAction(fallbackAction);
    }
}