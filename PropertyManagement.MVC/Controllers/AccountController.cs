using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.API.Models;
using PropertyManagement.MVC.Models;

namespace PropertyManagement.MVC.Controllers
{
    // handles login logout and registration for all user types
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager   = userManager;
        }

        // shows the login form
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // if the user is already logged in just send them home
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // processes the login form submission
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            // creates the auth cookie on success
            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // only redirect to returnUrl if it is a safe local path
                // this prevents open redirect attacks where an attacker sends the user to an external site
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            // we use a generic message so attackers cannot tell if an email exists in the system
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // shows the tenant registration form
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        // creates a new tenant account and signs them in immediately
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // we create a Tenant type user so EF Core stores it with the correct TPH discriminator
            var tenant = new Tenant
            {
                UserName         = model.Email,
                Email            = model.Email,
                PhoneNumber      = model.PhoneNumber,
                CPR              = model.CPR,
                Occupation       = model.Occupation,
                EmergencyContact = model.EmergencyContact,
                RegistrationDate = DateTime.UtcNow,
                // we skip email confirmation for this system since there is no email service set up
                EmailConfirmed   = true
            };

            var result = await _userManager.CreateAsync(tenant, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            // assign the Tenant role so the role-based authorization works throughout the app
            await _userManager.AddToRoleAsync(tenant, "Tenant");
            // sign them in right away so they land on the dashboard without needing to log in again
            await _signInManager.SignInAsync(tenant, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        // signs the user out and clears their auth cookie
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // shown when a user tries to access a page they do not have permission for
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }
}
