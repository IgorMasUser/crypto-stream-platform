using Microsoft.AspNetCore.Identity;

namespace TradingApp.Dashboard.Web.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
