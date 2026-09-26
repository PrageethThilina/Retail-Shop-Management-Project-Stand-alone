using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using RetailShop.Core.Common;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;
using RetailShop.Core.Security;
using RetailShop.UI.Controls;
using RetailShop.UI.Services;
using RetailShop.UI.Styles;

namespace RetailShop.UI.Views;

public class ProductsView : UserControl
{
    private readonly IProductRepository _productRepository;
    private readonly PdfReportService? _pdfReportService;

    private DataGridView _dgvProducts = null!;
    private PaginationControl _pager = null!;
    private TextBox _txtSearch = null!;
    private CheckBox _chkLowStockOnly = null!;
    private Button _btnAdd = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Button _btnExportPdf = null!;

    public ProductsView(IProductRepository productRepository, PdfReportService? pdfReportService = null)
    {
        _productRepository = productRepository;
        _pdfReportService = pdfReportService;

        InitializeComponents();
        _ = LoadDataAsync();
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ModernTheme.BgLight;
        this.Padding = new Padding(16);
        this.Font = ModernTheme.BodyFont;

        // Top Action Bar
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(12)
        };

        var lblSearch = new Label
        {
            Text = "Search Inventory:",
            Location = new Point(12, 18),
            AutoSize = true,
            Font = ModernTheme.BodyBoldFont
        };

        _txtSearch = new TextBox
        {
            Location = new Point(165, 14),
            Size = new Size(220, 28),
            Font = ModernTheme.BodyFont
        };
        _txtSearch.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                await LoadDataAsync(1);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };

        var btnSearch = new Button
        {
            Text = "🔍 Search",
            Location = new Point(395, 13),
            Size = new Size(90, 30)
        };
        ModernTheme.ApplyPrimaryButtonStyle(btnSearch);
        btnSearch.Click += async (s, e) => await LoadDataAsync(1);

        _chkLowStockOnly = new CheckBox
        {
            Text = "⚠️ Low Stock Only",
            Location = new Point(495, 18),
            AutoSize = true,
            ForeColor = ModernTheme.DangerRose,
            Font = ModernTheme.BodyBoldFont,
            Cursor = Cursors.Hand
        };
        _chkLowStockOnly.CheckedChanged += async (s, e) => await LoadDataAsync(1);

        // Action Buttons
        _btnExportPdf = new Button
        {
            Text = "📄 Export PDF",
            Location = new Point(pnlTop.Width - 485, 14),
            Size = new Size(115, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ModernTheme.ApplySecondaryButtonStyle(_btnExportPdf);
        _btnExportPdf.Click += async (s, e) => await ExportInventoryReportPdfAsync();

        _btnAdd = new Button
        {
            Text = "➕ New Product",
            Location = new Point(pnlTop.Width - 360, 14),
            Size = new Size(130, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ModernTheme.ApplySuccessButtonStyle(_btnAdd);
        _btnAdd.Click += (s, e) => ShowProductDialog(null);

        _btnEdit = new Button
        {
            Text = "✏️ Edit",
            Location = new Point(pnlTop.Width - 220, 14),
            Size = new Size(95, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ModernTheme.ApplySecondaryButtonStyle(_btnEdit);
        _btnEdit.Click += (s, e) => EditSelectedProduct();

        _btnDelete = new Button
        {
            Text = "🗑 Delete",
            Location = new Point(pnlTop.Width - 115, 14),
            Size = new Size(95, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ModernTheme.ApplyDangerButtonStyle(_btnDelete);
        _btnDelete.Click += async (s, e) => await DeleteSelectedProductAsync();

        // RBAC Enforcement: Cashier cannot add, edit, or delete inventory items
        var canManage = UserSession.Current.HasPermission(RolePermissions.ManageProducts);
        _btnAdd.Visible = canManage;
        _btnEdit.Visible = canManage;
        _btnDelete.Visible = canManage;

        pnlTop.Controls.Add(lblSearch);
        pnlTop.Controls.Add(_txtSearch);
        pnlTop.Controls.Add(btnSearch);
        pnlTop.Controls.Add(_chkLowStockOnly);
        pnlTop.Controls.Add(_btnExportPdf);
        pnlTop.Controls.Add(_btnAdd);
        pnlTop.Controls.Add(_btnEdit);
        pnlTop.Controls.Add(_btnDelete);

        // DataGridView
        _dgvProducts = new DataGridView
        {
            Dock = DockStyle.Fill
        };
        ModernTheme.ApplyDataGridStyle(_dgvProducts);

        _dgvProducts.Columns.Add("Id", "ID");
        _dgvProducts.Columns.Add("ProductCode", "Product Code");
        _dgvProducts.Columns.Add("Name", "Product Name");
        _dgvProducts.Columns.Add("Category", "Category");
        _dgvProducts.Columns.Add("BuyingPrice", "Cost (LKR)");
        _dgvProducts.Columns.Add("SellingPrice", "Price (LKR)");
        _dgvProducts.Columns.Add("Quantity", "Stock Qty");
        _dgvProducts.Columns.Add("Threshold", "Min Threshold");
        _dgvProducts.Columns.Add("Status", "Inventory Status");

        _dgvProducts.Columns[0].Width = 60;
        _dgvProducts.Columns[1].Width = 140;
        _dgvProducts.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _dgvProducts.Columns[3].Width = 140;
        _dgvProducts.Columns[4].Width = 120;
        _dgvProducts.Columns[5].Width = 120;
        _dgvProducts.Columns[6].Width = 110;
        _dgvProducts.Columns[7].Width = 130;
        _dgvProducts.Columns[8].Width = 150;

        _dgvProducts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _dgvProducts.ColumnHeadersHeight = 44;

        _dgvProducts.CellFormatting += (s, e) =>
        {
            if (e.RowIndex >= 0 && _dgvProducts.Columns[e.ColumnIndex].Name == "Status")
            {
                var val = e.Value?.ToString();
                if (val == "LOW STOCK")
                {
                    e.CellStyle!.ForeColor = ModernTheme.DangerRose;
                    e.CellStyle.Font = ModernTheme.BodyBoldFont;
                }
                else
                {
                    e.CellStyle!.ForeColor = ModernTheme.SuccessEmerald;
                }
            }
        };

        // Pager
        _pager = new PaginationControl
        {
            Dock = DockStyle.Bottom
        };
        _pager.PageChanged += async (s, page) => await LoadDataAsync(page);
        _pager.PageSizeChanged += async (s, size) => await LoadDataAsync(1);

        this.Controls.Add(_dgvProducts);
        this.Controls.Add(_pager);
        this.Controls.Add(pnlTop);
    }

    public async Task LoadDataAsync(int page = 1)
    {
        try
        {
            var request = new PagedRequest
            {
                PageNumber = page,
                PageSize = _pager.SelectedPageSize,
                Search = _txtSearch.Text.Trim()
            };

            var result = await _productRepository.GetPagedAsync(request);

            _dgvProducts.Rows.Clear();
            foreach (var p in result.Items)
            {
                if (_chkLowStockOnly.Checked && !p.IsLowStock)
                    continue;

                var rowIndex = _dgvProducts.Rows.Add(
                    p.Id,
                    p.ProductCode,
                    p.Name,
                    p.Category,
                    p.BuyingPrice.ToString("N2"),
                    p.SellingPrice.ToString("N2"),
                    p.Quantity,
                    p.LowStockThreshold,
                    p.IsLowStock ? "LOW STOCK" : "In Stock"
                );

                if (p.IsLowStock)
                {
                    _dgvProducts.Rows[rowIndex].Cells["Quantity"].Style.ForeColor = ModernTheme.DangerRose;
                    _dgvProducts.Rows[rowIndex].Cells["Quantity"].Style.Font = ModernTheme.BodyBoldFont;
                }
            }

            _pager.BindPagedResult(result);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load products: {ex.Message}", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void EditSelectedProduct()
    {
        if (_dgvProducts.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select a product to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!int.TryParse(_dgvProducts.SelectedRows[0].Cells["Id"].Value?.ToString(), out var id))
            return;

        _ = OpenEditDialogAsync(id);
    }

    private async Task OpenEditDialogAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product != null)
        {
            ShowProductDialog(product);
        }
    }

    private async Task DeleteSelectedProductAsync()
    {
        if (_dgvProducts.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select a product to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!int.TryParse(_dgvProducts.SelectedRows[0].Cells["Id"].Value?.ToString(), out var id))
            return;

        var name = _dgvProducts.SelectedRows[0].Cells["Name"].Value?.ToString() ?? "Product";

        var confirm = MessageBox.Show($"Are you sure you want to delete product '{name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm == DialogResult.Yes)
        {
            await _productRepository.DeleteAsync(id);
            await LoadDataAsync(_pager.CurrentPage);
        }
    }

    private void ShowProductDialog(Product? existing)
    {
        using var dlg = new Form
        {
            Text = existing == null ? "Add New Inventory Product" : $"Edit Product - {existing.Name}",
            Size = new Size(420, 480),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(20)
        };

        var lblCode = new Label { Text = "Product Code / Barcode:", Location = new Point(20, 15), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtCode = new TextBox { Location = new Point(20, 35), Size = new Size(360, 26), Text = existing?.ProductCode ?? "", Font = ModernTheme.BodyFont };

        var lblName = new Label { Text = "Product Name:", Location = new Point(20, 70), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtName = new TextBox { Location = new Point(20, 90), Size = new Size(360, 26), Text = existing?.Name ?? "", Font = ModernTheme.BodyFont };

        var lblCat = new Label { Text = "Category:", Location = new Point(20, 125), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtCat = new TextBox { Location = new Point(20, 145), Size = new Size(360, 26), Text = existing?.Category ?? "General", Font = ModernTheme.BodyFont };

        var lblBuy = new Label { Text = "Buying Cost (LKR):", Location = new Point(20, 180), AutoSize = true, Font = ModernTheme.SmallFont };
        var numBuy = new NumericUpDown { Location = new Point(20, 200), Size = new Size(170, 26), DecimalPlaces = 2, Maximum = 1000000, Value = existing?.BuyingPrice ?? 0, Font = ModernTheme.BodyFont };

        var lblSell = new Label { Text = "Selling Price (LKR):", Location = new Point(210, 180), AutoSize = true, Font = ModernTheme.SmallFont };
        var numSell = new NumericUpDown { Location = new Point(210, 200), Size = new Size(170, 26), DecimalPlaces = 2, Maximum = 1000000, Value = existing?.SellingPrice ?? 0, Font = ModernTheme.BodyFont };

        var lblQty = new Label { Text = "Initial Stock Quantity:", Location = new Point(20, 235), AutoSize = true, Font = ModernTheme.SmallFont };
        var numQty = new NumericUpDown { Location = new Point(20, 255), Size = new Size(170, 26), Maximum = 100000, Value = existing?.Quantity ?? 0, Font = ModernTheme.BodyFont };

        var lblThresh = new Label { Text = "Low Stock Alert Threshold:", Location = new Point(210, 235), AutoSize = true, Font = ModernTheme.SmallFont };
        var numThresh = new NumericUpDown { Location = new Point(210, 255), Size = new Size(170, 26), Maximum = 1000, Value = existing?.LowStockThreshold ?? 5, Font = ModernTheme.BodyFont };

        var btnSave = new Button { Text = existing == null ? "Save Product" : "Update Product", Location = new Point(20, 320), Size = new Size(360, 42) };
        ModernTheme.ApplyPrimaryButtonStyle(btnSave);

        btnSave.Click += async (s, e) =>
        {
            var code = txtCode.Text.Trim();
            var name = txtName.Text.Trim();

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Product Code and Name are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (await _productRepository.ProductCodeExistsAsync(code, existing?.Id))
            {
                MessageBox.Show($"Product Code '{code}' already exists! Please use a unique code.", "Duplicate Code", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (existing == null)
            {
                var newProd = new Product
                {
                    ProductCode = code,
                    Name = name,
                    Category = txtCat.Text.Trim(),
                    BuyingPrice = numBuy.Value,
                    SellingPrice = numSell.Value,
                    Quantity = (int)numQty.Value,
                    LowStockThreshold = (int)numThresh.Value,
                    IsActive = true
                };
                await _productRepository.CreateAsync(newProd);
            }
            else
            {
                existing.ProductCode = code;
                existing.Name = name;
                existing.Category = txtCat.Text.Trim();
                existing.BuyingPrice = numBuy.Value;
                existing.SellingPrice = numSell.Value;
                existing.Quantity = (int)numQty.Value;
                existing.LowStockThreshold = (int)numThresh.Value;
                await _productRepository.UpdateAsync(existing);
            }

            dlg.DialogResult = DialogResult.OK;
            dlg.Close();
        };

        dlg.Controls.AddRange(new Control[] {
            lblCode, txtCode, lblName, txtName, lblCat, txtCat,
            lblBuy, numBuy, lblSell, numSell, lblQty, numQty, lblThresh, numThresh, btnSave
        });

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadDataAsync(_pager.CurrentPage);
        }
    }

    private async Task ExportInventoryReportPdfAsync()
    {
        if (_pdfReportService == null)
        {
            MessageBox.Show("PDF Reporting Service is not available.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "PDF Document (*.pdf)|*.pdf",
            FileName = $"Polkotuwa_Inventory_Audit_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
            Title = "Save Inventory Audit PDF Report"
        };

        if (sfd.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                var products = await _productRepository.GetAllActiveAsync();
                await _pdfReportService.GenerateInventoryReportAsync(products, sfd.FileName);

                var prompt = MessageBox.Show(
                    $"Inventory Audit PDF Report generated successfully!\n\nFile saved to:\n{sfd.FileName}\n\nDo you want to open the report now?",
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
