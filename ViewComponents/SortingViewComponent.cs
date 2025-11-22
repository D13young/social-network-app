using Microsoft.AspNetCore.Mvc;

namespace SocialNetworkApp.ViewComponents
{
    public class SortingViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string currentSort)
        {
            ViewBag.CurrentSort = currentSort;
            return View();
        }
    }
}
