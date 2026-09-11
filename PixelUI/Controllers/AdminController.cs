using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PixelUI.ViewModels; // เรียกใช้ ViewModel ที่สร้างใหม่
using PixelUI.Models.Db;   // เรียกใช้ Model จากฐานข้อมูล


namespace PixelUI.Controllers
{
    public class AdminController : Controller
    {
        private readonly PixeluiDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminController(PixeluiDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Restrict Admin area: only SuperAdmin and Customer Support may access
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userRole = context.HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || (userRole != "SuperAdmin" && userRole != "Customer Support" && userRole != "Product Manager" && userRole != "Marketing"))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            await next();
        }

        public async Task<IActionResult> Userlist()
        {
            // Check role - only SuperAdmin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin")
            {
                return Unauthorized();
            }

            // ใช้ .Include(u => u.Role) เพื่อไปดึงข้อมูลจากตาราง Role มาด้วย
            var users = await _context.Users
                                      .Include(u => u.Role)
                                      .Where(u => u.Role.RoleName == "Member")
                                      .ToListAsync();


            // Send roles to view for the create user modal
            ViewBag.Roles = await _context.Roles.ToListAsync();

            return View(users);
        }

        private string HashPassword(string password)
{
    using (var sha256 = System.Security.Cryptography.SHA256.Create())
    {
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}

        // GET: Admin/CreateUser (removed - using modal form instead)

        // POST: Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser([Bind("Username,Email,Password,RoleId")] User user)
        {
            // Check role - only SuperAdmin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin")
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                // Check if username already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == user.Username);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "Username already exists");
                    ViewBag.Roles = await _context.Roles.ToListAsync();
                    return RedirectToAction(nameof(Userlist));
                }

                user.Password = HashPassword(user.Password);

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Userlist));
            }

            ViewBag.Roles = await _context.Roles.ToListAsync();
            return RedirectToAction(nameof(Userlist));
        }

        // --- เพิ่มต่อจาก Action เดิมใน AdminController ---

        // GET: Admin/CreateProduct
        public IActionResult CreateProduct()
        {
            // Check role - SuperAdmin or ProductManager
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Product Manager")
            {
                return Unauthorized();
            }

            return View();
        }

        // POST: Admin/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct([Bind("ProductName,BasePrice,ComplexityTier,TechStack,HtmlCode,CssCode,Category,Description,IncludedItems,Highlights,Format")] Product product)
        {
            // Check role - SuperAdmin or ProductManager
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Product Manager")
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                // Auto-populate Author from logged-in user
                var username = HttpContext.Session.GetString("Username");
                product.Author = username;
                product.Status = "Pending"; // Set status as Pending for QC

                _context.Add(product);
                await _context.SaveChangesAsync();

                // บันทึกเสร็จ และไปที่หน้า QC & Approve
                return RedirectToAction(nameof(QCApprove));
            }
            return View(product);
        }

        // GET: QC & Approve Products
        public async Task<IActionResult> QCApprove()
        {
            // Check role - SuperAdmin only
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Product Manager")
            {
                return Unauthorized();
            }

            var pendingProducts = await _context.Products
                .Where(p => p.Status == "Pending")
                .ToListAsync();

            return View(pendingProducts);
        }

        // POST: Approve Product
        [HttpPost]
        public async Task<IActionResult> ApproveProduct(int id)
        {
            // Check role - SuperAdmin only
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Product Manager")
            {
                return Unauthorized();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.Status = "Approved";
            _context.Update(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(QCApprove));
        }

        // POST: Reject Product
        [HttpPost]
        public async Task<IActionResult> RejectProduct(int id)
        {
            // Check role - SuperAdmin only
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Product Manager")
            {
                return Unauthorized();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.Status = "Rejected";
            _context.Update(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(QCApprove));
        }

        // GET: Edit User
        public async Task<IActionResult> EditUser(int id)
        {
            // Check role - only SuperAdmin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin")
            {
                return Unauthorized();
            }

            var user = await _context.Users
                                     .Include(u => u.Role)
                                     .FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Update User
        [HttpPost]
        public async Task<IActionResult> UpdateUser(int id, [Bind("UserId,Username,Email,RoleId,Password")] User user)
        {
            // Check role - only SuperAdmin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin")
            {
                return Unauthorized();
            }

            if (id != user.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Userlist));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View("EditUser", user);
        }

        // POST: Delete User
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // Check role - only SuperAdmin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin")
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Userlist));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }

        // GET: Admin/ManageStaffRoles
        public async Task<IActionResult> ManageStaffRoles()
        {
            // Check role - only SuperAdmin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin")
            {
                return Unauthorized();
            }

            var staffUsers = await _context.Users
                                          .Include(u => u.Role)
                                                  .Where(u =>
            u.Role.RoleName == "Customer Support" ||
            u.Role.RoleName == "Marketing" ||
            u.Role.RoleName == "Product Manager" ||
            u.Role.RoleName == "SuperAdmin"
        )
                                          .ToListAsync();
            ViewBag.Roles = await _context.Roles.ToListAsync();
            return View(staffUsers);
        }

        // POST: Admin/UpdateStaffRole
        [HttpPost]
        public async Task<IActionResult> UpdateStaffRole(int userId, int roleId)
        {
            // Check role - only SuperAdmin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin")
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            user.RoleId = roleId;
            _context.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ManageStaffRoles));
        }

        [HttpGet]
        public async Task<IActionResult> PreviewProduct(int id)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "ProductManager") return Unauthorized();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return View(product); // ตรวจสอบว่าไฟล์อยู่ที่ Views/Admin/PreviewProduct.cshtml
        }

        // GET: Admin/ManageProducts
        public async Task<IActionResult> ManageProducts()
        {
            // Check role - SuperAdmin or ProductManager
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Product Manager")
            {
                return Unauthorized();
            }

            var products = await _context.Products.ToListAsync();
            return View(products);
        }

        // GET: Admin/EditProduct
        public async Task<IActionResult> EditProduct(int id)
        {
            // Check role - SuperAdmin or ProductManager
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Product Manager")
            {
                return Unauthorized();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Admin/EditProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id,
            [Bind("ProductId,ProductName,BasePrice,ComplexityTier,TechStack,HtmlCode,CssCode,Category,Description,IncludedItems,Highlights,Format")] Product product)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Product Manager")
            {
                return Unauthorized();
            }

            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    //  ดึงของเดิมจาก DB มาก่อน
                    var existingProduct = await _context.Products.FindAsync(id);
                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    //  update เฉพาะ field ที่ต้องการ
                    existingProduct.ProductName = product.ProductName;
                    existingProduct.BasePrice = product.BasePrice;
                    existingProduct.ComplexityTier = product.ComplexityTier;
                    existingProduct.TechStack = product.TechStack;
                    existingProduct.HtmlCode = product.HtmlCode;
                    existingProduct.CssCode = product.CssCode;
                    existingProduct.Description = product.Description;
                    existingProduct.IncludedItems = product.IncludedItems;
                    existingProduct.Highlights = product.Highlights;
                    existingProduct.Format = product.Format;

                    // ❗ ไม่แตะ field สำคัญ เช่น Author, Status, ViewCount

                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(ManageProducts));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(product);
        }
        // POST: Admin/DeleteProduct
        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            // Check role - SuperAdmin or ProductManager
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Product Manager")
            {
                return Unauthorized();
            }

            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ManageProducts));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }


// GET
public IActionResult CreateSection()
{
    return View(new Product());
}
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateSection(Product product)
{
    product.TemplateType = 2; // Section
    product.TierLevel = 2;
    product.Status = "Pending";

    product.Author = HttpContext.Session.GetString("Username");

    _context.Products.Add(product);
    await _context.SaveChangesAsync();

    return RedirectToAction("ManageProducts");
}

        // GET: Admin/FinancialOverview
        public async Task<IActionResult> FinancialOverview()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin") return Unauthorized();

            var orders = await _context.Orders
                .Include(o => o.Product)
                .Include(o => o.User)
                .OrderByDescending(o => o.PurchaseDate)
                .ToListAsync();

            // คำนวณรายได้
            decimal totalRevenue = orders.Sum(o => o.FinalPrice ?? 0);

            // สถิติแยกตาม License Type (นับเฉพาะที่มีค่า)
            ViewBag.PersonalCount = orders.Count(o => o.LicenseType == "Personal");
            ViewBag.CommercialCount = orders.Count(o => o.LicenseType == "Commercial");
            ViewBag.StandardCount = orders.Count(o => o.LicenseType == "Standard");

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.PendingPayout = totalRevenue * 0.9m; 
            ViewBag.OrderCount = orders.Count;

            return View(orders);
        }

        // GET: Admin/Analytics - View vs Conversion Analysis
        public async Task<IActionResult> Analytics()
        {
            // Check role - SuperAdmin or ProductManager
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Marketing")
            {
                return Unauthorized();
            }

            // Get all products with their view counts and conversion data
            var products = await _context.Products.ToListAsync();

            // Create analytics data for each product
            var analyticsData = new List<dynamic>();

            foreach (var product in products)
            {
                var conversions = await _context.Orders
                    .Where(o => o.ProductId == product.ProductId)
                    .CountAsync();

                var conversionRate = product.ViewCount > 0
                    ? Math.Round((decimal)conversions / product.ViewCount * 100, 2)
                    : 0;

                analyticsData.Add(new
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ViewCount = product.ViewCount,
                    Conversions = conversions,
                    ConversionRate = conversionRate,
                    BasePrice = product.BasePrice,
                    Category = product.Category
                });
            }

            // Calculate summary statistics
            var totalViews = analyticsData.Sum(x => (int)x.ViewCount);
            var totalConversions = analyticsData.Sum(x => (int)x.Conversions);
            var averageConversionRate = analyticsData.Count > 0
                ? analyticsData.Average(x => (decimal)x.ConversionRate)
                : 0;

            ViewBag.TotalViews = totalViews;
            ViewBag.TotalConversions = totalConversions;
            ViewBag.AverageConversionRate = Math.Round(averageConversionRate, 2);
            ViewBag.TotalProducts = analyticsData.Count;

            return View(analyticsData);
        }

        // GET: Admin/ManagePromotions
        public async Task<IActionResult> ManagePromotions()
        {
            // Check role - SuperAdmin only
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Marketing")
            {
                return Unauthorized();
            }

            var promotions = await _context.Promotions.ToListAsync();
            return View(promotions);
        }

        // GET: Admin/CreatePromotion
        public async Task<IActionResult> CreatePromotion()
        {
            // Check role - SuperAdmin only
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Marketing")
            {
                return Unauthorized();
            }

            var viewModel = new PromotionViewModel();
            viewModel.AvailableProducts = await _context.Products
                .Select(p => new ProductDropdownItem
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    BasePrice = p.BasePrice,
                    Category = p.Category
                })
                .ToListAsync();

            return View(viewModel);
        }

        // POST: Admin/CreatePromotion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePromotion(PromotionViewModel viewModel)
        {
            // Check role - SuperAdmin only
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Marketing")
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // ✅ จัดการ PromoCode
                    if (viewModel.PromotionType == "BundleDeal")
                    {
                        viewModel.PromoCode = null;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(viewModel.PromoCode))
                        {
                            viewModel.PromoCode = GeneratePromoCode(viewModel.PromotionType);
                        }

                        // เช็คซ้ำ
                        var existingPromo = await _context.Promotions
                            .FirstOrDefaultAsync(p => p.PromoCode == viewModel.PromoCode);

                        if (existingPromo != null)
                        {
                            ModelState.AddModelError("PromoCode", "Promo code already exists!");

                            viewModel.AvailableProducts = await _context.Products
                                .Select(p => new ProductDropdownItem
                                {
                                    ProductId = p.ProductId,
                                    ProductName = p.ProductName,
                                    BasePrice = p.BasePrice,
                                    Category = p.Category
                                })
                                .ToListAsync();

                            return View(viewModel);
                        }
                    }

                    var promotion = new Promotion
                    {
                        PromoCode = viewModel.PromoCode,
                        DiscountPct = viewModel.DiscountPct,
                        PromotionType = viewModel.PromotionType,
                        Description = viewModel.Description,
                        Conditions = viewModel.Conditions,
                        BundleProductIds = viewModel.SelectedProductIds != null
                            ? string.Join(",", viewModel.SelectedProductIds)
                            : null,
                        Status = "Active",
                        CreatedDate = DateTime.Now,
                        StartDate = viewModel.StartDate,
                        ExpiryDate = viewModel.ExpiryDate,
                        MaxUsageCount = viewModel.MaxUsageCount,
                        CurrentUsageCount = 0
                    };

                    _context.Promotions.Add(promotion);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Promotion '{viewModel.PromoCode}' created successfully!";
                    return RedirectToAction(nameof(ManagePromotions));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating promotion: {ex.Message}");
                }
            }

            viewModel.AvailableProducts = await _context.Products
                .Select(p => new ProductDropdownItem
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    BasePrice = p.BasePrice
                })
                .ToListAsync();

            return View(viewModel);
        }



        // GET: /Admin/Tickets
        public async Task<IActionResult> Tickets()
        {
            var tickets = await _context.Tickets
                .Include(t => t.User)
                .Include(t => t.Product)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Messages)
                .Where(t => !t.IsHidden && t.Status != "Resolved")
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(tickets);
        }

        [HttpGet]
        public async Task<IActionResult> TicketDetail(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.User)
                .Include(t => t.Product)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Messages)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
                return NotFound();

            return View(ticket);
        }

        // (Ticket message history endpoint removed)

        // POST: /Admin/ReplyTicket/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyTicket(int id, string message, IFormFile? attachment)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            var userId = HttpContext.Session.GetInt32("UserId");


            var msg = new TicketMessage
            {
                TicketId = id,
                UserId = userId,
                Message = message,
                IsAdmin = true,
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

            _context.TicketMessages.Add(msg);

            ticket.Status = "InProgress";
            ticket.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("TicketDetail", new { id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToMe(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            var userId = HttpContext.Session.GetInt32("UserId");

            ticket.AssignedToUserId = userId;
            ticket.Status = "InProgress";

            await _context.SaveChangesAsync();

            // If AJAX request, return JSON so client can update UI without reload
            if (Request.Headers != null && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, status = ticket.Status, assignedTo = userId });
            }

            return RedirectToAction("TicketDetail", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HideTicket(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
                return NotFound();

            ticket.IsHidden = true;

            await _context.SaveChangesAsync();

            return RedirectToAction("Tickets");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseTicket(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            ticket.Status = "Closed";
            ticket.ClosedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            if (Request.Headers != null && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, status = ticket.Status, closedAt = ticket.ClosedAt });
            }

            return RedirectToAction("TicketDetail", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveTicket(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            ticket.Status = "Resolved";
            ticket.ClosedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            if (Request.Headers != null && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, status = ticket.Status, closedAt = ticket.ClosedAt });
            }

            return RedirectToAction("TicketDetail", new { id });
        }

        // GET: Admin/EditPromotion
        public async Task<IActionResult> EditPromotion(int id)
        {
            // Check role - SuperAdmin only
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Marketing")
            {
                return Unauthorized();
            }

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }

            var viewModel = new PromotionViewModel
            {
                PromotionId = promotion.PromotionId,
                PromoCode = promotion.PromoCode,
                DiscountPct = promotion.DiscountPct,
                PromotionType = promotion.PromotionType,
                Description = promotion.Description,
                Conditions = promotion.Conditions,
                Status = promotion.Status,
                StartDate = promotion.StartDate ?? DateTime.Now,
                ExpiryDate = promotion.ExpiryDate ?? DateTime.Now.AddDays(30),
                MaxUsageCount = promotion.MaxUsageCount,
                SelectedProductIds = !string.IsNullOrEmpty(promotion.BundleProductIds)
                    ? promotion.BundleProductIds.Split(',').Select(int.Parse).ToList()
                    : new List<int>()
            };

            viewModel.AvailableProducts = await _context.Products
                .Select(p => new ProductDropdownItem
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    BasePrice = p.BasePrice,
                    Category = p.Category
                })
                .ToListAsync();

            return View(viewModel);
        }

        // POST: Admin/EditPromotion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPromotion(int id, PromotionViewModel viewModel)
        {
            // Check role - SuperAdmin only
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Marketing")
            {
                return Unauthorized();
            }

            if (id != viewModel.PromotionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var promotion = await _context.Promotions.FindAsync(id);
                    if (promotion == null)
                    {
                        return NotFound();
                    }

                    promotion.DiscountPct = viewModel.DiscountPct;
                    promotion.Description = viewModel.Description;
                    promotion.Conditions = viewModel.Conditions;
                    promotion.BundleProductIds = viewModel.SelectedProductIds != null
                        ? string.Join(",", viewModel.SelectedProductIds)
                        : null;
                    promotion.Status = viewModel.Status;
                    promotion.StartDate = viewModel.StartDate;
                    promotion.ExpiryDate = viewModel.ExpiryDate;
                    promotion.MaxUsageCount = viewModel.MaxUsageCount;

                    _context.Promotions.Update(promotion);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Promotion updated successfully!";
                    return RedirectToAction(nameof(ManagePromotions));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating promotion: {ex.Message}");
                }
            }

            viewModel.AvailableProducts = await _context.Products
                .Select(p => new ProductDropdownItem
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    BasePrice = p.BasePrice,
                    Category = p.Category
                })
                .ToListAsync();

            return View(viewModel);
        }

        // POST: Admin/DeletePromotion
        [HttpPost]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            // Check role - SuperAdmin only
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Marketing")
            {
                return Unauthorized();
            }

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Promotion deleted successfully!";
            return RedirectToAction(nameof(ManagePromotions));
        }

        // GET: Admin/StudentVerification
        public async Task<IActionResult> StudentVerification()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Customer Support")
            {
                return Unauthorized();
            }

            var list = await _context.StudentVerifications
                .Include(s => s.User)
                 .Where(s => s.Status == "Pending")
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveStudent(int id)
        {
            var item = await _context.StudentVerifications
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (item == null) return NotFound();

            //  อัปเดตสถานะ
            item.Status = "Approved";

            //  อัปเดต Role User 
            item.User.RoleId = 6; 
            

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(StudentVerification));
        }
        [HttpPost]
        public async Task<IActionResult> RejectStudent(int id)
        {
            var item = await _context.StudentVerifications.FindAsync(id);
            if (item == null) return NotFound();

            item.Status = "Rejected";

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(StudentVerification));
        }

        // Helper method to generate promo code
        private string GeneratePromoCode(string? promotionType)
        {
            var prefix = promotionType?.Substring(0, 3).ToUpper() ?? "PROMO";
            var randomPart = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            return $"{prefix}-{randomPart}";
        }
    }
}

