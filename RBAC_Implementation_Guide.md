# PixelUI RBAC (Role-Based Access Control) Implementation Guide

## Overview
The PixelUI application implements a role-based access control system with 5 distinct roles:
- **Super Admin** - Full system access
- **Product Manager** - Product and code management
- **Marketing Team** - Promotions and analytics
- **Customer Support** - Support tickets and verifications
- **Member** - Regular users

## Current Implementation

### 1. Role Authorization Attribute
Located in: `Attributes/RequireRoleAttribute.cs`

The `[RequireRole]` attribute restricts controller actions to specific roles.

**Usage:**
```csharp
// Restrict to single role
[RequireRole("Super Admin")]
public IActionResult SomeAction() { }

// Restrict to multiple roles
[RequireRole("Super Admin", "Product Manager")]
public IActionResult CreateProduct() { }

// Apply to entire controller (overridable by action-level attributes)
[RequireRole("Super Admin", "Product Manager")]
public class AdminController : Controller { }
```

### 2. Current Role Restrictions

#### AdminController (`Controllers/AdminController.cs`)
- **Controller Level**: Requires `Super Admin` OR `Product Manager`
- **Userlist**: Requires `Super Admin` ONLY
- **CreateProduct**: Inherits controller-level access (Super Admin + Product Manager)
- **EditUser**: Requires `Super Admin` ONLY
- **UpdateUser**: Requires `Super Admin` ONLY
- **DeleteUser**: Requires `Super Admin` ONLY

### 3. Session-Based Authentication
User roles are stored in session after login:
```csharp
HttpContext.Session.SetString("UserRole", user.Role?.RoleName ?? "Member");
```

### 4. Auto-populated Author Field
When creating products, the Author field is automatically populated from the logged-in user:
```csharp
var username = HttpContext.Session.GetString("Username");
product.Author = username;
```

## How to Extend the System

### Step 1: Create New Controller with Role Restrictions
```csharp
using PixelUI.Attributes;

namespace PixelUI.Controllers
{
    // Apply to entire controller
    [RequireRole("Marketing Team")]
    public class MarketingController : Controller
    {
        // Action-level overrides are possible
        [RequireRole("Super Admin")] // Override: only Super Admin
        public IActionResult SpecialAction() { }
    }
}
```

### Step 2: Use Role Constants for Consistency
```csharp
using PixelUI.Helpers;

// Instead of hardcoding role names:
[RequireRole(RoleConstants.SuperAdmin, RoleConstants.ProductManager)]
public IActionResult SomeAction() { }
```

### Step 3: Check Roles in Views
```html
@{
    var userRole = Context.Session.GetString("UserRole");
    var isAdmin = userRole == "Super Admin" || userRole == "Product Manager";
}

@if (isAdmin)
{
    <a href="/Admin/Dashboard">Admin Panel</a>
}
```

### Step 4: Add Navigation Based on Roles
Create a helper method in your layout or base controller:
```csharp
public void SetNavigationByRole()
{
    var userRole = HttpContext.Session.GetString("UserRole");
    
    switch(userRole)
    {
        case "Super Admin":
            ViewBag.ShowAdminMenu = true;
            ViewBag.ShowFinance = true;
            break;
        case "Product Manager":
            ViewBag.ShowAdminMenu = true;
            break;
        case "Marketing Team":
            ViewBag.ShowMarketingMenu = true;
            break;
        // ... etc
    }
}
```

## Future Implementation Roadmap

### Phase 1: ✅ Complete
- [x] Role model and database
- [x] Super Admin and Product Manager roles
- [x] Admin controller restrictions
- [x] Auto-populated Author field

### Phase 2: In Progress
- [ ] Marketing Controller for promotions and analytics
- [ ] Support Controller for tickets and verifications
- [ ] Role-specific navigation menus
- [ ] Dashboard views for each role

### Phase 3: Planned
- [ ] Fine-grained permissions (e.g., can only edit own content)
- [ ] Department-based hierarchies
- [ ] Audit logging for role changes
- [ ] Bulk role management UI
- [ ] Role templates for onboarding

## Common Patterns

### Pattern 1: Department-Specific Controller
```csharp
[RequireRole(RoleConstants.GetStaffRoles())] // All staff roles
public class DepartmentController : Controller
{
    [RequireRole(RoleConstants.SuperAdmin)]
    public IActionResult AdminOnly() { }
    
    public IActionResult StaffAction() { } // All staff can access
}
```

### Pattern 2: Progressive Disclosure in Views
```html
@{
    var userRole = Context.Session.GetString("UserRole");
}

<div class="dashboard">
    @if (userRole == "Super Admin")
    {
        <section>Financial Reports</section>
        <section>User Management</section>
    }
    
    @if (userRole == "Product Manager")
    {
        <section>Product QC</section>
    }
    
    @if (userRole == "Marketing Team")
    {
        <section>Promotions Manager</section>
    }
</div>
```

### Pattern 3: Action Method with Conditional Logic
```csharp
public async Task<IActionResult> ManageProducts()
{
    var userRole = HttpContext.Session.GetString("UserRole");
    
    var products = _context.Products.AsQueryable();
    
    // Super Admin sees all products
    if (userRole != "Super Admin")
    {
        // Product Manager sees only products they manage
        var userId = HttpContext.Session.GetString("UserId");
        products = products.Where(p => p.AuthorId == int.Parse(userId));
    }
    
    return View(await products.ToListAsync());
}
```

## Database Setup

Run the SQL script in `SQL/RoleSetup.sql` to create the roles:
```sql
INSERT INTO Role (RoleName) VALUES ('Super Admin');
INSERT INTO Role (RoleName) VALUES ('Product Manager');
INSERT INTO Role (RoleName) VALUES ('Marketing Team');
INSERT INTO Role (RoleName) VALUES ('Customer Support');
INSERT INTO Role (RoleName) VALUES ('Member');
```

Then assign roles to users:
```sql
UPDATE User SET RoleId = (SELECT RoleId FROM Role WHERE RoleName = 'Super Admin')
WHERE Email = 'admin@pixelui.com';
```

## Troubleshooting

### Issue: "Access Denied" on admin pages
**Solution**: Check that the user's RoleId in the database matches one of the allowed roles.

### Issue: New users don't have a role
**Solution**: Ensure the registration process assigns a RoleId (default: 'Member'). Check `HomeController.Register()`.

### Issue: Role attribute not working
**Solution**: Verify the using statement: `using PixelUI.Attributes;`

## Best Practices

1. **Always use RoleConstants** instead of hardcoding role names
2. **Test role restrictions** after any role changes
3. **Log role-based access** for audit purposes
4. **Keep role definitions** in one place (RoleConstants.cs)
5. **Use meaningful role names** that describe responsibilities
6. **Document role permissions** in your README or wiki
7. **Review access levels** regularly for security

## Dependencies
- `Microsoft.AspNetCore.Session` - For session management
- `Microsoft.AspNetCore.Mvc` - For attribute and controller implementation
