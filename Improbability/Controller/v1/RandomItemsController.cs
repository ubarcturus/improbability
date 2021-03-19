using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Improbability.Data;
using Improbability.Models;
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

        // GET: api/RandomItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RandomItem>>> GetRandomItems()
        {
            return await _context.RandomItems.ToListAsync();
        }

        // GET: api/RandomItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RandomItem>> GetRandomItem(int id)
        {
            var randomItem = await _context.RandomItems.FindAsync(id);

            if (randomItem == null)
            {
                return NotFound();
            }

            return randomItem;
        }

        // PUT: api/RandomItems/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRandomItem(int id, RandomItem randomItem)
        {
            if (id != randomItem.Id)
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

            return NoContent();
        }

        // POST: api/RandomItems
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<RandomItem>> PostRandomItem(RandomItem randomItem)
        {
            _context.RandomItems.Add(randomItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRandomItem", new { id = randomItem.Id }, randomItem);
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

        /// <summary>
        /// Checks if the authorization value in the header is valid, if the api-key is assigned to a user.
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

        // private bool RandomItemExists(int id)
        // {
        //     return _context.RandomItems.Any(e => e.Id == id);
        // }
    }
}
