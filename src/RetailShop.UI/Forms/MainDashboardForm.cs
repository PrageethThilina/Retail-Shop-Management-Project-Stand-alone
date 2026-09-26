using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using RetailShop.Core.Enums;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Security;
using RetailShop.UI.Services;
using RetailShop.UI.Styles;
using RetailShop.UI.Views;

namespace RetailShop.UI.Forms;

public class MainDashboardForm : Form
{
    private readonly IServiceProvider _serviceProvider;

    private Panel _pnlSidebar = null!;
    private Panel _pnlHeader = null!;
    private Panel _pnlContent = null!;
    private Label _lblActiveTitle = null!;

    private readonly Dictionary<string, Button> _navButtons = new();
    private readonly Dictionary<string, UserControl> _cachedViews = new();
    private string _currentViewKey = "Dashboard";

    public MainDashboardForm(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        InitializeComponents();
        ApplyRolePermissions();
        NavigateTo("Dashboard");
    }

    private void InitializeComponents()
    {
        this.Text = "Polkotuwa Super Stores - Enterprise ERP Management System (2026)";
        this.Size = new Size(1366, 820);
        this.MinimumSize = new Size(1100, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.WindowState = FormWindowState.Maximized;
        this.BackColor = ModernTheme.BgLight;
        this.Font = ModernTheme.BodyFont;

        // TOP HEADER
        _pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(20, 0, 20, 0)
        };

        _pnlHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernTheme.BorderColor, 1);
            e.Graphics.DrawLine(pen, 0, _pnlHeader.Height - 1, _pnlHeader.Width, _pnlHeader.Height - 1);
        };

        _lblActiveTitle = new Label
        {
            Text = "Dashboard Overview",
            Font = ModernTheme.TitleFont,
            ForeColor = ModernTheme.TextPrimary,
            Location = new Point(24, 18),
            AutoSize = true
        };

        // User profile pill on the right
        var pnlUserCard = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 16, 0, 0)
        };

        var user = UserSession.Current;
        var lblUserName = new Label
        {
            Text = $"👤 {user.FullName}",
            Font = ModernTheme.BodyBoldFont,
            ForeColor = ModernTheme.TextPrimary,
            AutoSize = true,
            Margin = new Padding(0, 8, 12, 0)
        };

        var (roleBg, roleFg) = user.Role switch
        {
            UserRole.Admin => (Color.FromArgb(243, 232, 255), Color.FromArgb(107, 33, 168)),
            UserRole.Manager => (Color.FromArgb(224, 231, 255), Color.FromArgb(49, 46, 129)),
            _ => (Color.FromArgb(209, 250, 229), Color.FromArgb(6, 95, 70))
        };

        var lblRoleBadge = new Label
        {
            Text = $" {user.Role} ",
            Font = ModernTheme.SmallFont,
            BackColor = roleBg,
            ForeColor = roleFg,
            AutoSize = true,
            Margin = new Padding(0, 7, 16, 0),
            Padding = new Padding(6, 3, 6, 3)
        };

        var btnChangePass = new Button
        {
            Text = "🔑 Password",
            Size = new Size(100, 32),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0)
        };
        ModernTheme.ApplySecondaryButtonStyle(btnChangePass);
        btnChangePass.Click += (s, e) =>
        {
            using var dlg = new ChangePasswordDialog(_serviceProvider.GetRequiredService<IUserRepository>());
            dlg.ShowDialog(this);
        };

        var btnLogout = new Button
        {
            Text = "🚪 Logout",
            Size = new Size(90, 32),
            Cursor = Cursors.Hand
        };
        ModernTheme.ApplyDangerButtonStyle(btnLogout);
        btnLogout.Click += (s, e) =>
        {
            var confirm = MessageBox.Show("Are you sure you want to log out?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                this.Close();
            }
        };

        pnlUserCard.Controls.Add(lblUserName);
        pnlUserCard.Controls.Add(lblRoleBadge);
        pnlUserCard.Controls.Add(btnChangePass);
        pnlUserCard.Controls.Add(btnLogout);

        _pnlHeader.Controls.Add(_lblActiveTitle);
        _pnlHeader.Controls.Add(pnlUserCard);

        // LEFT SIDEBAR
        _pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 265,
            BackColor = ModernTheme.SidebarBg,
            Padding = new Padding(0)
        };

        _pnlSidebar.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(30, 41, 59), 1);
            e.Graphics.DrawLine(pen, _pnlSidebar.Width - 1, 0, _pnlSidebar.Width - 1, _pnlSidebar.Height);
        };

        // Brand logo/title in sidebar
        var pnlBrand = new Panel
        {
            Dock = DockStyle.Top,
            Height = 84,
            BackColor = Color.Transparent
        };

        var lblBrandIcon = new Label
        {
            Text = "🏪  POLKOTUWA",
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(18, 16),
            AutoSize = true
        };

        var lblBrandSub = new Label
        {
            Text = "Enterprise Retail System • 2026",
            Font = ModernTheme.SmallFont,
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(20, 48),
            AutoSize = true
        };

        pnlBrand.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(40, 53, 72), 1);
            e.Graphics.DrawLine(pen, 16, pnlBrand.Height - 1, pnlBrand.Width - 16, pnlBrand.Height - 1);
        };

        pnlBrand.Controls.Add(lblBrandIcon);
        pnlBrand.Controls.Add(lblBrandSub);

        // Sidebar Navigation Stack
        var pnlNav = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(12, 14, 12, 14),
            BackColor = Color.Transparent
        };

        AddNavButton(pnlNav, "Dashboard", "📊  Dashboard Overview");
        AddNavButton(pnlNav, "Billing", "🛒  POS & Billing Checkout");
        AddNavButton(pnlNav, "Products", "📦  Products & Stock");
        AddNavButton(pnlNav, "Customers", "👥  Customer Directory");
        AddNavButton(pnlNav, "Suppliers", "🚚  Suppliers & Vendors");
        AddNavButton(pnlNav, "Employees", "👔  Staff Directory");
        AddNavButton(pnlNav, "Attendance", "📅  Attendance & Salary");
        AddNavButton(pnlNav, "Budget", "💰  Budget & Financials");
        AddNavButton(pnlNav, "Users", "🔐  User Management");

        // CRITICAL WinForms docking order:
        // pnlBrand must dock to Top first, and pnlNav must dock into remaining client space.
        _pnlSidebar.Controls.Add(pnlNav);
        _pnlSidebar.Controls.Add(pnlBrand);
        pnlBrand.SendToBack();
        pnlNav.BringToFront();

        // CONTENT PANEL
        _pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ModernTheme.BgLight
        };

        this.Controls.Add(_pnlContent);
        this.Controls.Add(_pnlHeader);
        this.Controls.Add(_pnlSidebar);
    }

    private void AddNavButton(FlowLayoutPanel container, string key, string text)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(238, 42),
            FlatStyle = FlatStyle.Flat,
            BackColor = ModernTheme.SidebarBg,
            ForeColor = Color.FromArgb(203, 213, 225),
            Font = ModernTheme.BodyBoldFont,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 4),
            UseMnemonic = false // Crucial: prevents '&' from being treated as accelerator mnemonic
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.MouseEnter += (s, e) => { if (_currentViewKey != key && btn.Enabled) btn.BackColor = ModernTheme.SidebarHover; };
        btn.MouseLeave += (s, e) => { if (_currentViewKey != key && btn.Enabled) btn.BackColor = ModernTheme.SidebarBg; };
        btn.Click += (s, e) => NavigateTo(key);

        _navButtons[key] = btn;
        container.Controls.Add(btn);
    }

    private void ApplyRolePermissions()
    {
        var role = UserSession.Current.Role;

        // Cashier restrictions
        if (role == UserRole.Cashier)
        {
            DisableNavButton("Employees", "Staff Directory", "Admin/Manager only");
            DisableNavButton("Attendance", "Attendance & Salary", "Admin only");
            DisableNavButton("Budget", "Budget & Finance", "Admin only");
            DisableNavButton("Users", "User Management", "Admin only");
        }
        else if (role == UserRole.Manager)
        {
            DisableNavButton("Users", "User Management", "Admin only");
        }
    }

    private void DisableNavButton(string key, string title, string roleRequirement)
    {
        if (_navButtons.TryGetValue(key, out var btn))
        {
            btn.Enabled = false;
            btn.ForeColor = Color.FromArgb(100, 116, 139);
            btn.Text = "🔒  " + title;
            btn.Cursor = Cursors.No;

            var tip = new ToolTip();
            tip.SetToolTip(btn, $"{title} (Restricted: {roleRequirement})");
        }
    }

    public void NavigateTo(string key)
    {
        // Check permission before navigation
        if (!IsNavigationAllowed(key))
        {
            MessageBox.Show($"Access Denied: Your role ({UserSession.Current.Role}) does not have permission to view {key}.", "Access Control", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _currentViewKey = key;

        // Update button styles
        foreach (var (k, btn) in _navButtons)
        {
            if (btn.Enabled)
            {
                if (k == key)
                {
                    btn.BackColor = ModernTheme.PrimaryIndigo;
                    btn.ForeColor = Color.White;
                }
                else
                {
                    btn.BackColor = ModernTheme.SidebarBg;
                    btn.ForeColor = Color.FromArgb(203, 213, 225);
                }
            }
        }

        // Update Title
        _lblActiveTitle.Text = key switch
        {
            "Dashboard" => "Dashboard Overview",
            "Billing" => "Point of Sale & Billing Terminal",
            "Products" => "Inventory & Products Catalog",
            "Customers" => "Customer Accounts Directory",
            "Suppliers" => "Vendor & Supplier Directory",
            "Employees" => "Human Resources & Staff Directory",
            "Attendance" => "Attendance & Monthly Payroll",
            "Budget" => "Budget Analysis & Profit / Loss",
            "Users" => "System User Management (RBAC)",
            _ => "Polkotuwa Super Stores"
        };

        // Load or display view
        if (!_cachedViews.TryGetValue(key, out var view))
        {
            view = CreateView(key);
            _cachedViews[key] = view;
        }

        _pnlContent.SuspendLayout();
        _pnlContent.Controls.Clear();
        _pnlContent.Controls.Add(view);
        _pnlContent.ResumeLayout(true);
    }

    private bool IsNavigationAllowed(string key)
    {
        var role = UserSession.Current.Role;
        return key switch
        {
            "Users" => role == UserRole.Admin,
            "Budget" => role == UserRole.Admin,
            "Attendance" => role == UserRole.Admin || role == UserRole.Manager,
            "Employees" => role == UserRole.Admin || role == UserRole.Manager,
            _ => true
        };
    }

    private UserControl CreateView(string key)
    {
        return key switch
        {
            "Dashboard" => new DashboardView(_serviceProvider.GetRequiredService<IDashboardRepository>(), NavigateTo, _serviceProvider.GetRequiredService<PdfReportService>()),
            "Billing" => new BillingView(_serviceProvider.GetRequiredService<ISaleRepository>(), _serviceProvider.GetRequiredService<IProductRepository>(), _serviceProvider.GetRequiredService<ReceiptPrinter>(), _serviceProvider.GetRequiredService<PdfReportService>()),
            "Products" => new ProductsView(_serviceProvider.GetRequiredService<IProductRepository>(), _serviceProvider.GetRequiredService<PdfReportService>()),
            "Customers" => new CustomersView(_serviceProvider.GetRequiredService<ICustomerRepository>()),
            "Suppliers" => new SuppliersView(_serviceProvider.GetRequiredService<ISupplierRepository>()),
            "Employees" => new EmployeesView(_serviceProvider.GetRequiredService<IEmployeeRepository>()),
            "Attendance" => new AttendanceSalaryView(_serviceProvider.GetRequiredService<IAttendanceSalaryRepository>(), _serviceProvider.GetRequiredService<IEmployeeRepository>(), _serviceProvider.GetRequiredService<PdfReportService>()),
            "Budget" => new BudgetView(_serviceProvider.GetRequiredService<IBudgetRepository>()),
            "Users" => new UsersView(_serviceProvider.GetRequiredService<IUserRepository>()),
            _ => new DashboardView(_serviceProvider.GetRequiredService<IDashboardRepository>(), NavigateTo, _serviceProvider.GetRequiredService<PdfReportService>())
        };
    }
}
