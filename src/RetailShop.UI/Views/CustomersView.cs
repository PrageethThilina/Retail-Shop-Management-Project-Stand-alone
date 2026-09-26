using System.Drawing;
using System.Windows.Forms;
using RetailShop.Core.Common;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;
using RetailShop.UI.Controls;
using RetailShop.UI.Styles;

namespace RetailShop.UI.Views;

public class CustomersView : UserControl
{
    private readonly ICustomerRepository _customerRepository;

    private DataGridView _dgv = null!;
    private PaginationControl _pager = null!;
    private TextBox _txtSearch = null!;

    public CustomersView(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;

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

        var lblSearch = new Label { Text = "Search Customers:", Location = new Point(12, 18), AutoSize = true, Font = ModernTheme.BodyBoldFont };
        _txtSearch = new TextBox { Location = new Point(170, 14), Size = new Size(230, 28), Font = ModernTheme.BodyFont };
        _txtSearch.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await LoadDataAsync(1); };

        var btnSearch = new Button { Text = "🔍 Search", Location = new Point(410, 13), Size = new Size(90, 30) };
        ModernTheme.ApplyPrimaryButtonStyle(btnSearch);
        btnSearch.Click += async (s, e) => await LoadDataAsync(1);

        var btnAdd = new Button { Text = "➕ New Customer", Location = new Point(pnlTop.Width - 150, 13), Size = new Size(135, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        ModernTheme.ApplySuccessButtonStyle(btnAdd);
        btnAdd.Click += (s, e) => ShowCustomerDialog(null);

        pnlTop.Controls.Add(lblSearch);
        pnlTop.Controls.Add(_txtSearch);
        pnlTop.Controls.Add(btnSearch);
        pnlTop.Controls.Add(btnAdd);

        _dgv = new DataGridView { Dock = DockStyle.Fill };
        ModernTheme.ApplyDataGridStyle(_dgv);

        _dgv.Columns.Add("Id", "ID");
        _dgv.Columns.Add("Name", "Customer Name");
        _dgv.Columns.Add("Phone", "Phone Number");
        _dgv.Columns.Add("Email", "Email Address");
        _dgv.Columns.Add("Address", "Address");
        _dgv.Columns.Add("TotalPurchases", "Lifetime Purchases (LKR)");
        _dgv.Columns.Add("CreatedAt", "Customer Since");

        _dgv.Columns[0].Width = 60;
        _dgv.Columns[1].Width = 200;
        _dgv.Columns[2].Width = 140;
        _dgv.Columns[3].Width = 190;
        _dgv.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _dgv.Columns[5].Width = 180;
        _dgv.Columns[6].Width = 130;

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

            var result = await _customerRepository.GetPagedAsync(request);

            _dgv.Rows.Clear();
            foreach (var c in result.Items)
            {
                _dgv.Rows.Add(
                    c.Id,
                    c.Name,
                    c.Phone,
                    c.Email,
                    c.Address,
                    c.TotalPurchases.ToString("N2"),
                    c.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd")
                );
            }

            _pager.BindPagedResult(result);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load customer list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowCustomerDialog(Customer? existing)
    {
        using var dlg = new Form
        {
            Text = existing == null ? "Add New Customer" : "Edit Customer",
            Size = new Size(380, 360),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(20)
        };

        var lblName = new Label { Text = "Customer Name:", Location = new Point(20, 15), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtName = new TextBox { Location = new Point(20, 35), Size = new Size(320, 26), Text = existing?.Name ?? "", Font = ModernTheme.BodyFont };

        var lblPhone = new Label { Text = "Phone Number:", Location = new Point(20, 70), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtPhone = new TextBox { Location = new Point(20, 90), Size = new Size(320, 26), Text = existing?.Phone ?? "", Font = ModernTheme.BodyFont };

        var lblEmail = new Label { Text = "Email Address:", Location = new Point(20, 125), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtEmail = new TextBox { Location = new Point(20, 145), Size = new Size(320, 26), Text = existing?.Email ?? "", Font = ModernTheme.BodyFont };

        var lblAddress = new Label { Text = "Address:", Location = new Point(20, 180), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtAddress = new TextBox { Location = new Point(20, 200), Size = new Size(320, 26), Text = existing?.Address ?? "", Font = ModernTheme.BodyFont };

        var btnSave = new Button { Text = "Save Customer", Location = new Point(20, 250), Size = new Size(320, 40) };
        ModernTheme.ApplyPrimaryButtonStyle(btnSave);

        btnSave.Click += async (s, e) =>
        {
            var name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Customer Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (existing == null)
            {
                await _customerRepository.CreateAsync(new Customer
                {
                    Name = name,
                    Phone = txtPhone.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Address = txtAddress.Text.Trim()
                });
            }
            else
            {
                existing.Name = name;
                existing.Phone = txtPhone.Text.Trim();
                existing.Email = txtEmail.Text.Trim();
                existing.Address = txtAddress.Text.Trim();
                await _customerRepository.UpdateAsync(existing);
            }

            dlg.DialogResult = DialogResult.OK;
            dlg.Close();
        };

        dlg.Controls.AddRange(new Control[] { lblName, txtName, lblPhone, txtPhone, lblEmail, txtEmail, lblAddress, txtAddress, btnSave });

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadDataAsync(_pager.CurrentPage);
        }
    }
}
