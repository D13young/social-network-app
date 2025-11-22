using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SocialNetworkApp.Data;
using SocialNetworkApp.Models;
using Microsoft.EntityFrameworkCore;

namespace SocialNetworkApp.Controllers
{
    public class UserManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserManagementController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Policy = "RequireModerator")]
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.Posts)
                .Include(u => u.Comments)
                .Include(u => u.PostLikes)
                .ToListAsync();

            return View(users);
        }

        [HttpPost]
        [Authorize(Policy = "RequireModerator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBan(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            user.IsBanned = !user.IsBanned;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
