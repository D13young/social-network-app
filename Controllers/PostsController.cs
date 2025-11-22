using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialNetworkApp.Data;
using SocialNetworkApp.Models;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SocialNetworkApp.Services;

namespace SocialNetworkApp.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserCountersService _countersService;

        public PostsController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment, UserManager<ApplicationUser> userManager, UserCountersService countersService)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _userManager = userManager;
            _countersService = countersService;
        }

        public async Task<IActionResult> Index(string sortBy = "newest", int page = 1)
        {
            int pageSize = 10;

            IQueryable<Post> query = _context.Posts
                .Include(p => p.Community)
                .Include(p => p.Comments)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostLikes)
                .Include(p => p.ApplicationUser)
                .Where(p => p.ApplicationUser == null || !p.ApplicationUser.IsBanned);

            switch (sortBy.ToLower())
            {
                case "popular":
                    query = query.OrderByDescending(p => p.PostLikes.Count(pl => pl.IsLike));
                    break;
                case "discussed":
                    query = query.OrderByDescending(p => p.Comments.Count);
                    break;
                default: // newest
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            var totalPosts = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);

            var posts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SortBy = sortBy;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View(posts);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var post = await _context.Posts
                .Include(p => p.Community)
                .Include(p => p.Comments).ThenInclude(c => c.ApplicationUser)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostLikes)
                .Include(p => p.ApplicationUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (post == null) return NotFound();
            return View(post);
        }

        [Authorize(Policy = "RequireUser")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Communities = await _context.Communities.ToListAsync();
            ViewBag.Tags = await _context.Tags.ToListAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "RequireUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post post, IFormFile ImageFile, List<int> SelectedTagIds, string NewTagName)
        {
            ModelState.Remove("ImageFile");
            ModelState.Remove("NewTagName");
            ModelState.Remove("SelectedTagIds");
            ModelState.Remove("ApplicationUserId");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("\n=== ОШИБКИ МОДЕЛИ ===");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state?.Errors != null && state.Errors.Count > 0)
                    {
                        foreach (var error in state.Errors)
                        {
                            Console.WriteLine($"{key}: {error.ErrorMessage}");
                        }
                    }
                }
                Console.WriteLine("=====================\n");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(fileStream);
                        }

                        post.ImagePath = "/uploads/" + uniqueFileName;
                    }

                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser == null)
                    {
                        return Unauthorized();
                    }

                    post.ApplicationUserId = currentUser.Id;
                    post.CreatedAt = DateTime.Now;

                    _context.Add(post);
                    await _context.SaveChangesAsync();

                    await _countersService.UpdatePostCount(currentUser.Id, 1);

                    if (SelectedTagIds != null)
                    {
                        foreach (var tagId in SelectedTagIds)
                        {
                            post.PostTags.Add(new PostTag { TagId = tagId });
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(NewTagName))
                    {
                        var tagName = NewTagName.Trim();
                        var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);

                        if (existingTag == null)
                        {
                            existingTag = new Tag { Name = tagName };
                            _context.Tags.Add(existingTag);
                            await _context.SaveChangesAsync();
                        }

                        if (!post.PostTags.Any(pt => pt.TagId == existingTag.Id))
                        {
                            post.PostTags.Add(new PostTag { TagId = existingTag.Id });
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Исключение при сохранении: {ex.Message}");
                    ModelState.AddModelError("", "Произошла ошибка при сохранении поста.");
                }
            }

            ViewBag.Communities = await _context.Communities.ToListAsync();
            ViewBag.Tags = await _context.Tags.ToListAsync();
            return View(post);
        }

        [Authorize(Policy = "RequireUser")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var post = await _context.Posts
                .Include(p => p.PostTags)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (post == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            if (post.ApplicationUserId != currentUser.Id && !User.IsInRole("Moderator") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            ViewBag.Communities = await _context.Communities.ToListAsync();
            ViewBag.Tags = await _context.Tags.ToListAsync();
            ViewBag.SelectedTagIds = post.PostTags.Select(pt => pt.TagId).ToList();
            return View(post);
        }

        [HttpPost]
        [Authorize(Policy = "RequireUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Post post, IFormFile ImageFile, List<int> SelectedTagIds, string NewTagName)
        {
            ModelState.Remove("ImageFile");
            ModelState.Remove("NewTagName");
            ModelState.Remove("SelectedTagIds");
            ModelState.Remove("ApplicationUserId");

            if (id != post.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingPost = await _context.Posts
                        .Include(p => p.PostTags)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (existingPost == null) return NotFound();

                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser == null)
                    {
                        return Unauthorized();
                    }

                    if (existingPost.ApplicationUserId != currentUser.Id && !User.IsInRole("Moderator") && !User.IsInRole("Admin"))
                    {
                        return Forbid();
                    }

                    existingPost.Title = post.Title;
                    existingPost.Content = post.Content;
                    existingPost.CommunityId = post.CommunityId;

                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(existingPost.ImagePath))
                        {
                            var oldFilePath = Path.Combine(_hostingEnvironment.WebRootPath, existingPost.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                                System.IO.File.Delete(oldFilePath);
                        }

                        var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(fileStream);
                        }

                        existingPost.ImagePath = "/uploads/" + uniqueFileName;
                    }

                    existingPost.PostTags.Clear();

                    if (SelectedTagIds != null)
                    {
                        foreach (var tagId in SelectedTagIds)
                        {
                            existingPost.PostTags.Add(new PostTag { TagId = tagId });
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(NewTagName))
                    {
                        var tagName = NewTagName.Trim();
                        var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);

                        if (existingTag == null)
                        {
                            existingTag = new Tag { Name = tagName };
                            _context.Tags.Add(existingTag);
                            await _context.SaveChangesAsync();
                        }

                        if (!existingPost.PostTags.Any(pt => pt.TagId == existingTag.Id))
                        {
                            existingPost.PostTags.Add(new PostTag { TagId = existingTag.Id });
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка при редактировании: {ex.Message}");
                    ModelState.AddModelError("", "Произошла ошибка при обновлении поста.");
                }
            }

            ViewBag.Communities = await _context.Communities.ToListAsync();
            ViewBag.Tags = await _context.Tags.ToListAsync();
            ViewBag.SelectedTagIds = await _context.Posts
                .Where(p => p.Id == id)
                .Include(p => p.PostTags)
                .SelectMany(p => p.PostTags.Select(pt => pt.TagId))
                .ToListAsync();

            return View(post);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var post = await _context.Posts
                .Include(p => p.Community)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (post == null) return NotFound();
            return View(post);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = "RequireUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.Posts
                .Include(p => p.PostTags)
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post != null)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                if (post.ApplicationUserId != currentUser.Id && !User.IsInRole("Moderator") && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                if (!string.IsNullOrEmpty(post.ImagePath))
                {
                    var filePath = Path.Combine(_hostingEnvironment.WebRootPath, post.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }

                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();

                await _countersService.UpdatePostCount(currentUser.Id, -1);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.Id == id);
        }
    }
}