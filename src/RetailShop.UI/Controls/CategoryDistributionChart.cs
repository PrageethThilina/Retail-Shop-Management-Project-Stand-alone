using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using RetailShop.Core.Models;
using RetailShop.UI.Styles;

namespace RetailShop.UI.Controls;

public class CategoryDistributionChart : Control
{
    private List<CategoryDistributionItem> _data = new();

    private static readonly Color[] Palette = new[]
    {
        ModernTheme.PrimaryIndigo,
        ModernTheme.SuccessEmerald,
        Color.FromArgb(14, 165, 233),   // Sky Blue
        ModernTheme.WarningAmber,
        Color.FromArgb(168, 85, 247),  // Purple
        ModernTheme.DangerRose,
        Color.FromArgb(20, 184, 166),   // Teal
        Color.FromArgb(249, 115, 22)   // Orange
    };

    public CategoryDistributionChart()
    {
        this.DoubleBuffered = true;
        this.ResizeRedraw = true;
        this.Font = ModernTheme.BodyFont;
        this.BackColor = ModernTheme.SurfaceWhite;
    }

    public void SetData(List<CategoryDistributionItem> data)
    {
        _data = data ?? new List<CategoryDistributionItem>();
        this.Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (this.Width < 30 || this.Height < 30) return;

        try
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Card container border
            using (var borderPen = new Pen(ModernTheme.BorderColor, 1))
            {
                g.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
            }

            // Header Title
            using (var titleBrush = new SolidBrush(ModernTheme.TextPrimary))
            {
                g.DrawString("📦  Inventory Valuation by Category", ModernTheme.SubHeaderFont, titleBrush, 16, 14);
            }

            decimal totalValue = _data.Sum(x => x.TotalStockValue);
            using (var subBrush = new SolidBrush(ModernTheme.TextMuted))
            {
                g.DrawString($"Total Portfolio: LKR {totalValue:N0} across {_data.Sum(x => x.ProductCount)} items", ModernTheme.SmallFont, subBrush, 16, 36);
            }

            if (_data.Count == 0)
            {
                using var brush = new SolidBrush(ModernTheme.TextMuted);
                g.DrawString("No category distribution data found.", ModernTheme.BodyFont, brush, 20, 75);
                return;
            }

            int startY = 70;
            int availableHeight = this.Height - startY - 15;
            if (availableHeight <= 30) return;

            int maxItems = Math.Min(_data.Count, 5);
            int itemHeight = availableHeight / Math.Max(1, maxItems);
            int barHeight = 10;

            for (int i = 0; i < maxItems; i++)
            {
                var item = _data[i];
                var color = Palette[i % Palette.Length];
                int currentY = startY + (i * itemHeight);

                // Category Name + Color dot
                using (var dotBrush = new SolidBrush(color))
                {
                    g.FillEllipse(dotBrush, 18, currentY + 3, 10, 10);
                }

                using (var catBrush = new SolidBrush(ModernTheme.TextPrimary))
                {
                    g.DrawString(item.Category, ModernTheme.BodyBoldFont, catBrush, 36, currentY);
                }

                // Valuation & Percentage on the right
                string statsText = $"{item.Percentage:0.0}% • LKR {item.TotalStockValue:N0} ({item.ProductCount} SKUs)";
                using (var statBrush = new SolidBrush(ModernTheme.TextMuted))
                {
                    var sz = g.MeasureString(statsText, ModernTheme.SmallFont);
                    g.DrawString(statsText, ModernTheme.SmallFont, statBrush, Math.Max(150, this.Width - sz.Width - 18), currentY + 1);
                }

                // Progress bar track
                int trackY = currentY + 22;
                int trackWidth = this.Width - 54;
                if (trackWidth > 10)
                {
                    using (var trackBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
                    {
                        DrawBar(g, trackBrush, new RectangleF(36, trackY, trackWidth, barHeight), 4);
                    }

                    // Progress bar fill
                    float fillWidth = Math.Max(6f, (float)(item.Percentage / 100.0) * trackWidth);
                    if (fillWidth > trackWidth) fillWidth = trackWidth;

                    using (var fillBrush = new SolidBrush(color))
                    {
                        DrawBar(g, fillBrush, new RectangleF(36, trackY, fillWidth, barHeight), 4);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CategoryDistributionChart paint error: {ex.Message}");
        }
    }

    private static void DrawBar(Graphics g, Brush brush, RectangleF rect, float radius)
    {
        if (rect.Width <= 0 || rect.Height <= 0) return;

        float d = radius * 2;
        if (rect.Width <= d || rect.Height <= d)
        {
            g.FillRectangle(brush, rect);
            return;
        }

        using var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
