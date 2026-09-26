using System.Drawing;
using System.Windows.Forms;
using RetailShop.Core.Common;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;
using RetailShop.UI.Controls;
using RetailShop.UI.Styles;

namespace RetailShop.UI.Views;

public class BudgetView : UserControl
{
    private readonly IBudgetRepository _budgetRepository;

    private DataGridView _dgv = null!;
    private PaginationControl _pager = null!;
    private NumericUpDown _numYear = null!;

    public BudgetView(IBudgetRepository budgetRepository)
    {
        _budgetRepository = budgetRepository;

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

        var lblYear = new Label { Text = "Filter By Year:", Location = new Point(12, 18), AutoSize = true, Font = ModernTheme.BodyBoldFont };
        _numYear = new NumericUpDown { Location = new Point(145, 14), Size = new Size(85, 28), Minimum = 2020, Maximum = 2035, Value = DateTime.UtcNow.Year, Font = ModernTheme.BodyFont };
        _numYear.ValueChanged += async (s, e) => await LoadDataAsync(1);

        var btnAdd = new Button { Text = "➕ Record Monthly Budget", Location = new Point(pnlTop.Width - 210, 13), Size = new Size(195, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        ModernTheme.ApplySuccessButtonStyle(btnAdd);
        btnAdd.Click += (s, e) => ShowBudgetDialog(null);

        var btnDelete = new Button { Text = "🗑 Delete", Location = new Point(pnlTop.Width - 315, 13), Size = new Size(95, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        ModernTheme.ApplyDangerButtonStyle(btnDelete);
        btnDelete.Click += async (s, e) => await DeleteSelectedBudgetAsync();

        pnlTop.Controls.Add(lblYear);
        pnlTop.Controls.Add(_numYear);
        pnlTop.Controls.Add(btnAdd);
        pnlTop.Controls.Add(btnDelete);

        _dgv = new DataGridView { Dock = DockStyle.Fill };
        ModernTheme.ApplyDataGridStyle(_dgv);

        _dgv.Columns.Add("Id", "ID");
        _dgv.Columns.Add("Period", "Year / Month");
        _dgv.Columns.Add("TotalIncome", "Gross Revenue (LKR)");
        _dgv.Columns.Add("InventoryCost", "Inventory (LKR)");
        _dgv.Columns.Add("ElectricityBill", "Electricity (LKR)");
        _dgv.Columns.Add("WaterBill", "Water (LKR)");
        _dgv.Columns.Add("SalariesTotal", "Staff Salaries (LKR)");
        _dgv.Columns.Add("OtherExpenses", "Other (LKR)");
        _dgv.Columns.Add("TotalExpenses", "Total Expenses (LKR)");
        _dgv.Columns.Add("NetIncome", "Net Profit/Loss (LKR)");
        _dgv.Columns.Add("Notes", "Notes");

        _dgv.Columns[0].Width = 50;
        _dgv.Columns[1].Width = 110;
        _dgv.Columns[2].Width = 160;
        _dgv.Columns[3].Width = 130;
        _dgv.Columns[4].Width = 130;
        _dgv.Columns[5].Width = 110;
        _dgv.Columns[6].Width = 140;
        _dgv.Columns[7].Width = 110;
        _dgv.Columns[8].Width = 150;
        _dgv.Columns[9].Width = 160;
        _dgv.Columns[10].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        _dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _dgv.ColumnHeadersHeight = 44;

        _dgv.CellFormatting += (s, e) =>
        {
            if (e.RowIndex >= 0 && _dgv.Columns[e.ColumnIndex].Name == "NetIncome")
            {
                var val = e.Value?.ToString() ?? "";
                if (decimal.TryParse(val.Replace(",", ""), out var net))
                {
                    e.CellStyle!.ForeColor = net >= 0 ? ModernTheme.SuccessEmerald : ModernTheme.DangerRose;
                    e.CellStyle.Font = ModernTheme.BodyBoldFont;
                }
            }
        };

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
                PageSize = _pager.SelectedPageSize
            };

            var result = await _budgetRepository.GetPagedAsync(request, (int)_numYear.Value);

            _dgv.Rows.Clear();
            foreach (var b in result.Items)
            {
                _dgv.Rows.Add(
                    b.Id,
                    $"{b.Year} / {b.Month:D2}",
                    b.TotalIncome.ToString("N2"),
                    b.InventoryCost.ToString("N2"),
                    b.ElectricityBill.ToString("N2"),
                    b.WaterBill.ToString("N2"),
                    b.SalariesTotal.ToString("N2"),
                    b.OtherExpenses.ToString("N2"),
                    b.TotalExpenses.ToString("N2"),
                    b.NetIncomeMonth.ToString("N2"),
                    b.Notes
                );
            }

            _pager.BindPagedResult(result);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load budget: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task DeleteSelectedBudgetAsync()
    {
        if (_dgv.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select a budget record to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!int.TryParse(_dgv.SelectedRows[0].Cells["Id"].Value?.ToString(), out var id))
            return;

        var period = _dgv.SelectedRows[0].Cells["Period"].Value?.ToString() ?? "Budget Record";

        var confirm = MessageBox.Show($"Are you sure you want to delete budget record for '{period}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm == DialogResult.Yes)
        {
            await _budgetRepository.DeleteAsync(id);
            await LoadDataAsync(_pager.CurrentPage);
        }
    }

    private void ShowBudgetDialog(BudgetRecord? existing)
    {
        using var dlg = new Form
        {
            Text = existing == null ? "Add Monthly Budget Record" : "Edit Monthly Budget",
            Size = new Size(420, 520),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(20)
        };

        var lblYear = new Label { Text = "Year:", Location = new Point(20, 15), AutoSize = true, Font = ModernTheme.SmallFont };
        var numYear = new NumericUpDown { Location = new Point(20, 35), Size = new Size(160, 26), Minimum = 2020, Maximum = 2035, Value = existing?.Year ?? (int)_numYear.Value, Font = ModernTheme.BodyFont };

        var lblMonth = new Label { Text = "Month:", Location = new Point(200, 15), AutoSize = true, Font = ModernTheme.SmallFont };
        var numMonth = new NumericUpDown { Location = new Point(200, 35), Size = new Size(160, 26), Minimum = 1, Maximum = 12, Value = existing?.Month ?? DateTime.UtcNow.Month, Font = ModernTheme.BodyFont };

        var lblRev = new Label { Text = "Gross Revenue / Total Sales (LKR):", Location = new Point(20, 70), AutoSize = true, Font = ModernTheme.SmallFont };
        var numRev = new NumericUpDown { Location = new Point(20, 90), Size = new Size(340, 26), Maximum = 100000000, DecimalPlaces = 2, Value = existing?.TotalIncome ?? 0, Font = ModernTheme.BodyFont };

        var lblInv = new Label { Text = "Inventory / Stock Purchases (LKR):", Location = new Point(20, 125), AutoSize = true, Font = ModernTheme.SmallFont };
        var numInv = new NumericUpDown { Location = new Point(20, 145), Size = new Size(340, 26), Maximum = 100000000, DecimalPlaces = 2, Value = existing?.InventoryCost ?? 0, Font = ModernTheme.BodyFont };

        var lblElec = new Label { Text = "Electricity Bill (LKR):", Location = new Point(20, 180), AutoSize = true, Font = ModernTheme.SmallFont };
        var numElec = new NumericUpDown { Location = new Point(20, 200), Size = new Size(160, 26), Maximum = 10000000, DecimalPlaces = 2, Value = existing?.ElectricityBill ?? 0, Font = ModernTheme.BodyFont };

        var lblWater = new Label { Text = "Water Bill (LKR):", Location = new Point(200, 180), AutoSize = true, Font = ModernTheme.SmallFont };
        var numWater = new NumericUpDown { Location = new Point(200, 200), Size = new Size(160, 26), Maximum = 10000000, DecimalPlaces = 2, Value = existing?.WaterBill ?? 0, Font = ModernTheme.BodyFont };

        var lblSal = new Label { Text = "Total Staff Salaries (LKR):", Location = new Point(20, 235), AutoSize = true, Font = ModernTheme.SmallFont };
        var numSal = new NumericUpDown { Location = new Point(20, 255), Size = new Size(160, 26), Maximum = 100000000, DecimalPlaces = 2, Value = existing?.SalariesTotal ?? 0, Font = ModernTheme.BodyFont };

        var lblOther = new Label { Text = "Other Expenses (LKR):", Location = new Point(200, 235), AutoSize = true, Font = ModernTheme.SmallFont };
        var numOther = new NumericUpDown { Location = new Point(200, 255), Size = new Size(160, 26), Maximum = 10000000, DecimalPlaces = 2, Value = existing?.OtherExpenses ?? 0, Font = ModernTheme.BodyFont };

        var lblNotes = new Label { Text = "Notes / Remarks:", Location = new Point(20, 290), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtNotes = new TextBox { Location = new Point(20, 310), Size = new Size(340, 26), Text = existing?.Notes ?? "", Font = ModernTheme.BodyFont };

        var btnSave = new Button { Text = "Save Budget Record", Location = new Point(20, 370), Size = new Size(340, 42) };
        ModernTheme.ApplyPrimaryButtonStyle(btnSave);

        btnSave.Click += async (s, e) =>
        {
            var budget = new BudgetRecord
            {
                Year = (int)numYear.Value,
                Month = (int)numMonth.Value,
                TotalIncome = numRev.Value,
                InventoryCost = numInv.Value,
                ElectricityBill = numElec.Value,
                WaterBill = numWater.Value,
                SalariesTotal = numSal.Value,
                OtherExpenses = numOther.Value,
                Notes = txtNotes.Text.Trim()
            };

            await _budgetRepository.SaveAsync(budget);
            dlg.DialogResult = DialogResult.OK;
            dlg.Close();
        };

        dlg.Controls.AddRange(new Control[] {
            lblYear, numYear, lblMonth, numMonth, lblRev, numRev, lblInv, numInv,
            lblElec, numElec, lblWater, numWater, lblSal, numSal, lblOther, numOther,
            lblNotes, txtNotes, btnSave
        });

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadDataAsync(_pager.CurrentPage);
        }
    }
}
