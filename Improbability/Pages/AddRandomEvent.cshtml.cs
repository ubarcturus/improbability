using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Improbability.Data;
using Improbability.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;

namespace Improbability.Pages
{
    public class AddRandomEventModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AddRandomEventModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty] public RandomEvent RandomEvent { get; set; }

        [BindProperty] public ApplicationUser ApplicationUser { get; init; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var apiUrl = $"/api/v1/randomevents?randomitemid={RandomEvent.RandomItemId}";
            var requestUri = new Uri($"http://localhost:45347{apiUrl}");
            using var httpClient = new HttpClient();
            using var requestContent = new StringContent($"[{JsonConvert.SerializeObject(RandomEvent)}]", Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Key", ApplicationUser.ApiKey);
            var response = await httpClient.PostAsync(requestUri, requestContent);

            if (response.IsSuccessStatusCode)
            {
                RandomEvent = response.Content.ReadFromJsonAsync<Collection<RandomEvent>>().Result!.First();
                var page = Page();
                page.StatusCode = 201;
                return page;
            }

            return Page();

            // _context.RandomEvents.Add(RandomEvent);
            // await _context.SaveChangesAsync();

            // return RedirectToPage("./Index");
        }
    }
}
