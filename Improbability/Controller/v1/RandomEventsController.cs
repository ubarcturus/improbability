using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// <summary>
    /// A web API that can manage random events stored in a database.
    /// </summary>
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

        /// <summary>
        /// Get RandomEvents
        /// </summary>
        /// <param name="randomItemId">The ID of the associated RandomItem</param>
        /// <param name="authorization" example="Key 5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ">The API-Key</param>
        /// <response code="200">OK: Return an array of RandomEvents</response>
        /// <response code="400">Bad Request: The randomItemId in URL is in the wrong format</response>
        /// <response code="401">Unauthorized: Your API-Key is wrong or in the wrong format, or you have no permissions for the RandomItem with this randomItemId</response>
        /// <response code="404">Not Found: There is no RandomItem with this randomItemId</response>
        [HttpGet]
        [ProducesResponseType(typeof(Collection<RandomEvent>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<IEnumerable<RandomEvent>>> GetRandomEvents([FromQuery] string randomItemId, [FromHeader] string authorization)
        {
            if (!IsAuthenticated(authorization))
            {
                return Unauthorized();
            }

            if (randomItemId == "teapot") { return StatusCode(418); }

            if (string.IsNullOrWhiteSpace(randomItemId))
            {
                return await GetRandomEventsFromUserAsync(authorization);
            }

            if (!IsInteger(randomItemId, out var itemId))
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

        /// <summary>
        /// Get a RandomEvent by ID
        /// </summary>
        /// <param name="id">The ID of the desired RandomEvent</param>
        /// <param name="authorization" example="Key 5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ">The API-Key</param>
        /// <response code="200">OK: Return the RandomEvent with this id</response>
        /// <response code="401">Unauthorized: Your API-Key is wrong or in the wrong format, or you have no permissions for the RandomEvent with this id</response>
        /// <response code="404">Not Found: There is no RandomEvent with this id</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RandomEvent), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
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

        /// <summary>
        /// Update a RandomEvent
        /// </summary>
        /// <remarks>
        /// **DANGER: Every missing argument will reset the existing value to null or zero!**
        ///
        /// Sample request:
        ///
        ///     PUT /api/v1/randomevents/1
        ///     Authorization: Key 5BLBAI3PGNUPVO5GFKMUSSPC6KCAE2M7
        ///     Content-Type: application/json
        ///
        ///     {
        ///         "id": 1,
        ///         "name": "is optional",
        ///         "time": "2021-03-23T16:35:44+01:00",
        ///         "result": 0,
        ///         "description": "is optional",
        ///         "randomItemId": 1
        ///     }
        ///
        /// </remarks>
        /// <param name="id">The ID of the desired RandomEvent</param>
        /// <param name="authorization" example="Key 5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ">The API-Key</param>
        /// <param name="randomEvent">The new Data for the RandomEvent</param>
        /// <response code="200">OK: Return the new RandomEvent</response>
        /// <response code="400">Bad Request: The id in URL and the RandomEvent are different, the RandomItemId in randomEvent is not set or zero, or the RandomEvent is in the wrong format</response>
        /// <response code="401">Unauthorized: Your API-Key is wrong or in the wrong format, or you have no permissions for the RandomEvent with this id</response>
        /// <response code="404">Not Found: There is no RandomEvent with this id</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(RandomEvent), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
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

            if (randomEvent == null || id != randomEvent.Id || randomEvent.RandomItemId == 0)
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

        /// <summary>
        /// Add some RandomEvents
        /// </summary>
        /// <remarks>
        /// **ATTENTION: Don't set an id!** An id is always automatically generated. If you do, the Request will fail.
        ///
        /// Sample request:
        ///
        ///     POST /api/v1/randomevents?randomItemid=2
        ///     Authorization: Key 5BLBAI3PGNUPVO5GFKMUSSPC6KCAE2M7
        ///     Content-Type: application/json
        ///
        ///     [
        ///         {
        ///             "name": "is optional",
        ///             "time": "2021-03-23T16:33:57+01:00",
        ///             "result": 3,
        ///             "description": "is optional",
        ///             "randomItemId": 2
        ///         },
        ///         {
        ///             "name": "is optional",
        ///             "time": "2021-03-23T16:35:44+01:00",
        ///             "result": 8,
        ///             "description": "is optional",
        ///             "randomItemId": 2
        ///         }
        ///     ]
        ///
        /// </remarks>
        /// <param name="randomItemId">The ID of the associated RandomItem</param>
        /// <param name="authorization" example="Key 5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ">The API-Key</param>
        /// <param name="randomEvents">An array with the new RandomEvents</param>
        /// <response code="201">Created: Return an array with the new RandomEvents</response>
        /// <response code="400">Bad Request: The randomItemId in URL is in the wrong format, the RandomItemId in the randomEvents and URL are different, or the RandomEvents are in the wrong format</response>
        /// <response code="401">Unauthorized: Your API-Key is wrong or in the wrong format, or you have no permissions for the RandomItem with this randomItemId</response>
        [HttpPost]
        [ProducesResponseType(typeof(Collection<RandomEvent>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
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

        /// <summary>
        /// Add some RandomItems from csv file
        /// </summary>
        /// <remarks>
        /// **ATTENTION: Don't set an id!** An id is always automatically generated. If you do, the Request will fail.
        ///
        /// Sample request:
        ///
        ///     POST /api/v1/randomevents/csv?randomitemid=1
        ///     Authorization: Key 5BLBAI3PGNUPVO5GFKMUSSPC6KCAE2M7
        ///     Content-Type: multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW
        ///
        ///     ------WebKitFormBoundary7MA4YWxkTrZu0gW
        ///     Content-Disposition: form-data; name="csv"; filename="filename.csv"
        ///     Content-Type: text/csv
        ///
        ///     First,2021-03-23T15:23:12.0857267+01:00,3,Description,1
        ///     ,2021-03-23T14:00:22+01:00,6,,1
        ///     Third,2021-03-23T14:00:22+01:00,2,"Something, else",1
        ///     ------WebKitFormBoundary7MA4YWxkTrZu0gW--
        ///
        /// </remarks>
        /// <param name="randomItemId">The ID of the associated RandomItem</param>
        /// <param name="authorization" example="Key 5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ">The API-Key</param>
        /// <param name="csv">The CSV data or file</param>
        /// <response code="201">Created: Return an array with the new RandomEvents</response>
        /// <response code="400">Bad Request: The randomItemId in URL is in the wrong format, the CSV file is missing or in the wrong format or the RandomItemId in the randomEvents and URL are different </response>
        /// <response code="401">Unauthorized: Your API-Key is wrong or in the wrong format, or you have no permissions for the RandomItem with this randomItemId</response>
        [HttpPost("csv")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(Collection<RandomEvent>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
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

        /// <summary>
        /// Remove a RandomEvent
        /// </summary>
        /// <param name="id">The ID of the desired RandomEvent</param>
        /// <param name="authorization" example="Key 5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ">The API-Key</param>
        /// <response code="204">No Content: RandomEvent is deleted</response>
        /// <response code="401">Unauthorized: Your API-Key is wrong or in the wrong format, or you have no permissions for the RandomEvent with this id</response>
        /// <response code="404">Not Found: There is no RandomEvent with this id</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
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
