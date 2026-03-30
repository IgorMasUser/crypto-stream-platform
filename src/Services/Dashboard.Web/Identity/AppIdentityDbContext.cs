using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TradingApp.Dashboard.Web.Identity;

public sealed class AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Override default AspNet* table names to snake_case
        builder.Entity<ApplicationUser>().ToTable("identity_users");
        builder.Entity<IdentityRole>().ToTable("identity_roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("identity_user_roles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("identity_user_claims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("identity_user_logins");
        builder.Entity<IdentityUserToken<string>>().ToTable("identity_user_tokens");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("identity_role_claims");
    }
}
