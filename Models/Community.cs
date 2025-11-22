using System.ComponentModel.DataAnnotations;

namespace SocialNetworkApp.Models
{
    public class Community
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название сообщества обязательно")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public ICollection<Post>? Posts { get; set; } = new List<Post>();
    }
}