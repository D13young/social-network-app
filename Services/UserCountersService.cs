using SocialNetworkApp.Data;
using SocialNetworkApp.Models;

namespace SocialNetworkApp.Services
{
    public class UserCountersService
    {
        private readonly ApplicationDbContext _context;

        public UserCountersService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task UpdatePostCount(string userId, int delta)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.PostsCount += delta;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateCommentCount(string userId, int delta)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.CommentsCount += delta;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateLikeCount(string userId, int delta)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LikesCount += delta;
                await _context.SaveChangesAsync();
            }
        }
    }
}
