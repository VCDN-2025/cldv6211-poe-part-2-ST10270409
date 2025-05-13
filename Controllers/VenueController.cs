using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using lonnieDb.Models;
using Microsoft.AspNetCore.Http;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

namespace lonnieDb.Controllers
{
    public class VenueController : Controller
    {
        private readonly LonnieDbContext _context;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IConfiguration _configuration;

        // Constructor with BlobServiceClient injection
        public VenueController(LonnieDbContext context, IConfiguration configuration, BlobServiceClient blobServiceClient)
        {
            _context = context;
            _configuration = configuration;
            _blobServiceClient = blobServiceClient;  // Initialize BlobServiceClient
        }

        // Index action to list all venues
        public async Task<IActionResult> Index()
        {
            var venues = await _context.Venues.ToListAsync();
            return View(venues);
        }

        // Details action to show venue details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues
                .FirstOrDefaultAsync(m => m.Id == id);

            if (venue == null) return NotFound();

            return View(venue);
        }

        // Create action to display create venue form
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create action to create a new venue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Venue venue, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    try
                    {
                        // Upload the image to Azure Blob Storage
                        var blobClient = _blobServiceClient.GetBlobContainerClient(_configuration["BlobStorage:ContainerName"]);
                        var blob = blobClient.GetBlobClient(imageFile.FileName);

                        // Upload the file to the blob
                        using (var stream = imageFile.OpenReadStream())
                        {
                            await blob.UploadAsync(stream);
                        }

                        // Save the Blob URL to the venue's ImageUrl field
                        venue.ImageUrl = blob.Uri.AbsoluteUri;  // Storing the image URL in the model
                    }
                    catch (Exception ex)
                    {
                        // Log the exception (or handle it as needed)
                        ModelState.AddModelError("", $"Image upload failed: {ex.Message}");
                        return View(venue);
                    }
                }

                // Add venue to the database and save
                _context.Add(venue);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Venue created successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Please fill in all required fields.";
            return View(venue);
        }

        // Edit action to display venue editing form
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }
            return View(venue);
        }

        // POST: Edit action to update venue details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Venue venue)
        {
            if (id != venue.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(venue);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Venue updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Venues.Any(v => v.Id == venue.Id)) return NotFound();
                    throw;
                }
            }

            TempData["Error"] = "Please fill in all required fields.";
            return View(venue);
        }

        // Delete action to display the delete confirmation view
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues
                .FirstOrDefaultAsync(m => m.Id == id);

            if (venue == null) return NotFound();

            return View(venue);
        }

        // POST: Delete action to delete a venue
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var venue = await _context.Venues.Include(v => v.Bookings).FirstOrDefaultAsync(v => v.Id == id);

            if (venue == null)
            {
                TempData["Error"] = "Venue not found.";
                return RedirectToAction(nameof(Index));
            }

            // Prevent deletion of venues linked to bookings
            if (venue.Bookings.Any())
            {
                TempData["Error"] = "This venue is linked to bookings and cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            _context.Venues.Remove(venue);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Venue deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
