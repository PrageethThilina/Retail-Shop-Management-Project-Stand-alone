using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using RetailShop.Core.Common;
using RetailShop.Core.Enums;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;
using RetailShop.Core.Security;
using RetailShop.UI.Controls;
using RetailShop.UI.Services;
using RetailShop.UI.Styles;

namespace RetailShop.UI.Views;

public class BillingView : UserControl
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly ReceiptPrinter _receiptPrinter;
    private readonly PdfReportService? _pdfReportService;

    // POS Controls
    private ComboBox _cmbProducts = null!;
    private Label _lblUnitPrice = null!;
    private Label _lblAvailableStock = null!;
    private NumericUpDown _numQuantity = null!;
    private Button _btnAddToCart = null!;
    private DataGridView _dgvCart = null!;

    // Customer & Payment Controls
    private TextBox _txtCustomerName = null!;
    private TextBox _txtCustomerPhone = null!;
    private ComboBox _cmbPaymentMethod = null!;
    private Label _lblSubTotal = null!;
    private NumericUpDown _numDiscount = null!;
    private Label _lblNetTotal = null!;
    private NumericUpDown _numPaidAmount = null!;
    private Label _lblBalance = null!;
    private Button _btnCheckout = null!;
    private Button _btnClearCart = null!;

    // History Tab Controls
    private DataGridView _dgvHistory = null!;
    private PaginationControl _pagerHistory = null!;
    private TextBox _txtSearchHistory = null!;

    private readonly List<SaleItem> _cartItems = new();
    private IReadOnlyList<Product> _allProducts = Array.Empty<Product>();

    public BillingView(ISaleRepository saleRepository, IProductRepository productRepository, ReceiptPrinter receiptPrinter, PdfReportService? pdfReportService = null)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _receiptPrinter = receiptPrinter;
        _pdfReportService = pdfReportService;

        InitializeComponents();
        _ = LoadProductsAsync();
        _ = LoadHistoryAsync();
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ModernTheme.BgLight;
        this.Padding = new Padding(16);
        this.Font = ModernTheme.BodyFont;

        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = ModernTheme.BodyBoldFont
        };

        var tabPos = new TabPage("🛒 POS Checkout Terminal");
        tabPos.BackColor = ModernTheme.BgLight;
        tabPos.Padding = new Padding(12);

        var tabHistory = new TabPage("📜 Transaction History (Paged)");
        tabHistory.BackColor = ModernTheme.BgLight;
        tabHistory.Padding = new Padding(12);

        // Build POS Tab
        BuildPosTab(tabPos);

        // Build History Tab
        BuildHistoryTab(tabHistory);

        tabControl.TabPages.Add(tabPos);
        tabControl.TabPages.Add(tabHistory);

        this.Controls.Add(tabControl);
    }

    private void BuildPosTab(TabPage tab)
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 640,
            BackColor = ModernTheme.BorderColor
        };

        // LEFT PANEL: Product Selection + Cart
        var pnlLeft = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(16)
        };

        var lblSelectTitle = new Label
        {
            Text = "Select Product to Add",
            Font = ModernTheme.BodyBoldFont,
            ForeColor = ModernTheme.TextPrimary,
            Location = new Point(16, 12),
            AutoSize = true
        };

        _cmbProducts = new ComboBox
        {
            Location = new Point(16, 36),
            Size = new Size(320, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Font = ModernTheme.BodyFont
        };
        _cmbProducts.SelectedIndexChanged += (s, e) => OnProductSelected();

        _lblUnitPrice = new Label
        {
            Text = "Price: LKR 0.00",
            Font = ModernTheme.BodyBoldFont,
            ForeColor = ModernTheme.PrimaryIndigo,
            Location = new Point(350, 40),
            AutoSize = true
        };

        _lblAvailableStock = new Label
        {
            Text = "Stock: 0",
            Font = ModernTheme.SmallFont,
            ForeColor = ModernTheme.TextMuted,
            Location = new Point(480, 42),
            AutoSize = true
        };

        var lblQty = new Label
        {
            Text = "Qty:",
            Font = ModernTheme.BodyFont,
            Location = new Point(16, 78),
            AutoSize = true
        };

        _numQuantity = new NumericUpDown
        {
            Location = new Point(55, 75),
            Size = new Size(80, 28),
            Minimum = 1,
            Maximum = 9999,
            Value = 1,
            Font = ModernTheme.BodyBoldFont
        };

        _btnAddToCart = new Button
        {
            Text = "➕ Add to Cart",
            Location = new Point(150, 72),
            Size = new Size(130, 32)
        };
        ModernTheme.ApplyPrimaryButtonStyle(_btnAddToCart);
        _btnAddToCart.Click += (s, e) => AddCurrentItemToCart();

        _btnClearCart = new Button
        {
            Text = "🗑 Clear Cart",
            Location = new Point(290, 72),
            Size = new Size(110, 32)
        };
        ModernTheme.ApplySecondaryButtonStyle(_btnClearCart);
        _btnClearCart.Click += (s, e) => ClearCart();

        // Cart DataGridView
        _dgvCart = new DataGridView
        {
            Location = new Point(16, 120),
            Size = new Size(split.SplitterDistance - 32, 420),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        ModernTheme.ApplyDataGridStyle(_dgvCart);

        _dgvCart.Columns.Add("Code", "Code");
        _dgvCart.Columns.Add("Name", "Product Name");
        _dgvCart.Columns.Add("UnitPrice", "Unit Price (LKR)");
        _dgvCart.Columns.Add("Quantity", "Qty");
        _dgvCart.Columns.Add("TotalPrice", "Total (LKR)");

        var btnRemoveCol = new DataGridViewButtonColumn
        {
            Name = "Remove",
            HeaderText = "Action",
            Text = "Remove",
            UseColumnTextForButtonValue = true,
            Width = 90
        };
        _dgvCart.Columns.Add(btnRemoveCol);

        _dgvCart.Columns[0].Width = 100;
        _dgvCart.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _dgvCart.Columns[2].Width = 130;
        _dgvCart.Columns[3].Width = 70;
        _dgvCart.Columns[4].Width = 120;
        _dgvCart.Columns[5].Width = 90;

        _dgvCart.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _dgvCart.ColumnHeadersHeight = 44;

        _dgvCart.CellContentClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && _dgvCart.Columns[e.ColumnIndex].Name == "Remove")
            {
                _cartItems.RemoveAt(e.RowIndex);
                RefreshCartGrid();
            }
        };

        pnlLeft.Controls.Add(lblSelectTitle);
        pnlLeft.Controls.Add(_cmbProducts);
        pnlLeft.Controls.Add(_lblUnitPrice);
        pnlLeft.Controls.Add(_lblAvailableStock);
        pnlLeft.Controls.Add(lblQty);
        pnlLeft.Controls.Add(_numQuantity);
        pnlLeft.Controls.Add(_btnAddToCart);
        pnlLeft.Controls.Add(_btnClearCart);
        pnlLeft.Controls.Add(_dgvCart);

        // RIGHT PANEL: Payment & Totals
        var pnlRight = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(20)
        };

        var lblCheckoutTitle = new Label
        {
            Text = "Invoice & Checkout",
            Font = ModernTheme.TitleFont,
            ForeColor = ModernTheme.TextPrimary,
            Location = new Point(20, 15),
            AutoSize = true
        };

        // Customer Info
        var lblCust = new Label { Text = "Customer Name:", Location = new Point(20, 60), AutoSize = true, Font = ModernTheme.SmallFont };
        _txtCustomerName = new TextBox { Location = new Point(20, 80), Size = new Size(260, 26), Text = "Walk-in Customer", Font = ModernTheme.BodyFont };

        var lblPhone = new Label { Text = "Customer Mobile:", Location = new Point(20, 115), AutoSize = true, Font = ModernTheme.SmallFont };
        _txtCustomerPhone = new TextBox { Location = new Point(20, 135), Size = new Size(260, 26), Font = ModernTheme.BodyFont };

        var lblMethod = new Label { Text = "Payment Method:", Location = new Point(20, 170), AutoSize = true, Font = ModernTheme.SmallFont };
        _cmbPaymentMethod = new ComboBox { Location = new Point(20, 190), Size = new Size(260, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = ModernTheme.BodyFont };
        _cmbPaymentMethod.Items.AddRange(new object[] { "Cash", "Card", "Bank Transfer" });
        _cmbPaymentMethod.SelectedIndex = 0;

        // Separator
        var sep = new Panel { Location = new Point(20, 230), Size = new Size(260, 1), BackColor = ModernTheme.BorderColor };

        // Calculations
        var lblSubTitle = new Label { Text = "Sub Total:", Location = new Point(20, 245), AutoSize = true, Font = ModernTheme.BodyFont, ForeColor = ModernTheme.TextMuted };
        _lblSubTotal = new Label { Text = "LKR 0.00", Location = new Point(140, 245), AutoSize = true, Font = ModernTheme.BodyBoldFont, ForeColor = ModernTheme.TextPrimary };

        var lblDisc = new Label { Text = "Discount (LKR):", Location = new Point(20, 280), AutoSize = true, Font = ModernTheme.BodyFont, ForeColor = ModernTheme.TextMuted };
        _numDiscount = new NumericUpDown
        {
            Location = new Point(140, 276),
            Size = new Size(140, 26),
            Maximum = 1000000,
            DecimalPlaces = 2,
            Font = ModernTheme.BodyFont
        };
        _numDiscount.ValueChanged += (s, e) => RecalculateTotals();

        var lblNetTitle = new Label { Text = "NET TOTAL:", Location = new Point(20, 315), AutoSize = true, Font = ModernTheme.SubHeaderFont, ForeColor = ModernTheme.TextPrimary };
        _lblNetTotal = new Label { Text = "LKR 0.00", Location = new Point(140, 315), AutoSize = true, Font = ModernTheme.TitleFont, ForeColor = ModernTheme.PrimaryIndigo };

        var lblPaid = new Label { Text = "Paid Amount (LKR):", Location = new Point(20, 360), AutoSize = true, Font = ModernTheme.BodyFont, ForeColor = ModernTheme.TextMuted };
        _numPaidAmount = new NumericUpDown
        {
            Location = new Point(140, 356),
            Size = new Size(140, 26),
            Maximum = 10000000,
            DecimalPlaces = 2,
            Font = ModernTheme.BodyBoldFont
        };
        _numPaidAmount.ValueChanged += (s, e) => RecalculateTotals();

        var lblBalTitle = new Label { Text = "Balance / Change:", Location = new Point(20, 400), AutoSize = true, Font = ModernTheme.BodyBoldFont, ForeColor = ModernTheme.TextPrimary };
        _lblBalance = new Label { Text = "LKR 0.00", Location = new Point(140, 400), AutoSize = true, Font = ModernTheme.SubHeaderFont, ForeColor = ModernTheme.SuccessEmerald };

        _btnCheckout = new Button
        {
            Text = "💳 Complete & Print Receipt",
            Location = new Point(20, 445),
            Size = new Size(260, 48)
        };
        ModernTheme.ApplySuccessButtonStyle(_btnCheckout);
        _btnCheckout.Click += async (s, e) => await ProcessCheckoutAsync();

        pnlRight.Controls.Add(lblCheckoutTitle);
        pnlRight.Controls.Add(lblCust);
        pnlRight.Controls.Add(_txtCustomerName);
        pnlRight.Controls.Add(lblPhone);
        pnlRight.Controls.Add(_txtCustomerPhone);
        pnlRight.Controls.Add(lblMethod);
        pnlRight.Controls.Add(_cmbPaymentMethod);
        pnlRight.Controls.Add(sep);
        pnlRight.Controls.Add(lblSubTitle);
        pnlRight.Controls.Add(_lblSubTotal);
        pnlRight.Controls.Add(lblDisc);
        pnlRight.Controls.Add(_numDiscount);
        pnlRight.Controls.Add(lblNetTitle);
        pnlRight.Controls.Add(_lblNetTotal);
        pnlRight.Controls.Add(lblPaid);
        pnlRight.Controls.Add(_numPaidAmount);
        pnlRight.Controls.Add(lblBalTitle);
        pnlRight.Controls.Add(_lblBalance);
        pnlRight.Controls.Add(_btnCheckout);

        split.Panel1.Controls.Add(pnlLeft);
        split.Panel2.Controls.Add(pnlRight);
        tab.Controls.Add(split);
    }

    private void BuildHistoryTab(TabPage tab)
    {
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(12)
        };

        var lblSearch = new Label { Text = "Search Invoice/Customer:", Location = new Point(12, 16), AutoSize = true, Font = ModernTheme.BodyBoldFont };
        _txtSearchHistory = new TextBox { Location = new Point(215, 12), Size = new Size(220, 28), Font = ModernTheme.BodyFont };

        var btnSearch = new Button { Text = "🔍 Search", Location = new Point(445, 11), Size = new Size(95, 30) };
        ModernTheme.ApplyPrimaryButtonStyle(btnSearch);
        btnSearch.Click += async (s, e) => await LoadHistoryAsync(1);

        var btnReprint = new Button { Text = "🖨 Re-Print Selected", Location = new Point(550, 11), Size = new Size(160, 30) };
        ModernTheme.ApplySecondaryButtonStyle(btnReprint);
        btnReprint.Click += async (s, e) => await ReprintSelectedInvoiceAsync();

        var btnExportPdf = new Button { Text = "📄 Export PDF", Location = new Point(720, 11), Size = new Size(125, 30) };
        ModernTheme.ApplySecondaryButtonStyle(btnExportPdf);
        btnExportPdf.Click += async (s, e) => await ExportSalesReportPdfAsync();

        pnlTop.Controls.Add(lblSearch);
        pnlTop.Controls.Add(_txtSearchHistory);
        pnlTop.Controls.Add(btnSearch);
        pnlTop.Controls.Add(btnReprint);
        pnlTop.Controls.Add(btnExportPdf);

        _dgvHistory = new DataGridView
        {
            Dock = DockStyle.Fill
        };
        ModernTheme.ApplyDataGridStyle(_dgvHistory);

        _dgvHistory.Columns.Add("InvoiceNumber", "Invoice #");
        _dgvHistory.Columns.Add("CustomerName", "Customer");
        _dgvHistory.Columns.Add("SaleDate", "Date & Time");
        _dgvHistory.Columns.Add("SubTotal", "Sub Total (LKR)");
        _dgvHistory.Columns.Add("Discount", "Discount (LKR)");
        _dgvHistory.Columns.Add("NetTotal", "Net Total (LKR)");
        _dgvHistory.Columns.Add("PaidAmount", "Paid (LKR)");
        _dgvHistory.Columns.Add("Balance", "Balance (LKR)");
        _dgvHistory.Columns.Add("CashierName", "Cashier");

        _dgvHistory.Columns[0].Width = 160;
        _dgvHistory.Columns[1].Width = 170;
        _dgvHistory.Columns[2].Width = 140;
        _dgvHistory.Columns[3].Width = 120;
        _dgvHistory.Columns[4].Width = 120;
        _dgvHistory.Columns[5].Width = 130;
        _dgvHistory.Columns[6].Width = 120;
        _dgvHistory.Columns[7].Width = 120;
        _dgvHistory.Columns[8].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        _dgvHistory.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _dgvHistory.ColumnHeadersHeight = 44;

        _pagerHistory = new PaginationControl
        {
            Dock = DockStyle.Bottom
        };
        _pagerHistory.PageChanged += async (s, page) => await LoadHistoryAsync(page);
        _pagerHistory.PageSizeChanged += async (s, size) => await LoadHistoryAsync(1);

        tab.Controls.Add(_dgvHistory);
        tab.Controls.Add(_pagerHistory);
        tab.Controls.Add(pnlTop);
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            _allProducts = await _productRepository.GetAllActiveAsync();
            _cmbProducts.Items.Clear();

            foreach (var prod in _allProducts)
            {
                _cmbProducts.Items.Add($"{prod.ProductCode} - {prod.Name} (Stock: {prod.Quantity})");
            }

            if (_cmbProducts.Items.Count > 0)
                _cmbProducts.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load product list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnProductSelected()
    {
        if (_cmbProducts.SelectedIndex < 0 || _cmbProducts.SelectedIndex >= _allProducts.Count)
            return;

        var product = _allProducts[_cmbProducts.SelectedIndex];
        _lblUnitPrice.Text = $"Price: LKR {product.SellingPrice:N2}";
        _lblAvailableStock.Text = $"Stock: {product.Quantity}";
        _lblAvailableStock.ForeColor = product.IsLowStock ? ModernTheme.DangerRose : ModernTheme.TextMuted;
        _numQuantity.Maximum = Math.Max(1, product.Quantity);
    }

    private void AddCurrentItemToCart()
    {
        if (_cmbProducts.SelectedIndex < 0 || _cmbProducts.SelectedIndex >= _allProducts.Count)
            return;

        var product = _allProducts[_cmbProducts.SelectedIndex];
        var qty = (int)_numQuantity.Value;

        if (product.Quantity < qty)
        {
            MessageBox.Show($"Cannot add {qty} items. Only {product.Quantity} in stock.", "Insufficient Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var existing = _cartItems.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing != null)
        {
            if (existing.Quantity + qty > product.Quantity)
            {
                MessageBox.Show($"Total quantity in cart would exceed available stock ({product.Quantity}).", "Stock Limit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            existing.Quantity += qty;
        }
        else
        {
            _cartItems.Add(new SaleItem
            {
                ProductId = product.Id,
                ProductCode = product.ProductCode,
                ProductName = product.Name,
                UnitPrice = product.SellingPrice,
                Quantity = qty
            });
        }

        RefreshCartGrid();
    }

    private void ClearCart()
    {
        _cartItems.Clear();
        RefreshCartGrid();
    }

    private void RefreshCartGrid()
    {
        _dgvCart.Rows.Clear();
        foreach (var item in _cartItems)
        {
            _dgvCart.Rows.Add(item.ProductCode, item.ProductName, item.UnitPrice.ToString("N2"), item.Quantity, item.TotalPrice.ToString("N2"));
        }
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        var subTotal = _cartItems.Sum(i => i.TotalPrice);
        var discount = _numDiscount.Value;
        var netTotal = Math.Max(0, subTotal - discount);
        var paid = _numPaidAmount.Value;
        var balance = paid - netTotal;

        _lblSubTotal.Text = $"LKR {subTotal:N2}";
        _lblNetTotal.Text = $"LKR {netTotal:N2}";
        _lblBalance.Text = $"LKR {balance:N2}";
        _lblBalance.ForeColor = balance >= 0 ? ModernTheme.SuccessEmerald : ModernTheme.DangerRose;
    }

    private async Task ProcessCheckoutAsync()
    {
        if (_cartItems.Count == 0)
        {
            MessageBox.Show("Cart is empty! Add products before checking out.", "Empty Cart", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var subTotal = _cartItems.Sum(i => i.TotalPrice);
        var discount = _numDiscount.Value;
        var netTotal = Math.Max(0, subTotal - discount);
        var paid = _numPaidAmount.Value;

        if (paid < netTotal && _cmbPaymentMethod.SelectedIndex == 0) // Cash
        {
            MessageBox.Show($"Insufficient cash paid! Required: LKR {netTotal:N2}, Given: LKR {paid:N2}", "Payment Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var sale = new Sale
        {
            CustomerName = string.IsNullOrWhiteSpace(_txtCustomerName.Text) ? "Walk-in Customer" : _txtCustomerName.Text.Trim(),
            CustomerPhone = _txtCustomerPhone.Text.Trim(),
            SubTotal = subTotal,
            Discount = discount,
            NetTotal = netTotal,
            PaidAmount = paid == 0 && _cmbPaymentMethod.SelectedIndex != 0 ? netTotal : paid,
            Balance = Math.Max(0, paid - netTotal),
            PaymentMethod = (PaymentMethod)(_cmbPaymentMethod.SelectedIndex + 1),
            CashierId = UserSession.Current.UserId ?? 1,
            CashierName = UserSession.Current.FullName,
            SaleDate = DateTime.UtcNow,
            Items = new List<SaleItem>(_cartItems)
        };

        _btnCheckout.Enabled = false;
        try
        {
            var invoiceNum = await _saleRepository.CreateSaleAsync(sale);
            sale.InvoiceNumber = invoiceNum;

            // Prompt print
            var confirmPrint = MessageBox.Show($"Sale completed successfully!\nInvoice: {invoiceNum}\nNet Total: LKR {netTotal:N2}\n\nWould you like to print the receipt now?",
                "Checkout Successful", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (confirmPrint == DialogResult.Yes)
            {
                _receiptPrinter.ShowPrintPreview(sale, this.FindForm());
            }

            // Reset cart
            ClearCart();
            _txtCustomerName.Text = "Walk-in Customer";
            _txtCustomerPhone.Clear();
            _numDiscount.Value = 0;
            _numPaidAmount.Value = 0;

            // Refresh products stock & history
            await LoadProductsAsync();
            await LoadHistoryAsync(1);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Checkout failed: {ex.Message}", "Transaction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnCheckout.Enabled = true;
        }
    }

    private async Task LoadHistoryAsync(int page = 1)
    {
        try
        {
            var request = new PagedRequest
            {
                PageNumber = page,
                PageSize = _pagerHistory.SelectedPageSize,
                Search = _txtSearchHistory?.Text.Trim()
            };

            var result = await _saleRepository.GetPagedAsync(request);

            _dgvHistory.Rows.Clear();
            foreach (var s in result.Items)
            {
                _dgvHistory.Rows.Add(
                    s.InvoiceNumber,
                    s.CustomerName,
                    s.SaleDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    s.SubTotal.ToString("N2"),
                    s.Discount.ToString("N2"),
                    s.NetTotal.ToString("N2"),
                    s.PaidAmount.ToString("N2"),
                    s.Balance.ToString("N2"),
                    s.CashierName
                );
            }

            _pagerHistory.BindPagedResult(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading history: {ex.Message}");
        }
    }

    private async Task ReprintSelectedInvoiceAsync()
    {
        if (_dgvHistory.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select an invoice from the history table to reprint.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var invoiceNum = _dgvHistory.SelectedRows[0].Cells["InvoiceNumber"].Value?.ToString();
        if (string.IsNullOrWhiteSpace(invoiceNum)) return;

        var sale = await _saleRepository.GetByInvoiceNumberAsync(invoiceNum);
        if (sale != null)
        {
            _receiptPrinter.ShowPrintPreview(sale, this.FindForm());
        }
    }

    private async Task ExportSalesReportPdfAsync()
    {
        if (_pdfReportService == null)
        {
            MessageBox.Show("PDF Reporting Service is not available.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "PDF Document (*.pdf)|*.pdf",
            FileName = $"Polkotuwa_Sales_Audit_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
            Title = "Save Sales Audit PDF Report"
        };

        if (sfd.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                var salesResult = await _saleRepository.GetPagedAsync(new PagedRequest { PageNumber = 1, PageSize = 200, Search = _txtSearchHistory.Text.Trim() });
                await _pdfReportService.GenerateSalesReportAsync(salesResult.Items, sfd.FileName);

                var prompt = MessageBox.Show(
                    $"Sales Audit PDF Report generated successfully!\n\nFile saved to:\n{sfd.FileName}\n\nDo you want to open the report now?",
                    "Report Generated",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (prompt == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate PDF Report: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
