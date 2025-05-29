using HaveASeat.Data;
using HaveASeat.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Stripe;
using System.Security.Claims;
using HaveASeat.Utilities.Enum;

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

		public IActionResult CreateCheckoutSession(string id)
		{
			if (id == null)
				return BadRequest("Utente non loggato");
			return View();
		}

		[HttpPost]
		public IActionResult CreateCheckoutSession()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return BadRequest("L'utente non esiste.");
			}

			var checkPiano = _context.PianoSelezionato.FirstOrDefault(x => x.ApplicationUserId == userId && x.Confermato == false);
			if (checkPiano == null)
			{
				return BadRequest("Nessun piano selezionato.");
			}

			var piano = _context.Abbonamento.FirstOrDefault(x => x.AbbonamentoId == checkPiano.AbbonamentoId);
			if (piano == null)
			{
				return BadRequest("Piano non trovato.");
			}

			var total = piano.Prezzo;
			var saloneId = _context.Salone.FirstOrDefault(x => x.ApplicationUserId == userId && x.Stato == Stato.InAttesaDiApprovazione)?.SaloneId;
			if (saloneId == null)
			{
				return BadRequest("Salone non trovato.");
			}

			var customerId = CreateOrGetStripeCustomer();

			var options = new SessionCreateOptions
			{
				Customer = customerId,
				PaymentMethodTypes = new List<string> { "card", "klarna", "paypal", "samsung_pay", "sepa_debit" },
				LineItems = new List<SessionLineItemOptions>
				{
					new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							UnitAmount = Convert.ToInt64(total * 100),
							Currency = "eur",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = "Registrazione Have A Seat",
								Metadata = new Dictionary<string, string>
								{
									{"P_IVA", GetCustomerMetadata("P_IVA")},
									{"SaloneId", saloneId.ToString()}
								}
							},
						},
						Quantity = 1,
					},
				},
				Mode = "payment",
				SuccessUrl = $"{Request.Scheme}://{Request.Host}/Partner/PaymentSuccess?sessionId={{CHECKOUT_SESSION_ID}}",
				CancelUrl = Url.Action("PaymentCancel", "Partner", null, Request.Scheme),
				PaymentIntentData = new SessionPaymentIntentDataOptions
				{
					Metadata = new Dictionary<string, string>
					{
						{"P_IVA", GetCustomerMetadata("P_IVA")},
						{"SaloneId", saloneId.ToString()}
					}
				}
			};

			var service = new SessionService();
			var session = service.Create(options);

			return Json(new { url = session.Url });
		}

		private string CreateOrGetStripeCustomer()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var salone = _context.Salone.FirstOrDefault(x => x.ApplicationUserId == userId && x.Stato == Stato.InAttesaDiApprovazione);

			var customerService = new CustomerService();

			var searchOptions = new CustomerSearchOptions
			{
				Query = $"email:'{salone?.Email}' AND metadata['P_IVA']:'{salone?.PartitaIVA}'"
			};

			var existingCustomers = customerService.Search(searchOptions);
			if (existingCustomers.Any())
				return existingCustomers.First().Id;

			var customerOptions = new CustomerCreateOptions
			{
				Email = salone?.Email,
				Name = salone?.Nome,
				Address = new AddressOptions
				{
					City = salone?.Citta,
					Country = "IT",
					Line1 = salone?.Indirizzo,
					PostalCode = salone?.CAP,
					State = salone?.Provincia
				},
				Phone = salone?.Telefono,
				Metadata = new Dictionary<string, string>
				{
					{"P_IVA", salone?.PartitaIVA}
				}
			};

			var newCustomer = customerService.Create(customerOptions);
			return newCustomer.Id;
		}

		private string GetCustomerMetadata(string key)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var salone = _context.Salone.FirstOrDefault(x => x.ApplicationUserId == userId && x.Stato == Stato.InAttesaDiApprovazione);
			return key switch
			{
				"P_IVA" => salone?.PartitaIVA,
				_ => string.Empty
			};
		}

		public IActionResult PaymentSuccess(string sessionId)
		{
			var service = new SessionService();
			Session session = service.Get(sessionId);

			if (session.PaymentStatus == "paid")
			{
				var paymentIntentService = new PaymentIntentService();
				PaymentIntent paymentIntent = paymentIntentService.Get(session.PaymentIntentId);
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				var salone = _context.Salone.FirstOrDefault(x => x.ApplicationUserId == userId && x.Stato == Stato.InAttesaDiApprovazione);
				if (salone != null)
				{
					salone.Stato = Stato.Attivo;
					_context.Salone.Update(salone);
					var pianoSelezionato = _context.PianoSelezionato.FirstOrDefault(x => x.ApplicationUserId == userId && x.Confermato == false);
					if (pianoSelezionato == null)
					{
						ViewBag.Errore = "Errore: Piano selezionato non trovato.";
						return View("Index");
					}
					
					pianoSelezionato.Confermato = true;
					_context.PianoSelezionato.Update(pianoSelezionato);
					
					var saloneAbbonamento = new SaloneAbbonamento
					{
						SaloneAbbonamentoId = Guid.NewGuid(),
						SaloneId = salone.SaloneId,
						AbbonamentoId = pianoSelezionato.AbbonamentoId,
						DataInizio = DateTime.Now,
						DataFine = DateTime.Now.AddMonths(1),
						Stato = Stato.Attivo,
						StripeCustomerId = paymentIntent.CustomerId,
						StripeSubscriptionId = paymentIntent.Id,
						IsTrial = false
					};
					_context.SaloneAbbonamento.Add(saloneAbbonamento);
					_context.SaveChanges();
					ViewBag.Messaggio = "Registrazione effettuata con successo!";
					return View("Index");
				}
				else
				{
					ViewBag.Errore = "Errore: Salone non trovato.";
				}
			}
			else
			{
				ViewBag.Errore = "Errore: Il pagamento non è stato completato.";
			}

			return View("PaymentResult");
		}

		public IActionResult PaymentCancel()
		{
			ViewBag.Errore = "Pagamento annullato.";
			return View("Index");
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
