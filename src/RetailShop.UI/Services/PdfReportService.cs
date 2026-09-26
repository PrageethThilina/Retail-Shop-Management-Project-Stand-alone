using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RetailShop.Core.Models;
using RetailShop.Core.Security;

namespace RetailShop.UI.Services;

public class PdfReportService
{
    static PdfReportService()
    {
        // QuestPDF Community License for educational / small business / portfolio use
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
    }

    public Task GenerateInventoryReportAsync(IEnumerable<Product> products, string filePath)
    {
        return Task.Run(() =>
        {
            var productList = products.ToList();
            var totalProducts = productList.Count;
            var totalUnits = productList.Sum(p => p.Quantity);
            var lowStockCount = productList.Count(p => p.Quantity <= p.LowStockThreshold);
            var totalValuation = productList.Sum(p => p.Quantity * p.SellingPrice);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Lato"));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(brand =>
                            {
                                brand.Item().Text("POLKOTUWA SUPER STORES").FontSize(16).Bold().FontColor("#0F172A");
                                brand.Item().Text("Enterprise Inventory Valuation & Stock Audit Report").FontSize(11).FontColor("#475569");
                            });

                            row.RelativeItem().AlignRight().Column(meta =>
                            {
                                meta.Item().Text($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor("#64748B");
                                meta.Item().Text($"Auditor: {UserSession.Current.FullName} ({UserSession.Current.Role})").FontSize(8).FontColor("#64748B");
                                meta.Item().Text("CLASSIFICATION: INTERNAL CONFIDENTIAL").FontSize(7).Bold().FontColor("#DC2626");
                            });
                        });

                        col.Item().PaddingTop(12).PaddingBottom(12).LineHorizontal(1).LineColor("#CBD5E1");

                        // KPI Summary Boxes
                        col.Item().PaddingBottom(14).Row(kpis =>
                        {
                            kpis.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(c =>
                            {
                                c.Item().Text("ACTIVE SKUs").FontSize(7.5f).Bold().FontColor("#64748B");
                                c.Item().Text($"{totalProducts:N0} Products").FontSize(13).Bold().FontColor("#1E293B");
                            });

                            kpis.Spacing(10);

                            kpis.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(c =>
                            {
                                c.Item().Text("TOTAL STOCK UNITS").FontSize(7.5f).Bold().FontColor("#64748B");
                                c.Item().Text($"{totalUnits:N0} Units").FontSize(13).Bold().FontColor("#0284C7");
                            });

                            kpis.Spacing(10);

                            kpis.RelativeItem().Border(1).BorderColor(lowStockCount > 0 ? "#FECDD3" : "#E2E8F0").Background(lowStockCount > 0 ? "#FFF1F2" : "#F8FAFC").Padding(8).Column(c =>
                            {
                                c.Item().Text("LOW STOCK WARNINGS").FontSize(7.5f).Bold().FontColor(lowStockCount > 0 ? "#E11D48" : "#64748B");
                                c.Item().Text($"{lowStockCount:N0} Items").FontSize(13).Bold().FontColor(lowStockCount > 0 ? "#BE123C" : "#1E293B");
                            });

                            kpis.Spacing(10);

                            kpis.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(c =>
                            {
                                c.Item().Text("TOTAL STOCK VALUE").FontSize(7.5f).Bold().FontColor("#64748B");
                                c.Item().Text($"LKR {totalValuation:N2}").FontSize(13).Bold().FontColor("#059669");
                            });
                        });
                    });

                    // Content Table
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(28);            // #
                            cols.ConstantColumn(85);            // Code
                            cols.RelativeColumn(3.2f);          // Product Name
                            cols.RelativeColumn(1.8f);          // Category
                            cols.RelativeColumn(1.4f);          // Cost
                            cols.RelativeColumn(1.4f);          // Price
                            cols.ConstantColumn(50);            // Stock
                            cols.ConstantColumn(50);            // Min
                            cols.ConstantColumn(85);            // Status
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background("#1E293B").Padding(6).Text("#").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).Text("Product Code").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).Text("Product Name").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).Text("Category").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).AlignRight().Text("Cost (LKR)").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).AlignRight().Text("Price (LKR)").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).AlignCenter().Text("Qty").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).AlignCenter().Text("Min").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).AlignCenter().Text("Status").FontColor("#FFFFFF").Bold().FontSize(8);
                        });

                        // Rows
                        int index = 1;
                        foreach (var p in productList)
                        {
                            var bg = index % 2 == 0 ? "#F8FAFC" : "#FFFFFF";
                            var isLow = p.Quantity <= p.LowStockThreshold;

                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(index.ToString()).FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(p.ProductCode).Bold().FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(p.Name).FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(p.Category).FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignRight().Text($"{p.BuyingPrice:N2}").FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignRight().Text($"{p.SellingPrice:N2}").Bold().FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignCenter().Text(p.Quantity.ToString()).FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignCenter().Text(p.LowStockThreshold.ToString()).FontSize(8);

                            if (isLow)
                            {
                                table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignCenter().Text("⚠️ LOW STOCK").Bold().FontSize(7.5f).FontColor("#DC2626");
                            }
                            else
                            {
                                table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignCenter().Text("✓ IN STOCK").Bold().FontSize(7.5f).FontColor("#16A34A");
                            }

                            index++;
                        }
                    });

                    // Footer
                    page.Footer().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Text("Polkotuwa Super Stores • ERP Stand-alone 2026 • Confidential Document").FontSize(8).FontColor("#94A3B8");
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Page ").FontSize(8).FontColor("#94A3B8");
                            text.CurrentPageNumber().FontSize(8).FontColor("#94A3B8");
                            text.Span(" of ").FontSize(8).FontColor("#94A3B8");
                            text.TotalPages().FontSize(8).FontColor("#94A3B8");
                        });
                    });
                });
            }).GeneratePdf(filePath);
        });
    }

    public Task GenerateSalesReportAsync(IEnumerable<Sale> sales, string filePath, DateTime? fromDate = null, DateTime? toDate = null)
    {
        return Task.Run(() =>
        {
            var salesList = sales.ToList();
            var totalInvoices = salesList.Count;
            var totalRevenue = salesList.Sum(s => s.NetTotal);
            var totalDiscount = salesList.Sum(s => s.Discount);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Lato"));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(brand =>
                            {
                                brand.Item().Text("POLKOTUWA SUPER STORES").FontSize(16).Bold().FontColor("#0F172A");
                                brand.Item().Text("Sales & Point-of-Sale Transactions Audit Report").FontSize(11).FontColor("#475569");
                            });

                            row.RelativeItem().AlignRight().Column(meta =>
                            {
                                meta.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor("#64748B");
                                meta.Item().Text($"User: {UserSession.Current.FullName} ({UserSession.Current.Role})").FontSize(8).FontColor("#64748B");
                                meta.Item().Text("FINANCIAL AUDIT RECORD").FontSize(7).Bold().FontColor("#2563EB");
                            });
                        });

                        col.Item().PaddingTop(12).PaddingBottom(12).LineHorizontal(1).LineColor("#CBD5E1");

                        // KPI Cards
                        col.Item().PaddingBottom(14).Row(kpis =>
                        {
                            kpis.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(c =>
                            {
                                c.Item().Text("TOTAL TRANSACTIONS").FontSize(7.5f).Bold().FontColor("#64748B");
                                c.Item().Text($"{totalInvoices:N0} Invoices").FontSize(13).Bold().FontColor("#1E293B");
                            });

                            kpis.Spacing(10);

                            kpis.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(c =>
                            {
                                c.Item().Text("TOTAL DISCOUNTS GIVEN").FontSize(7.5f).Bold().FontColor("#64748B");
                                c.Item().Text($"LKR {totalDiscount:N2}").FontSize(13).Bold().FontColor("#DC2626");
                            });

                            kpis.Spacing(10);

                            kpis.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(c =>
                            {
                                c.Item().Text("NET REVENUE REALIZED").FontSize(7.5f).Bold().FontColor("#64748B");
                                c.Item().Text($"LKR {totalRevenue:N2}").FontSize(13).Bold().FontColor("#059669");
                            });
                        });
                    });

                    // Table
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(28);            // #
                            cols.ConstantColumn(120);           // Invoice Number
                            cols.ConstantColumn(105);           // Date
                            cols.RelativeColumn(2.5f);          // Customer
                            cols.RelativeColumn(1.5f);          // Cashier
                            cols.ConstantColumn(80);            // Payment Method
                            cols.RelativeColumn(1.3f);          // Sub Total
                            cols.RelativeColumn(1.2f);          // Discount
                            cols.RelativeColumn(1.4f);          // Net Total
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#1E293B").Padding(6).Text("#").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).Text("Invoice Number").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).Text("Date & Time").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).Text("Customer").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).Text("Cashier").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).AlignCenter().Text("Method").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).AlignRight().Text("Sub Total (LKR)").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).AlignRight().Text("Discount (LKR)").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).AlignRight().Text("Net Total (LKR)").FontColor("#FFFFFF").Bold().FontSize(8);
                        });

                        int index = 1;
                        foreach (var s in salesList)
                        {
                            var bg = index % 2 == 0 ? "#F8FAFC" : "#FFFFFF";

                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(index.ToString()).FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(s.InvoiceNumber).Bold().FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(s.SaleDate.ToString("yyyy-MM-dd HH:mm")).FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(s.CustomerName).FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(s.CashierName).FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignCenter().Text(s.PaymentMethod.ToString()).FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignRight().Text($"{s.SubTotal:N2}").FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignRight().Text($"{s.Discount:N2}").FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignRight().Text($"{s.NetTotal:N2}").Bold().FontSize(8);

                            index++;
                        }
                    });

                    // Footer
                    page.Footer().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Text("Polkotuwa Super Stores • ERP Stand-alone 2026 • Financial Transactions Audit").FontSize(8).FontColor("#94A3B8");
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Page ").FontSize(8).FontColor("#94A3B8");
                            text.CurrentPageNumber().FontSize(8).FontColor("#94A3B8");
                            text.Span(" of ").FontSize(8).FontColor("#94A3B8");
                            text.TotalPages().FontSize(8).FontColor("#94A3B8");
                        });
                    });
                });
            }).GeneratePdf(filePath);
        });
    }

    public Task GeneratePayrollReportAsync(IEnumerable<SalaryRecord> salaries, int year, int month, string filePath)
    {
        return Task.Run(() =>
        {
            var salaryList = salaries.ToList();
            var totalBasic = salaryList.Sum(s => s.BasicSalary);
            var totalAllowances = salaryList.Sum(s => s.Allowances);
            var totalDeductions = salaryList.Sum(s => s.Deductions);
            var totalNet = salaryList.Sum(s => s.NetSalary);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Lato"));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(brand =>
                            {
                                brand.Item().Text("POLKOTUWA SUPER STORES").FontSize(16).Bold().FontColor("#0F172A");
                                brand.Item().Text($"Staff Monthly Payroll Disbursement Audit • {year}-{month:D2}").FontSize(11).FontColor("#475569");
                            });

                            row.RelativeItem().AlignRight().Column(meta =>
                            {
                                meta.Item().Text($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor("#64748B");
                                meta.Item().Text($"Authorized By: {UserSession.Current.FullName}").FontSize(8).FontColor("#64748B");
                                meta.Item().Text("PAYROLL DISBURSEMENT RECORD").FontSize(7).Bold().FontColor("#059669");
                            });
                        });

                        col.Item().PaddingTop(12).PaddingBottom(12).LineHorizontal(1).LineColor("#CBD5E1");

                        col.Item().PaddingBottom(14).Row(kpis =>
                        {
                            kpis.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(c =>
                            {
                                c.Item().Text("STAFF COUNT").FontSize(7.5f).Bold().FontColor("#64748B");
                                c.Item().Text($"{salaryList.Count:N0} Employees").FontSize(13).Bold().FontColor("#1E293B");
                            });

                            kpis.Spacing(10);

                            kpis.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(c =>
                            {
                                c.Item().Text("TOTAL ALLOWANCES").FontSize(7.5f).Bold().FontColor("#64748B");
                                c.Item().Text($"LKR {totalAllowances:N2}").FontSize(13).Bold().FontColor("#0284C7");
                            });

                            kpis.Spacing(10);

                            kpis.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(c =>
                            {
                                c.Item().Text("TOTAL DISBURSED").FontSize(7.5f).Bold().FontColor("#64748B");
                                c.Item().Text($"LKR {totalNet:N2}").FontSize(13).Bold().FontColor("#059669");
                            });
                        });
                    });

                    // Table
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(28);            // #
                            cols.RelativeColumn(2.5f);          // Employee Name
                            cols.RelativeColumn(1.4f);          // Basic
                            cols.RelativeColumn(1.4f);          // Allowances
                            cols.RelativeColumn(1.4f);          // Deductions
                            cols.RelativeColumn(1.6f);          // Net Salary
                            cols.ConstantColumn(80);            // Paid Date
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#1E293B").Padding(6).Text("#").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).Text("Employee Name").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).AlignRight().Text("Basic (LKR)").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).AlignRight().Text("Allowances (LKR)").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).AlignRight().Text("Deductions (LKR)").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).AlignRight().Text("Net Paid (LKR)").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background("#1E293B").Padding(6).AlignCenter().Text("Paid Date").FontColor("#FFFFFF").Bold().FontSize(8);
                        });

                        int index = 1;
                        foreach (var s in salaryList)
                        {
                            var bg = index % 2 == 0 ? "#F8FAFC" : "#FFFFFF";

                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(index.ToString()).FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(s.EmployeeName).Bold().FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignRight().Text($"{s.BasicSalary:N2}").FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignRight().Text($"{s.Allowances:N2}").FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignRight().Text($"{s.Deductions:N2}").FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignRight().Text($"{s.NetSalary:N2}").Bold().FontSize(8);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignCenter().Text(s.PaidDate.ToString("yyyy-MM-dd")).FontSize(8);

                            index++;
                        }
                    });

                    // Footer
                    page.Footer().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Text("Polkotuwa Super Stores • Staff HR & Payroll Audit System").FontSize(8).FontColor("#94A3B8");
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Page ").FontSize(8).FontColor("#94A3B8");
                            text.CurrentPageNumber().FontSize(8).FontColor("#94A3B8");
                            text.Span(" of ").FontSize(8).FontColor("#94A3B8");
                            text.TotalPages().FontSize(8).FontColor("#94A3B8");
                        });
                    });
                });
            }).GeneratePdf(filePath);
        });
    }
}
