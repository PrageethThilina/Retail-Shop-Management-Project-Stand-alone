using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using RetailShop.Core.Models;

namespace RetailShop.UI.Services;

public class ReceiptPrinter
{
    private readonly IConfiguration _configuration;

    public ReceiptPrinter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ShowPrintPreview(Sale sale, IWin32Window? parent = null)
    {
        using var printDoc = new PrintDocument();
        printDoc.DocumentName = $"Receipt_{sale.InvoiceNumber}";
        printDoc.DefaultPageSettings.PaperSize = new PaperSize("Receipt", 315, 600); // 80mm roll width approx
        printDoc.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);

        printDoc.PrintPage += (s, e) => DrawReceipt(e, sale);

        using var previewDlg = new PrintPreviewDialog
        {
            Document = printDoc,
            Width = 450,
            Height = 650,
            StartPosition = FormStartPosition.CenterParent
        };

        if (parent != null)
        {
            previewDlg.ShowDialog(parent);
        }
        else
        {
            previewDlg.ShowDialog();
        }
    }

    private void DrawReceipt(PrintPageEventArgs e, Sale sale)
    {
        var g = e.Graphics!;
        var storeName = _configuration["StoreSettings:StoreName"] ?? "Polkotuwa Super Stores";
        var branch = _configuration["StoreSettings:Branch"] ?? "Main Branch";
        var address = _configuration["StoreSettings:Address"] ?? "Colombo, Sri Lanka";
        var phone = _configuration["StoreSettings:Phone"] ?? "011-2345678";
        var footer = _configuration["StoreSettings:ReceiptFooter"] ?? "Thank you for shopping with us!";

        using var titleFont = new Font("Segoe UI", 12f, FontStyle.Bold);
        using var boldFont = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var regularFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var monoFont = new Font("Consolas", 8.5f, FontStyle.Regular);
        using var brush = new SolidBrush(Color.Black);
        using var pen = new Pen(Color.Black, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };

        float y = 15;
        float left = 15;
        float right = e.PageBounds.Width - 15;
        float width = right - left;

        // Store Header (Centered)
        using var centerFormat = new StringFormat { Alignment = StringAlignment.Center };
        g.DrawString(storeName, titleFont, brush, new RectangleF(left, y, width, 22), centerFormat);
        y += 24;

        g.DrawString(branch, regularFont, brush, new RectangleF(left, y, width, 16), centerFormat);
        y += 16;
        g.DrawString(address, regularFont, brush, new RectangleF(left, y, width, 16), centerFormat);
        y += 16;
        g.DrawString($"Tel: {phone}", regularFont, brush, new RectangleF(left, y, width, 16), centerFormat);
        y += 22;

        g.DrawLine(pen, left, y, right, y);
        y += 8;

        // Invoice Meta
        g.DrawString($"Invoice: {sale.InvoiceNumber}", boldFont, brush, left, y);
        y += 16;
        g.DrawString($"Date:    {sale.SaleDate.ToLocalTime():yyyy-MM-dd HH:mm}", regularFont, brush, left, y);
        y += 16;
        g.DrawString($"Cashier: {sale.CashierName}", regularFont, brush, left, y);
        y += 16;
        g.DrawString($"Customer:{sale.CustomerName}", regularFont, brush, left, y);
        y += 20;

        g.DrawLine(pen, left, y, right, y);
        y += 6;

        // Table Header
        g.DrawString("ITEM", boldFont, brush, left, y);
        g.DrawString("QTY", boldFont, brush, left + 140, y);
        g.DrawString("PRICE", boldFont, brush, left + 180, y);
        using var rightAlign = new StringFormat { Alignment = StringAlignment.Far };
        g.DrawString("TOTAL", boldFont, brush, right, y, rightAlign);
        y += 18;

        g.DrawLine(pen, left, y, right, y);
        y += 6;

        // Line Items
        foreach (var item in sale.Items)
        {
            var displayName = item.ProductName.Length > 20 ? item.ProductName[..18] + ".." : item.ProductName;
            g.DrawString(displayName, regularFont, brush, left, y);
            g.DrawString(item.Quantity.ToString(), regularFont, brush, left + 145, y);
            g.DrawString(item.UnitPrice.ToString("N2"), regularFont, brush, left + 180, y);
            g.DrawString(item.TotalPrice.ToString("N2"), regularFont, brush, right, y, rightAlign);
            y += 16;
        }

        y += 4;
        g.DrawLine(pen, left, y, right, y);
        y += 8;

        // Totals
        g.DrawString("Sub Total:", regularFont, brush, left + 110, y);
        g.DrawString(sale.SubTotal.ToString("N2"), regularFont, brush, right, y, rightAlign);
        y += 16;

        if (sale.Discount > 0)
        {
            g.DrawString("Discount:", regularFont, brush, left + 110, y);
            g.DrawString($"-{sale.Discount:N2}", regularFont, brush, right, y, rightAlign);
            y += 16;
        }

        g.DrawString("NET TOTAL:", boldFont, brush, left + 110, y);
        g.DrawString($"LKR {sale.NetTotal:N2}", boldFont, brush, right, y, rightAlign);
        y += 20;

        g.DrawString("Paid Amount:", regularFont, brush, left + 110, y);
        g.DrawString(sale.PaidAmount.ToString("N2"), regularFont, brush, right, y, rightAlign);
        y += 16;

        g.DrawString("Balance / Change:", boldFont, brush, left + 110, y);
        g.DrawString(sale.Balance.ToString("N2"), boldFont, brush, right, y, rightAlign);
        y += 22;

        g.DrawLine(pen, left, y, right, y);
        y += 12;

        // Footer & Barcode simulation
        g.DrawString($"*{sale.InvoiceNumber}*", monoFont, brush, new RectangleF(left, y, width, 18), centerFormat);
        y += 20;

        g.DrawString(footer, regularFont, brush, new RectangleF(left, y, width, 32), centerFormat);
    }
}
