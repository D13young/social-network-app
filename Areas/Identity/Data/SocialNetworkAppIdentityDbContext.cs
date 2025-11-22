using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SocialNetworkApp.Areas.Identity.Data;

public class SocialNetworkAppIdentityDbContext : IdentityDbContext<IdentityUser>
{
    public SocialNetworkAppIdentityDbContext(DbContextOptions<SocialNetworkAppIdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}
