using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using PixelUI.ViewModels; // เรียกใช้ ViewModel ที่สร้างใหม่
using PixelUI.Models.Db;   // เรียกใช้ Model จากฐานข้อมูล
using System.IO.Compression;

namespace PixelUI.Controllers
{
    public class ProductController : Controller
    {
        private readonly PixeluiDbContext _context;

        public ProductController(PixeluiDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Browse(string category)
        {
            // เริ่มต้นที่การเตรียมดึงข้อมูลทั้งหมด - เฉพาะ Approved products
            var query = _context.Products.Where(p => p.Status == "Approved").AsQueryable();

            // ถ้ามีการส่งค่าหมวดหมู่มา (เช่น ?category=Loaders) ให้กรองตามคอลัมน์ Category เป๊ะๆ
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category == category);
                ViewBag.Title = category;
            }
            else
            {
                ViewBag.Title = "Browse all"; // default
            }

            var products = await query.ToListAsync();

            // ส่งข้อมูลไปโชว์ที่หน้าเดิม
            return View(products);
        }
        public async Task<IActionResult> Loader()
        {
            // กรองเอาเฉพาะสินค้าที่มีคำว่า 'Loader' อยู่ในชื่อ และ Status = Approved
            var loaders = await _context.Products
                .Where(p => (p.ProductName.Contains("Loader") || p.ProductName.Contains("New2") || p.ProductName.Contains("Achi"))
                         && p.Status == "Approved")
                .ToListAsync();

            // ส่งไปที่หน้า Browse.cshtml เพื่อใช้ Layout เดิม แต่ข้อมูลเปลี่ยนไป
            return View("Browse", loaders);
        }

        // GET: Admin/PreviewProduct/5
        public async Task<IActionResult> PreviewProduct(int id)
        {
            // เช็คสิทธิ์ว่าเป็น Admin หรือ SuperAdmin หรือไม่
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "SuperAdmin" && userRole != "Admin")
            {
                return Unauthorized();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product); // จะวิ่งไปหาไฟล์ Views/Admin/PreviewProduct.cshtml
        }

        public async Task<IActionResult> Detail(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            //ดึงข้อมูล User จาก Session
            var userRole = HttpContext.Session.GetString("UserRole");

            // Logic ใหม่: ถ้าไม่ใช่ Approved "และ" ไม่ใช่ Admin/SuperAdmin ให้บล็อก
            if (product.Status != "Approved" && userRole != "Admin" && userRole != "SuperAdmin")
            {
                return NotFound();
            }

            // --- ส่วนที่เหลือคงเดิม ---
            // Increment view count (อาจจะเช็คไม่ให้นับยอดวิวถ้าเป็น Admin ดูเองก็ได้)
            if (userRole != "Admin" && userRole != "SuperAdmin")
            {
                product.ViewCount++;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
            }

            var userIdSession = HttpContext.Session.GetString("UserId");
            User currentUser = null;
            bool isLoggedIn = false;
            bool isPurchased = false;

            // ถ้าเป็นฟรี หรือ เป็น Admin ให้ถือว่า "ซื้อแล้ว/เข้าถึงได้" เสมอ
            if (product.BasePrice == null || product.BasePrice == 0 || userRole == "Admin" || userRole == "SuperAdmin")
            {
                isPurchased = true;
            }

            if (!string.IsNullOrEmpty(userIdSession) && int.TryParse(userIdSession, out int parsedUserId))
            {
                currentUser = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == parsedUserId);

                if (currentUser != null)
                {
                    isLoggedIn = true;
                    if (!isPurchased)
                    {
                        isPurchased = await _context.Orders
                            .AnyAsync(o => o.UserId == parsedUserId && o.ProductId == id);
                    }
                }
            }

            // Browser compatibility ข้อมูลสมมติ
            var browserCompatibility = new List<BrowserCompatibility>
    {
        new BrowserCompatibility { BrowserName = "Chrome", MinVersion = "90+", IsSupported = true },
        new BrowserCompatibility { BrowserName = "Firefox", MinVersion = "88+", IsSupported = true },
        new BrowserCompatibility { BrowserName = "Safari", MinVersion = "14+", IsSupported = true },
        new BrowserCompatibility { BrowserName = "Edge", MinVersion = "90+", IsSupported = true }
    };

            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                ScopedCssCode = $"#scoped-preview-{product.ProductId} {{ {product.CssCode} }}",
                CurrentUser = currentUser,
                UserRole = userRole ?? "Guest",
                IsLoggedIn = isLoggedIn,
                IsPurchased = isPurchased,
                BrowserCompatibility = browserCompatibility
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Download(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var userRole = HttpContext.Session.GetString("UserRole");

            // 1. เช็คเงื่อนไขพื้นฐาน
            bool isFree = product.BasePrice == null || product.BasePrice == 0;
            bool isAdmin = userRole == "SuperAdmin";

            // 2. ถ้าเป็น SuperAdmin หรือ ของฟรี ให้ข้ามไปโหลดได้เลย
            // (ใช้ Logic ที่เราคุยกันคือข้ามส่วนการเช็ค Order ไปเลย)
            if (!isFree && !isAdmin)
            {
                var userIdSession = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdSession) || !int.TryParse(userIdSession, out int parsedUserId))
                {
                    return Unauthorized();
                }

                // เช็คว่า Member ทั่วไปซื้อหรือยัง
                bool hasPurchased = await _context.Orders
                    .AnyAsync(o => o.UserId == parsedUserId && o.ProductId == id);

                if (!hasPurchased)
                {
                    return Unauthorized();
                }
            }

            // --- ส่วนการสร้างไฟล์ ZIP (โค้ดเดิมของอาชิ) ---
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    // ใส่ HTML
                    var htmlEntry = archive.CreateEntry("index.html");
                    using (var entryStream = htmlEntry.Open())
                    using (var writer = new StreamWriter(entryStream))
                    {
                        await writer.WriteLineAsync("<!DOCTYPE html>");
                        await writer.WriteLineAsync("<html lang=\"en\">");
                        await writer.WriteLineAsync("<head>");
                        await writer.WriteLineAsync("    <meta charset=\"UTF-8\">");
                        await writer.WriteLineAsync($"    <title>{product.ProductName}</title>");
                        await writer.WriteLineAsync("    <link rel=\"stylesheet\" href=\"style.css\">");
                        await writer.WriteLineAsync("</head>");
                        await writer.WriteLineAsync("<body>");
                        await writer.WriteLineAsync(product.HtmlCode ?? "");
                        await writer.WriteLineAsync("</body></html>");
                    }

                    // ใส่ CSS
                    var cssEntry = archive.CreateEntry("style.css");
                    using (var entryStream = cssEntry.Open())
                    using (var writer = new StreamWriter(entryStream))
                    {
                        await writer.WriteAsync(product.CssCode ?? "");
                    }

                    // ใส่ README
                    var readmeEntry = archive.CreateEntry("README.md");
                    using (var entryStream = readmeEntry.Open())
                    using (var writer = new StreamWriter(entryStream))
                    {
                        await writer.WriteLineAsync($"# {product.ProductName}");
                        await writer.WriteLineAsync($"**Author:** {product.Author}");
                    }
                }

                memoryStream.Position = 0;
                return File(memoryStream.ToArray(), "application/zip", $"{product.ProductName.Replace(" ", "_")}.zip");
            }
        }

        [HttpPost]
        public IActionResult Export([FromBody] ExportRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Html)) return BadRequest();

            string result = "";
            string css = request.Css ?? "";
            string html = request.Html;

            // เลือก Template ตาม Framework ที่ส่งมาจาก JS
            switch (request.Framework.ToLower())
            {
                case "react":
                    // แปลง class เป็น className แบบง่าย (Regex จะแม่นยำกว่า)
                    string reactHtml = html.Replace("class=", "className=");
                    result = $@"import React from 'react';
import styled from 'styled-components';

const StyledWrapper = styled.div`
  {css}
`;

const Component = () => {{
  return (
    <StyledWrapper>
      {reactHtml}
    </StyledWrapper>
  );
}};

export default Component;";
                    break;

                case "vue":
                    result = $@"<template>
  <div class=""styled-wrapper"">
    {html}
  </div>
</template>

<script>
export default {{
  name: ""PixelComponent""
}};
</script>

<style scoped>
{css}
</style>";
                    break;

                case "svelte":
                    result = $@"<div class=""styled-wrapper"">
  {html}
</div>

<style>
{css}
</style>";
                    break;

                case "lit":
                    result = $@"import {{ LitElement, html, css }} from 'lit';

class PixelComponent extends LitElement {{
  static styles = css`
    {css}
  `;

  render() {{
    return html`{html}`;
  }}
}}
customElements.define('pixel-component', PixelComponent);";
                    break;

                default:
                    return BadRequest("Unsupported framework");
            }

            return Content(result);
        }

        // สร้าง class เล็กๆ ไว้รับ JSON (ไว้ล่างสุดของไฟล์หรือนอก class Controller ก็ได้)
        public class ExportRequest
        {
            public string Html { get; set; }
            public string Css { get; set; }
            public string Framework { get; set; }
        }


    }
}