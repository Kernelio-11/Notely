
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Notely.Models;
namespace Notely.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _context;

        public AccountController(
           UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment env,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
            _context = context;
        }

        [HttpGet]
        
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Notes");
            }

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Notes");
            }

            if (!ModelState.IsValid)
                return View(model);
            string? filePath = null;

            if (model.ProfileImage != null)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "imgs");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProfileImage.FileName);

                var fullPath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(stream);
                }

                filePath = "/imgs/" + fileName;
            }

            bool hasAdmin = await _context.Users.AnyAsync(u => u.Role == "Admin");

            var user = new ApplicationUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.Email,
                Email = model.Email,
                ProfileImagepath = filePath,
                Role = hasAdmin ? "User" : "Admin",
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: true);
                TempData["Toast"] = "Welcome to Notely! 🎉";

                return RedirectToAction("Index", "Notes");
            }

            foreach (var error in result.Errors)
            {
                if (error.Code == "DuplicateUserName")
                    continue;
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Notes");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Notes");
            }

            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                TempData["Toast"] = "Welcome back! 👋";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Notes");
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var notes = _context.Notes
                .Where(n => n.UserId == user.Id)
                .ToList();

            var model = new ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ProfileImagePath = user.ProfileImagepath,
                Notes = notes
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string FirstName, string LastName, string Email, IFormFile? ProfileImage)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login");

            user.FirstName = FirstName;
            user.LastName = LastName;
            user.Email = Email;
            user.UserName = Email;

            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                if (!string.IsNullOrEmpty(user.ProfileImagepath))
                {
                    var oldImagePath = Path.Combine(
                        _env.WebRootPath,
                        user.ProfileImagepath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
                    );

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string uploadsFolder = Path.Combine(_env.WebRootPath, "imgs");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ProfileImage.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(stream);
                }

                user.ProfileImagepath = "/imgs/" + fileName;
            }

            await _userManager.UpdateAsync(user);
            return RedirectToAction("Profile");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login");

            // get all user notes
            var notes = _context.Notes
                .Where(n => n.UserId == user.Id)
                .ToList();

            // delete note images
            foreach (var note in notes)
            {
                if (!string.IsNullOrEmpty(note.ImagePath))
                {
                    var imagePath = Path.Combine(
                        _env.WebRootPath,
                        note.ImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)

                    );

                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
            }


            _context.Notes.RemoveRange(notes);
            await _context.SaveChangesAsync();

            // delete user image
            if (!string.IsNullOrEmpty(user.ProfileImagepath))
            {
                var profilePath = Path.Combine(
                    _env.WebRootPath,
                    user.ProfileImagepath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
                );

                if (System.IO.File.Exists(profilePath))
                {
                    System.IO.File.Delete(profilePath);
                }
            }

            await _signInManager.SignOutAsync();
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }
    }
}
