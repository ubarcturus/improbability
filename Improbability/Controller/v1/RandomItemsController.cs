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
    /// A web API that can manage random items stored in a database.
    /// </summary>
    [Produces("application/json")]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class RandomItemsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RandomItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all RandomItems
        /// </summary>
        /// <param name="authorization" example="Key 5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ">The API-Key</param>
        /// <response code="200">OK: Return an array of RandomItems</response>
        /// <response code="401">Unauthorized: Your API-Key is wrong or in the wrong format</response>
        [HttpGet]
        [ProducesResponseType(typeof(Collection<RandomItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<IEnumerable<RandomItem>>> GetRandomItems([FromHeader] string authorization)
        {
            if (!IsAuthorized(authorization))
            {
                return Unauthorized();
            }

            return await GetRandomItemsFromUserAsync(authorization);
        }

        /// <summary>
        /// Get a RandomItem by ID
        /// </summary>
        /// <param name="id">The ID of the desired RandomItem</param>
        /// <param name="authorization" example="Key 5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ">Your API-Key</param>
        /// <response code="200">OK: Return the RandomItem with this id</response>
        /// <response code="401">Unauthorized: Your API-Key is wrong or in the wrong format, or you have no permissions for the RandomItem with this id</response>
        /// <response code="404">Not Found: There is no RandomItem with this id</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RandomItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<RandomItem>> GetRandomItem(int id, [FromHeader] string authorization)
        {
            if (!IsAuthorized(authorization, id))
            {
                return Unauthorized();
            }

            var randomItem = await _context.RandomItems.FindAsync(id);

            return randomItem ?? (ActionResult<RandomItem>)NotFound();
        }

        /// <summary>
        /// Update a RandomItem
        /// </summary>
        /// <remarks>
        /// **DANGER: Every missing argument will reset the existing value to null or zero!**
        /// 
        /// Sample request:
        /// 
        ///     PUT /api/v1/randomitems/1
        ///     Authorization: Key 5BLBAI3PGNUPVO5GFKMUSSPC6KCAE2M7
        ///     Content-Type: application/json
        /// 
        ///     {
        ///         "id": 1,
        ///         "name": "W10",
        ///         "numberOfPossibleResults": 10,
        ///         "description": "Ten sites"
        ///     }
        /// 
        /// </remarks>
        /// <param name="id">The ID of the desired RandomItem</param>
        /// <param name="authorization" example="Key 5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ">Your API-Key</param>
        /// <param name="randomItem">The new Data for the RandomItem</param>
        /// <response code="200">OK: Return the new RandomItem</response>
        /// <response code="400">Bad Request: The id in URL and the RandomItem are different or the RandomItem is in the wrong format</response>
        /// <response code="401">Unauthorized: Your API-Key is wrong or in the wrong format, or you have no permissions for the RandomItem with this id</response>
        /// <response code="404">Not Found: There is no RandomItem with this id</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(RandomItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<RandomItem>> PutRandomItem(int id, [FromHeader] string authorization, RandomItem randomItem)
        {
            if (!IsAuthorized(authorization, id))
            {
                return Unauthorized();
            }

            if (randomItem == null || id != randomItem.Id)
            {
                return BadRequest();
            }

            _context.Entry(randomItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RandomItemExists(id))
                {
                    return NotFound();
                }

                throw;
            }

            return Ok(randomItem);
        }

        /// <summary>
        /// Add some RandomItems
        /// </summary>
        /// <remarks>
        /// **ATTENTION: Don't set an id!** An id is always automatically generated. If you do, the Request will fail.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/v1/randomitems/
        ///     Authorization: Key 5BLBAI3PGNUPVO5GFKMUSSPC6KCAE2M7
        ///     Content-Type: application/json
        /// 
        ///     [
        ///         {
        ///             "name": "W10",
        ///             "numberOfPossibleResults": 10,
        ///             "description": "Ten sites"
        ///         },
        ///         {
        ///             "name": "W6",
        ///             "numberOfPossibleResults": 6,
        ///             "description": "Six sites"
        ///         }
        ///     ]
        /// 
        /// </remarks>
        /// <param name="authorization" example="Key 5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ">Your API-Key</param>
        /// <param name="randomItems">An array with the new RandomItems</param>
        /// <response code="201">Created: Return an array with the new RandomItems</response>
        /// <response code="400">Bad Request: The array or one RandomItem is in the wrong format</response>
        /// <response code="401">Unauthorized: Your API-Key is wrong or in the wrong format</response>
        [HttpPost]
        [ProducesResponseType(typeof(Collection<RandomItem>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<Collection<RandomItem>>> PostRandomItems([FromHeader] string authorization, Collection<RandomItem> randomItems)
        {
            if (!IsAuthorized(authorization))
            {
                return Unauthorized();
            }

            if (randomItems == null || randomItems.Count == 0)
            {
                return BadRequest();
            }

            var randomItemsFromUser = await GetRandomItemsFromUserAsync(authorization);
            foreach (var randomItem in randomItems)
            {
                randomItemsFromUser.Add(randomItem);
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRandomItems), randomItems);
        }

        /// <summary>
        /// Add some RandomItems from csv file
        /// </summary>
        /// <remarks>
        /// **ATTENTION: Don't set an id!** An id is always automatically generated. If you do, the Request will fail.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/v1/randomitems/csv
        ///     Authorization: Key 5BLBAI3PGNUPVO5GFKMUSSPC6KCAE2M7
        ///     Content-Type: multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW
        /// 
        ///     ------WebKitFormBoundary7MA4YWxkTrZu0gW
        ///     Content-Disposition: form-data; name="csv"; filename="filename.csv"
        ///     Content-Type: text/csv
        /// 
        ///     W14,14
        ///     W10,10,"Ten, sides"
        ///     W8,8,Eight
        ///     W6,6,Six sides
        ///     ------WebKitFormBoundary7MA4YWxkTrZu0gW--
        /// 
        /// </remarks>
        /// <param name="authorization" example="Key 5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ">Your API-Key</param>
        /// <param name="csv" example="Key 5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ">The CSV data or file</param>
        /// <response code="201">Created: Return an array with the new RandomItems</response>
        /// <response code="400">Bad Request: The CSV file is missing or in the wrong format</response>
        /// <response code="401">Unauthorized: Your API-Key is wrong or in the wrong format</response>
        [HttpPost("csv")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(Collection<RandomItem>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<Collection<RandomItem>>> PostRandomItems([FromHeader] string authorization, IFormFile csv)
        {
            if (!IsAuthorized(authorization))
            {
                return Unauthorized();
            }

            if (csv == null || csv.Length == 0)
            {
                return BadRequest();
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

            var randomItems = new List<RandomItem>();
            try
            {
                randomItems = csvReader.GetRecords<RandomItem>().ToList();
            }
            catch (TypeConverterException)
            {
                return BadRequest();
            }

            var randomItemsFromUser = await GetRandomItemsFromUserAsync(authorization);
            foreach (var randomItem in randomItems)
            {
                randomItemsFromUser.Add(randomItem);
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRandomItems), randomItems);
        }

        /// <summary>
        /// Remove a RandomItem
        /// </summary>
        /// <param name="id">The id of the desired RandomItem</param>
        /// <param name="authorization" example="Key 5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ">Your API-Key</param>
        /// <response code="204">No Content: RandomItem is deleted</response>
        /// <response code="401">Unauthorized: Your API-Key is wrong or in wrong format, or you have no permissions for the RandomItem with this id</response>
        /// <response code="404">Not Found: There is no RandomItem with this id</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> DeleteRandomItem(int id, [FromHeader] string authorization)
        {
            if (!IsAuthorized(authorization, id))
            {
                return Unauthorized();
            }

            var randomItem = await _context.RandomItems.FindAsync(id);

            if (randomItem == null)
            {
                return NotFound();
            }

            _context.RandomItems.Remove(randomItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool IsAuthorized(string authorization)
        {
            return UserIsAuthenticated(authorization, out _);
        }

        private bool IsAuthorized(string authorization, int id)
        {
            if (!UserIsAuthenticated(authorization, out var applicationUser))
            {
                return false;
            }

            if (applicationUser.RandomItems.Any(r => r.Id == id) || !_context.RandomItems.Any(r => r.Id == id))
            {
                return true;
            }

            return false;
        }

        private bool UserIsAuthenticated(string authorization, out ApplicationUser applicationUser)
        {
            applicationUser = null;

            if (!AuthenticationHeaderValue.TryParse(authorization, out var authenticationHeaderValue))
            {
                return false;
            }

            applicationUser = _context.ApplicationUsers.Include(a => a.RandomItems).AsNoTracking()
                .FirstOrDefault(a => a.ApiKey == authenticationHeaderValue.Parameter);

            if (applicationUser == null)
            {
                return false;
            }

            return true;
        }

        private async Task<Collection<RandomItem>> GetRandomItemsFromUserAsync(string authorization)
        {
            var apiKey = AuthenticationHeaderValue.Parse(authorization).Parameter;
            return (await _context.ApplicationUsers.Include(a => a.RandomItems)
                .FirstAsync(a => a.ApiKey == apiKey)).RandomItems;
        }

        private bool RandomItemExists(int id)
        {
            return _context.RandomItems.Any(r => r.Id == id);
        }
    }
}
