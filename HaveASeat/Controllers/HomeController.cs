using System.Diagnostics;
using HaveASeat.Data;
using HaveASeat.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Session;

namespace HaveASeat.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
		private readonly ApplicationDbContext _context;

		public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
		{
			_logger = logger;
			_context = context;
		}

		public IActionResult Index()
        {
            return View();
        }
        public IActionResult ForPartner()
        {
            return View();
        }

        [HttpPost]
		public IActionResult SelectPlan(string id)
		{
			TempData["SelectedPianoId"] = id;
            var piano = _context.Abbonamento.Find(Guid.Parse(id));
			return Json(new { success = true, redirectUrl = "/Auth/NewPartner", selectedPlan = piano }); 
		}

		public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
