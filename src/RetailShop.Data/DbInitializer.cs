using Dapper;
using Microsoft.Data.SqlClient;
using RetailShop.Core.Enums;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Security;

namespace RetailShop.Data;

public class DbInitializer : IDbInitializer
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IPasswordHasher _passwordHasher;

    public DbInitializer(ISqlConnectionFactory connectionFactory, IPasswordHasher passwordHasher)
    {
        _connectionFactory = connectionFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task InitializeAsync()
    {
        await EnsureDatabaseCreatedAsync();
        await CreateTablesAsync();
        await SeedInitialDataAsync();
    }

    private async Task EnsureDatabaseCreatedAsync()
    {
        const string checkDbSql = @"
            IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'RetailShopDb')
            BEGIN
                CREATE DATABASE RetailShopDb;
            END";

        using var masterConn = _connectionFactory.CreateMasterConnection();
        await masterConn.OpenAsync();
        await masterConn.ExecuteAsync(checkDbSql);
    }

    private async Task CreateTablesAsync()
    {
        const string createTablesSql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
            BEGIN
                CREATE TABLE Users (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Username NVARCHAR(50) NOT NULL UNIQUE,
                    PasswordHash NVARCHAR(255) NOT NULL,
                    FullName NVARCHAR(100) NOT NULL,
                    Email NVARCHAR(100) NULL,
                    Address NVARCHAR(200) NULL,
                    DateOfBirth DATE NULL,
                    Role INT NOT NULL,
                    IsActive BIT NOT NULL DEFAULT 1,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    LastLoginAt DATETIME2 NULL
                );
                CREATE INDEX IX_Users_Username ON Users(Username);
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
            BEGIN
                CREATE TABLE Products (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    ProductCode NVARCHAR(50) NOT NULL UNIQUE,
                    Name NVARCHAR(100) NOT NULL,
                    Category NVARCHAR(50) NOT NULL DEFAULT 'General',
                    BuyingPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                    SellingPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                    Quantity INT NOT NULL DEFAULT 0,
                    LowStockThreshold INT NOT NULL DEFAULT 5,
                    IsActive BIT NOT NULL DEFAULT 1,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    UpdatedAt DATETIME2 NULL
                );
                CREATE INDEX IX_Products_ProductCode ON Products(ProductCode);
                CREATE INDEX IX_Products_Name ON Products(Name);
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Suppliers')
            BEGIN
                CREATE TABLE Suppliers (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    SupplierCode NVARCHAR(50) NOT NULL UNIQUE,
                    Name NVARCHAR(100) NOT NULL,
                    ContactPerson NVARCHAR(100) NULL,
                    Phone NVARCHAR(50) NULL,
                    Email NVARCHAR(100) NULL,
                    Address NVARCHAR(200) NULL,
                    IsActive BIT NOT NULL DEFAULT 1,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                );
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers')
            BEGIN
                CREATE TABLE Customers (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(100) NOT NULL,
                    Phone NVARCHAR(50) NULL,
                    Email NVARCHAR(100) NULL,
                    Address NVARCHAR(200) NULL,
                    TotalPurchases DECIMAL(18,2) NOT NULL DEFAULT 0,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                );
                CREATE INDEX IX_Customers_Phone ON Customers(Phone);
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sales')
            BEGIN
                CREATE TABLE Sales (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    InvoiceNumber NVARCHAR(50) NOT NULL UNIQUE,
                    CustomerName NVARCHAR(100) NOT NULL DEFAULT 'Walk-in Customer',
                    CustomerPhone NVARCHAR(50) NULL,
                    SubTotal DECIMAL(18,2) NOT NULL,
                    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
                    NetTotal DECIMAL(18,2) NOT NULL,
                    PaidAmount DECIMAL(18,2) NOT NULL,
                    Balance DECIMAL(18,2) NOT NULL DEFAULT 0,
                    PaymentMethod INT NOT NULL DEFAULT 1,
                    CashierId INT NOT NULL,
                    CashierName NVARCHAR(100) NOT NULL,
                    SaleDate DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                );
                CREATE INDEX IX_Sales_InvoiceNumber ON Sales(InvoiceNumber);
                CREATE INDEX IX_Sales_SaleDate ON Sales(SaleDate);
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SaleItems')
            BEGIN
                CREATE TABLE SaleItems (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    SaleId INT NOT NULL REFERENCES Sales(Id) ON DELETE CASCADE,
                    ProductId INT NOT NULL,
                    ProductCode NVARCHAR(50) NOT NULL,
                    ProductName NVARCHAR(100) NOT NULL,
                    UnitPrice DECIMAL(18,2) NOT NULL,
                    Quantity INT NOT NULL,
                    TotalPrice DECIMAL(18,2) NOT NULL
                );
                CREATE INDEX IX_SaleItems_SaleId ON SaleItems(SaleId);
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Employees')
            BEGIN
                CREATE TABLE Employees (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    EmployeeCode NVARCHAR(50) NOT NULL UNIQUE,
                    Name NVARCHAR(100) NOT NULL,
                    Nic NVARCHAR(50) NULL,
                    Phone NVARCHAR(50) NULL,
                    Email NVARCHAR(100) NULL,
                    Address NVARCHAR(200) NULL,
                    DateOfBirth DATE NULL,
                    Designation NVARCHAR(50) NOT NULL DEFAULT 'Staff',
                    BasicSalary DECIMAL(18,2) NOT NULL DEFAULT 0,
                    BankDetails NVARCHAR(200) NULL,
                    JoinDate DATE NOT NULL DEFAULT GETDATE(),
                    IsActive BIT NOT NULL DEFAULT 1
                );
                CREATE INDEX IX_Employees_EmployeeCode ON Employees(EmployeeCode);
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Attendances')
            BEGIN
                CREATE TABLE Attendances (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    EmployeeId INT NOT NULL REFERENCES Employees(Id),
                    EmployeeName NVARCHAR(100) NOT NULL,
                    Year INT NOT NULL,
                    Month INT NOT NULL,
                    TotalDays INT NOT NULL,
                    WorkingDays INT NOT NULL,
                    PresentDays INT NOT NULL,
                    AbsentDays INT NOT NULL,
                    RecordedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                );
                CREATE INDEX IX_Attendances_YearMonth ON Attendances(Year, Month);
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Salaries')
            BEGIN
                CREATE TABLE Salaries (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    EmployeeId INT NOT NULL REFERENCES Employees(Id),
                    EmployeeName NVARCHAR(100) NOT NULL,
                    Year INT NOT NULL,
                    Month INT NOT NULL,
                    BasicSalary DECIMAL(18,2) NOT NULL,
                    DailyRate DECIMAL(18,2) NOT NULL,
                    Allowances DECIMAL(18,2) NOT NULL DEFAULT 0,
                    Deductions DECIMAL(18,2) NOT NULL DEFAULT 0,
                    NetSalary DECIMAL(18,2) NOT NULL,
                    PaidDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    Notes NVARCHAR(200) NULL
                );
                CREATE INDEX IX_Salaries_YearMonth ON Salaries(Year, Month);
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Budgets')
            BEGIN
                CREATE TABLE Budgets (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Year INT NOT NULL,
                    Month INT NOT NULL,
                    InventoryCost DECIMAL(18,2) NOT NULL DEFAULT 0,
                    WaterBill DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ElectricityBill DECIMAL(18,2) NOT NULL DEFAULT 0,
                    SalariesTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
                    OtherExpenses DECIMAL(18,2) NOT NULL DEFAULT 0,
                    TotalIncome DECIMAL(18,2) NOT NULL DEFAULT 0,
                    NetIncomeYear DECIMAL(18,2) NOT NULL DEFAULT 0,
                    Notes NVARCHAR(200) NULL,
                    RecordedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    CONSTRAINT UQ_Budgets_YearMonth UNIQUE (Year, Month)
                );
                CREATE INDEX IX_Budgets_YearMonth ON Budgets(Year, Month);
            END;
        ";

        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync(createTablesSql);
    }

    private async Task SeedInitialDataAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        // 1. Seed Users if empty
        var userCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users");
        if (userCount == 0)
        {
            var adminHash = _passwordHasher.HashPassword("Admin@2026!");
            var managerHash = _passwordHasher.HashPassword("Manager@2026!");
            var cashierHash = _passwordHasher.HashPassword("Cashier@2026!");

            const string insertUserSql = @"
                INSERT INTO Users (Username, PasswordHash, FullName, Email, Address, Role, IsActive, CreatedAt)
                VALUES 
                (@AdminUser, @AdminHash, 'System Administrator', 'admin@polkotuwa.lk', 'Colombo, Sri Lanka', @AdminRole, 1, GETUTCDATE()),
                (@ManagerUser, @ManagerHash, 'Operations Manager', 'manager@polkotuwa.lk', 'Kandy, Sri Lanka', @ManagerRole, 1, GETUTCDATE()),
                (@CashierUser, @CashierHash, 'Senior Cashier', 'cashier@polkotuwa.lk', 'Galle, Sri Lanka', @CashierRole, 1, GETUTCDATE());
            ";

            await conn.ExecuteAsync(insertUserSql, new
            {
                AdminUser = "admin",
                AdminHash = adminHash,
                AdminRole = (int)UserRole.Admin,
                ManagerUser = "manager",
                ManagerHash = managerHash,
                ManagerRole = (int)UserRole.Manager,
                CashierUser = "cashier",
                CashierHash = cashierHash,
                CashierRole = (int)UserRole.Cashier
            });
        }

        // 2. Seed Products if empty
        var productCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Products");
        if (productCount == 0)
        {
            const string insertProductsSql = @"
                INSERT INTO Products (ProductCode, Name, Category, BuyingPrice, SellingPrice, Quantity, LowStockThreshold, IsActive)
                VALUES
                ('P-1001', 'Premium Ceylon BOPF Tea 500g', 'Beverages', 700.00, 850.00, 45, 10, 1),
                ('P-1002', 'Araliya Basmati Rice 5kg', 'Grains & Rice', 2150.00, 2450.00, 30, 8, 1),
                ('P-1003', 'Highland Full Cream Milk 1L', 'Dairy', 380.00, 460.00, 4, 10, 1),
                ('P-1004', 'Munchee Super Cream Cracker 490g', 'Bakery & Biscuits', 310.00, 380.00, 60, 15, 1),
                ('P-1005', 'Sunlight Lemon Soap 115g', 'Household', 95.00, 120.00, 80, 20, 1),
                ('P-1006', 'White Granulated Sugar 1kg', 'Pantry', 260.00, 310.00, 3, 10, 1),
                ('P-1007', 'Nescafe Classic Jar 50g', 'Beverages', 540.00, 650.00, 25, 5, 1),
                ('P-1008', 'Anchor Pure Salted Butter 227g', 'Dairy', 980.00, 1150.00, 12, 5, 1),
                ('P-1009', 'Maliban Real Chocolate Biscuit 200g', 'Bakery & Biscuits', 210.00, 260.00, 50, 10, 1),
                ('P-1010', 'MD Mixed Fruit Jam 500g', 'Breakfast', 430.00, 520.00, 18, 5, 1),
                ('P-1011', 'Signal Herbal Fluoride Toothpaste 160g', 'Personal Care', 230.00, 290.00, 40, 10, 1),
                ('P-1012', 'Prima All-Purpose Wheat Flour 1kg', 'Pantry', 230.00, 280.00, 35, 10, 1),
                ('P-1013', 'Fortune Sunflower Cooking Oil 1L', 'Oils & Ghee', 820.00, 960.00, 22, 6, 1),
                ('P-1014', 'Clogard Clove Mouthwash 250ml', 'Personal Care', 410.00, 490.00, 15, 5, 1),
                ('P-1015', 'Samahan Herbal Infusion Pack (25)', 'Health & Wellness', 650.00, 775.00, 28, 8, 1);
            ";

            await conn.ExecuteAsync(insertProductsSql);
        }

        // 3. Seed Suppliers if empty
        var supplierCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Suppliers");
        if (supplierCount == 0)
        {
            const string insertSuppliersSql = @"
                INSERT INTO Suppliers (SupplierCode, Name, ContactPerson, Phone, Email, Address, IsActive)
                VALUES
                ('SUP-001', 'Ceylon Biscuits Limited (CBL)', 'Kamal Perera', '0112456789', 'orders@cbl.lk', 'Pannipitiya, Colombo', 1),
                ('SUP-002', 'Highland Milco Lanka Ltd', 'Sunil Fernando', '0112876543', 'distribution@milco.lk', 'Narahenpita, Colombo', 1),
                ('SUP-003', 'Unilever Sri Lanka Limited', 'Anura Jayasuriya', '0112345678', 'supply@unilever.lk', 'Grandpass, Colombo', 1),
                ('SUP-004', 'Prima Ceylon Pvt Ltd', 'Nimal Silva', '0112987654', 'sales@prima.lk', 'Trincomalee / Colombo', 1);
            ";

            await conn.ExecuteAsync(insertSuppliersSql);
        }

        // 4. Seed Employees if empty
        var employeeCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Employees");
        if (employeeCount == 0)
        {
            const string insertEmployeesSql = @"
                INSERT INTO Employees (EmployeeCode, Name, Nic, Phone, Email, Address, Designation, BasicSalary, BankDetails, JoinDate, IsActive)
                VALUES
                ('EMP-001', 'Kasun Bandara', '199245678912', '0771234567', 'kasun@polkotuwa.lk', 'Kandy Road, Kiribathgoda', 'Store Manager', 85000.00, 'BOC 789456123 - Kadawatha', '2023-01-15', 1),
                ('EMP-002', 'Dilshan Ruwan', '199587654321', '0719876543', 'dilshan@polkotuwa.lk', 'Station Road, Kelaniya', 'Chief Cashier', 55000.00, 'Commercial Bank 88992211 - Kelaniya', '2023-06-01', 1),
                ('EMP-003', 'Malsha Nimanthi', '199823456789', '0754567890', 'malsha@polkotuwa.lk', 'Highlevel Road, Nugegoda', 'Inventory Clerk', 48000.00, 'Sampath Bank 11029384 - Nugegoda', '2024-02-10', 1);
            ";

            await conn.ExecuteAsync(insertEmployeesSql);
        }

        // 5. Seed Attendance and Salary sample records
        var attendanceCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Attendances");
        if (attendanceCount == 0)
        {
            const string insertAttendanceSql = @"
                INSERT INTO Attendances (EmployeeId, EmployeeName, Year, Month, TotalDays, WorkingDays, PresentDays, AbsentDays, RecordedAt)
                VALUES
                (1, 'Kasun Bandara', 2026, 9, 30, 24, 23, 1, GETUTCDATE()),
                (2, 'Dilshan Ruwan', 2026, 9, 30, 24, 24, 0, GETUTCDATE()),
                (3, 'Malsha Nimanthi', 2026, 9, 30, 24, 22, 2, GETUTCDATE());
            ";
            await conn.ExecuteAsync(insertAttendanceSql);
        }

        var salaryCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Salaries");
        if (salaryCount == 0)
        {
            const string insertSalarySql = @"
                INSERT INTO Salaries (EmployeeId, EmployeeName, Year, Month, BasicSalary, DailyRate, Allowances, Deductions, NetSalary, PaidDate, Notes)
                VALUES
                (1, 'Kasun Bandara', 2026, 8, 85000.00, 3541.66, 5000.00, 2000.00, 88000.00, '2026-08-31', 'August Salary Paid'),
                (2, 'Dilshan Ruwan', 2026, 8, 55000.00, 2291.66, 3000.00, 1000.00, 57000.00, '2026-08-31', 'August Salary Paid'),
                (3, 'Malsha Nimanthi', 2026, 8, 48000.00, 2000.00, 2500.00, 1500.00, 49000.00, '2026-08-31', 'August Salary Paid');
            ";
            await conn.ExecuteAsync(insertSalarySql);
        }

        // 6. Seed Sample Budget
        var budgetCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Budgets");
        if (budgetCount == 0)
        {
            const string insertBudgetSql = @"
                INSERT INTO Budgets (Year, Month, InventoryCost, WaterBill, ElectricityBill, SalariesTotal, OtherExpenses, TotalIncome, NetIncomeYear, Notes)
                VALUES
                (2026, 8, 420000.00, 4500.00, 22000.00, 194000.00, 15000.00, 850000.00, 2350000.00, 'August 2026 Financial Close'),
                (2026, 9, 380000.00, 4200.00, 21500.00, 194000.00, 12000.00, 920000.00, 2658300.00, 'September 2026 Provisional Budget');
            ";
            await conn.ExecuteAsync(insertBudgetSql);
        }

        // 7. Seed Initial Sales and Customers
        var customerCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Customers");
        if (customerCount == 0)
        {
            const string insertCustomersSql = @"
                INSERT INTO Customers (Name, Phone, Email, Address, TotalPurchases)
                VALUES
                ('Saman Kumara', '0772345678', 'saman@gmail.com', 'Kandy Road, Kadawatha', 4500.00),
                ('Anula Wijeratne', '0713456789', 'anula@yahoo.com', 'Temple Road, Kiribathgoda', 12300.00),
                ('Rohan Wickramasinghe', '0764567890', 'rohan@outlook.com', 'Colombo 03', 8900.00);
            ";
            await conn.ExecuteAsync(insertCustomersSql);
        }

        var salesCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Sales");
        if (salesCount == 0)
        {
            const string insertSalesSql = @"
                INSERT INTO Sales (InvoiceNumber, CustomerName, CustomerPhone, SubTotal, Discount, NetTotal, PaidAmount, Balance, PaymentMethod, CashierId, CashierName, SaleDate)
                VALUES
                ('INV-20260926-0001', 'Saman Kumara', '0772345678', 3300.00, 100.00, 3200.00, 5000.00, 1800.00, 1, 3, 'Senior Cashier', GETUTCDATE()),
                ('INV-20260926-0002', 'Walk-in Customer', '', 1420.00, 0.00, 1420.00, 2000.00, 580.00, 1, 3, 'Senior Cashier', DATEADD(hour, -2, GETUTCDATE())),
                ('INV-20260925-0001', 'Anula Wijeratne', '0713456789', 5100.00, 250.00, 4850.00, 5000.00, 150.00, 2, 3, 'Senior Cashier', DATEADD(day, -1, GETUTCDATE()));

                INSERT INTO SaleItems (SaleId, ProductId, ProductCode, ProductName, UnitPrice, Quantity, TotalPrice)
                VALUES
                (1, 1, 'P-1001', 'Premium Ceylon BOPF Tea 500g', 850.00, 1, 850.00),
                (1, 2, 'P-1002', 'Araliya Basmati Rice 5kg', 2450.00, 1, 2450.00),
                (2, 4, 'P-1004', 'Munchee Super Cream Cracker 490g', 380.00, 2, 760.00),
                (2, 7, 'P-1007', 'Nescafe Classic Jar 50g', 650.00, 1, 650.00);
            ";
            await conn.ExecuteAsync(insertSalesSql);
        }
    }
}
