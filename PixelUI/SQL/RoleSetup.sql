-- Role Setup SQL Script for PixelUI
-- Run this script to populate the Role table with RBAC roles

-- Clear existing roles (optional - use if you want to start fresh)
-- DELETE FROM Role;

-- Insert Role-Based Access Control Roles
INSERT INTO Role (RoleName) VALUES ('Super Admin');
INSERT INTO Role (RoleName) VALUES ('Product Manager');
INSERT INTO Role (RoleName) VALUES ('Marketing Team');
INSERT INTO Role (RoleName) VALUES ('Customer Support');
INSERT INTO Role (RoleName) VALUES ('Member');

-- =====================================================
-- Role Descriptions & Responsibilities
-- =====================================================

/**
1. SUPER ADMIN (RoleId: 1) - เจ้าของระบบ
   Access: 100% Full Access
   
   Permissions:
   - ✓ ดูภาพรวมการเงิน (Revenue & Payouts)
   - ✓ จัดการสิทธิ์พนักงาน (Create/Delete Staff Accounts)
   - ✓ ตัดสินใจเคสสำคัญ (High-value Refunds, User Ban)
   - ✓ เข้าถึง Admin Dashboard ทั้งหมด
   - ✓ จัดการ Roles and Permissions

   View Access:
   - /Admin/Userlist
   - /Admin/CreateProduct
   - /Admin/EditUser
   - /Admin/UpdateUser
   - /Admin/DeleteUser

---

2. PRODUCT MANAGER (RoleId: 2) - ผู้ดูแลสินค้าและเทคนิค
   Access: Inventory System, Source Code Management
   
   Permissions:
   - ✓ QC & Approve โค้ดก่อนขึ้นขาย (Clean Code Check)
   - ✓ จัดการ Version Control (v1.0 -> v1.1)
   - ✓ จัดการหมวดหมู่ Tech Stack
   - ✓ เพิ่ม/แก้ไขสินค้า (Products)

   View Access:
   - /Admin/CreateProduct
   - /Admin/EditProduct (future)
   - /Admin/ProductList (future)

   Denied:
   - ✗ /Admin/Userlist
   - ✗ /Admin/EditUser
   - ✗ /Admin/DeleteUser

---

3. MARKETING TEAM (RoleId: 3) - ฝ่ายการตลาด
   Access: Promotion Engine, CMS, Basic Analytics
   
   Permissions:
   - ✓ สร้างคูปองส่วนลดและตั้งค่า Bundle Deal
   - ✓ ตกแต่งหน้า Homepage/Landing Page (future)
   - ✓ วิเคราะห์ยอด View vs Conversion
   - ✓ จัดการโปรโมชั่น

   View Access:
   - /Marketing/CreatePromotion (future)
   - /Marketing/Analytics (future)

   Denied:
   - ✗ /Admin/* (all admin pages)

---

4. CUSTOMER SUPPORT (RoleId: 4) - ฝ่ายบริการลูกค้า
   Access: Ticket System, User Verification
   
   Permissions:
   - ✓ ตอบคำถามการใช้งาน (Help Center)
   - ✓ ตรวจสอบเอกสารนักศึกษา (Student License Verification)
   - ✓ แก้ปัญหาเบื้องต้น (Basic Troubleshooting)

   View Access:
   - /Support/Tickets (future)
   - /Support/UserVerification (future)

   Denied:
   - ✗ /Admin/* (all admin pages)

---

5. MEMBER - สมาชิกทั่วไป
   Access: Limited (Browse products, purchase, etc.)
   
   Permissions:
   - ✓ ดูรายการสินค้า
   - ✓ ซื้อสินค้า
   - ✓ ดูประวัติการซื้อ

   Denied:
   - ✗ /Admin/* (all admin pages)
   - ✗ /Marketing/* (all marketing pages)
   - ✗ /Support/* (all support pages)

*/

-- To assign a role to a user, update their RoleId:
-- UPDATE User SET RoleId = (SELECT RoleId FROM Role WHERE RoleName = 'Super Admin') WHERE UserId = 1;
-- UPDATE User SET RoleId = (SELECT RoleId FROM Role WHERE RoleName = 'Product Manager') WHERE UserId = 2;
