using System.Drawing;
using System.Windows.Forms;
using RetailShop.Core.Common;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;
using RetailShop.UI.Controls;
using RetailShop.UI.Styles;

namespace RetailShop.UI.Views;

public class SuppliersView : UserControl
{
    private readonly ISupplierRepository _supplierRepository;

    private DataGridView _dgv = null!;
    private PaginationControl _pager = null!;
    private TextBox _txtSearch = null!;

    public SuppliersView(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;

        InitializeComponents();
        _ = LoadDataAsync();
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ModernTheme.BgLight;
        this.Padding = new Padding(16);
        this.Font = ModernTheme.BodyFont;

        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(12)
        };

        var lblSearch = new Label { Text = "Search Vendors/Suppliers:", Location = new Point(12, 18), AutoSize = true, Font = ModernTheme.BodyBoldFont };
        _txtSearch = new TextBox { Location = new Point(220, 14), Size = new Size(220, 28), Font = ModernTheme.BodyFont };
        _txtSearch.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await LoadDataAsync(1); };

        var btnSearch = new Button { Text = "🔍 Search", Location = new Point(450, 13), Size = new Size(90, 30) };
        ModernTheme.ApplyPrimaryButtonStyle(btnSearch);
        btnSearch.Click += async (s, e) => await LoadDataAsync(1);

        var btnAdd = new Button { Text = "➕ New Supplier", Location = new Point(pnlTop.Width - 145, 13), Size = new Size(130, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        ModernTheme.ApplySuccessButtonStyle(btnAdd);
        btnAdd.Click += (s, e) => ShowSupplierDialog(null);

        var btnDelete = new Button { Text = "🗑 Delete", Location = new Point(pnlTop.Width - 250, 13), Size = new Size(95, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        ModernTheme.ApplyDangerButtonStyle(btnDelete);
        btnDelete.Click += async (s, e) => await DeleteSelectedSupplierAsync();

        pnlTop.Controls.Add(lblSearch);
        pnlTop.Controls.Add(_txtSearch);
        pnlTop.Controls.Add(btnSearch);
        pnlTop.Controls.Add(btnAdd);
        pnlTop.Controls.Add(btnDelete);

        _dgv = new DataGridView { Dock = DockStyle.Fill };
        ModernTheme.ApplyDataGridStyle(_dgv);

        _dgv.Columns.Add("Id", "ID");
        _dgv.Columns.Add("SupplierCode", "Vendor Code");
        _dgv.Columns.Add("Name", "Company Name");
        _dgv.Columns.Add("ContactPerson", "Contact Representative");
        _dgv.Columns.Add("Phone", "Phone Number");
        _dgv.Columns.Add("Email", "Email");
        _dgv.Columns.Add("Address", "Address");

        _dgv.Columns[0].Width = 60;
        _dgv.Columns[1].Width = 130;
        _dgv.Columns[2].Width = 220;
        _dgv.Columns[3].Width = 190;
        _dgv.Columns[4].Width = 130;
        _dgv.Columns[5].Width = 180;
        _dgv.Columns[6].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        _dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _dgv.ColumnHeadersHeight = 44;

        _pager = new PaginationControl { Dock = DockStyle.Bottom };
        _pager.PageChanged += async (s, page) => await LoadDataAsync(page);
        _pager.PageSizeChanged += async (s, size) => await LoadDataAsync(1);

        this.Controls.Add(_dgv);
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

            var result = await _supplierRepository.GetPagedAsync(request);

            _dgv.Rows.Clear();
            foreach (var s in result.Items)
            {
                _dgv.Rows.Add(s.Id, s.SupplierCode, s.Name, s.ContactPerson, s.Phone, s.Email, s.Address);
            }

            _pager.BindPagedResult(result);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load suppliers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task DeleteSelectedSupplierAsync()
    {
        if (_dgv.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select a supplier to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!int.TryParse(_dgv.SelectedRows[0].Cells["Id"].Value?.ToString(), out var id))
            return;

        var name = _dgv.SelectedRows[0].Cells["Name"].Value?.ToString() ?? "Supplier";

        var confirm = MessageBox.Show($"Are you sure you want to delete supplier '{name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm == DialogResult.Yes)
        {
            await _supplierRepository.DeleteAsync(id);
            await LoadDataAsync(_pager.CurrentPage);
        }
    }

    private void ShowSupplierDialog(Supplier? existing)
    {
        using var dlg = new Form
        {
            Text = existing == null ? "Add New Supplier" : "Edit Supplier",
            Size = new Size(380, 400),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(20)
        };

        var lblCode = new Label { Text = "Supplier Code:", Location = new Point(20, 15), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtCode = new TextBox { Location = new Point(20, 35), Size = new Size(320, 26), Text = existing?.SupplierCode ?? "", Font = ModernTheme.BodyFont };

        var lblName = new Label { Text = "Supplier / Company Name:", Location = new Point(20, 70), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtName = new TextBox { Location = new Point(20, 90), Size = new Size(320, 26), Text = existing?.Name ?? "", Font = ModernTheme.BodyFont };

        var lblPerson = new Label { Text = "Contact Person:", Location = new Point(20, 125), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtPerson = new TextBox { Location = new Point(20, 145), Size = new Size(320, 26), Text = existing?.ContactPerson ?? "", Font = ModernTheme.BodyFont };

        var lblPhone = new Label { Text = "Phone:", Location = new Point(20, 180), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtPhone = new TextBox { Location = new Point(20, 200), Size = new Size(320, 26), Text = existing?.Phone ?? "", Font = ModernTheme.BodyFont };

        var lblAdd = new Label { Text = "Address:", Location = new Point(20, 235), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtAdd = new TextBox { Location = new Point(20, 255), Size = new Size(320, 26), Text = existing?.Address ?? "", Font = ModernTheme.BodyFont };

        var btnSave = new Button { Text = "Save Supplier", Location = new Point(20, 305), Size = new Size(320, 40) };
        ModernTheme.ApplyPrimaryButtonStyle(btnSave);

        btnSave.Click += async (s, e) =>
        {
            var code = txtCode.Text.Trim();
            var name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Supplier Code and Name are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (existing == null)
            {
                await _supplierRepository.CreateAsync(new Supplier
                {
                    SupplierCode = code,
                    Name = name,
                    ContactPerson = txtPerson.Text.Trim(),
                    Phone = txtPhone.Text.Trim(),
                    Address = txtAdd.Text.Trim()
                });
            }
            else
            {
                existing.SupplierCode = code;
                existing.Name = name;
                existing.ContactPerson = txtPerson.Text.Trim();
                existing.Phone = txtPhone.Text.Trim();
                existing.Address = txtAdd.Text.Trim();
                await _supplierRepository.UpdateAsync(existing);
            }

            dlg.DialogResult = DialogResult.OK;
            dlg.Close();
        };

        dlg.Controls.AddRange(new Control[] { lblCode, txtCode, lblName, txtName, lblPerson, txtPerson, lblPhone, txtPhone, lblAdd, txtAdd, btnSave });

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadDataAsync(_pager.CurrentPage);
        }
    }
}
