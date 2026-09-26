# 🛡️ Security Audit & Architecture Modernization Report (2026)

**Project:** Retail Shop Standalone Management System  
**Audit Scope:** Legacy .NET Framework 4.6.1 Coursework Project vs. 2026 Modernized .NET 9 Enterprise Architecture  
**Author:** Prageeth Thilina (Senior Software Engineer, 5+ Years Industry Experience)  
**Date:** September 2026  
**Status:** Completed & Remediated  

---

## 1. Executive Summary

This security assessment and architectural refactoring report documents the comprehensive audit and modernization of a legacy standalone retail shop desktop application originally developed during undergraduate studies. 

The original codebase exhibited multiple high-severity security vulnerabilities (including ubiquitous **SQL Injection**, **Plaintext Password Storage**, **Lack of Authorization**, and **Catastrophic Data Modification bugs**). 

The application has been completely re-architected into a **2026 enterprise-grade .NET 9 desktop application** featuring:
- **Clean Layered Architecture** (`Core`, `Data`, `UI`).
- **Cryptographic Security** via **BCrypt** with enhanced work factor and cryptographic salts.
- **Enterprise Role-Based Access Control (RBAC)** governing Admin, Manager, and Cashier operational tiers.
- **Server-Side SQL Pagination** on all data tables with reusable UI controls.
- **Transactional POS Engine** guaranteeing atomic stock decrements and concurrency safety.
- **Zero-Friction Database Initializer** with automated migrations and seed data.

---

## 2. Comprehensive Vulnerability Analysis & Remediation Matrix

| ID | Vulnerability Classification | CWE | Legacy Risk Level | Remediated Status |
|:---|:---|:---:|:---:|:---:|
| **SEC-01** | SQL Injection via String Concatenation | [CWE-89](https://cwe.mitre.org/data/definitions/89.html) | 🔴 Critical | ✅ Resolved (Parameterized SQL / Dapper) |
| **SEC-02** | Plaintext Password Storage & Table Exposure | [CWE-256](https://cwe.mitre.org/data/definitions/256.html) / [CWE-312](https://cwe.mitre.org/data/definitions/312.html) | 🔴 Critical | ✅ Resolved (BCrypt Work Factor 11) |
| **SEC-03** | Broken Authorization (Missing RBAC) | [CWE-285](https://cwe.mitre.org/data/definitions/285.html) / [CWE-862](https://cwe.mitre.org/data/definitions/862.html) | 🔴 Critical | ✅ Resolved (RolePermissions & UserSession) |
| **SEC-04** | Missing WHERE Clause on Stock UPDATE (Data Corruption) | [CWE-787](https://cwe.mitre.org/data/definitions/787.html) | 🔴 Critical | ✅ Resolved (Scoped ID-based parameterized updates) |
| **SEC-05** | Database Connection & Resource Leaks | [CWE-404](https://cwe.mitre.org/data/definitions/404.html) | 🟠 High | ✅ Resolved (`IDisposable` / Connection Pooling) |
| **SEC-06** | Information Disclosure & Improper Error Handling | [CWE-209](https://cwe.mitre.org/data/definitions/209.html) | 🟠 High | ✅ Resolved (Sanitized user errors & structured logging) |
| **SEC-07** | Hardcoded Absolute Machine File Paths & Fragile COM | [CWE-73](https://cwe.mitre.org/data/definitions/73.html) | 🟠 High | ✅ Resolved (GDI+ Receipt Engine & portable paths) |
| **SEC-08** | Lack of Pagination & Memory Exhaustion | [CWE-770](https://cwe.mitre.org/data/definitions/770.html) | 🟡 Medium | ✅ Resolved (Server-side SQL Offset/Fetch Pagination) |
| **SEC-09** | Fragmented Multi-Database Architecture | Architectural Flaw | 🟡 Medium | ✅ Resolved (Unified relational schema in `RetailShopDb`) |

---

## 3. In-Depth Vulnerability Findings & Code Evidence

### 3.1 SEC-01: Ubiquitous SQL Injection (CWE-89)
- **Vulnerability Description:** Across all legacy forms (`Login.cs`, `Stock.cs`, `Billing Page.cs`, `Employee Register.cs`, `Budget_Page.cs`), dynamic SQL queries were assembled by directly concatenating raw user input from textboxes.
- **Legacy Code Snippet (`Login.cs` lines 49-51):**
  ```csharp
  // VULNERABLE: Direct string concatenation allows complete authentication bypass
  SqlConnection con = new SqlConnection(@"Data Source=PRAGEETH\SQLEXPRESS;Initial Catalog=pt_login_now;Integrated Security=True");
  SqlDataAdapter sda = new SqlDataAdapter("Select Count(*) From [dbo].[Login] where USERNAME='" + textBox1.Text + "'and PASSWORD='" + textBox2.Text + "'", con);
  ```
- **Exploitation Impact:** By entering `' OR '1'='1` into `textBox1` and `textBox2`, any unauthorized individual bypassed authentication and gained full system access. Attackers could also append stacked queries (`'; DROP TABLE Users; --`).
- **2026 Modern Remediation (`UserRepository.cs`):**
  ```csharp
  // SECURE: Parameterized query + BCrypt verification
  const string sql = "SELECT * FROM Users WHERE Username = @Username AND IsActive = 1";
  using var conn = _connectionFactory.CreateConnection();
  var user = await conn.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });
  if (user == null || !_passwordHasher.VerifyPassword(plainPassword, user.PasswordHash))
      return null;
  ```

---

### 3.2 SEC-02: Plaintext Passwords & Direct Grid Exposure (CWE-256 / CWE-312)
- **Vulnerability Description:** Passwords were saved into the database in cleartext without any hashing or cryptographic salt. Even worse, the legacy `User Register.cs` screen populated the data grid with plaintext passwords:
- **Legacy Code Snippet (`User Register.cs` line 63):**
  ```csharp
  // VULNERABLE: Plaintext password fetched from DB and displayed directly on screen
  dataGridView1.Rows[n].Cells["USERNAME"].Value = row["PASSWORD"].ToString();
  ```
- **Exploitation Impact:** Shoulder surfing, data export, or reading memory leaked user credentials immediately.
- **2026 Modern Remediation (`BCryptPasswordHasher.cs`):**
  - Upgraded to industry standard **BCrypt with Work Factor 11**:
  ```csharp
  public class BCryptPasswordHasher : IPasswordHasher
  {
      private const int WorkFactor = 11;

      public string HashPassword(string plainPassword) =>
          BCrypt.Net.BCrypt.EnhancedHashPassword(plainPassword, WorkFactor);

      public bool VerifyPassword(string plainPassword, string passwordHash) =>
          BCrypt.Net.BCrypt.EnhancedVerify(plainPassword, passwordHash);
  }
  ```
  - `PasswordHash` is never selected or mapped to UI grids.

---

### 3.3 SEC-03: Missing Role-Based Authorization (RBAC) (CWE-285)
- **Vulnerability Description:** Once a user logged in, every menu option and button was active and accessible. A cashier could access `User Register` to elevate privileges or access `Budget_Page` to view confidential profits.
- **2026 Modern Remediation (`RolePermissions.cs` & `MainDashboardForm.cs`):**
  - Implemented formal RBAC policies mapping roles (`Admin`, `Manager`, `Cashier`) to discrete permissions:
  ```csharp
  private void ApplyRolePermissions()
  {
      var role = UserSession.Current.Role;
      if (role == UserRole.Cashier)
      {
          DisableNavButton("Employees", "Employees (Admin/Manager only)");
          DisableNavButton("Attendance", "Attendance/Payroll (Admin only)");
          DisableNavButton("Budget", "Budget & Finance (Admin only)");
          DisableNavButton("Users", "User Management (Admin only)");
      }
      else if (role == UserRole.Manager)
      {
          DisableNavButton("Users", "User Management (Admin only)");
      }
  }
  ```

---

### 3.4 SEC-04: Catastrophic Stock Data Corruption Bug
- **Vulnerability Description:** In `Stock.cs`, updating an item executed an `UPDATE` query without a `WHERE` clause:
- **Legacy Code Snippet (`Stock.cs` line 170):**
  ```csharp
  // CATASTROPHIC BUG: Updates EVERY row in the Stock table!
  con2.dataSend2("UPDATE [dbo].[Stock] SET Item_Id ='" + textBox1.Text + "', Item_Name ='" + textBox2.Text + "', Item_Price ='" + textBox3.Text + "', Item_Quantity ='" + textBox4.Text + "'");
  ```
- **Exploitation Impact:** When a user edited a single item, **all items** in the entire inventory database were overwritten with that single item's details!
- **2026 Modern Remediation (`ProductRepository.cs`):**
  ```csharp
  const string sql = @"
      UPDATE Products
      SET ProductCode = @ProductCode,
          Name = @Name,
          Category = @Category,
          BuyingPrice = @BuyingPrice,
          SellingPrice = @SellingPrice,
          Quantity = @Quantity,
          LowStockThreshold = @LowStockThreshold,
          IsActive = @IsActive,
          UpdatedAt = GETUTCDATE()
      WHERE Id = @Id;";
  using var conn = _connectionFactory.CreateConnection();
  var rows = await conn.ExecuteAsync(sql, product);
  ```

---

### 3.5 SEC-05: Missing Table Pagination (CWE-770)
- **Vulnerability Description:** The legacy application executed unqualified `SELECT * FROM [dbo].[Stock]` or `SELECT * FROM [dbo].[Billing_Details]`, creating hundreds of UI row controls sequentially in a UI loop.
- **Exploitation Impact:** In a production store with 10,000+ stock items and 50,000+ sales transactions, loading all rows caused out-of-memory crashes, latency spikes, and complete UI freezing.
- **2026 Modern Remediation:**
  - Standardized `PagedRequest` and `PagedResult<T>` data contracts.
  - Server-side SQL Paging using `OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY`.
  - Reusable custom WinForms `PaginationControl` with page size selection (`10`, `25`, `50`, `100`), page jumpers, and total record statistics.

---

### 3.6 SEC-06: POS Checkout Concurrency & Transactional Safety
- **Legacy Issue:** In `Billing Page.cs`, billing deducted stock via loose string updates without transactions. It allowed sales when inventory was zero or negative.
- **2026 Modern Remediation (`SaleRepository.cs`):**
  - Atomic database transactions (`conn.BeginTransaction()`).
  - Strict conditional decrement: `WHERE Id = @ProductId AND Quantity >= @Quantity`.
  - Rollback on insufficient stock:
  ```csharp
  var affected = await conn.ExecuteAsync(deductStockSql, new { Quantity = item.Quantity, ProductId = item.ProductId }, transaction);
  if (affected == 0)
  {
      throw new InvalidOperationException($"Insufficient inventory available for item: {item.ProductName}");
  }
  ```

---

## 4. Architectural Transformation Summary

```
Legacy Architecture (2019 Coursework)          Modern Architecture (2026 Enterprise)
┌─────────────────────────────────┐            ┌─────────────────────────────────┐
│ WinForms (.NET Framework 4.6.1) │            │ Presentation Layer (.NET 9 Win) │
│ - UI tightly coupled with SQL   │            │ - DI Host & appsettings.json    │
│ - Raw String Concat SQL Queries │            │ - Reusable PaginationControl    │
│ - Direct Form-to-Form Dangling  │            │ - GDI+ Thermal Receipt Preview  │
├─────────────────────────────────┤            ├─────────────────────────────────┤
│ Fragmented Connections (x3)     │            │ Data Access Layer (Dapper/SQL)  │
│ - Connection1 (pt_login_now)    │   ====>    │ - ISqlConnectionFactory (Pool)  │
│ - Connection2 (Stock_Db)        │            │ - Parameterized Repositories    │
│ - Connection3 (Employee)        │            │ - Automated DbInitializer (DDL) │
├─────────────────────────────────┤            ├─────────────────────────────────┤
│ Hardcoded Machine LocalDB       │            │ Core Domain Layer               │
│ - PRAGEETH\SQLEXPRESS           │            │ - Entities, DTOs & PagedResult  │
│ - Crystal Reports (.rpt)        │            │ - BCrypt Security & RBAC Policies│
│ - COM WMPLib (pt.mp3)           │            │ - Thread-Safe UserSession       │
└─────────────────────────────────┘            └─────────────────────────────────┘
```

---

## 5. Verification & Testing Results

- **Compiler Status:** 0 Errors, 0 Warnings across all projects (`RetailShop.Core`, `RetailShop.Data`, `RetailShop.UI`).
- **Target Runtime:** .NET 9.0 LTS on Windows 10/11 x64.
- **Database Self-Healing Test:** Successfully tested against local Microsoft SQL Server (`.\SQLEXPRESS`). Verified automatic database creation (`RetailShopDb`), table DDL execution, index provisioning, and default account seeding.
- **Role Isolation Test:** Tested authentication and menu restrictions across `Admin`, `Manager`, and `Cashier` roles.
- **Paging Stress Test:** Verified responsive pagination and instant navigation across inventory, customer, transaction history, and staff records.

---

## 6. Conclusion

The application has been transformed from an insecure, single-machine university student prototype into an enterprise-ready, professional software product reflecting **5+ years of senior software engineering standards**. It serves as an exemplary portfolio project demonstrating mastery of software security, clean architecture, performance optimization, and modern C# design patterns.
