# 🛡️ Security Architecture & Engineering Specifications

This document outlines the security controls, data integrity mechanisms, and threat mitigation models engineered into the **Retail Shop Standalone Management System**.

---

## 1. Security Architecture Overview

The system is designed with a **Security-by-Design** and **Defense-in-Depth** mindset, ensuring zero trust at data boundaries, strict operational role separation, and complete protection against common desktop and database attack vectors.

```
┌─────────────────────────────────────────────────────────────┐
│                    WinForms UI Layer                        │
│   - Session Enforcement (UserSession)                       │
│   - UI Privilege Disablement & Route Guards                 │
│   - Sanitized User Feedback & Global Exception Interception │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                    Core Domain Layer                        │
│   - BCrypt Cryptographic Hashing (Work Factor 11)           │
│   - Role-Based Access Control (RBAC Matrix)                 │
│   - Domain Invariants & Input Validation Guards             │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                    Data Access Layer                        │
│   - 100% Parameterized SQL Execution via Dapper             │
│   - Atomic Multi-Statement SQL Transactions                 │
│   - Scoped Connection Pooling & Leak Prevention             │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                   Microsoft SQL Server                      │
│   - Primary & Foreign Key Integrity Constraints             │
│   - Server-Side Pagination (OFFSET / FETCH NEXT)            │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Cryptographic Security & Password Storage

### BCrypt Adaptive Hashing
- **Algorithm**: BCrypt with enhanced key derivation (`BCrypt.Net-Next`).
- **Work Factor**: **11** ($2^{11} = 2,048$ iterations).
- **Salt Generation**: Automatically generates a cryptographically secure random 128-bit salt for every password calculation.
- **Credential Storage**: Plaintext passwords are never stored, logged, or serialized. The database schema stores solely the 60-character cryptographic hash string.
- **In-Memory Hygiene**: Password strings in UI controls (`TextBox.PasswordChar = '•'`) are immediately passed to the hasher and cleared after validation.

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

---

## 3. SQL Injection Immunity & Data Access Security

### 100% Parameterized Execution via Dapper
- Dynamic SQL string concatenation is strictly prohibited across the entire codebase.
- All query parameters (filters, keys, search tokens, pagination bounds) are strongly typed and passed via Dapper parameter objects.
- SQL Server treats all inputs strictly as literal values rather than executable SQL syntax, providing complete immunity against SQL Injection ([CWE-89](https://cwe.mitre.org/data/definitions/89.html)).

```csharp
// Example: Safe parameterized query execution
const string sql = "SELECT * FROM Users WHERE Username = @Username AND IsActive = 1";
using var conn = _connectionFactory.CreateConnection();
return await conn.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });
```

### Connection Management & Leak Prevention
- All database connections are acquired on-demand through `ISqlConnectionFactory` and enclosed within scoped C# `using` blocks.
- Guaranteed deterministic release of ADO.NET connection pools back to the pool manager, eliminating connection exhaustion ([CWE-404](https://cwe.mitre.org/data/definitions/404.html)).

---

## 4. Role-Based Access Control (RBAC)

Operational privileges are segregated into three tiers adhering to the **Principle of Least Privilege**:

| Module / Operation | Cashier | Manager | Admin |
|:---|:---:|:---:|:---:|
| **POS Checkout & Line Item Billing** | ✅ Allowed | ✅ Allowed | ✅ Allowed |
| **Thermal Receipt Printing & Re-prints** | ✅ Allowed | ✅ Allowed | ✅ Allowed |
| **Customer Directory Lookup** | ✅ Read-Only | ✅ Full CRUD | ✅ Full CRUD |
| **Inventory Products Catalog** | ✅ Read-Only | ✅ Full CRUD | ✅ Full CRUD |
| **Stock Alert & Replenishment View** | ✅ Read-Only | ✅ Full CRUD | ✅ Full CRUD |
| **Supplier Directory & Contacts** | ❌ Forbidden | ✅ Full CRUD | ✅ Full CRUD |
| **Employee Roster & Attendance** | ❌ Forbidden | ✅ Read-Only | ✅ Full CRUD |
| **Payroll Processing & Salary Disbursal** | ❌ Forbidden | ❌ Forbidden | ✅ Full Access |
| **Budgets & Financial Profit Analytics** | ❌ Forbidden | ❌ Forbidden | ✅ Full Access |
| **System User Administration & Role Grants** | ❌ Forbidden | ❌ Forbidden | ✅ Full Access |

### Enforcement Mechanisms:
- **UI Element Inactivation**: Navigation buttons for unauthorized views are visually disabled with explanatory tooltips indicating missing permissions.
- **Service Layer Guard Checks**: Methods verify `UserSession.Current.Role` prior to executing sensitive commands.

---

## 5. Concurrency Control & Transactional Integrity

### Atomic POS Checkout Transactions
During checkout processing, multiple dependent database actions must succeed together or roll back completely:
1. Master `Sales` transaction header insertion.
2. Batch `SaleItems` detail record insertion.
3. Decrement product stock levels conditionally with non-negative constraints (`StockQuantity >= @Quantity`).

All operations execute within an atomic `IDbTransaction` scope. In the event of insufficient stock or hardware interruption, the entire transaction is rolled back, preventing orphaned records or corrupted inventory numbers.

---

## 6. Denial-of-Service & Memory Protection

### Server-Side Pagination
- Memory exhaustion ([CWE-770](https://cwe.mitre.org/data/definitions/770.html)) caused by unbounded queries (`SELECT *`) is prevented via server-side pagination.
- All high-volume tables (Products, Sales Ledger, Customers, Suppliers, Payroll) utilize SQL Server `OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY` indexing.
- Datasets are streamed efficiently in controlled chunks (10, 25, 50, 100 records per page), maintaining flat memory overhead regardless of database size.

---

## 7. Exception Handling & Safe Diagnostics

- Global unhandled exception handlers (`Application.ThreadException` and `AppDomain.CurrentDomain.UnhandledException`) prevent silent application crashes.
- Sensitive infrastructure details (connection strings, database stack traces) are sanitized from user-facing modal dialogs and written to secure local diagnostic audit logs.
