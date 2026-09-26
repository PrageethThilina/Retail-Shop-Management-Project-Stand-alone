using System.Drawing;
using System.Windows.Forms;
using RetailShop.Core.Common;
using RetailShop.Core.Enums;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;
using RetailShop.Core.Security;
using RetailShop.UI.Controls;
using RetailShop.UI.Styles;

namespace RetailShop.UI.Views;

public class UsersView : UserControl
{
    private readonly IUserRepository _userRepository;

    private DataGridView _dgv = null!;
    private PaginationControl _pager = null!;
    private TextBox _txtSearch = null!;

    public UsersView(IUserRepository userRepository)
    {
        _userRepository = userRepository;

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

        var lblSearch = new Label { Text = "Search Users:", Location = new Point(12, 18), AutoSize = true, Font = ModernTheme.BodyBoldFont };
        _txtSearch = new TextBox { Location = new Point(140, 14), Size = new Size(220, 28), Font = ModernTheme.BodyFont };
        _txtSearch.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await LoadDataAsync(1); };

        var btnSearch = new Button { Text = "🔍 Search", Location = new Point(370, 13), Size = new Size(90, 30) };
        ModernTheme.ApplyPrimaryButtonStyle(btnSearch);
        btnSearch.Click += async (s, e) => await LoadDataAsync(1);

        var btnAdd = new Button { Text = "➕ New User Account", Location = new Point(pnlTop.Width - 175, 13), Size = new Size(160, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        ModernTheme.ApplySuccessButtonStyle(btnAdd);
        btnAdd.Click += (s, e) => ShowUserDialog();

        var btnDelete = new Button { Text = "🗑 Delete", Location = new Point(pnlTop.Width - 280, 13), Size = new Size(95, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        ModernTheme.ApplyDangerButtonStyle(btnDelete);
        btnDelete.Click += async (s, e) => await DeleteSelectedUserAsync();

        pnlTop.Controls.Add(lblSearch);
        pnlTop.Controls.Add(_txtSearch);
        pnlTop.Controls.Add(btnSearch);
        pnlTop.Controls.Add(btnAdd);
        pnlTop.Controls.Add(btnDelete);

        _dgv = new DataGridView { Dock = DockStyle.Fill };
        ModernTheme.ApplyDataGridStyle(_dgv);

        _dgv.Columns.Add("Id", "ID");
        _dgv.Columns.Add("Username", "Username");
        _dgv.Columns.Add("FullName", "Full Name");
        _dgv.Columns.Add("Email", "Email");
        _dgv.Columns.Add("Role", "Role (RBAC)");
        _dgv.Columns.Add("IsActive", "Status");
        _dgv.Columns.Add("LastLoginAt", "Last Login");
        _dgv.Columns.Add("CreatedAt", "Created Date");

        _dgv.Columns[0].Width = 70;
        _dgv.Columns[1].Width = 140;
        _dgv.Columns[2].Width = 180;
        _dgv.Columns[3].Width = 200;
        _dgv.Columns[4].Width = 130;
        _dgv.Columns[5].Width = 100;
        _dgv.Columns[6].Width = 150;
        _dgv.Columns[7].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

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

            var result = await _userRepository.GetPagedAsync(request);

            _dgv.Rows.Clear();
            foreach (var u in result.Items)
            {
                _dgv.Rows.Add(
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.Email,
                    u.Role.ToString(),
                    u.IsActive ? "Active" : "Disabled",
                    u.LastLoginAt.HasValue ? u.LastLoginAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "Never",
                    u.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd")
                );
            }

            _pager.BindPagedResult(result);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load users: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task DeleteSelectedUserAsync()
    {
        if (_dgv.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select a user to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!int.TryParse(_dgv.SelectedRows[0].Cells["Id"].Value?.ToString(), out var id))
            return;

        var username = _dgv.SelectedRows[0].Cells["Username"].Value?.ToString() ?? "User";

        if (id == UserSession.Current.UserId)
        {
            MessageBox.Show("You cannot delete your own logged-in account!", "Operation Prevented", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show($"Are you sure you want to permanently delete user '{username}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm == DialogResult.Yes)
        {
            await _userRepository.DeleteAsync(id);
            await LoadDataAsync(_pager.CurrentPage);
        }
    }

    private void ShowUserDialog()
    {
        using var dlg = new Form
        {
            Text = "Register New System User",
            Size = new Size(400, 480),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(20)
        };

        var lblUser = new Label { Text = "Username (Login ID):", Location = new Point(20, 15), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtUser = new TextBox { Location = new Point(20, 35), Size = new Size(340, 26), Font = ModernTheme.BodyFont };

        var lblName = new Label { Text = "Full Name:", Location = new Point(20, 70), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtName = new TextBox { Location = new Point(20, 90), Size = new Size(340, 26), Font = ModernTheme.BodyFont };

        var lblEmail = new Label { Text = "Email Address:", Location = new Point(20, 125), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtEmail = new TextBox { Location = new Point(20, 145), Size = new Size(340, 26), Font = ModernTheme.BodyFont };

        var lblRole = new Label { Text = "Access Role (RBAC):", Location = new Point(20, 180), AutoSize = true, Font = ModernTheme.SmallFont };
        var cmbRole = new ComboBox { Location = new Point(20, 200), Size = new Size(340, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = ModernTheme.BodyFont };
        cmbRole.Items.AddRange(new object[] { "Cashier", "Manager", "Admin" });
        cmbRole.SelectedIndex = 0;

        var lblPass = new Label { Text = "Initial Password (min 8 chars):", Location = new Point(20, 240), AutoSize = true, Font = ModernTheme.SmallFont };
        var txtPass = new TextBox { Location = new Point(20, 260), Size = new Size(340, 26), PasswordChar = '●', Font = ModernTheme.BodyFont };

        var btnSave = new Button { Text = "Create User Account", Location = new Point(20, 330), Size = new Size(340, 42) };
        ModernTheme.ApplyPrimaryButtonStyle(btnSave);

        btnSave.Click += async (s, e) =>
        {
            var username = txtUser.Text.Trim();
            var fullName = txtName.Text.Trim();
            var password = txtPass.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username, Full Name, and Password are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (password.Length < 8)
            {
                MessageBox.Show("Password must be at least 8 characters long for security compliance.", "Weak Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (await _userRepository.UsernameExistsAsync(username))
            {
                MessageBox.Show($"Username '{username}' already exists. Choose a different one.", "Duplicate Username", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var role = cmbRole.SelectedIndex switch
            {
                1 => UserRole.Manager,
                2 => UserRole.Admin,
                _ => UserRole.Cashier
            };

            await _userRepository.CreateAsync(new User
            {
                Username = username,
                FullName = fullName,
                Email = txtEmail.Text.Trim(),
                Role = role,
                IsActive = true
            }, password);

            MessageBox.Show($"User '{username}' created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            dlg.DialogResult = DialogResult.OK;
            dlg.Close();
        };

        dlg.Controls.AddRange(new Control[] { lblUser, txtUser, lblName, txtName, lblEmail, txtEmail, lblRole, cmbRole, lblPass, txtPass, btnSave });

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadDataAsync(_pager.CurrentPage);
        }
    }
}
