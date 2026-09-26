using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Security;
using RetailShop.UI.Controls;
using RetailShop.UI.Services;
using RetailShop.UI.Styles;

namespace RetailShop.UI.Views;

public class DashboardView : UserControl
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly Action<string> _navigateAction;
    private readonly PdfReportService? _pdfReportService;

    private Label _lblTodaySales = null!;
    private Label _lblMonthSales = null!;
    private Label _lblTotalProducts = null!;
    private Label _lblLowStock = null!;
    private Label _lblEmployees = null!;
    private Label _lblSuppliers = null!;

    private WeeklySalesTrendChart _salesChart = null!;
    private CategoryDistributionChart _categoryChart = null!;
    private Panel _pnlCharts = null!;

    public DashboardView(IDashboardRepository dashboardRepository, Action<string> navigateAction, PdfReportService? pdfReportService = null)
    {
        _dashboardRepository = dashboardRepository;
        _navigateAction = navigateAction;
        _pdfReportService = pdfReportService;

        InitializeComponents();
        _ = LoadMetricsAsync();
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ModernTheme.BgLight;
        this.AutoScroll = true;
        this.Padding = new Padding(24);
        this.Font = ModernTheme.BodyFont;

        // Container panel to hold scrollable contents with fixed width
        var pnlScroll = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent
        };

        // 1. TOP HEADER
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.Transparent
        };

        var lblGreeting = new Label
        {
            Text = $"Welcome back, {UserSession.Current.FullName}!",
            Font = ModernTheme.TitleFont,
            ForeColor = ModernTheme.TextPrimary,
            Location = new Point(0, 5),
            AutoSize = true
        };

        var lblRoleInfo = new Label
        {
            Text = $"Logged in as {UserSession.Current.Role} | Real-time Enterprise Business Intelligence",
            Font = ModernTheme.SmallFont,
            ForeColor = ModernTheme.TextMuted,
            Location = new Point(0, 36),
            AutoSize = true
        };

        var btnRefresh = new Button
        {
            Text = "🔄 Refresh Metrics",
            Size = new Size(150, 36),
            Location = new Point(pnlHeader.Width - 160, 12),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ModernTheme.ApplySecondaryButtonStyle(btnRefresh);
        btnRefresh.Click += async (s, e) => await LoadMetricsAsync();

        pnlHeader.Controls.Add(lblGreeting);
        pnlHeader.Controls.Add(lblRoleInfo);
        pnlHeader.Controls.Add(btnRefresh);

        // 2. KPI METRICS CARDS
        var flowCards = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 265,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 0)
        };

        var card1 = CreateMetricCard("TODAY'S REVENUE", "LKR 0.00", "0 invoices today", ModernTheme.PrimaryIndigo, out _lblTodaySales);
        var card2 = CreateMetricCard("MONTH'S REVENUE", "LKR 0.00", "Current month sales", ModernTheme.SuccessEmerald, out _lblMonthSales);
        var card3 = CreateMetricCard("INVENTORY ITEMS", "0 Products", "Active in catalog", Color.FromArgb(14, 165, 233), out _lblTotalProducts);
        var card4 = CreateMetricCard("LOW STOCK ALERTS", "0 Items", "Requires replenishment", ModernTheme.DangerRose, out _lblLowStock);
        var card5 = CreateMetricCard("ACTIVE STAFF", "0 Employees", "Registered team", Color.FromArgb(168, 85, 247), out _lblEmployees);
        var card6 = CreateMetricCard("SUPPLIERS", "0 Suppliers", "Vendor partners", ModernTheme.WarningAmber, out _lblSuppliers);

        flowCards.Controls.AddRange(new Control[] { card1, card2, card3, card4, card5, card6 });

        // 3. ANALYTICS & CHARTS SECTION
        _pnlCharts = new Panel
        {
            Dock = DockStyle.Top,
            Height = 310,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 16, 0, 16)
        };

        _salesChart = new WeeklySalesTrendChart
        {
            Location = new Point(0, 0),
            Size = new Size(600, 300)
        };

        _categoryChart = new CategoryDistributionChart
        {
            Location = new Point(616, 0),
            Size = new Size(420, 300)
        };

        _pnlCharts.Controls.Add(_salesChart);
        _pnlCharts.Controls.Add(_categoryChart);

        _pnlCharts.Resize += (s, e) =>
        {
            int totalW = _pnlCharts.ClientSize.Width;
            int totalH = _pnlCharts.ClientSize.Height;
            if (totalW < 300 || totalH < 50) return;

            int spacing = 16;
            int leftW = (int)((totalW - spacing) * 0.58f);
            int rightW = totalW - spacing - leftW;

            if (leftW > 50 && rightW > 50 && totalH > 50)
            {
                _salesChart.SetBounds(0, 0, leftW, totalH);
                _categoryChart.SetBounds(leftW + spacing, 0, rightW, totalH);
            }
        };

        // 4. QUICK ACTIONS SECTION
        var pnlActions = new Panel
        {
            Dock = DockStyle.Top,
            Height = 150,
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(20),
            Margin = new Padding(0, 16, 0, 24)
        };

        pnlActions.Paint += (s, e) =>
        {
            if (pnlActions.Width > 2 && pnlActions.Height > 2)
            {
                using var pen = new Pen(ModernTheme.BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlActions.Width - 1, pnlActions.Height - 1);
            }
        };

        var lblActionsTitle = new Label
        {
            Text = "⚡ Quick Actions & Workflow Shortcuts",
            Font = ModernTheme.SubHeaderFont,
            ForeColor = ModernTheme.TextPrimary,
            Location = new Point(20, 15),
            AutoSize = true
        };

        var flowActions = new FlowLayoutPanel
        {
            Location = new Point(20, 50),
            Size = new Size(950, 80),
            FlowDirection = FlowDirection.LeftToRight
        };

        var btnNewSale = CreateQuickActionButton("🛒 Open POS & Checkout", () => _navigateAction("Billing"), ModernTheme.PrimaryIndigo);
        var btnStock = CreateQuickActionButton("📦 Manage Products", () => _navigateAction("Products"), Color.FromArgb(14, 165, 233));
        var btnCustomers = CreateQuickActionButton("👥 Customer Directory", () => _navigateAction("Customers"), ModernTheme.SuccessEmerald);
        var btnBudget = CreateQuickActionButton("💰 Budget & Profits", () => _navigateAction("Budget"), ModernTheme.WarningAmber);

        flowActions.Controls.Add(btnNewSale);
        flowActions.Controls.Add(btnStock);
        flowActions.Controls.Add(btnCustomers);

        if (UserSession.Current.HasPermission(RolePermissions.ManageBudget))
        {
            flowActions.Controls.Add(btnBudget);
        }

        pnlActions.Controls.Add(lblActionsTitle);
        pnlActions.Controls.Add(flowActions);

        // Assembly (order in Top docking: bottom-most docked first)
        pnlScroll.Controls.Add(pnlActions);
        pnlScroll.Controls.Add(_pnlCharts);
        pnlScroll.Controls.Add(flowCards);
        pnlScroll.Controls.Add(pnlHeader);

        pnlHeader.BringToFront();
        flowCards.BringToFront();
        _pnlCharts.BringToFront();
        pnlActions.BringToFront();

        this.Controls.Add(pnlScroll);
    }

    private Panel CreateMetricCard(string title, string initialValue, string subtext, Color accentColor, out Label valueLabel)
    {
        var card = new Panel
        {
            Size = new Size(270, 115),
            BackColor = ModernTheme.SurfaceWhite,
            Margin = new Padding(0, 0, 16, 16),
            Padding = new Padding(16)
        };

        card.Paint += (s, e) =>
        {
            if (card.Width > 2 && card.Height > 4)
            {
                using var pen = new Pen(ModernTheme.BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);

                // Colored top accent stripe
                using var brush = new SolidBrush(accentColor);
                e.Graphics.FillRectangle(brush, 0, 0, card.Width, 4);
            }
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = ModernTheme.TextMuted,
            Location = new Point(14, 14),
            AutoSize = true
        };

        var lblValue = new Label
        {
            Text = initialValue,
            Font = ModernTheme.TitleFont,
            ForeColor = ModernTheme.TextPrimary,
            Location = new Point(12, 38),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = subtext,
            Font = ModernTheme.SmallFont,
            ForeColor = accentColor,
            Location = new Point(14, 76),
            AutoSize = true
        };

        card.Controls.Add(lblTitle);
        card.Controls.Add(lblValue);
        card.Controls.Add(lblSub);

        valueLabel = lblValue;
        return card;
    }

    private Button CreateQuickActionButton(string text, Action onClick, Color color)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(190, 48),
            FlatStyle = FlatStyle.Flat,
            BackColor = color,
            ForeColor = Color.White,
            Font = ModernTheme.BodyBoldFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 16, 0)
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += (s, e) => onClick();
        return btn;
    }

    public async Task LoadMetricsAsync()
    {
        try
        {
            var metricsTask = _dashboardRepository.GetMetricsAsync();
            var trendTask = _dashboardRepository.GetWeeklySalesTrendAsync();
            var categoryTask = _dashboardRepository.GetCategoryDistributionAsync();

            await Task.WhenAll(metricsTask, trendTask, categoryTask);

            var metrics = await metricsTask;
            _lblTodaySales.Text = $"LKR {metrics.TodaySales:N2}";
            _lblMonthSales.Text = $"LKR {metrics.MonthSales:N2}";
            _lblTotalProducts.Text = $"{metrics.TotalProducts:N0} Products";
            _lblLowStock.Text = $"{metrics.LowStockCount:N0} Items";
            _lblEmployees.Text = $"{metrics.TotalEmployees:N0} Staff";
            _lblSuppliers.Text = $"{metrics.TotalSuppliers:N0} Vendors";

            _salesChart.SetData(await trendTask);
            _categoryChart.SetData(await categoryTask);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading dashboard metrics: {ex.Message}");
        }
    }
}
