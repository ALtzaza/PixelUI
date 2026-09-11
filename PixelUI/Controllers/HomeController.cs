using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixelUI.Models.Db; // ตรวจสอบ Namespace ให้ตรงกับโปรเจกต์คุณ
using PixelUI.ViewModels;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;

namespace PixelUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly PixeluiDbContext _context;
        private readonly IWebHostEnvironment _env;

        public HomeController(PixeluiDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }




        // ใน UserController.cs
        public async Task<IActionResult> MyPurchases()
        {
            var userIdSession = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdSession) || !int.TryParse(userIdSession, out int userId))
            {
                return RedirectToAction("Login", "Home");
            }

            // ดึงรายการ Order ของ User คนนี้ออกมา
            var myOrders = await _context.Orders
                .Include(o => o.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.PurchaseDate)
                .ToListAsync();

            return View(myOrders);
        }
        public async Task<IActionResult> Index()
        {
            // 1. นับจำนวนสินค้าและผู้ใช้ (เหมือนเดิม)
            int productCount = await _context.Products.Where(p => p.Status == "Approved").CountAsync();
            int totalUsers = await _context.Users.CountAsync();

            ViewBag.productCount = productCount;
            ViewBag.totalUsers = totalUsers;

            // 2. ดึงข้อมูลสินค้าเฉพาะ Approved และเรียงตามยอดการสั่งซื้อ (Order Count)
            var products = await _context.Products
                .Where(p => p.Status == "Approved")
                .Select(p => new
                {
                    Product = p,
                    // นับจำนวน Order ที่เกี่ยวข้องกับ ProductID นี้
                    OrderCount = _context.Orders.Count(o => o.ProductId == p.ProductId)
                })
                .OrderByDescending(x => x.OrderCount) // กรองตัวที่ขายดีที่สุดขึ้นก่อน
                .ThenByDescending(x => x.Product.ProductId) // ถ้าขายได้เท่ากัน ให้เอาของใหม่ขึ้นก่อน
                .Take(6) // เอาแค่ 6 ชิ้นแรก
                .Select(x => x.Product)
                .ToListAsync();

            // Load active bundle promotions and their products
            var bundlePromos = await _context.Promotions
                .Where(p => p.Status == "Active" && p.PromotionType == "BundleDeal")
                .ToListAsync();

            var promoDisplay = new List<object>();
            foreach (var promo in bundlePromos)
            {
                var prods = new List<Product>();
                if (!string.IsNullOrEmpty(promo.BundleProductIds))
                {
                    var ids = promo.BundleProductIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => { int v; return int.TryParse(s, out v) ? v : -1; })
                                .Where(i => i > 0).ToList();
                    if (ids.Any())
                    {
                        prods = await _context.Products.Where(p => ids.Contains(p.ProductId)).ToListAsync();
                    }
                }
                promoDisplay.Add(new { Promo = promo, Products = prods });
            }

            ViewBag.BundlePromotions = promoDisplay;

            return View(products);
        }

        // Login GET
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Login POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // หาผู้ใช้จากอีเมลหรือชื่อผู้ใช้
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == model.Email || u.Username == model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            // ตรวจสอบรหัสผ่าน
            if (!VerifyPassword(model.Password, user.Password))
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            // บันทึก session
            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("UserRole", user.Role?.RoleName ?? "Member");
            HttpContext.Session.SetString("UserEmail", user.Email);

            var role = user.Role?.RoleName ?? "Member";
            // Cookie remember me (optional)
            if (model.RememberMe)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                };
                Response.Cookies.Append("UserId", user.UserId.ToString(), cookieOptions);
            }

            

switch (role)
{
    case "SuperAdmin":
        return RedirectToAction("Userlist", "Admin");

    case "Product Manager":
        return RedirectToAction("ManageProducts", "Admin");

    case "Marketing":
        return RedirectToAction("Analytics", "Admin");

    case "Customer Support":
        return RedirectToAction("Tickets", "Admin");

    default:
        return RedirectToAction("Index", "Home");
}

           
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Email not found");
                return View(model);
            }

            //  สร้าง Token
            var token = Guid.NewGuid().ToString();

            // TODO: เก็บ token ลง DB (แนะนำสร้าง table PasswordResetTokens)
            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.Now.AddMinutes(30);

            await _context.SaveChangesAsync();

            //  สร้างลิงก์
            var resetLink = Url.Action("ResetPassword", "Home",
                new { token = token, email = user.Email },
                Request.Scheme);

            // ตอนนี้ยังไม่ส่ง email -> แสดง link ไปก่อน
            ViewBag.Message = "Reset link sent to your email";
            ViewBag.ResetLink = resetLink;

            return View(model);
        }


        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            return View(new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.ResetToken == model.Token);

            if (user == null || user.ResetTokenExpiry < DateTime.Now)
            {
                ModelState.AddModelError("", "Invalid or expired token");
                return View(model);
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match");
                return View(model);
            }

            //  เปลี่ยนรหัส
            user.Password = HashPassword(model.NewPassword);

            // ล้าง token
            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        // Register GET
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Register POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ตรวจสอบว่าอีเมลมีผู้ใช้แล้วหรือไม่
            var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "Email is already in use");
                return View(model);
            }

            // ตรวจสอบว่า Username มีผู้ใช้แล้วหรือไม่
            var existingUsername = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (existingUsername != null)
            {
                ModelState.AddModelError("Username", "Username is already taken");
                return View(model);
            }

            // สร้าง Role ถ้าไม่มี (ค่าเริ่มต้น Member)
            var memberRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Member");
            if (memberRole == null)
            {
                memberRole = new Role { RoleName = "Member" };
                _context.Roles.Add(memberRole);
                await _context.SaveChangesAsync();
            }

            // สร้างผู้ใช้ใหม่
            var newUser = new User
            {
                Username = model.Username,
                Email = model.Email,
                Password = HashPassword(model.Password),
                RoleId = memberRole.RoleId
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // บันทึก session โดยอัตโนมัติ
            HttpContext.Session.SetString("UserId", newUser.UserId.ToString());
            HttpContext.Session.SetString("Username", newUser.Username);
            HttpContext.Session.SetString("UserRole", memberRole.RoleName);
            HttpContext.Session.SetString("UserEmail", newUser.Email);

            return RedirectToAction("Index");
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("UserId");
            return RedirectToAction("Index");
        }

        // Hash Password using SHA256
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Verify Password
        private bool VerifyPassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == hash;
        }

        // Checkout GET
        [HttpGet]
        public async Task<IActionResult> CheckOut(int? productId, string? promoCode, string? bundleIds)
        {
            if (!productId.HasValue)
            {
                return RedirectToAction("Index");
            }

            var userIdSession = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdSession) || !int.TryParse(userIdSession, out int userId))
            {
                return RedirectToAction("Login");
            }

            var product = await _context.Products.FindAsync(productId.Value);
            if (product == null || product.BasePrice == null || product.BasePrice == 0)
            {
                return RedirectToAction("Index");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            var viewModel = new CheckoutViewModel
            {
                ProductId = productId.Value,
                UserId = userId,
                ProductName = product.ProductName,
                ProductPrice = product.BasePrice ?? 0,
                UserEmail = user?.Email,
                UserName = user?.Username
            };

            // ดึง Active Promotions จากฐานข้อมูล
            var activePromotions = await _context.Promotions
                .Where(p => p.Status == "Active"
                    && p.StartDate <= DateTime.Now
                    && p.ExpiryDate >= DateTime.Now
                    && !string.IsNullOrEmpty(p.PromoCode))
                .ToListAsync();

            // ส่ง Promotions เป็น JSON string สำหรับ JavaScript (รวม usage count)
            var promosDict = activePromotions.ToDictionary(
                p => p.PromoCode,
                p => new { discount = p.DiscountPct ?? 0, currentUsage = p.CurrentUsageCount ?? 0, maxUsage = p.MaxUsageCount ?? 999 }
            );

            ViewBag.AvailablePromos = System.Text.Json.JsonSerializer.Serialize(promosDict);

            // If caller provided a promo code (e.g., coming from a bundle), prefill it
            if (!string.IsNullOrEmpty(promoCode))
            {
                ViewBag.InitialPromoCode = promoCode;
                // Precompute discount pct for display
                var promoObj = await _context.Promotions.FirstOrDefaultAsync(p => p.PromoCode.ToLower() == promoCode.ToLower() && p.Status == "Active");
                if (promoObj != null)
                {
                    ViewBag.InitialAppliedDiscount = promoObj.DiscountPct ?? 0;
                }
            }

            // If bundleIds provided, resolve products and compute subtotal for the bundle
            if (!string.IsNullOrEmpty(bundleIds))
            {
                var ids = bundleIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => { int v; return int.TryParse(s, out v) ? v : -1; })
                            .Where(i => i > 0).ToList();
                if (ids.Any())
                {
                    var bundleProducts = await _context.Products.Where(p => ids.Contains(p.ProductId)).ToListAsync();
                    ViewBag.BundleProducts = bundleProducts;
                    ViewBag.IsBundle = true;
                    ViewBag.BundleProductIds = bundleIds;
                    // set subtotal in the viewmodel product price for display
                    viewModel.ProductPrice = bundleProducts.Sum(p => p.BasePrice ?? 0);
                    viewModel.IsBundle = true;
                    viewModel.BundleProductIds = bundleIds;
                }
            }

            // If no promoCode provided but this is a bundle, try to find a promotion attached to the same bundle ids
            if (string.IsNullOrEmpty(ViewBag.InitialPromoCode as string) && !string.IsNullOrEmpty(bundleIds))
            {
                var promoForBundle = await _context.Promotions.FirstOrDefaultAsync(p => p.BundleProductIds == bundleIds && p.Status == "Active");
                if (promoForBundle != null)
                {
                    ViewBag.InitialPromoCode = promoForBundle.PromoCode;
                    ViewBag.InitialAppliedDiscount = promoForBundle.DiscountPct ?? 0;
                }
            }

            return View(viewModel);
        }

        // Bundle detail view - shows products included in a bundle promotion
        [HttpGet]
        public async Task<IActionResult> Bundle(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null)
            {
                return RedirectToAction("Index");
            }

            var prods = new List<Product>();
            if (!string.IsNullOrEmpty(promo.BundleProductIds))
            {
                var ids = promo.BundleProductIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => { int v; return int.TryParse(s, out v) ? v : -1; })
                            .Where(i => i > 0).ToList();
                if (ids.Any())
                {
                    prods = await _context.Products.Where(p => ids.Contains(p.ProductId)).ToListAsync();
                }
            }

            ViewBag.Promo = promo;
            ViewBag.BundleProducts = prods;

            return View();
        }

        // Checkout POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(CheckoutViewModel model)
        {
            var userIdSession = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdSession) || !int.TryParse(userIdSession, out int userId) || userId != model.UserId)
            {
                return RedirectToAction("Login");
            }

            // If bundle checkout, handle multiple products
            if (!string.IsNullOrEmpty(model.BundleProductIds))
            {
                var ids = model.BundleProductIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => { int v; return int.TryParse(s, out v) ? v : -1; })
                            .Where(i => i > 0).ToList();
                var products = await _context.Products.Where(p => ids.Contains(p.ProductId)).ToListAsync();
                if (products == null || !products.Any()) return NotFound();

                // Check if already purchased any of these products
                var existing = await _context.Orders
                    .Where(o => o.UserId == userId && o.ProductId.HasValue && ids.Contains(o.ProductId.Value))
                    .Select(o => o.ProductId.Value)
                    .ToListAsync();

                // Calculate subtotal and discount
                decimal subtotal = products.Sum(p => p.BasePrice ?? 0);
                decimal discountPct = model.AppliedDiscount;
                if (!string.IsNullOrEmpty(model.AppliedPromoCode))
                {
                    var promo = await _context.Promotions.FirstOrDefaultAsync(p => p.PromoCode.ToLower() == model.AppliedPromoCode.ToLower() && p.Status == "Active");
                    if (promo != null)
                    {
                        discountPct = promo.DiscountPct ?? discountPct;
                        promo.CurrentUsageCount = (promo.CurrentUsageCount ?? 0) + 1;
                        _context.Promotions.Update(promo);
                    }
                }

                decimal discountAmountTotal = subtotal * (discountPct / 100m);

                using (var tx = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        foreach (var p in products)
                        {
                            if (existing.Contains(p.ProductId)) continue; // skip already purchased
                            // proportional discount allocation
                            decimal prop = (p.BasePrice ?? 0) / (subtotal == 0 ? 1 : subtotal);
                            decimal itemDiscount = discountAmountTotal * prop;
                            var itemOrder = new Order
                            {
                                UserId = userId,
                                ProductId = p.ProductId,
                                FinalPrice = (p.BasePrice ?? 0) - itemDiscount,
                                PurchaseDate = DateTime.Now,
                                LicenseType = model.LicenseType
                            };
                            _context.Orders.Add(itemOrder);
                        }

                        await _context.SaveChangesAsync();
                        await tx.CommitAsync();
                    }
                    catch
                    {
                        await tx.RollbackAsync();
                        throw;
                    }
                }

                // Redirect to first created order's success page (approx)
                var firstOrder = await _context.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.PurchaseDate).FirstOrDefaultAsync();
                if (firstOrder != null) return RedirectToAction("PurchaseSuccess", new { orderId = firstOrder.OrderId });
                return RedirectToAction("MyPurchases", "Home");
            }

            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null)
            {
                return NotFound();
            }

            // Check if already purchased
            var existingOrder = await _context.Orders
                .FirstOrDefaultAsync(o => o.UserId == userId && o.ProductId == model.ProductId);
            if (existingOrder != null)
            {
                ViewBag.Message = "You have already purchased this product.";
                return View(model);
            }

            // ตรวจสอบและอัปเดต usage count ของ promo code (ถ้ามี)
            decimal discountAmount = 0;
            if (!string.IsNullOrEmpty(model.AppliedPromoCode))
            {
                var promo = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.PromoCode.ToLower() == model.AppliedPromoCode.ToLower()
                        && p.Status == "Active"
                        && p.StartDate <= DateTime.Now
                        && p.ExpiryDate >= DateTime.Now);

                if (promo != null)
                {
                    // เช็ค usage limit
                    if (promo.MaxUsageCount.HasValue && (promo.CurrentUsageCount ?? 0) >= promo.MaxUsageCount)
                    {
                        ViewBag.Message = "This promo code has reached its usage limit.";
                        return View(model);
                    }

                    // คำนวณ discount
                    discountAmount = (product.BasePrice ?? 0) * ((promo.DiscountPct ?? 0) / 100m);

                    // อัปเดต usage count
                    promo.CurrentUsageCount = (promo.CurrentUsageCount ?? 0) + 1;
                    _context.Promotions.Update(promo);
                }
            }

            // Create new order
            var order = new Order
            {
                UserId = userId,
                ProductId = model.ProductId,
                FinalPrice = (product.BasePrice ?? 0) - discountAmount,
                PurchaseDate = DateTime.Now,
                LicenseType = model.LicenseType
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return RedirectToAction("PurchaseSuccess", new { orderId = order.OrderId });
        }

        // Purchase Success
        public async Task<IActionResult> PurchaseSuccess(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpGet]
        public IActionResult CreateTicket()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTicket(string subject, string message, int? productId, IFormFile? attachment)
        {
            var userIdSession = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdSession) || !int.TryParse(userIdSession, out int userId))
            {
                return RedirectToAction("Login", "Home");
            }

            // If this report came from a product page, include product context in the message
            string composedMessage = message;
            if (productId.HasValue)
            {
                var prod = await _context.Products.FindAsync(productId.Value);
                if (prod != null)
                {
                    composedMessage = $"[Product] {prod.ProductName} (#{prod.ProductId})\n\n" + message;
                }
                else
                {
                    composedMessage = $"[Product] #{productId.Value}\n\n" + message;
                }
            }

            var ticket = new Ticket
            {
                UserId = userId,
                Subject = subject,
                Message = composedMessage,
                Status = "Open",
                Priority = "Normal",
                CreatedAt = DateTime.Now
            };

            // Save product association when reporting from a product page
            if (productId.HasValue)
            {
                ticket.ProductId = productId.Value;
            }

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // ✅ สร้าง message แรก
            var firstMsg = new TicketMessage
            {
                TicketId = ticket.TicketId,
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
                firstMsg.AttachmentPath = "/uploads/tickets/" + fname;
            }

            _context.TicketMessages.Add(firstMsg);
            await _context.SaveChangesAsync();

            return RedirectToAction("Profile", "User");
        }

        // GET: Report a bug page
        [HttpGet]
        public IActionResult ReportBug()
        {
            return View();
        }

        // POST: Submit a bug report from the standalone ReportBug page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportBug(string message)
        {
            int? userId = null;
            var userIdSession = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdSession) && int.TryParse(userIdSession, out int parsed))
            {
                userId = parsed;
            }

            var ticket = new Ticket
            {
                UserId = userId,
                Subject = "Bug Report",
                Message = message,
                Status = "Pending",
                CreatedAt = DateTime.Now,
                ProductId = null
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thank you — your bug report was submitted.";
            return RedirectToAction("ReportBug");
        }
    }
}