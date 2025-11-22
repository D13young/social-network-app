using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SocialNetworkApp.Models;
using Microsoft.EntityFrameworkCore;
using SocialNetworkApp.Data;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace SocialNetworkApp.Controllers
{
    [Authorize]
    public class UserProfilesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public UserProfilesController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _userManager = userManager;
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound();
            }

            var userPosts = await _context.Posts
                .Where(p => p.ApplicationUserId == currentUser.Id)
                .Include(p => p.Community)
                .Include(p => p.Comments)
                .Include(p => p.PostLikes)
                .ToListAsync();

            var userComments = await _context.Comments
                .Where(c => c.ApplicationUserId == currentUser.Id)
                .Include(c => c.Post)
                .ToListAsync();

            ViewBag.UserPosts = userPosts;
            ViewBag.UserComments = userComments;

            return View(currentUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string phoneNumber, IFormFile AvatarFile)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound();
            }

            currentUser.FirstName = firstName;
            currentUser.LastName = lastName;
            currentUser.PhoneNumber = phoneNumber;

            if (AvatarFile != null && AvatarFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(currentUser.AvatarPath))
                {
                    var oldFilePath = Path.Combine(_hostingEnvironment.WebRootPath, currentUser.AvatarPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                var avatarsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(avatarsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(AvatarFile.FileName);
                var filePath = Path.Combine(avatarsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await AvatarFile.CopyToAsync(fileStream);
                }

                currentUser.AvatarPath = "/uploads/avatars/" + uniqueFileName;
            }

            var result = await _userManager.UpdateAsync(currentUser);
            if (result.Succeeded)
            {
                TempData["Success"] = "Профиль обновлен успешно";
            }
            else
            {
                TempData["Error"] = "Ошибка при обновлении профиля";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
