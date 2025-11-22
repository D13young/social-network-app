using Microsoft.AspNetCore.Mvc;

namespace SocialNetworkApp.ViewComponents
{
    public class PaginationViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(int currentPage, int totalPages, string action, string controller, object? routeValues = null)
        {
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.Action = action;
            ViewBag.Controller = controller;
            ViewBag.RouteValues = routeValues;
            return View();
        }
    }
}
