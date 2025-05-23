using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HaveASeat.Controllers
{
    //[Authorize(Roles = "Partner")]
    public class PartnerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
