using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Domain.Enums;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Services;

namespace RestaurantManagement.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebConfigCacheService _configCache;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAuthService authService,
        IUnitOfWork unitOfWork,
        IWebConfigCacheService configCache,
        ILogger<AccountController> logger)
    {
        _authService = authService;
        _unitOfWork = unitOfWork;
        _configCache = configCache;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var model = await BuildLoginViewModelAsync(returnUrl);
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
        {
            var repopulated = await BuildLoginViewModelAsync(model.ReturnUrl);
            repopulated.Username = model.Username ?? string.Empty;
            repopulated.ErrorMessage = "Please enter both User ID and password.";
            repopulated.ErrorKey = "auth.missingCredentials";
            return View(repopulated);
        }

        var loginResult = await _authService.LoginAsync(new LoginRequestDto
        {
            Username = model.Username.Trim(),
            Password = model.Password
        });

        if (!loginResult.Success || loginResult.User == null)
        {
            var repopulated = await BuildLoginViewModelAsync(model.ReturnUrl);
            repopulated.Username = model.Username;
            repopulated.ErrorMessage = "Invalid User ID or password.";
            repopulated.ErrorKey = "auth.invalidCredentials";
            return View(repopulated);
        }

        var user = loginResult.User;
        var setting = await _configCache.GetSettingsAsync();

        var cookieLang = Request.Cookies["rms_lang"];
        var activeLang = (cookieLang == "mr" || cookieLang == "en") ? cookieLang : user.PreferredLanguage;

        // Build security claims with exact role for authorization
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.GivenName, string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName),
            new Claim("FullNameMr", string.IsNullOrWhiteSpace(user.FullNameMr) ? (string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName) : user.FullNameMr),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim("RoleName", user.RoleName),
            new Claim("RestaurantName", setting.RestaurantName),
            new Claim("PreferredLanguage", activeLang)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        _logger.LogInformation("User {Username} ({Role}) logged in successfully", user.Username, user.Role);

        // After successful login, always redirect directly to Dashboard page
        return Redirect("/Dashboard");
    }

    [HttpGet, HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("GiranaRMS.Auth");
        Response.Cookies.Delete(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "You have been signed out successfully.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Username))
        {
            return Json(new { success = false, message = "Please enter your username." });
        }

        if (string.IsNullOrWhiteSpace(dto.ManagerPin))
        {
            return Json(new { success = false, message = "Manager PIN is required." });
        }

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 4)
        {
            return Json(new { success = false, message = "New password must be at least 4 characters long." });
        }

        if (dto.NewPassword != dto.ConfirmPassword)
        {
            return Json(new { success = false, message = "Passwords do not match." });
        }

        var settings = await _unitOfWork.RestaurantSettings.GetAllAsync();
        var setting = settings.FirstOrDefault() ?? new RestaurantSetting();

        string expectedPin = string.IsNullOrWhiteSpace(setting.ManagerPIN) ? "1234" : setting.ManagerPIN.Trim();
        if (dto.ManagerPin.Trim() != expectedPin)
        {
            return Json(new { success = false, message = "Invalid Manager Security PIN." });
        }

        var existingUser = await _authService.GetUserByUsernameAsync(dto.Username.Trim());
        if (existingUser == null)
        {
            return Json(new { success = false, message = $"User '{dto.Username}' was not found." });
        }

        var resetResult = await _authService.ResetPasswordByUsernameAsync(dto.Username.Trim(), dto.NewPassword);
        if (!resetResult)
        {
            return Json(new { success = false, message = "Failed to update password." });
        }

        _logger.LogInformation("Password reset for user {Username}", dto.Username);
        return Json(new { 
            success = true, 
            message = $"Password for '{dto.Username}' reset successfully! You can now log in.",
            username = dto.Username
        });
    }

    [HttpGet("/api/restaurant-settings/public")]
    [AllowAnonymous]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetPublicRestaurantSettings()
    {
        var dto = await _configCache.GetPublicSettingsAsync();
        return Json(dto);
    }

    [HttpPost("/Account/SetLanguage")]
    [AllowAnonymous]
    public async Task<IActionResult> SetLanguage([FromBody] LanguagePreferenceDto dto)
    {
        var lang = dto?.Language == "mr" ? "mr" : "en";
        Response.Cookies.Append("rms_lang", lang, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = false,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });

        if (User.Identity?.IsAuthenticated == true &&
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            await _authService.UpdatePreferredLanguageAsync(userId, lang);
        }

        return Json(new { success = true, language = lang });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task<LoginViewModel> BuildLoginViewModelAsync(string? returnUrl)
    {
        var setting = await _configCache.GetSettingsAsync();
        var cookieLang = Request.Cookies["rms_lang"];

        return new LoginViewModel
        {
            ReturnUrl = returnUrl,
            RestaurantName = setting.RestaurantName,
            RestaurantNameMr = setting.RestaurantNameMr,
            Tagline = setting.Tagline,
            LogoUrl = setting.LogoUrl ?? string.Empty,
            PreferredLanguage = (cookieLang == "mr" || cookieLang == "en") ? cookieLang : "en",
            Username = string.Empty,
            Password = string.Empty
        };
    }

    public class LanguagePreferenceDto
    {
        public string Language { get; set; } = "en";
    }
}
