# 🏪 Retail Shop Standalone Management System (Enterprise ERP - 2026 Edition)

[![.NET 9](https://img.shields.io/badge/.NET-9.0%20Windows-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C# 13](https://img.shields.io/badge/Language-C%23%2013-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Database](https://img.shields.io/badge/Database-Microsoft%20SQL%20Server-CC292B?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/en-us/sql-server)
[![Security](https://img.shields.io/badge/Security-BCrypt%20Hashing%20%7C%20RBAC-10B981?logo=security&logoColor=white)](SECURITY_AUDIT_REPORT.md)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20%7C%20Repository%20Pattern-4F46E5)](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## 🌟 Overview & Transformation Story

This project is a complete modernization of an undergraduate software engineering university module project into a **2026 enterprise-grade retail standalone desktop ERP application**. 

As a software engineer with **5+ years of industry experience**, this repository has been comprehensively refactored to eliminate critical legacy vulnerabilities (SQL injection, plaintext credentials, missing authorization, catastrophic SQL update bugs, and connection leaks) and elevated to modern high-performance C# / .NET 9 standards with **Clean Architecture**, **Dependency Injection**, **Role-Based Access Control (RBAC)**, **Server-Side Pagination**, and **Automated Database Self-Healing**.

> 📄 **Looking for the Security Assessment?**  
> Read the complete [SECURITY_AUDIT_REPORT.md](SECURITY_AUDIT_REPORT.md) for detailed CWE vulnerability audits, exploit mechanisms, and remediation proofs.

---

## 🚀 Key Modern Features (2026)

- 🔒 **Enterprise Authentication & BCrypt Security**:
  - Secure password hashing using **BCrypt (Work Factor 11)** with unique cryptographic salts.
  - Zero plaintext credential storage.
  - Secure password changes with complexity verification and current password validation.
- 🛡️ **Role-Based Access Control (RBAC)**:
  - Multi-tier operational security: **Admin**, **Manager**, and **Cashier**.
  - Dynamic UI permission enforcement (hides or disables unauthorized financial and staff management screens).
- ⚡ **Server-Side Pagination on All Tables**:
  - Eliminates memory bloat and UI freezing on large datasets.
  - Reusable `PaginationControl` with page size switching (10, 25, 50, 100), record statistics, and instant navigation.
  - Applied to **Inventory/Products**, **Sales History**, **Customers**, **Suppliers**, **Employees**, **Attendance & Payroll**, and **Budgets**.
- 🛒 **Modern Point-of-Sale (POS) & Checkout Engine**:
  - Fast barcode/product search, cart line item calculations, subtotal, discount, net total, and cash change computation.
  - **Atomic SQL Transactions**: Inventory stock is conditionally decremented with safety checks, preventing race conditions and negative inventory.
- 🖨️ **Built-in GDI+ Receipt Engine**:
  - Replaces fragile, proprietary Crystal Reports and COM dependencies with a clean, native thermal-style receipt preview and print dialog.
- 📊 **Real-Time KPI Dashboard & Interactive BI Analytics**:
  - Live financial metrics: Today's Revenue, Monthly Revenue, Low Stock Alerts, Total Active Products, and Staff count.
  - **7-Day Revenue Trend Chart**: Custom vector GDI+ chart with anti-aliasing, rounded bars, interactive hover tooltips, and dynamic resize handling.
  - **Inventory Category Distribution**: Real-time category valuation breakdown with SKU counts, total stock value, and percentage distribution badges.
- 📑 **Enterprise PDF Audit & Report Generation**:
  - Powered by **QuestPDF 2026**, generating publication-ready vector documents.
  - **Inventory Valuation & Stock Audit Report**: Full landscape catalog audit with category breakdowns, unit costs, retail prices, margin percentages, and stock status indicators.
  - **Sales Transaction Audit Report**: Comprehensive ledger with cashier attribution, payment breakdown (Cash, Card, Credit), and net sales summary.
  - **Payroll & Attendance Disbursement Report**: Monthly staff payroll register with gross wages, overtime, deductions, and net disbursements.
- 🎨 **Modern High-DPI UI & Polished UX**:
  - Consistent modern color tokens (`#0F172A`, `#1E293B`, `#6366F1`, `#10B981`) with flat, clean aesthetics.
  - Ergonomic toolbar layouts with zero text clipping on search inputs and filters across all 8 modules.
  - Fixed sidebar navigation docking, expanded width (265px), and contextual tooltips on restricted modules for unprivileged roles.
- 🛠️ **Zero-Friction Database Provisioning**:
  - Built-in `DbInitializer` automatically verifies and creates `RetailShopDb` on SQL Server, executes DDL table creation with indexes, and seeds default accounts and catalog items out-of-the-box.

---

## 🏗️ Architecture & Solution Structure

The solution follows **Clean Layered Architecture** with strict separation of concerns and dependency inversion:

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
│   │   ├── Repositories/              # UserRepository, ProductRepository, SaleRepository, etc.
│   │   ├── ISqlConnectionFactory.cs   # Centralized SQL Server connection manager
│   │   ├── DbInitializer.cs           # Automated DDL schema migration & data seeder
│   │   └── DataServiceExtensions.cs   # Dependency Injection service registrations
│   │
│   └── RetailShop.UI/                 # Modern WinForms Presentation Layer (.NET 9)
│       ├── Controls/                  # Reusable PaginationControl
│       ├── Forms/                     # LoginForm, MainDashboardForm
│       ├── Services/                  # ReceiptPrinter (GDI+ thermal printing)
│       ├── Styles/                    # ModernTheme color tokens & control styling
│       ├── Views/                     # DashboardView, BillingView, ProductsView, etc.
│       ├── appsettings.json           # Connection strings & store configuration
│       └── Program.cs                 # Generic Host, DI Container, & App bootstrap
│
├── Project-Files/                     # Original university coursework project files (preserved)
├── SECURITY_AUDIT_REPORT.md           # Formal vulnerability assessment & remediation report
├── RetailShopManagement.sln           # Standard Visual Studio solution
└── RetailShopManagement.slnx          # Modern .NET 9 XML solution
```

---

## 🛡️ Security & Engineering Comparison (Before vs After)

| Feature / Domain | Legacy University Code (2019) | Modernized Enterprise Code (2026) |
|:---|:---|:---|
| **Target Framework** | .NET Framework 4.6.1 (`packages.config`) | **.NET 9.0 Windows** (SDK-style project) |
| **SQL Execution** | Raw string concatenation (`'"+txt.Text+"'`) | **100% Parameterized Queries via Dapper** |
| **Password Storage** | Plaintext in database & displayed in grids | **BCrypt Enhanced Hashing (Work Factor 11)** |
| **Authorization** | None (All users had god-mode access) | **Role-Based Access Control (Admin, Manager, Cashier)** |
| **Table Pagination** | None (`SELECT *` loaded entire DB into UI) | **Server-Side SQL Pagination (`OFFSET/FETCH`)** |
| **Data Integrity** | `UPDATE Stock` had no `WHERE` clause (wiped DB) | **Atomic Scoped Updates & SQL Transactions** |
| **Connection Health** | Unclosed connections & memory leaks in `dataGet` | **Connection Pooling with `using` Scopes** |
| **Receipt Printing** | Hardcoded file paths to `.rpt` Crystal Reports | **Native .NET GDI+ Thermal Receipt Preview** |
| **App Architecture** | Sprawling monolithic form partials | **Clean Layered Architecture with Microsoft DI** |
| **Database Setup** | 3 fragmented local databases on machine | **Unified `RetailShopDb` with Auto-Provisioning** |

---

## 👥 Role-Based Access Control (RBAC) Matrix

| Module / Action | Cashier | Manager | Admin |
|:---|:---:|:---:|:---:|
| **POS Checkout & Billing** | ✅ Full Access | ✅ Full Access | ✅ Full Access |
| **Print / Re-print Receipts** | ✅ Full Access | ✅ Full Access | ✅ Full Access |
| **View Customer Accounts** | ✅ View / Search | ✅ Full Access | ✅ Full Access |
| **Inventory Stock Lookup** | ✅ Read-Only | ✅ Full CRUD | ✅ Full CRUD |
| **Low Stock Alerts** | ✅ View | ✅ Full Access | ✅ Full Access |
| **Supplier Directory** | ❌ Restricted | ✅ Full Access | ✅ Full Access |
| **Employee & Staff Directory** | ❌ Restricted | ✅ Read-Only | ✅ Full CRUD |
| **Attendance & Payroll Processing** | ❌ Restricted | ❌ Restricted | ✅ Full Access |
| **Financial Budget & Profit Analysis** | ❌ Restricted | ❌ Restricted | ✅ Full Access |
| **User Account & Role Management** | ❌ Restricted | ❌ Restricted | ✅ Full Access |

---

## 🔑 Quick Demo Credentials

The application includes an **automated database provisioner and seeder**. On first run, it connects to Microsoft SQL Server (`.\SQLEXPRESS`), creates `RetailShopDb`, and seeds the following test accounts:

| Role | Username | Default Password | Permissions |
|:---|:---|:---|:---|
| 👑 **Administrator** | `admin` | `Admin@2026!` | Complete administrative control |
| 👔 **Manager** | `manager` | `Manager@2026!` | Operations, inventory, suppliers, sales |
| 🛒 **Cashier** | `cashier` | `Cashier@2026!` | POS terminal checkout & customer search |

*(Quick-fill buttons are also available on the Login screen for instant one-click testing.)*

---

## 💻 Getting Started

### Prerequisites
1. **.NET 9.0 SDK** (or later) installed: [Download .NET](https://dotnet.microsoft.com/download)
2. **Microsoft SQL Server** (2016 or newer, Express, Developer, or LocalDB) running locally.
3. Windows 10 or 11.

### Configuration
Open [appsettings.json](src/RetailShop.UI/appsettings.json) to adjust your SQL Server connection string if needed:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=RetailShopDb;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

### Build & Run
Run the following commands from the root directory:

```bash
# 1. Restore & Build the solution
dotnet build RetailShopManagement.sln

# 2. Run the application
dotnet run --project ./src/RetailShop.UI/RetailShop.UI.csproj
```

The application will self-initialize the database and display the login screen.

---

## 📜 License

This project is licensed under the MIT License - see the LICENSE file for details.
