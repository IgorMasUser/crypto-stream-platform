namespace TradingApp.Dashboard.Web.Identity;

public static class AppRoles
{
    public const string Admin  = "Admin";
    public const string Viewer = "Viewer";

    public static readonly string[] All = [Admin, Viewer];
}
