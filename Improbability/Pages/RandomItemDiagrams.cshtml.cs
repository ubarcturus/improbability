using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Improbability.Data;
using Improbability.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Improbability.Pages
{
    public class RandomItemDiagramsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RandomItemDiagramsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty] public RandomEvent RandomEvent { get; set; }
        [BindProperty] public ApplicationUser ApplicationUser { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostGetFromApiAsync([FromBody] FetchRequestBody requestBody)
        {
            var apiUrl = new Uri("http://localhost:45347/api/v1/");

            var randomItem = await GetRandomItemAsync(apiUrl, requestBody);
            var randomEvents = await GetRandomEventsAsync(apiUrl, requestBody);

            // var returnBody = (randomItem, randomEvents);
            var returnBody = new FetchReturnBody { RandomItem = randomItem, RandomEvents = randomEvents };

            return new JsonResult(returnBody);

            // _context.RandomEvents.Add(RandomEvent);
            // await _context.SaveChangesAsync();

            // return RedirectToPage("./Index");
        }

        private async Task<JsonElement> GetRandomItemAsync(Uri apiUrl, FetchRequestBody requestBody)
        {
            var randomItemUri = new Uri($"{apiUrl}randomitems/{requestBody.RandomItemId}");
            using var httpClient = new HttpClient { DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Key", requestBody.ApiKey) } };
            var randomItemJson = await (await httpClient.GetAsync(randomItemUri)).Content.ReadFromJsonAsync<JsonElement>();
            return randomItemJson;
        }

        private async Task<JsonElement> GetRandomEventsAsync(Uri apiUrl, FetchRequestBody requestBody)
        {
            var randomEventUri = new Uri($"{apiUrl}randomevents?randomitemid={requestBody.RandomItemId}");
            using var httpClient = new HttpClient { DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Key", requestBody.ApiKey) } };
            var randomEventsJson = await (await httpClient.GetAsync(randomEventUri)).Content.ReadFromJsonAsync<JsonElement>();
            return randomEventsJson;
        }
    }

    public class FetchRequestBody
    {
        public string ApiKey { get; set; }
        public int RandomItemId { get; set; }
    }

    internal class FetchReturnBody
    {
        public JsonElement RandomItem { get; set; }
        public JsonElement RandomEvents { get; set; }
    }
}
