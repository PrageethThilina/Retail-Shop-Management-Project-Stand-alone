using System.Drawing;
using System.Windows.Forms;

namespace RetailShop.UI.Styles;

public static class ModernTheme
{
    // Color Palette
    public static readonly Color DarkNavy = Color.FromArgb(15, 23, 42);          // #0F172A
    public static readonly Color SidebarBg = Color.FromArgb(30, 41, 59);         // #1E293B
    public static readonly Color SidebarHover = Color.FromArgb(51, 65, 85);      // #334155
    public static readonly Color PrimaryIndigo = Color.FromArgb(79, 70, 229);    // #4F46E5
    public static readonly Color PrimaryIndigoHover = Color.FromArgb(67, 56, 202);// #4338CA
    public static readonly Color SuccessEmerald = Color.FromArgb(16, 185, 129);  // #10B981
    public static readonly Color DangerRose = Color.FromArgb(239, 68, 68);       // #EF4444
    public static readonly Color WarningAmber = Color.FromArgb(245, 158, 11);    // #F59E0B
    public static readonly Color SurfaceWhite = Color.FromArgb(255, 255, 255);
    public static readonly Color BgLight = Color.FromArgb(248, 250, 252);        // #F8FAFC
    public static readonly Color BorderColor = Color.FromArgb(226, 232, 240);    // #E2E8F0
    public static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);       // #0F172A
    public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);      // #64748B

    // Fonts
    public static readonly Font TitleFont = new("Segoe UI", 16f, FontStyle.Bold);
    public static readonly Font SubHeaderFont = new("Segoe UI", 12f, FontStyle.Bold);
    public static readonly Font BodyFont = new("Segoe UI", 9.5f, FontStyle.Regular);
    public static readonly Font BodyBoldFont = new("Segoe UI", 9.5f, FontStyle.Bold);
    public static readonly Font SmallFont = new("Segoe UI", 8.5f, FontStyle.Regular);
    public static readonly Font MonoFont = new("Consolas", 9.5f, FontStyle.Regular);

    public static void ApplyDataGridStyle(DataGridView dgv)
    {
        dgv.BackgroundColor = SurfaceWhite;
        dgv.BorderStyle = BorderStyle.None;
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgv.GridColor = BorderColor;
        dgv.RowHeadersVisible = false;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.MultiSelect = false;
        dgv.AllowUserToAddRows = false;
        dgv.AllowUserToDeleteRows = false;
        dgv.AllowUserToResizeRows = false;
        dgv.ReadOnly = true;
        dgv.EnableHeadersVisualStyles = false;
        dgv.RowTemplate.Height = 36;
        dgv.Font = BodyFont;

        // Configure headers to have fixed height and never auto-shrink or clip text
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgv.ColumnHeadersHeight = 44;

        // Header style
        dgv.ColumnHeadersDefaultCellStyle.BackColor = SidebarBg;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = SidebarBg;
        dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
        dgv.ColumnHeadersDefaultCellStyle.Font = BodyBoldFont;
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 2, 10, 2);
        dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;

        // Ensure header height persists after layout/column addition
        dgv.HandleCreated += (s, e) =>
        {
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 44;
        };

        // Default cell style
        dgv.DefaultCellStyle.BackColor = SurfaceWhite;
        dgv.DefaultCellStyle.ForeColor = TextPrimary;
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 242, 255); // Indigo-50
        dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(49, 46, 129);  // Indigo-900
        dgv.DefaultCellStyle.Padding = new Padding(10, 4, 10, 4);

        // Alternating row style
        dgv.AlternatingRowsDefaultCellStyle.BackColor = BgLight;
        dgv.AlternatingRowsDefaultCellStyle.ForeColor = TextPrimary;
        dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 242, 255);
        dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(49, 46, 129);
        dgv.AlternatingRowsDefaultCellStyle.Padding = new Padding(10, 4, 10, 4);
    }

    public static void ApplyPrimaryButtonStyle(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = PrimaryIndigo;
        btn.ForeColor = Color.White;
        btn.Font = BodyBoldFont;
        btn.Cursor = Cursors.Hand;
    }

    public static void ApplySuccessButtonStyle(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = SuccessEmerald;
        btn.ForeColor = Color.White;
        btn.Font = BodyBoldFont;
        btn.Cursor = Cursors.Hand;
    }

    public static void ApplyDangerButtonStyle(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = DangerRose;
        btn.ForeColor = Color.White;
        btn.Font = BodyBoldFont;
        btn.Cursor = Cursors.Hand;
    }

    public static void ApplySecondaryButtonStyle(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderColor = BorderColor;
        btn.FlatAppearance.BorderSize = 1;
        btn.BackColor = SurfaceWhite;
        btn.ForeColor = TextPrimary;
        btn.Font = BodyFont;
        btn.Cursor = Cursors.Hand;
    }
}
