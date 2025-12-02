using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyProject.Data;
using MyProject.Service;
using Microsoft.EntityFrameworkCore;

namespace MyProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class ProductImageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public ProductImageController(ApplicationDbContext context, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image == null)
            {
                return NotFound();
            }

            try
            {
                // Delete from Cloudinary
                await _cloudinaryService.DeleteImageAsync(image.ImageUrl);

                // Delete from database
                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error deleting image: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimary(int id)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image == null)
            {
                return NotFound();
            }

            try
            {
                // Remove primary flag from all images of this product
                var productImages = await _context.ProductImages
                    .Where(pi => pi.ProductId == image.ProductId)
                    .ToListAsync();

                foreach (var img in productImages)
                {
                    img.IsPrimary = false;
                }

                // Set this image as primary
                image.IsPrimary = true;

                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error setting primary image: {ex.Message}" });
            }
        }
    }
}
