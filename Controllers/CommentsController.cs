using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialNetworkApp.Data;
using SocialNetworkApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SocialNetworkApp.Services;

namespace SocialNetworkApp.Controllers
{
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserCountersService _countersService;

        public CommentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, UserCountersService countersService)
        {
            _context = context;
            _userManager = userManager;
            _countersService = countersService;
        }

        [HttpPost]
        [Authorize(Policy = "RequireUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int PostId, string Text)
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                TempData["Error"] = "Комментарий не может быть пустым";
                return RedirectToAction("Details", "Posts", new { id = PostId });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            if (currentUser.IsBanned)
            {
                TempData["Error"] = "Ваш аккаунт заблокирован";
                return RedirectToAction("Details", "Posts", new { id = PostId });
            }
            var comment = new Comment
            {
                PostId = PostId,
                Text = Text,
                CreatedAt = DateTime.Now,
                ApplicationUserId = currentUser.Id
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            await _countersService.UpdateCommentCount(currentUser.Id, 1);

            return RedirectToAction("Details", "Posts", new { id = PostId });
        }

        [HttpPost]
        [Authorize(Policy = "RequireUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var comment = await _context.Comments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            if (comment.ApplicationUserId != currentUser.Id && !User.IsInRole("Moderator") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var postId = comment.PostId;
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            await _countersService.UpdateCommentCount(currentUser.Id, -1);

            return RedirectToAction("Details", "Posts", new { id = postId });
        }
    }
}