using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SocialNetworkApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FirstName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public string? AvatarPath { get; set; }

        public int PostsCount { get; set; } = 0;
        public int CommentsCount { get; set; } = 0;
        public int LikesCount { get; set; } = 0;

        public bool IsBanned { get; set; } = false;

        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();
    }
}
