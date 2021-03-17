using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Improbability.Data;
using Improbability.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Improbability.Controller.v1
{
    [Route("api/[controller]")]
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

        private bool RandomItemExists(int id)
        {
            return _context.RandomItems.Any(e => e.Id == id);
        }
    }
}
