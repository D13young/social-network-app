using Microsoft.AspNetCore.Mvc;
using SocialNetworkApp.Models;

namespace SocialNetworkApp.ViewComponents
{
    public class PostCardViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Post post)
        {
            return View(post);
        }
    }
}
