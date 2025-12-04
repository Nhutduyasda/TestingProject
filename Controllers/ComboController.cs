using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyProject.Data;
using MyProject.Models.Shared;

namespace MyProject.Controllers
{
    public class ComboController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ComboController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var combos = await _context.Combos
                .Include(c => c.ComboProducts)
                    .ThenInclude(cp => cp.Product)
                        .ThenInclude(p => p.Images)
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            return View(combos);
        }

        public async Task<IActionResult> Details(int id)
        {
            var combo = await _context.Combos
                .Include(c => c.ComboProducts)
                    .ThenInclude(cp => cp.Product)
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.ComboId == id && c.IsActive && !c.IsDeleted);

            if (combo == null)
            {
                return NotFound();
            }

            return View(combo);
        }
    }
}
