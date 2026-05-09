using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notely.Models;

namespace Notely.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public AdminController(AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        private async Task<bool> IsAdmin()
        {
            var user = await GetCurrentUser();
            return user != null && string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<ApplicationUser?> GetCurrentUser()
        {
            return await _userManager.GetUserAsync(User);
        }

        private async Task<IActionResult?> RequireAdmin()
        {
            if (!await IsAdmin())
                return StatusCode(403);

            return null;
        }

        public async Task<IActionResult> Index()
        {
            var guardResult = await RequireAdmin();
            if (guardResult != null)
                return guardResult;

            var model = new AdminDashboardViewModel
            {
                Users = await _context.Users
                    .OrderBy(u => u.Created_at)
                    .ToListAsync(),
                Notes = await _context.Notes
                    .Include(n => n.User)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var guardResult = await RequireAdmin();
            if (guardResult != null)
                return guardResult;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            var currentUser = await GetCurrentUser();
            if (currentUser == null)
                return StatusCode(403);

            if (currentUser.Id == user.Id)
            {
                TempData["AdminMessage"] = "You can’t delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                TempData["AdminMessage"] = "Admin accounts can’t be deleted.";
                return RedirectToAction(nameof(Index));
            }

            var notes = await _context.Notes
                .Where(n => n.UserId == user.Id)
                .ToListAsync();

            foreach (var note in notes)
            {
                DeleteImageFile(note.ImagePath);
            }

            _context.Notes.RemoveRange(notes);
            await _context.SaveChangesAsync();

            DeleteImageFile(user.ProfileImagepath);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNote(int id)
        {
            var guardResult = await RequireAdmin();
            if (guardResult != null)
                return guardResult;

            var note = await _context.Notes.FindAsync(id);

            if (note == null)
                return NotFound();

            DeleteImageFile(note.ImagePath);

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private void DeleteImageFile(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return;

            var fullPath = Path.Combine(
                _env.WebRootPath,
                imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
            );

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
