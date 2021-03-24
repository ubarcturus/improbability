using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Improbability.Data;
using Improbability.Models;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http.Json;
using Microsoft.VisualBasic;
using System.Collections.ObjectModel;

namespace Improbability.Pages
{
    public class AddRandomEventModel : PageModel
    {
        private readonly Improbability.Data.ApplicationDbContext _context;

        public AddRandomEventModel(Improbability.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            // return RedirectToPage("./AddRandomEvent");
            return Page();
        }

        [BindProperty]
        public RandomEvent RandomEvent { get; set; }
        [BindProperty] public ApplicationUser ApplicationUser { get; set; }

        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
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
            var responseContent = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                RandomEvent = response.Content.ReadFromJsonAsync<Collection<RandomEvent>>().Result.First();
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
