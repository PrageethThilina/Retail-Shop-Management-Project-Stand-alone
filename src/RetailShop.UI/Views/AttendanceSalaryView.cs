using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using RetailShop.Core.Common;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;
using RetailShop.UI.Controls;
using RetailShop.UI.Services;
using RetailShop.UI.Styles;

namespace RetailShop.UI.Views;

public class AttendanceSalaryView : UserControl
{
    private readonly IAttendanceSalaryRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly PdfReportService? _pdfReportService;

    private NumericUpDown _numYear = null!;
    private ComboBox _cmbMonth = null!;

    // Attendance Tab
    private DataGridView _dgvAttendance = null!;
    private PaginationControl _pagerAttendance = null!;
    private ComboBox _cmbEmployeeAtt = null!;
    private NumericUpDown _numTotalDays = null!;
    private NumericUpDown _numWorkingDays = null!;
    private NumericUpDown _numPresentDays = null!;
    private NumericUpDown _numAbsentDays = null!;
    private Button _btnSaveAttendance = null!;

    // Salary Tab
    private DataGridView _dgvSalary = null!;
    private PaginationControl _pagerSalary = null!;
    private ComboBox _cmbEmployeeSal = null!;
    private Label _lblBasicSalary = null!;
    private NumericUpDown _numAllowances = null!;
    private NumericUpDown _numDeductions = null!;
    private Label _lblNetSalary = null!;
    private Button _btnSaveSalary = null!;
    private Label _lblTotalSalaryExpense = null!;

    private IReadOnlyList<Employee> _employees = Array.Empty<Employee>();

    public AttendanceSalaryView(IAttendanceSalaryRepository repository, IEmployeeRepository employeeRepository, PdfReportService? pdfReportService = null)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _pdfReportService = pdfReportService;

        InitializeComponents();
        _ = LoadEmployeesAsync();
        _ = LoadAttendanceDataAsync();
        _ = LoadSalaryDataAsync();
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ModernTheme.BgLight;
        this.Padding = new Padding(16);
        this.Font = ModernTheme.BodyFont;

        // Top Filter Bar (Year / Month)
        var pnlFilter = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(12)
        };

        var lblYear = new Label { Text = "Year:", Location = new Point(12, 16), AutoSize = true, Font = ModernTheme.BodyBoldFont };
        _numYear = new NumericUpDown { Location = new Point(65, 13), Size = new Size(80, 28), Minimum = 2020, Maximum = 2035, Value = DateTime.UtcNow.Year, Font = ModernTheme.BodyFont };
        _numYear.ValueChanged += async (s, e) => { await LoadAttendanceDataAsync(1); await LoadSalaryDataAsync(1); };

        var lblMonth = new Label { Text = "Month:", Location = new Point(165, 16), AutoSize = true, Font = ModernTheme.BodyBoldFont };
        _cmbMonth = new ComboBox { Location = new Point(235, 13), Size = new Size(115, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = ModernTheme.BodyFont };
        _cmbMonth.Items.AddRange(new object[] { "1 - Jan", "2 - Feb", "3 - Mar", "4 - Apr", "5 - May", "6 - Jun", "7 - Jul", "8 - Aug", "9 - Sep", "10 - Oct", "11 - Nov", "12 - Dec" });
        _cmbMonth.SelectedIndex = DateTime.UtcNow.Month - 1;
        _cmbMonth.SelectedIndexChanged += async (s, e) => { await LoadAttendanceDataAsync(1); await LoadSalaryDataAsync(1); };

        _lblTotalSalaryExpense = new Label
        {
            Text = "Total Payroll Expense: LKR 0.00",
            Font = ModernTheme.BodyBoldFont,
            ForeColor = ModernTheme.PrimaryIndigo,
            Location = new Point(pnlFilter.Width - 320, 16),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        var btnExportSalaryPdf = new Button
        {
            Text = "📄 Export Payroll PDF",
            Location = new Point(pnlFilter.Width - 500, 12),
            Size = new Size(165, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ModernTheme.ApplySecondaryButtonStyle(btnExportSalaryPdf);
        btnExportSalaryPdf.Click += async (s, e) => await ExportPayrollReportPdfAsync();

        pnlFilter.Controls.Add(lblYear);
        pnlFilter.Controls.Add(_numYear);
        pnlFilter.Controls.Add(lblMonth);
        pnlFilter.Controls.Add(_cmbMonth);
        pnlFilter.Controls.Add(btnExportSalaryPdf);
        pnlFilter.Controls.Add(_lblTotalSalaryExpense);

        // Tab Control
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = ModernTheme.BodyBoldFont
        };

        var tabAttendance = new TabPage("📅 Staff Attendance Records");
        tabAttendance.BackColor = ModernTheme.BgLight;
        tabAttendance.Padding = new Padding(12);

        var tabSalary = new TabPage("💵 Payroll & Monthly Salary");
        tabSalary.BackColor = ModernTheme.BgLight;
        tabSalary.Padding = new Padding(12);

        BuildAttendanceTab(tabAttendance);
        BuildSalaryTab(tabSalary);

        tabControl.TabPages.Add(tabAttendance);
        tabControl.TabPages.Add(tabSalary);

        this.Controls.Add(tabControl);
        this.Controls.Add(pnlFilter);
    }

    private void BuildAttendanceTab(TabPage tab)
    {
        // Entry panel
        var pnlEntry = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(12)
        };

        var lblEmp = new Label { Text = "Employee:", Location = new Point(12, 10), AutoSize = true, Font = ModernTheme.SmallFont };
        _cmbEmployeeAtt = new ComboBox { Location = new Point(12, 30), Size = new Size(200, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = ModernTheme.BodyFont };

        var lblTotal = new Label { Text = "Total Days:", Location = new Point(225, 10), AutoSize = true, Font = ModernTheme.SmallFont };
        _numTotalDays = new NumericUpDown { Location = new Point(225, 30), Size = new Size(70, 26), Maximum = 31, Value = 30, Font = ModernTheme.BodyFont };

        var lblWork = new Label { Text = "Working:", Location = new Point(310, 10), AutoSize = true, Font = ModernTheme.SmallFont };
        _numWorkingDays = new NumericUpDown { Location = new Point(310, 30), Size = new Size(70, 26), Maximum = 31, Value = 24, Font = ModernTheme.BodyFont };

        var lblPres = new Label { Text = "Present:", Location = new Point(395, 10), AutoSize = true, Font = ModernTheme.SmallFont };
        _numPresentDays = new NumericUpDown { Location = new Point(395, 30), Size = new Size(70, 26), Maximum = 31, Value = 24, Font = ModernTheme.BodyFont };
        _numPresentDays.ValueChanged += (s, e) =>
        {
            _numAbsentDays.Value = Math.Max(0, _numWorkingDays.Value - _numPresentDays.Value);
        };

        var lblAbs = new Label { Text = "Absent:", Location = new Point(480, 10), AutoSize = true, Font = ModernTheme.SmallFont };
        _numAbsentDays = new NumericUpDown { Location = new Point(480, 30), Size = new Size(70, 26), Maximum = 31, Value = 0, Font = ModernTheme.BodyFont };

        _btnSaveAttendance = new Button { Text = "💾 Record Attendance", Location = new Point(570, 26), Size = new Size(160, 32) };
        ModernTheme.ApplyPrimaryButtonStyle(_btnSaveAttendance);
        _btnSaveAttendance.Click += async (s, e) => await SaveAttendanceAsync();

        pnlEntry.Controls.AddRange(new Control[] {
            lblEmp, _cmbEmployeeAtt, lblTotal, _numTotalDays, lblWork, _numWorkingDays,
            lblPres, _numPresentDays, lblAbs, _numAbsentDays, _btnSaveAttendance
        });

        // Grid
        _dgvAttendance = new DataGridView { Dock = DockStyle.Fill };
        ModernTheme.ApplyDataGridStyle(_dgvAttendance);

        _dgvAttendance.Columns.Add("EmployeeName", "Employee Name");
        _dgvAttendance.Columns.Add("Year", "Year");
        _dgvAttendance.Columns.Add("Month", "Month");
        _dgvAttendance.Columns.Add("TotalDays", "Total Days");
        _dgvAttendance.Columns.Add("WorkingDays", "Working Days");
        _dgvAttendance.Columns.Add("PresentDays", "Present Days");
        _dgvAttendance.Columns.Add("AbsentDays", "Absent Days");
        _dgvAttendance.Columns.Add("RecordedAt", "Recorded On");

        _dgvAttendance.Columns[0].Width = 200;
        _dgvAttendance.Columns[1].Width = 80;
        _dgvAttendance.Columns[2].Width = 80;
        _dgvAttendance.Columns[3].Width = 110;
        _dgvAttendance.Columns[4].Width = 120;
        _dgvAttendance.Columns[5].Width = 120;
        _dgvAttendance.Columns[6].Width = 120;
        _dgvAttendance.Columns[7].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        _dgvAttendance.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _dgvAttendance.ColumnHeadersHeight = 44;

        _pagerAttendance = new PaginationControl { Dock = DockStyle.Bottom };
        _pagerAttendance.PageChanged += async (s, page) => await LoadAttendanceDataAsync(page);
        _pagerAttendance.PageSizeChanged += async (s, size) => await LoadAttendanceDataAsync(1);

        tab.Controls.Add(_dgvAttendance);
        tab.Controls.Add(_pagerAttendance);
        tab.Controls.Add(pnlEntry);
    }

    private void BuildSalaryTab(TabPage tab)
    {
        var pnlEntry = new Panel
        {
            Dock = DockStyle.Top,
            Height = 95,
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(12)
        };

        var lblEmp = new Label { Text = "Employee:", Location = new Point(12, 10), AutoSize = true, Font = ModernTheme.SmallFont };
        _cmbEmployeeSal = new ComboBox { Location = new Point(12, 30), Size = new Size(200, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = ModernTheme.BodyFont };
        _cmbEmployeeSal.SelectedIndexChanged += (s, e) => OnSalaryEmployeeSelected();

        _lblBasicSalary = new Label { Text = "Basic: LKR 0.00", Location = new Point(225, 34), AutoSize = true, Font = ModernTheme.BodyBoldFont, ForeColor = ModernTheme.TextPrimary };

        var lblAllow = new Label { Text = "Allowances:", Location = new Point(360, 10), AutoSize = true, Font = ModernTheme.SmallFont };
        _numAllowances = new NumericUpDown { Location = new Point(360, 30), Size = new Size(110, 26), Maximum = 500000, DecimalPlaces = 2, Font = ModernTheme.BodyFont };
        _numAllowances.ValueChanged += (s, e) => RecalculateNetSalary();

        var lblDeduct = new Label { Text = "Deductions:", Location = new Point(485, 10), AutoSize = true, Font = ModernTheme.SmallFont };
        _numDeductions = new NumericUpDown { Location = new Point(485, 30), Size = new Size(110, 26), Maximum = 500000, DecimalPlaces = 2, Font = ModernTheme.BodyFont };
        _numDeductions.ValueChanged += (s, e) => RecalculateNetSalary();

        var lblNet = new Label { Text = "Net Salary:", Location = new Point(610, 10), AutoSize = true, Font = ModernTheme.SmallFont };
        _lblNetSalary = new Label { Text = "LKR 0.00", Location = new Point(610, 34), AutoSize = true, Font = ModernTheme.SubHeaderFont, ForeColor = ModernTheme.SuccessEmerald };

        _btnSaveSalary = new Button { Text = "💰 Pay / Save Salary", Location = new Point(740, 26), Size = new Size(160, 34) };
        ModernTheme.ApplySuccessButtonStyle(_btnSaveSalary);
        _btnSaveSalary.Click += async (s, e) => await SaveSalaryAsync();

        pnlEntry.Controls.AddRange(new Control[] {
            lblEmp, _cmbEmployeeSal, _lblBasicSalary, lblAllow, _numAllowances,
            lblDeduct, _numDeductions, lblNet, _lblNetSalary, _btnSaveSalary
        });

        // Grid
        _dgvSalary = new DataGridView { Dock = DockStyle.Fill };
        ModernTheme.ApplyDataGridStyle(_dgvSalary);

        _dgvSalary.Columns.Add("EmployeeName", "Employee Name");
        _dgvSalary.Columns.Add("Year", "Year");
        _dgvSalary.Columns.Add("Month", "Month");
        _dgvSalary.Columns.Add("BasicSalary", "Basic (LKR)");
        _dgvSalary.Columns.Add("Allowances", "Allowances (LKR)");
        _dgvSalary.Columns.Add("Deductions", "Deductions (LKR)");
        _dgvSalary.Columns.Add("NetSalary", "Net Paid (LKR)");
        _dgvSalary.Columns.Add("PaidDate", "Paid On");

        _dgvSalary.Columns[0].Width = 190;
        _dgvSalary.Columns[1].Width = 80;
        _dgvSalary.Columns[2].Width = 80;
        _dgvSalary.Columns[3].Width = 130;
        _dgvSalary.Columns[4].Width = 140;
        _dgvSalary.Columns[5].Width = 140;
        _dgvSalary.Columns[6].Width = 140;
        _dgvSalary.Columns[7].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        _dgvSalary.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _dgvSalary.ColumnHeadersHeight = 44;

        _pagerSalary = new PaginationControl { Dock = DockStyle.Bottom };
        _pagerSalary.PageChanged += async (s, page) => await LoadSalaryDataAsync(page);
        _pagerSalary.PageSizeChanged += async (s, size) => await LoadSalaryDataAsync(1);

        tab.Controls.Add(_dgvSalary);
        tab.Controls.Add(_pagerSalary);
        tab.Controls.Add(pnlEntry);
    }

    private async Task LoadEmployeesAsync()
    {
        try
        {
            _employees = await _employeeRepository.GetAllActiveAsync();
            _cmbEmployeeAtt.Items.Clear();
            _cmbEmployeeSal.Items.Clear();

            foreach (var emp in _employees)
            {
                _cmbEmployeeAtt.Items.Add($"{emp.EmployeeCode} - {emp.Name}");
                _cmbEmployeeSal.Items.Add($"{emp.EmployeeCode} - {emp.Name}");
            }

            if (_cmbEmployeeAtt.Items.Count > 0) _cmbEmployeeAtt.SelectedIndex = 0;
            if (_cmbEmployeeSal.Items.Count > 0) _cmbEmployeeSal.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading employees: {ex.Message}");
        }
    }

    private void OnSalaryEmployeeSelected()
    {
        if (_cmbEmployeeSal.SelectedIndex < 0 || _cmbEmployeeSal.SelectedIndex >= _employees.Count)
            return;

        var emp = _employees[_cmbEmployeeSal.SelectedIndex];
        _lblBasicSalary.Text = $"Basic: LKR {emp.BasicSalary:N2}";
        RecalculateNetSalary();
    }

    private void RecalculateNetSalary()
    {
        if (_cmbEmployeeSal.SelectedIndex < 0 || _cmbEmployeeSal.SelectedIndex >= _employees.Count)
            return;

        var emp = _employees[_cmbEmployeeSal.SelectedIndex];
        var net = emp.BasicSalary + _numAllowances.Value - _numDeductions.Value;
        _lblNetSalary.Text = $"LKR {Math.Max(0, net):N2}";
    }

    private async Task SaveAttendanceAsync()
    {
        if (_cmbEmployeeAtt.SelectedIndex < 0 || _cmbEmployeeAtt.SelectedIndex >= _employees.Count)
            return;

        var emp = _employees[_cmbEmployeeAtt.SelectedIndex];
        var att = new Attendance
        {
            EmployeeId = emp.Id,
            EmployeeName = emp.Name,
            Year = (int)_numYear.Value,
            Month = _cmbMonth.SelectedIndex + 1,
            TotalDays = (int)_numTotalDays.Value,
            WorkingDays = (int)_numWorkingDays.Value,
            PresentDays = (int)_numPresentDays.Value,
            AbsentDays = (int)_numAbsentDays.Value
        };

        await _repository.SaveAttendanceAsync(att);
        MessageBox.Show($"Attendance recorded for {emp.Name}!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        await LoadAttendanceDataAsync(_pagerAttendance.CurrentPage);
    }

    private async Task SaveSalaryAsync()
    {
        if (_cmbEmployeeSal.SelectedIndex < 0 || _cmbEmployeeSal.SelectedIndex >= _employees.Count)
            return;

        var emp = _employees[_cmbEmployeeSal.SelectedIndex];
        var net = emp.BasicSalary + _numAllowances.Value - _numDeductions.Value;

        var sal = new SalaryRecord
        {
            EmployeeId = emp.Id,
            EmployeeName = emp.Name,
            Year = (int)_numYear.Value,
            Month = _cmbMonth.SelectedIndex + 1,
            BasicSalary = emp.BasicSalary,
            DailyRate = emp.BasicSalary / 24,
            Allowances = _numAllowances.Value,
            Deductions = _numDeductions.Value,
            NetSalary = Math.Max(0, net),
            Notes = $"Paid for month {_cmbMonth.SelectedIndex + 1}/{_numYear.Value}"
        };

        await _repository.SaveSalaryAsync(sal);
        MessageBox.Show($"Salary of LKR {sal.NetSalary:N2} processed for {emp.Name}!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        await LoadSalaryDataAsync(_pagerSalary.CurrentPage);
    }

    public async Task LoadAttendanceDataAsync(int page = 1)
    {
        try
        {
            var year = (int)_numYear.Value;
            var month = _cmbMonth.SelectedIndex + 1;

            var request = new PagedRequest
            {
                PageNumber = page,
                PageSize = _pagerAttendance.SelectedPageSize
            };

            var result = await _repository.GetAttendancePagedAsync(request, year, month);

            _dgvAttendance.Rows.Clear();
            foreach (var a in result.Items)
            {
                _dgvAttendance.Rows.Add(a.EmployeeName, a.Year, a.Month, a.TotalDays, a.WorkingDays, a.PresentDays, a.AbsentDays, a.RecordedAt.ToLocalTime().ToString("yyyy-MM-dd"));
            }

            _pagerAttendance.BindPagedResult(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading attendance: {ex.Message}");
        }
    }

    public async Task LoadSalaryDataAsync(int page = 1)
    {
        try
        {
            var year = (int)_numYear.Value;
            var month = _cmbMonth.SelectedIndex + 1;

            var request = new PagedRequest
            {
                PageNumber = page,
                PageSize = _pagerSalary.SelectedPageSize
            };

            var result = await _repository.GetSalaryPagedAsync(request, year, month);

            _dgvSalary.Rows.Clear();
            foreach (var s in result.Items)
            {
                _dgvSalary.Rows.Add(s.EmployeeName, s.Year, s.Month, s.BasicSalary.ToString("N2"), s.Allowances.ToString("N2"), s.Deductions.ToString("N2"), s.NetSalary.ToString("N2"), s.PaidDate.ToLocalTime().ToString("yyyy-MM-dd"));
            }

            _pagerSalary.BindPagedResult(result);

            // Update monthly total payroll
            var totalExpense = await _repository.GetTotalSalaryExpenseAsync(year, month);
            _lblTotalSalaryExpense.Text = $"Total Payroll ({month}/{year}): LKR {totalExpense:N2}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading salary: {ex.Message}");
        }
    }

    private async Task ExportPayrollReportPdfAsync()
    {
        if (_pdfReportService == null)
        {
            MessageBox.Show("PDF Reporting Service is not available.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int year = (int)_numYear.Value;
        int month = _cmbMonth.SelectedIndex + 1;

        using var sfd = new SaveFileDialog
        {
            Filter = "PDF Document (*.pdf)|*.pdf",
            FileName = $"Polkotuwa_Payroll_Audit_{year}_{month:D2}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
            Title = "Save Staff Payroll PDF Report"
        };

        if (sfd.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                var salaryResult = await _repository.GetSalaryPagedAsync(new PagedRequest { PageNumber = 1, PageSize = 200 }, year, month);
                await _pdfReportService.GeneratePayrollReportAsync(salaryResult.Items, year, month, sfd.FileName);

                var prompt = MessageBox.Show(
                    $"Payroll Audit PDF Report generated successfully!\n\nFile saved to:\n{sfd.FileName}\n\nDo you want to open the report now?",
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
