using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace frontend.Services
{
    public class RoleService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;

        public RoleService(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task InitializeRolesAsync()
        {
            var roles = new[] { "Admin", "User" };

            foreach(var role in roles)
            {
                if(!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Optionally, create a default admin user
            var adminUser = new IdentityUser { UserName = "admin@example.com", Email = "admin@example.com" };
            var result = await _userManager.CreateAsync(adminUser, "AdminPassword123!");

            if(result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}
