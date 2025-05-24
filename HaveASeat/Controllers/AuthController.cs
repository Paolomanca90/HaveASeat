using HaveASeat.Models;
using HaveASeat.Utilities.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HaveASeat.Controllers
{
	public class AuthController : Controller
	{
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IUserStore<ApplicationUser> _userStore;
		private readonly IUserEmailStore<ApplicationUser> _emailStore;

		public AuthController(
			UserManager<ApplicationUser> userManager,
			IUserStore<ApplicationUser> userStore,
			SignInManager<ApplicationUser> signInManager)
		{
			_userManager = userManager;
			_userStore = userStore;
			_emailStore = GetEmailStore();
			_signInManager = signInManager;
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult NewPartner()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> NewPartner(ApplicationUserDto model)
		{
			if (!ModelState.IsValid)
				return View(model);

			var user = CreateUser();
			user.Nome = model.Nome;
			user.Cognome = model.Cognome;
			user.PhoneNumber = model.PhoneNumber;
			user.Indirizzo = model.Address;
			user.Città = model.City;
			user.CAP = model.PostalCode;
			user.Provincia = model.Province;
			user.CodiceFiscale = model.CodiceFiscale;
			user.AcceptNewsletter = model.AcceptNewsletter;

			await _userStore.SetUserNameAsync(user, model.Email, CancellationToken.None);
			await _emailStore.SetEmailAsync(user, model.Email, CancellationToken.None);
			var result = await _userManager.CreateAsync(user, model.Password);

			if (result.Succeeded)
			{
				await _signInManager.SignInAsync(user, isPersistent: false);
				TempData["UserId"] = user.Id;
				return RedirectToAction("NewShop");
			}
			else
			{
				foreach (var error in result.Errors)
				{
					ModelState.AddModelError(string.Empty, error.Description);
				}
				return View(model);
			}
		}

		public IActionResult NewShop()
		{
			return View();
		}

		private ApplicationUser CreateUser()
		{
			try
			{
				return Activator.CreateInstance<ApplicationUser>();
			}
			catch
			{
				throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
					$"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
					$"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
			}
		}

		private IUserEmailStore<ApplicationUser> GetEmailStore()
		{
			if (!_userManager.SupportsUserEmail)
			{
				throw new NotSupportedException("The default UI requires a user store with email support.");
			}
			return (IUserEmailStore<ApplicationUser>)_userStore;
		}
	}
}
