using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using lonnieDb.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace lonnieDb.Controllers
{
    public class EventController : Controller
    {
        private readonly LonnieDbContext _context;

        public EventController(LonnieDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _context.Events.Include(e => e.Venue).ToListAsync();
            return View(events);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var eventItem = await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (eventItem == null) return NotFound();

            return View(eventItem);
        }

        public IActionResult Create()
        {
            ViewData["VenueId"] = new SelectList(_context.Venues, "Id", "VenueName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event eventItem)
        {
            if (ModelState.IsValid)
            {
                // Prevent double booking on the same date
                var conflictingBooking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.VenueId == eventItem.VenueId && b.Date == eventItem.Date);

                if (conflictingBooking != null)
                {
                    TempData["Error"] = "The venue is already booked on the selected date.";
                    return View(eventItem);
                }

                _context.Add(eventItem);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Event created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["VenueId"] = new SelectList(_context.Venues, "Id", "VenueName", eventItem.VenueId);
            TempData["Error"] = "Please fill in all required fields.";
            return View(eventItem);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return NotFound();

            ViewData["VenueId"] = new SelectList(_context.Venues, "Id", "VenueName", eventItem.VenueId);
            return View(eventItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event eventItem)
        {
            if (id != eventItem.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Prevent double booking on the same date
                    var conflictingBooking = await _context.Bookings
                        .FirstOrDefaultAsync(b => b.VenueId == eventItem.VenueId && b.Date == eventItem.Date && b.EventId != eventItem.Id);

                    if (conflictingBooking != null)
                    {
                        TempData["Error"] = "The venue is already booked on the selected date.";
                        return View(eventItem);
                    }

                    _context.Update(eventItem);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Event updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Events.Any(e => e.Id == eventItem.Id)) return NotFound();
                    throw;
                }
            }

            ViewData["VenueId"] = new SelectList(_context.Venues, "Id", "VenueName", eventItem.VenueId);
            TempData["Error"] = "Please fill in all required fields.";
            return View(eventItem);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var eventItem = await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (eventItem == null) return NotFound();

            return View(eventItem);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eventItem = await _context.Events.Include(e => e.Bookings).FirstOrDefaultAsync(e => e.Id == id);

            if (eventItem == null)
            {
                TempData["Error"] = "Event not found.";
                return RedirectToAction(nameof(Index));
            }

            // Prevent deletion of events linked to bookings
            if (eventItem.Bookings.Any())
            {
                TempData["Error"] = "This event is linked to bookings and cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Event deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
