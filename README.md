# PixelUI

A modern e-commerce platform built with ASP.NET Core (C#) for managing and selling digital products.

## 📝 Project Overview

**PixelUI** is a solo learning project where I'm exploring **C#** for the first time. This is my first venture into the C# ecosystem, and I'm using this real-world e-commerce project as a practical learning ground.

The platform provides:
- User authentication & role-based access control (RBAC)
- Product catalog with detailed product management
- Shopping cart & checkout functionality
- Order management system
- Promotion & discount system
- Admin dashboard with analytics
- Ticket/support system for customer service
- Student verification system
- User profile & purchase history

## 🛠️ Tech Stack

- **Framework**: ASP.NET Core (.NET 9.0)
- **Language**: C#
- **Database**: Entity Framework Core (with SQL Server/LocalDB)
- **Frontend**: Razor Views, HTML, CSS, JavaScript
- **Architecture**: MVC (Model-View-Controller)

## 📋 Prerequisites

- .NET 9.0 SDK or later
- SQL Server LocalDB or compatible database
- Visual Studio 2022 or VS Code

## 🚀 Getting Started

### 1. Clone the repository
```bash
git clone <repository-url>
cd PixelUI
```

### 2. Install dependencies
```bash
dotnet restore
```

### 3. Configure the database
Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PixelUIDb;Trusted_Connection=true;"
  }
}
```

### 4. Run migrations
```bash
dotnet ef database update
```

### 5. Start the application
```bash
dotnet run
```

The application will start at `https://localhost:7000` or `http://localhost:5000`

## 📁 Project Structure

```
PixelUI/
├── Controllers/          # MVC Controllers (Admin, Home, Product, User)
├── Models/              # Database models & EF Core DbContext
├── Views/               # Razor view templates
├── ViewModels/          # View-specific data models
├── wwwroot/             # Static files (CSS, JS, images)
│   ├── css/             # Stylesheets
│   ├── js/              # Client-side scripts
│   └── uploads/         # User-uploaded files
├── SQL/                 # Database setup scripts
├── Properties/          # Launch settings
└── appsettings.json     # Application configuration
```

## 🔐 Key Features

### Authentication & Authorization
- User registration & login
- Session-based authentication
- Role-based access control (Admin, Staff, Customer)

### Admin Features
- Product management (create, edit, delete)
- User management
- Order analytics & financial overview
- Promotion creation & management
- Quality control (QC) approval system
- Staff role management
- Ticket management system

### Customer Features
- Browse and search products
- Product detail pages
- Shopping cart & checkout
- Order history & downloads
- Student verification
- Support ticket system
- Profile management

## 🗄️ Database Schema

Main entities:
- **User**: User accounts with roles
- **Product**: Digital products for sale
- **Order**: Customer purchases
- **OrderItem**: Line items in orders
- **Promotion**: Discount promotions
- **Ticket**: Support tickets
- **TicketMessage**: Ticket conversation
- **Role**: User roles (Admin, Staff, Customer)
- **Userlicense**: Product licenses
- **StudentVerification**: Student verification records

## 📝 Notes for Developers

This is a learning project, so:
- Code organization follows MVC patterns but may have improvements
- Error handling and validation are implemented with room for enhancement
- Some features are still in development
- Community feedback and pull requests are welcome

## 🎯 Future Improvements

- [ ] Unit testing coverage
- [ ] API documentation (Swagger/OpenAPI)
- [ ] Payment gateway integration
- [ ] Email notifications
- [ ] Advanced search & filtering
- [ ] Performance optimization
- [ ] Automated CI/CD pipeline

## 📄 License

This project is for educational purposes.

## 👤 Author

Solo developer learning C# and ASP.NET Core.

## 🤝 Contributing

This is a solo learning project, but suggestions and feedback are appreciated!

---

**Last Updated**: September 2026
