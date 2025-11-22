using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialNetworkApp.Data;
using SocialNetworkApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SocialNetworkApp.Services;

namespace SocialNetworkApp.Controllers
{
    public class PostLikesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserCountersService _countersService;

        public PostLikesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, UserCountersService countersService)
        {
            _context = context;
            _userManager = userManager;
            _countersService = countersService;
        }

        [HttpPost]
        [Authorize(Policy = "RequireUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int postId, bool isLike)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            if (currentUser.IsBanned)
            {
                TempData["Error"] = "Ваш аккаунт заблокирован";
                return RedirectToAction("Details", "Posts", new { id = postId });
            }

            var existingLike = await _context.PostLikes
                .FirstOrDefaultAsync(pl => pl.PostId == postId && pl.ApplicationUserId == currentUser.Id);

            if (existingLike != null)
            {
                if (existingLike.IsLike == isLike)
                {
                    _context.PostLikes.Remove(existingLike);
                    await _context.SaveChangesAsync();
                    await _countersService.UpdateLikeCount(currentUser.Id, -1);
                }
                else
                {
                    existingLike.IsLike = isLike;
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var postLike = new PostLike
                {
                    PostId = postId,
                    ApplicationUserId = currentUser.Id,
                    IsLike = isLike,
                    CreatedAt = DateTime.Now
                };
                _context.PostLikes.Add(postLike);
                await _context.SaveChangesAsync();
                await _countersService.UpdateLikeCount(currentUser.Id, 1);
            }

            return RedirectToAction("Details", "Posts", new { id = postId });
        }
    }
}
