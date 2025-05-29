using HaveASeat.Data;
using HaveASeat.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HaveASeat.Controllers
{
    [Authorize(Roles = "Partner")]
    public class PartnerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PartnerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var checkPiano = _context.PianoSelezionato.FirstOrDefault(x => x.ApplicationUserId == userId && x.Confermato == false);
            if (checkPiano != null)
                TempData["SelectedPianoId"] = checkPiano.ApplicationUserId;
            return View();
        }
        public IActionResult Calendario()
        {
            return View();
        }
        public IActionResult Servizi()
        {
            return View();
        }
        public IActionResult Clienti()
        {
            return View();
        }
        public IActionResult Staff()
        {
            return View();
        }
        public IActionResult Sedi()
        {
            return View();
        }
        public IActionResult ProfiloPartner()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            var pianoSelected = _context.PianoSelezionato.FirstOrDefault(x => x.ApplicationUserId == userId);
            if (pianoSelected != null)
            {
                var abbonamento = _context.Abbonamento
                    .FirstOrDefault(a => a.AbbonamentoId == pianoSelected.AbbonamentoId);

                if (abbonamento != null)
                {
                    TempData["AbbonamentoNome"] = abbonamento.Nome;
                }

                if (user == null)
                {
                    return NotFound();
                }

            }
                return View(user);
        }
    }
}
