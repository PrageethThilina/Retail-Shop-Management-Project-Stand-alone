# 🏪 Retail Shop Standalone Management System (Enterprise Desktop ERP)

[![.NET 9](https://img.shields.io/badge/.NET-9.0%20Windows-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C# 13](https://img.shields.io/badge/Language-C%23%2013-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Database](https://img.shields.io/badge/Database-Microsoft%20SQL%20Server-CC292B?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/en-us/sql-server)
[![Security](https://img.shields.io/badge/Security-BCrypt%20Hashing%20%7C%20RBAC-10B981?logo=security&logoColor=white)](SECURITY.md)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20%7C%20Repository%20Pattern-4F46E5)](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## 🌟 Executive Overview

**Retail Shop Standalone Management System** is a high-performance, enterprise-grade desktop ERP and Point-of-Sale (POS) application engineered in **C# 13** and **.NET 9 Windows Forms**. 

Designed for commercial retail outlets, supermarkets, and wholesale businesses, the solution delivers an end-to-end retail workflow: transactional cashier checkout, inventory cataloging with low-stock alerts, supplier/customer ledgers, staff attendance & payroll calculation, financial budgeting, and interactive business intelligence (BI) analytics.

The application is architected around **Clean Architecture**, **Dependency Injection**, **Repository Pattern**, **100% Parameterized Dapper Data Access**, and **Role-Based Access Control (RBAC)**.

> 📄 **Looking for the Security Specifications?**  
> Review [SECURITY.md](SECURITY.md) for details on password hashing, RBAC matrix, and concurrency safeguards.

---

## 🚀 Key Functional Modules & Features

### 🛒 1. Point of Sale (POS) & Billing Terminal
- **Fast Product Search**: Instant lookup by product code or item name with real-time stock availability.
- **Cart Line-Item Management**: Dynamic quantity adjustment, price calculation, item deletion, and subtotaling.
- **Discount & Tender Handling**: Custom discount percentage computation, cash received validation, and change return calculations.
- **Atomic Concurrency Protection**: Transactions execute under an atomic `IDbTransaction` scope that validates and decrements stock levels conditionally, preventing negative inventory or race conditions.
- **Thermal Receipt Engine**: Native .NET GDI+ vector thermal receipt preview with 80mm standard formatting and direct printer routing.
- **Historical Receipt Archive**: Complete historical receipt search, reprint capabilities, and cashier attribution.

### 📊 2. Real-Time KPI Dashboard & Interactive BI Analytics
- **Live Performance Tiles**: Today's Sales Revenue, Monthly Sales Total, Low Stock SKU Alerts, Active Product Count, and Active Staff Roster.
- **Weekly Sales Trend Vector Chart**: Custom GDI+ anti-aliased bar chart plotting the trailing 7-day revenue curve with dynamic tooltips and adaptive resizing.
- **Inventory Valuation Breakdown**: Real-time category asset valuation progress bars with item counts and proportional share metrics.

### 📦 3. Inventory & Catalog Management
- **Full Catalog Lifecycle**: Categorization, unit cost, retail price, stock on hand, and configurable minimum reorder thresholds.
- **Low Stock Filter**: One-click filter highlighting all SKUs requiring replenishment.
- **Server-Side Pagination**: Instant browsing through large inventories with customizable page sizes (10, 25, 50, 100).

### 📑 4. Enterprise PDF Report Generation (QuestPDF)
- **Inventory Valuation & Stock Audit Report**: Full landscape catalog audit with category breakdowns, unit costs, retail prices, margin percentages, and stock status indicators.
- **Sales Transaction Audit Report**: Comprehensive ledger with cashier attribution, payment breakdown (Cash, Card, Credit), and net sales summary.
- **Payroll Disbursement Register**: Monthly staff payroll register with gross wages, overtime, deductions, and net disbursements.

### 👥 5. Relationship & Staff Management
- **Customer Directory**: Customer profiles, loyalty discount tracking, and purchase transaction history.
- **Supplier Ledger**: Vendor contact records, company profiles, and supply catalog association.
- **Employee Roster & Attendance**: Staff tracking, daily check-in/out records, and monthly attendance tallying.
- **Payroll & Salary Processor**: Automated salary calculations factoring monthly base wage, overtime rate, working hours, and advance deductions.

### 💰 6. Financial Budgeting & Profit Analysis
- Annual and monthly expense vs. revenue comparison, net margin tracking, and financial forecasting.

---

## 🏗️ Architecture & Solution Layout

The solution strictly adheres to **Clean Layered Architecture** with unidirectional dependencies:

```
RetailShopManagement/
├── src/
│   ├── RetailShop.Core/               # Domain Layer (Zero external dependencies)
│   │   ├── Common/                    # PagedRequest, PagedResult<T>
│   │   ├── Enums/                     # UserRole, PaymentMethod
│   │   ├── Interfaces/                # Repository & Service Interfaces
│   │   ├── Models/                    # User, Product, Sale, Employee, Budget, etc.
│   │   └── Security/                  # BCryptPasswordHasher, UserSession, RolePermissions
│   │
│   ├── RetailShop.Data/               # Data Access Layer
│   │   ├── Repositories/              # Dapper repository implementations
│   │   ├── ISqlConnectionFactory.cs   # Centralized connection pooling manager
│   │   ├── DbInitializer.cs           # Automated DDL schema migration & data seeder
│   │   └── DataServiceExtensions.cs   # Dependency Injection registrations
│   │
│   └── RetailShop.UI/                 # Modern WinForms Presentation Layer (.NET 9)
│       ├── Controls/                  # PaginationControl, BI Vector Charts
│       ├── Forms/                     # LoginForm, MainDashboardForm
│       ├── Services/                  # ReceiptPrinter, PdfReportService
│       ├── Styles/                    # ModernTheme color tokens & styling
│       ├── Views/                     # DashboardView, BillingView, ProductsView, etc.
│       ├── appsettings.json           # Connection strings & store configuration
│       └── Program.cs                 # Generic Host, DI Container, & App bootstrap
│
├── SECURITY.md                        # Enterprise security architecture specifications
├── README.md                          # Solution documentation
├── .gitignore                         # Visual Studio & .NET exclusions
└── RetailShopManagement.sln           # Standard Visual Studio solution
```

---

## 👥 Role-Based Access Control (RBAC) Matrix

Operational security is governed by three distinct roles:

| Module / Operation | Cashier | Manager | Admin |
|:---|:---:|:---:|:---:|
| **POS Checkout & Billing** | ✅ Full Access | ✅ Full Access | ✅ Full Access |
| **Print / Re-print Receipts** | ✅ Full Access | ✅ Full Access | ✅ Full Access |
| **Customer Directory** | ✅ Read-Only | ✅ Full CRUD | ✅ Full CRUD |
| **Inventory Stock & Pricing** | ✅ Read-Only | ✅ Full CRUD | ✅ Full CRUD |
| **Low Stock Alerts** | ✅ View | ✅ Full Access | ✅ Full Access |
| **Supplier Directory** | ❌ Restricted | ✅ Full Access | ✅ Full Access |
| **Employee & Staff Directory** | ❌ Restricted | ✅ Read-Only | ✅ Full CRUD |
| **Attendance & Payroll Processing** | ❌ Restricted | ❌ Restricted | ✅ Full Access |
| **Budget & Financial Analysis** | ❌ Restricted | ❌ Restricted | ✅ Full Access |
| **User Account & Role Management** | ❌ Restricted | ❌ Restricted | ✅ Full Access |

---

## 🔑 Demo Access Accounts

The built-in self-healing `DbInitializer` automatically checks, creates, and seeds default demonstration accounts upon initial launch:

| Role | Username | Default Password | Permissions |
|:---|:---|:---|:---|
| 👑 **Administrator** | `admin` | `Admin@2026!` | Complete administrative control |
| 👔 **Manager** | `manager` | `Manager@2026!` | Operations, inventory, suppliers, sales |
| 🛒 **Cashier** | `cashier` | `Cashier@2026!` | POS checkout terminal & customer search |

*(Quick-fill demo buttons are provided on the Login screen for immediate one-click testing.)*

---

## 💻 Getting Started

### Prerequisites
1. **.NET 9.0 SDK** (or later) installed: [Download .NET](https://dotnet.microsoft.com/download)
2. **Microsoft SQL Server** (2016 or newer, Express, Developer, or LocalDB) running locally.
3. Windows 10 or 11 (High-DPI supported).

### Configuration
Configure your SQL Server connection string in [appsettings.json](src/RetailShop.UI/appsettings.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=RetailShopDb;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

### Build & Run
Execute the following commands from the root directory:

```bash
# 1. Restore & Build the solution
dotnet build RetailShopManagement.sln

# 2. Run the application
dotnet run --project ./src/RetailShop.UI/RetailShop.UI.csproj
```

The application will automatically verify the database schema, apply missing tables and indexes, seed demo records, and launch the login interface.

---

## 📜 License

This project is licensed under the MIT License - see the LICENSE file for details.
