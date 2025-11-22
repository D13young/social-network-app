using System.ComponentModel.DataAnnotations;

namespace SocialNetworkApp.Models
{
    public class Tag
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Тег обязателен")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    }
}