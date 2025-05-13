using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using lonnieDb.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace lonnieDb.Controllers
{
    public class BookingController : Controller
    {
        private readonly LonnieDbContext _context;

        public BookingController(LonnieDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings.Include(b => b.Event).Include(b => b.Venue).ToListAsync();
            return View(bookings);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        public IActionResult Create()
        {
            ViewData["EventId"] = new SelectList(_context.Events, "Id", "EventName");
            ViewData["VenueId"] = new SelectList(_context.Venues, "Id", "VenueName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            if (ModelState.IsValid)
            {
                // Prevent double booking on the same date and venue
                var conflictingBooking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.VenueId == booking.VenueId && b.Date == booking.Date);

                if (conflictingBooking != null)
                {
                    TempData["Error"] = "The venue is already booked for the selected date.";
                    return View(booking);
                }

                _context.Add(booking);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Booking created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["EventId"] = new SelectList(_context.Events, "Id", "EventName", booking.EventId);
            ViewData["VenueId"] = new SelectList(_context.Venues, "Id", "VenueName", booking.VenueId);
            TempData["Error"] = "Please fill in all required fields.";
            return View(booking);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            ViewData["EventId"] = new SelectList(_context.Events, "Id", "EventName", booking.EventId);
            ViewData["VenueId"] = new SelectList(_context.Venues, "Id", "VenueName", booking.VenueId);
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Booking booking)
        {
            if (id != booking.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Prevent double booking on the same date and venue
                    var conflictingBooking = await _context.Bookings
                        .FirstOrDefaultAsync(b => b.VenueId == booking.VenueId && b.Date == booking.Date && b.Id != booking.Id);

                    if (conflictingBooking != null)
                    {
                        TempData["Error"] = "The venue is already booked for the selected date.";
                        return View(booking);
                    }

                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Booking updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Bookings.Any(b => b.Id == booking.Id)) return NotFound();
                    throw;
                }
            }

            ViewData["EventId"] = new SelectList(_context.Events, "Id", "EventName", booking.EventId);
            ViewData["VenueId"] = new SelectList(_context.Venues, "Id", "VenueName", booking.VenueId);
            TempData["Error"] = "Please fill in all required fields.";
            return View(booking);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.Include(b => b.Event).FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction(nameof(Index));
            }

            // Prevent deletion of bookings linked to an event
            if (booking.Event != null)
            {
                TempData["Error"] = "This booking is linked to an event and cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Booking deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
