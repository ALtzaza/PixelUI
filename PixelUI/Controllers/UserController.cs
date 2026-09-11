using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixelUI.Models.Db;

namespace PixelUI.Controllers
{
    public class UserController : Controller
    {
        private readonly PixeluiDbContext _context;
        private readonly IWebHostEnvironment _env;

        public UserController(PixeluiDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Get current user ID from session
        private int? GetUserId()
        {
            var userIdSession = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdSession) || !int.TryParse(userIdSession, out int userId))
            {
                return null;
            }
            return userId;
        }

        // Redirect to login if not authenticated
        private IActionResult CheckAuth()
        {
            if (GetUserId() == null)
            {
                return RedirectToAction("Login", "Home");
            }
            return null;
        }

        // My Profile Page
        public async Task<IActionResult> Profile()
        {
            var userIdSession = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdSession) || !int.TryParse(userIdSession, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            var tickets = await _context.Tickets
                .Where(t => t.UserId == userId && !t.IsHidden)
                .Include(t => t.Messages)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.Tickets = tickets;

            var verification = await _context.StudentVerifications
    .FirstOrDefaultAsync(s => s.UserId == userId);

            ViewBag.StudentStatus = verification?.Status;

            return View(user);
        }

        // My Purchases Page
        public async Task<IActionResult> MyPurchases()
        {
            var auth = CheckAuth();
            if (auth != null) return auth;

            var userId = GetUserId().Value;
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Product)
                .OrderByDescending(o => o.PurchaseDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpGet]
        public IActionResult SubmitStudentVerification()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitStudentVerification(StudentVerification model, IFormFile file)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Home");
            }

            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a file");
                return View(model);
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "student");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var path = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // ✅ ต้องอยู่ใน method นี้เท่านั้น
            model.UserId = userId;

            model.ProofImage = "/uploads/student/" + fileName;
            model.Status = "Pending";

            _context.StudentVerifications.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Profile");
        }

        // Downloads Page
        public async Task<IActionResult> Downloads()
        {
            var auth = CheckAuth();
            if (auth != null) return auth;

            var userId = GetUserId().Value;
            var downloads = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Product)
                .OrderByDescending(o => o.PurchaseDate)
                .ToListAsync();

            return View(downloads);
        }

        // POST: User/ReplyTicket
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyTicket(int ticketId, string message, IFormFile? attachment)
        {
            var userIdSession = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdSession) || !int.TryParse(userIdSession, out int userId))
            {
                return RedirectToAction("Login", "Home");
            }

            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.TicketId == ticketId && t.UserId == userId && !t.IsHidden);
            if (ticket == null) return NotFound();

            if (string.Equals(ticket.Status, "Closed", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "This ticket is closed and cannot be replied to.";
                return RedirectToAction("Profile");
            }

            var msg = new TicketMessage
            {
                TicketId = ticketId,
                UserId = userId,
                Message = message,
                IsAdmin = false,
                CreatedAt = DateTime.Now
            };

            if (attachment != null && attachment.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "tickets");
                Directory.CreateDirectory(uploads);
                var ext = Path.GetExtension(attachment.FileName);
                var fname = Guid.NewGuid().ToString() + ext;
                var fpath = Path.Combine(uploads, fname);
                using (var fs = System.IO.File.Create(fpath))
                {
                    await attachment.CopyToAsync(fs);
                }
                msg.AttachmentPath = "/uploads/tickets/" + fname;
            }

            await _context.TicketMessages.AddAsync(msg);
            ticket.UpdatedAt = DateTime.Now;
            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();

            return RedirectToAction("Profile");
        }
    }
}
