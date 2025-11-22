namespace SocialNetworkApp.Models
{
    public class PostLike
    {
        public int Id { get; set; }

        public int PostId { get; set; }
        public Post? Post { get; set; }

        public string ApplicationUserId { get; set; } = null!;
        public ApplicationUser? ApplicationUser { get; set; }

        public bool IsLike { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
