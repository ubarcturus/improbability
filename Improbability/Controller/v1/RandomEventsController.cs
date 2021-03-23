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
    public class RandomEventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RandomEventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/RandomEvents
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RandomEvent>>> GetRandomEvents()
        {
            return await _context.RandomEvents.ToListAsync();
        }

        // GET: api/RandomEvents/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RandomEvent>> GetRandomEvent(int id)
        {
            var randomEvent = await _context.RandomEvents.FindAsync(id);

            if (randomEvent == null)
            {
                return NotFound();
            }

            return randomEvent;
        }

        // PUT: api/RandomEvents/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRandomEvent(int id, RandomEvent randomEvent)
        {
            if (id != randomEvent.Id)
            {
                return BadRequest();
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

            return NoContent();
        }

        // POST: api/RandomEvents
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<RandomEvent>> PostRandomEvent(RandomEvent randomEvent)
        {
            _context.RandomEvents.Add(randomEvent);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRandomEvent", new { id = randomEvent.Id }, randomEvent);
        }

        // DELETE: api/RandomEvents/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRandomEvent(int id)
        {
            var randomEvent = await _context.RandomEvents.FindAsync(id);
            if (randomEvent == null)
            {
                return NotFound();
            }

            _context.RandomEvents.Remove(randomEvent);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RandomEventExists(int id)
        {
            return _context.RandomEvents.Any(e => e.Id == id);
        }
    }
}
