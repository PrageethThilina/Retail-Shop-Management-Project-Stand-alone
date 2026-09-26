using System.Drawing;
using System.Windows.Forms;
using RetailShop.Core.Common;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;
using RetailShop.UI.Controls;
using RetailShop.UI.Styles;

namespace RetailShop.UI.Views;

public class EmployeesView : UserControl
{
    private readonly IEmployeeRepository _employeeRepository;

    private DataGridView _dgv = null!;
    private PaginationControl _pager = null!;
    private TextBox _txtSearch = null!;

    public EmployeesView(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;

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

        var lblSearch = new Label { Text = "Search Employees:", Location = new Point(12, 18), AutoSize = true, Font = ModernTheme.BodyBoldFont };
        _txtSearch = new TextBox { Location = new Point(175, 14), Size = new Size(230, 28), Font = ModernTheme.BodyFont };
        _txtSearch.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await LoadDataAsync(1); };

        var btnSearch = new Button { Text = "🔍 Search", Location = new Point(415, 13), Size = new Size(90, 30) };
        ModernTheme.ApplyPrimaryButtonStyle(btnSearch);
        btnSearch.Click += async (s, e) => await LoadDataAsync(1);

        var btnAdd = new Button { Text = "➕ New Employee", Location = new Point(pnlTop.Width - 150, 13), Size = new Size(135, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        ModernTheme.ApplySuccessButtonStyle(btnAdd);
        btnAdd.Click += (s, e) => ShowEmployeeDialog(null);

        var btnDelete = new Button { Text = "🗑 Delete", Location = new Point(pnlTop.Width - 255, 13), Size = new Size(95, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        ModernTheme.ApplyDangerButtonStyle(btnDelete);
        btnDelete.Click += async (s, e) => await DeleteSelectedEmployeeAsync();

        pnlTop.Controls.Add(lblSearch);
        pnlTop.Controls.Add(_txtSearch);
        pnlTop.Controls.Add(btnSearch);
        pnlTop.Controls.Add(btnAdd);
        pnlTop.Controls.Add(btnDelete);

        _dgv = new DataGridView { Dock = DockStyle.Fill };
        ModernTheme.ApplyDataGridStyle(_dgv);

        _dgv.Columns.Add("Id", "ID");
        _dgv.Columns.Add("EmployeeCode", "Staff ID");
        _dgv.Columns.Add("Name", "Full Name");
        _dgv.Columns.Add("Designation", "Designation");
        _dgv.Columns.Add("Phone", "Mobile");
        _dgv.Columns.Add("BasicSalary", "Basic Salary (LKR)");
        _dgv.Columns.Add("BankDetails", "Bank Details");
        _dgv.Columns.Add("JoinDate", "Join Date");

        _dgv.Columns[0].Width = 60;
        _dgv.Columns[1].Width = 100;
        _dgv.Columns[2].Width = 180;
        _dgv.Columns[3].Width = 140;
        _dgv.Columns[4].Width = 120;
        _dgv.Columns[5].Width = 150;
        _dgv.Columns[6].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _dgv.Columns[7].Width = 110;

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

            var result = await _employeeRepository.GetPagedAsync(request);

            _dgv.Rows.Clear();
            foreach (var emp in result.Items)
            {
                _dgv.Rows.Add(
                    emp.Id,
                    emp.EmployeeCode,
                    emp.Name,
                    emp.Designation,
                    emp.Phone,
                    emp.BasicSalary.ToString("N2"),
                    emp.BankDetails,
                    emp.JoinDate.ToString("yyyy-MM-dd")
                );
            }

            _pager.BindPagedResult(result);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load employee list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task DeleteSelectedEmployeeAsync()
    {
        if (_dgv.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select an employee to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!int.TryParse(_dgv.SelectedRows[0].Cells["Id"].Value?.ToString(), out var id))
            return;

        var name = _dgv.SelectedRows[0].Cells["Name"].Value?.ToString() ?? "Employee";

        var confirm = MessageBox.Show($"Are you sure you want to delete employee '{name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm == DialogResult.Yes)
        {
            await _employeeRepository.DeleteAsync(id);
            await LoadDataAsync(_pager.CurrentPage);
        }
    }

    private void ShowEmployeeDialog(Employee? existing)
    {
        using var dlg = new Form
        {
            Text = existing == null ? "Register New Employee" : "Edit Employee Record",
            Size = new Size(400, 480),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(20)
        };

        var lblCode = new Label { Text = "Employee Staff ID:", Location = new Point(20, 15), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtCode = new TextBox { Location = new Point(20, 35), Size = new Size(340, 26), Text = existing?.EmployeeCode ?? "", Font = ModernTheme.BodyFont };

        var lblName = new Label { Text = "Full Name:", Location = new Point(20, 70), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtName = new TextBox { Location = new Point(20, 90), Size = new Size(340, 26), Text = existing?.Name ?? "", Font = ModernTheme.BodyFont };

        var lblDesig = new Label { Text = "Designation / Role:", Location = new Point(20, 125), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtDesig = new TextBox { Location = new Point(20, 145), Size = new Size(340, 26), Text = existing?.Designation ?? "Staff", Font = ModernTheme.BodyFont };

        var lblPhone = new Label { Text = "Phone Number:", Location = new Point(20, 180), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtPhone = new TextBox { Location = new Point(20, 200), Size = new Size(340, 26), Text = existing?.Phone ?? "", Font = ModernTheme.BodyFont };

        var lblSal = new Label { Text = "Basic Salary (LKR):", Location = new Point(20, 235), AutoSize = true, Font = ModernTheme.SmallFont };
        var numSal = new NumericUpDown { Location = new Point(20, 255), Size = new Size(160, 26), Maximum = 10000000, DecimalPlaces = 2, Value = existing?.BasicSalary ?? 45000, Font = ModernTheme.BodyFont };

        var lblBank = new Label { Text = "Bank Details (Account / Bank):", Location = new Point(20, 290), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtBank = new TextBox { Location = new Point(20, 310), Size = new Size(340, 26), Text = existing?.BankDetails ?? "", Font = ModernTheme.BodyFont };

        var btnSave = new Button { Text = "Save Employee", Location = new Point(20, 360), Size = new Size(340, 42) };
        ModernTheme.ApplyPrimaryButtonStyle(btnSave);

        btnSave.Click += async (s, e) =>
        {
            var code = txtCode.Text.Trim();
            var name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Staff ID and Full Name are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (existing == null)
            {
                await _employeeRepository.CreateAsync(new Employee
                {
                    EmployeeCode = code,
                    Name = name,
                    Designation = txtDesig.Text.Trim(),
                    Phone = txtPhone.Text.Trim(),
                    BasicSalary = numSal.Value,
                    BankDetails = txtBank.Text.Trim(),
                    JoinDate = DateTime.UtcNow
                });
            }
            else
            {
                existing.EmployeeCode = code;
                existing.Name = name;
                existing.Designation = txtDesig.Text.Trim();
                existing.Phone = txtPhone.Text.Trim();
                existing.BasicSalary = numSal.Value;
                existing.BankDetails = txtBank.Text.Trim();
                await _employeeRepository.UpdateAsync(existing);
            }

            dlg.DialogResult = DialogResult.OK;
            dlg.Close();
        };

        dlg.Controls.AddRange(new Control[] { lblCode, txtCode, lblName, txtName, lblDesig, txtDesig, lblPhone, txtPhone, lblSal, numSal, lblBank, txtBank, btnSave });

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadDataAsync(_pager.CurrentPage);
        }
    }
}
