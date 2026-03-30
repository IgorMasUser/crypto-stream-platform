using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TradingApp.Dashboard.Web.Identity;

namespace TradingApp.Dashboard.Web.Pages;

public sealed class LogoutModel(SignInManager<ApplicationUser> signInManager) : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        await signInManager.SignOutAsync();
        return Redirect("/login");
    }
}
