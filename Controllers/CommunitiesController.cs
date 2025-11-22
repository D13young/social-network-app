using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialNetworkApp.Data;
using SocialNetworkApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace SocialNetworkApp.Controllers
{
    public class CommunitiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CommunitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Communities.ToListAsync());
        }

        [Authorize(Policy = "RequireModerator")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "RequireModerator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Community community)
        {
            if (ModelState.IsValid)
            {
                _context.Add(community);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(community);
        }

        [Authorize(Policy = "RequireModerator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var community = await _context.Communities.FindAsync(id);
            if (community == null) return NotFound();

            return View(community);
        }

        [HttpPost]
        [Authorize(Policy = "RequireModerator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Community community)
        {
            if (id != community.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(community);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommunityExists(community.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(community);
        }

        [Authorize(Policy = "RequireModerator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var community = await _context.Communities
                .FirstOrDefaultAsync(m => m.Id == id);

            if (community == null) return NotFound();

            return View(community);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = "RequireModerator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var community = await _context.Communities.FindAsync(id);
            if (community != null)
            {
                _context.Communities.Remove(community);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CommunityExists(int id)
        {
            return _context.Communities.Any(e => e.Id == id);
        }
    }
}
