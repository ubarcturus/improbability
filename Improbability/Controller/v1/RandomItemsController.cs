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
    ///     A web API that can manage random items stored in a database.
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
        /// <param name="authorization" example="Key 5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ">Your API-Key</param>
        /// <returns>All RandomItems.</returns>
        /// <response code="200">Return a JSON-Array of RandomItems</response>
        /// <response code="401">Unauthorized: Your API-Key is wrong or in wrong format</response>
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
        /// Get a RandomItem by ID.
        /// </summary>
        /// <param name="id">The ID of the desired RandomItem</param>
        /// <param name="authorization" example="Key 5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ">Your API-Key</param>
        /// <returns>The RandomItem with the id</returns>
        /// <response code="200">Return the RandomItem with this id</response>
        /// <response code="401">Unauthorized: Your API-Key is wrong or in wrong format, or you have no permissions for the RandomItem with this id</response>
        /// <response code="404">Not Found: There is no RandomItem with this id</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
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

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
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

        [HttpPost]
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

        [HttpPost("csv")]
        [Consumes("multipart/form-data")]
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
            if (false)
            {
                streamReader = new StreamReader(csv.OpenReadStream());
            }

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

        // DELETE: api/RandomItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRandomItem(int id)
        {
            var randomItem = await _context.RandomItems.FindAsync(id);
            if (randomItem == null)
            {
                return NotFound();
            }

            _context.RandomItems.Remove(randomItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        ///     Checks if the user is authorized.
        /// </summary>
        /// <param name="authorization">The value from authorization key in the header.</param>
        /// <returns>true, if the validation is successful, otherwise false.</returns>
        private bool IsAuthorized(string authorization)
        {
            return UserIsAuthenticated(authorization, out _);
        }

        /// <summary>
        ///     Checks if the user is authorized for the id.
        /// </summary>
        /// <param name="id">The id from the randomItem</param>
        /// <param name="authorization">The value from authorization key in the header.</param>
        /// <returns>true, if the validation is successful, otherwise false.</returns>
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

        /// <summary>
        ///     Checks if the authorization value in the header is valid and if the api-key is assigned to a user.
        /// </summary>
        /// <param name="authorization">The value from authorization key in the header.</param>
        /// <param name="applicationUser">The ApplicationUser or null.</param>
        /// <returns>true, if the validation is successful, otherwise false.</returns>
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
