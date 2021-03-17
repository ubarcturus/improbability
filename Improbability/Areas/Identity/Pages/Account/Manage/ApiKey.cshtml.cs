using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Improbability.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Improbability.Areas.Identity.Pages.Account.Manage
{
    public class ApiKeyModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ApiKeyModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [Display(Name = "Api-key")] public string ApiKey { get; set; }

        [TempData] public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            ApiKey = user.ApiKey;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                ApiKey = user.ApiKey;
                return Page();
            }

            user.ApiKey = _userManager.GenerateNewAuthenticatorKey();
            await _userManager.UpdateAsync(user);
            StatusMessage = "Your Api-key has changed.";
            return RedirectToPage();
        }
    }
}