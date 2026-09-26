using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using RetailShop.Core.Models;
using RetailShop.UI.Styles;

namespace RetailShop.UI.Controls;

public class WeeklySalesTrendChart : Control
{
    private List<DailySalesPoint> _data = new();
    private int _hoveredIndex = -1;
    private readonly ToolTip _toolTip = new();

    public WeeklySalesTrendChart()
    {
        this.DoubleBuffered = true;
        this.ResizeRedraw = true;
        this.Font = ModernTheme.BodyFont;
        this.BackColor = ModernTheme.SurfaceWhite;
    }

    public void SetData(List<DailySalesPoint> data)
    {
        _data = data ?? new List<DailySalesPoint>();
        _hoveredIndex = -1;
        this.Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_data.Count == 0) return;

        int newHover = GetBarIndexAt(e.Location);
        if (newHover != _hoveredIndex)
        {
            _hoveredIndex = newHover;
            this.Invalidate();

            if (_hoveredIndex >= 0 && _hoveredIndex < _data.Count)
            {
                var pt = _data[_hoveredIndex];
                _toolTip.Show($"{pt.DayLabel}\nRevenue: LKR {pt.Amount:N2}\nTransactions: {pt.OrderCount} invoices", this, e.X + 10, e.Y - 20, 2500);
            }
            else
            {
                _toolTip.Hide(this);
            }
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoveredIndex != -1)
        {
            _hoveredIndex = -1;
            this.Invalidate();
            _toolTip.Hide(this);
        }
    }

    private int GetBarIndexAt(Point p)
    {
        if (_data.Count == 0) return -1;

        int leftMargin = 65;
        int rightMargin = 25;
        int topMargin = 70;
        int bottomMargin = 45;

        int plotWidth = this.Width - leftMargin - rightMargin;
        int plotHeight = this.Height - topMargin - bottomMargin;
        if (plotWidth <= 0 || plotHeight <= 0) return -1;

        float colWidth = (float)plotWidth / _data.Count;
        for (int i = 0; i < _data.Count; i++)
        {
            float barX = leftMargin + (i * colWidth);
            if (p.X >= barX && p.X <= barX + colWidth && p.Y >= topMargin && p.Y <= topMargin + plotHeight + 15)
            {
                return i;
            }
        }
        return -1;
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
                g.DrawString("📈  Weekly Sales Revenue Trend", ModernTheme.SubHeaderFont, titleBrush, 16, 14);
            }

            using (var subBrush = new SolidBrush(ModernTheme.TextMuted))
            {
                g.DrawString("Daily revenue in LKR for the last 7 calendar days", ModernTheme.SmallFont, subBrush, 16, 36);
            }

            if (_data.Count == 0)
            {
                using var brush = new SolidBrush(ModernTheme.TextMuted);
                g.DrawString("No sales data available for the period.", ModernTheme.BodyFont, brush, 20, 75);
                return;
            }

            int leftMargin = 65;
            int rightMargin = 25;
            int topMargin = 72;
            int bottomMargin = 45;

            int plotWidth = this.Width - leftMargin - rightMargin;
            int plotHeight = this.Height - topMargin - bottomMargin;
            if (plotWidth <= 20 || plotHeight <= 20) return;

            decimal maxVal = _data.Max(p => p.Amount);
            if (maxVal <= 0) maxVal = 1000m;
            maxVal = Math.Ceiling(maxVal * 1.25m / 500m) * 500m;

            // Grid lines (3 horizontal dashed lines)
            using (var gridPen = new Pen(Color.FromArgb(235, 238, 245), 1) { DashStyle = DashStyle.Dash })
            using (var axisBrush = new SolidBrush(ModernTheme.TextMuted))
            {
                for (int step = 0; step <= 3; step++)
                {
                    decimal stepVal = (maxVal / 3) * step;
                    float y = topMargin + plotHeight - (plotHeight * (step / 3f));

                    g.DrawLine(gridPen, leftMargin, y, leftMargin + plotWidth, y);

                    string valStr = stepVal >= 1000000 ? $"{stepVal / 1000000:0.#}M" :
                                    stepVal >= 1000 ? $"{stepVal / 1000:0.#}k" : $"{stepVal:0}";
                    g.DrawString(valStr, ModernTheme.SmallFont, axisBrush, leftMargin - 48, y - 7);
                }
            }

            // Draw Bars
            float colWidth = (float)plotWidth / _data.Count;
            float barWidth = Math.Min(colWidth * 0.55f, 44f);

            for (int i = 0; i < _data.Count; i++)
            {
                var pt = _data[i];
                float colCenterX = leftMargin + (i * colWidth) + (colWidth / 2f);
                float barLeft = colCenterX - (barWidth / 2f);

                float barH = (float)(pt.Amount / maxVal) * plotHeight;
                if (barH < 4f && pt.Amount > 0) barH = 4f;
                float barTop = topMargin + plotHeight - barH;

                bool isHovered = (i == _hoveredIndex);

                // Bar background (Solid fill or safe rounded top)
                if (barH > 0 && barWidth > 2)
                {
                    var barColor = isHovered ? Color.FromArgb(79, 70, 229) : ModernTheme.PrimaryIndigo;
                    using var brush = new SolidBrush(barColor);

                    if (barH > 8 && barWidth > 8)
                    {
                        using var path = CreateRoundedTopRectangle(new RectangleF(barLeft, barTop, barWidth, barH), 4);
                        g.FillPath(brush, path);
                    }
                    else
                    {
                        g.FillRectangle(brush, barLeft, barTop, barWidth, barH);
                    }
                }

                // Day label below axis
                using (var lblBrush = new SolidBrush(isHovered ? ModernTheme.PrimaryIndigo : ModernTheme.TextPrimary))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center };
                    var font = isHovered ? ModernTheme.BodyBoldFont : ModernTheme.SmallFont;
                    g.DrawString(pt.DayLabel, font, lblBrush, colCenterX, topMargin + plotHeight + 10, sf);
                }

                // Amount label above bar
                if (pt.Amount > 0)
                {
                    string amtStr = pt.Amount >= 1000 ? $"{pt.Amount / 1000:0.#}k" : $"{pt.Amount:0}";
                    using (var amtBrush = new SolidBrush(isHovered ? ModernTheme.PrimaryIndigo : ModernTheme.TextMuted))
                    {
                        var sf = new StringFormat { Alignment = StringAlignment.Center };
                        g.DrawString(amtStr, ModernTheme.SmallFont, amtBrush, colCenterX, Math.Max(topMargin, barTop - 18), sf);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WeeklySalesTrendChart paint error: {ex.Message}");
        }
    }

    private static GraphicsPath CreateRoundedTopRectangle(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        if (rect.Height <= 0 || rect.Width <= 0) return path;

        radius = Math.Min(radius, rect.Height / 2f);
        radius = Math.Min(radius, rect.Width / 2f);
        float d = radius * 2;

        path.AddLine(rect.Left, rect.Bottom, rect.Left, rect.Top + radius);
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddLine(rect.Left + radius, rect.Top, rect.Right - radius, rect.Top);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddLine(rect.Right, rect.Top + radius, rect.Right, rect.Bottom);
        path.CloseFigure();
        return path;
    }
}
