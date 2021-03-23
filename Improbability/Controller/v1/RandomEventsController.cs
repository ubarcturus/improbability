using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Improbability.Data;
using Improbability.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Improbability.Controller.v1
{
    [Produces("application/json")]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class RandomEventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RandomEventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RandomEvent>>> GetRandomEvents([FromQuery] string randomItemId, [FromHeader] string authorization)
        {
            // if (randomItemId == "teapot") { return StatusCode(418); }

            if (!IsAuthenticated(authorization))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(randomItemId))
            {
                return await GetRandomEventsFromUserAsync(authorization);
            }

            if (!int.TryParse(randomItemId, NumberStyles.None, null, out var itemId))
            {
                return BadRequest();
            }

            if (!_context.RandomItems.Any(r => r.Id == itemId))
            {
                return NotFound();
            }

            if (!AccessIsAllowed(randomItemId, authorization))
            {
                return Unauthorized();
            }

            var randomEvents = await _context.RandomEvents.Where(r => r.RandomItemId == itemId).ToListAsync();
            return randomEvents;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RandomEvent>> GetRandomEvent(int id, [FromHeader] string authorization)
        {
            if (!IsAuthenticated(authorization))
            {
                return Unauthorized();
            }

            if (!RandomEventExists(id))
            {
                return NotFound();
            }

            if (!AccessIsAllowed(id, authorization))
            {
                return Unauthorized();
            }

            var randomEvent = await _context.RandomEvents.FindAsync(id);

            return randomEvent;
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<RandomEvent>> PutRandomEvent(int id, [FromHeader] string authorization, RandomEvent randomEvent)
        {
            if (!IsAuthenticated(authorization))
            {
                return Unauthorized();
            }

            if (!RandomEventExists(id))
            {
                return NotFound();
            }

            if (randomEvent == null || id != randomEvent.Id)
            {
                return BadRequest();
            }

            if (!AccessIsAllowed(id, authorization))
            {
                return Unauthorized();
            }

            _context.Entry(randomEvent).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RandomEventExists(id))
                {
                    return NotFound();
                }

                throw;
            }

            return randomEvent;
        }

        [HttpPost]
        public async Task<ActionResult<Collection<RandomEvent>>> PostRandomEvents([FromQuery] string randomItemId, [FromHeader] string authorization, Collection<RandomEvent> randomEvents)
        {
            if (!IsAuthenticated(authorization))
            {
                return Unauthorized();
            }

            if (!IsInteger(randomItemId, out var itemId))
            {
                return BadRequest();
            }

            if (randomEvents == null || randomEvents.Count == 0 || randomEvents.Any(r => r.RandomItemId != itemId))
            {
                return BadRequest();
            }

            if (!AccessIsAllowed(randomItemId, authorization))
            {
                return Unauthorized();
            }

            _context.RandomEvents.AddRange(randomEvents);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRandomEvents), randomEvents);
        }

        [HttpPost("csv")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Collection<RandomEvent>>> PostRandomEvents([FromQuery] string randomItemId, [FromHeader] string authorization, IFormFile csv)
        {
            if (!IsAuthenticated(authorization))
            {
                return Unauthorized();
            }

            if (!IsInteger(randomItemId, out var itemId))
            {
                return BadRequest();
            }

            if (csv == null || csv.Length == 0)
            {
                return BadRequest();
            }

            if (!AccessIsAllowed(randomItemId, authorization))
            {
                return Unauthorized();
            }

            StreamReader streamReader = null;

            var memoryStream = new MemoryStream();
            await csv.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            streamReader = new StreamReader(memoryStream);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                MissingFieldFound = null
            };
            using var csvReader = new CsvReader(streamReader, config);

            var randomEvents = new List<RandomEvent>();
            try
            {
                randomEvents = csvReader.GetRecords<RandomEvent>().ToList();
            }
            catch (TypeConverterException)
            {
                return BadRequest();
            }

            if (randomEvents.Any(r => r.RandomItemId != itemId))
            {
                return BadRequest();
            }

            _context.RandomEvents.AddRange(randomEvents);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRandomEvents), randomEvents);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRandomEvent(int id, [FromHeader] string authorization)
        {
            if (!IsAuthenticated(authorization))
            {
                return Unauthorized();
            }

            var randomEvent = await _context.RandomEvents.FindAsync(id);
            if (randomEvent == null)
            {
                return NotFound();
            }

            if (!AccessIsAllowed(id, authorization))
            {
                return Unauthorized();
            }

            _context.RandomEvents.Remove(randomEvent);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool IsAuthenticated(string authorization)
        {
            return AuthenticationHeaderValue.TryParse(authorization, out var authenticationHeaderValue)
                   && GetApplicationUserAsync(authenticationHeaderValue.Parameter).Result != null;
        }

        private bool AccessIsAllowed(int id, string authorization)
        {
            var apiKey = AuthenticationHeaderValue.Parse(authorization).Parameter;
            var applicationUser = GetApplicationUserAsync(apiKey).Result;
            var randomItemId = _context.RandomEvents.AsNoTracking().First(r => r.Id == id).RandomItemId;

            return applicationUser.RandomItems.Any(r => r.Id == randomItemId);
        }

        private bool AccessIsAllowed(string randomItemIdString, string authorization)
        {
            var apiKey = AuthenticationHeaderValue.Parse(authorization).Parameter;
            var applicationUser = GetApplicationUserAsync(apiKey).Result;
            var randomItemId = int.Parse(randomItemIdString, null);

            return applicationUser.RandomItems.Any(r => r.Id == randomItemId);
        }

        private async Task<List<RandomEvent>> GetRandomEventsFromUserAsync(string authorization)
        {
            var apiKey = AuthenticationHeaderValue.Parse(authorization).Parameter;
            var randomEvents = new List<RandomEvent>();
            var randomItemIds = (await GetApplicationUserAsync(apiKey)).RandomItems.Select(r => r.Id).ToList();

            foreach (var randomItemId in randomItemIds)
            {
                randomEvents.AddRange(_context.RandomEvents.Where(r => r.RandomItemId == randomItemId));
            }

            return randomEvents;
        }

        private bool RandomEventExists(int id)
        {
            return _context.RandomEvents.Any(r => r.Id == id);
        }

        private bool IsInteger(string randomItemId, out int itemId)
        {
            return int.TryParse(randomItemId, NumberStyles.None, null, out itemId);
        }

        private async Task<ApplicationUser> GetApplicationUserAsync(string apiKey)
        {
            var applicationUser = await _context.ApplicationUsers.Include(a => a.RandomItems).AsNoTracking()
                .FirstOrDefaultAsync(a => a.ApiKey == apiKey);

            return applicationUser;
        }
    }
}
