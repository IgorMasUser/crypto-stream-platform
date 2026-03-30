using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TradingApp.Dashboard.Web.Identity;

namespace TradingApp.Dashboard.Web.Pages;

public sealed class LoginModel(
    SignInManager<ApplicationUser> signInManager,
    ILogger<LoginModel> logger) : PageModel
{
    [BindProperty] public string Email    { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }
    public string? ReturnUrl    { get; private set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        var result = await signInManager.PasswordSignInAsync(
            Email, Password, isPersistent: true, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            logger.LogInformation("User {Email} logged in", Email);
            return Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : Redirect("/");
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("User {Email} account locked out", Email);
            ErrorMessage = "Account locked. Try again later.";
        }
        else
        {
            ErrorMessage = "Invalid email or password.";
        }

        ReturnUrl = returnUrl;
        return Page();
    }
}
