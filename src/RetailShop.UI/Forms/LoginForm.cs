using System.Drawing;
using System.Windows.Forms;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Security;
using RetailShop.UI.Styles;

namespace RetailShop.UI.Forms;

public class LoginForm : Form
{
    private readonly IUserRepository _userRepository;
    private readonly IServiceProvider _serviceProvider;

    private TextBox _txtUsername = null!;
    private TextBox _txtPassword = null!;
    private Button _btnLogin = null!;
    private CheckBox _chkShowPassword = null!;
    private Label _lblStatus = null!;

    public LoginForm(IUserRepository userRepository, IServiceProvider serviceProvider)
    {
        _userRepository = userRepository;
        _serviceProvider = serviceProvider;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.Text = "Polkotuwa Super Stores - Secure Login";
        this.Size = new Size(520, 680);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = ModernTheme.DarkNavy;

        // Container Card
        var card = new Panel
        {
            Size = new Size(440, 580),
            Location = new Point(40, 45),
            BackColor = ModernTheme.SurfaceWhite,
            Padding = new Padding(32)
        };

        // Header Title
        var lblBrand = new Label
        {
            Text = "🛒 POLKOTUWA STORES",
            Font = ModernTheme.SubHeaderFont,
            ForeColor = ModernTheme.PrimaryIndigo,
            AutoSize = false,
            Size = new Size(376, 24),
            Location = new Point(32, 28),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblTitle = new Label
        {
            Text = "Enterprise Portal Login",
            Font = ModernTheme.TitleFont,
            ForeColor = ModernTheme.TextPrimary,
            AutoSize = false,
            Size = new Size(376, 32),
            Location = new Point(32, 54),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblSubtitle = new Label
        {
            Text = "Sign in with your assigned credentials",
            Font = ModernTheme.SmallFont,
            ForeColor = ModernTheme.TextMuted,
            AutoSize = false,
            Size = new Size(376, 20),
            Location = new Point(32, 88),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Username
        var lblUsername = new Label
        {
            Text = "Username",
            Font = ModernTheme.BodyBoldFont,
            ForeColor = ModernTheme.TextPrimary,
            Location = new Point(32, 125),
            AutoSize = true
        };

        _txtUsername = new TextBox
        {
            Font = ModernTheme.BodyFont,
            Location = new Point(32, 148),
            Size = new Size(376, 32),
            BorderStyle = BorderStyle.FixedSingle
        };

        // Password
        var lblPassword = new Label
        {
            Text = "Password",
            Font = ModernTheme.BodyBoldFont,
            ForeColor = ModernTheme.TextPrimary,
            Location = new Point(32, 195),
            AutoSize = true
        };

        _txtPassword = new TextBox
        {
            Font = ModernTheme.BodyFont,
            Location = new Point(32, 218),
            Size = new Size(376, 32),
            PasswordChar = '●',
            BorderStyle = BorderStyle.FixedSingle
        };

        _chkShowPassword = new CheckBox
        {
            Text = "Show password",
            Font = ModernTheme.SmallFont,
            ForeColor = ModernTheme.TextMuted,
            Location = new Point(32, 255),
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        _chkShowPassword.CheckedChanged += (s, e) =>
        {
            _txtPassword.PasswordChar = _chkShowPassword.Checked ? '\0' : '●';
        };

        _lblStatus = new Label
        {
            Text = "",
            Font = ModernTheme.SmallFont,
            ForeColor = ModernTheme.DangerRose,
            Location = new Point(32, 282),
            Size = new Size(376, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Login Button
        _btnLogin = new Button
        {
            Text = "Sign In to Dashboard",
            Location = new Point(32, 308),
            Size = new Size(376, 44)
        };
        ModernTheme.ApplyPrimaryButtonStyle(_btnLogin);
        _btnLogin.Click += async (s, e) => await HandleLoginAsync();

        // Enter key submission
        _txtUsername.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (string.IsNullOrWhiteSpace(_txtUsername.Text))
                    _txtUsername.Focus();
                else
                    _txtPassword.Focus();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };

        _txtPassword.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                await HandleLoginAsync();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };

        // Demo Quick Credentials Section
        var pnlDemo = new Panel
        {
            Location = new Point(32, 375),
            Size = new Size(376, 170),
            BackColor = ModernTheme.BgLight,
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblDemoTitle = new Label
        {
            Text = "⚡ Quick Demo Accounts (Click to Auto-fill)",
            Font = ModernTheme.BodyBoldFont,
            ForeColor = ModernTheme.TextPrimary,
            Location = new Point(12, 10),
            AutoSize = true
        };

        var btnAdminDemo = CreateDemoPill("Admin", "admin", "Admin@2026!", 38, Color.FromArgb(243, 232, 255), Color.FromArgb(107, 33, 168));
        var btnManagerDemo = CreateDemoPill("Manager", "manager", "Manager@2026!", 78, Color.FromArgb(224, 231, 255), Color.FromArgb(49, 46, 129));
        var btnCashierDemo = CreateDemoPill("Cashier", "cashier", "Cashier@2026!", 118, Color.FromArgb(209, 250, 229), Color.FromArgb(6, 95, 70));

        pnlDemo.Controls.Add(lblDemoTitle);
        pnlDemo.Controls.Add(btnAdminDemo);
        pnlDemo.Controls.Add(btnManagerDemo);
        pnlDemo.Controls.Add(btnCashierDemo);

        card.Controls.Add(lblBrand);
        card.Controls.Add(lblTitle);
        card.Controls.Add(lblSubtitle);
        card.Controls.Add(lblUsername);
        card.Controls.Add(_txtUsername);
        card.Controls.Add(lblPassword);
        card.Controls.Add(_txtPassword);
        card.Controls.Add(_chkShowPassword);
        card.Controls.Add(_lblStatus);
        card.Controls.Add(_btnLogin);
        card.Controls.Add(pnlDemo);

        this.Controls.Add(card);
    }

    private Button CreateDemoPill(string role, string user, string pass, int top, Color bg, Color text)
    {
        var btn = new Button
        {
            Text = $"{role}: {user} / {pass}",
            Location = new Point(12, top),
            Size = new Size(350, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = bg,
            ForeColor = text,
            Font = ModernTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += (s, e) =>
        {
            _txtUsername.Text = user;
            _txtPassword.Text = pass;
            _lblStatus.Text = $"Loaded {role} credentials. Click Sign In.";
            _lblStatus.ForeColor = ModernTheme.SuccessEmerald;
            _btnLogin.Focus();
        };
        return btn;
    }

    private async Task HandleLoginAsync()
    {
        var username = _txtUsername.Text.Trim();
        var password = _txtPassword.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _lblStatus.ForeColor = ModernTheme.DangerRose;
            _lblStatus.Text = "Please enter both username and password.";
            return;
        }

        _btnLogin.Enabled = false;
        _btnLogin.Text = "Authenticating...";
        _lblStatus.Text = "";

        try
        {
            var user = await _userRepository.AuthenticateAsync(username, password);
            if (user == null)
            {
                _lblStatus.ForeColor = ModernTheme.DangerRose;
                _lblStatus.Text = "Invalid username or password.";
                _txtPassword.Clear();
                _txtPassword.Focus();
                return;
            }

            // Start user session
            UserSession.Current.StartSession(user);

            // Open Dashboard
            var dashboard = (MainDashboardForm)_serviceProvider.GetService(typeof(MainDashboardForm))!;
            dashboard.FormClosed += (s, e) =>
            {
                UserSession.Current.ClearSession();
                _txtPassword.Clear();
                _lblStatus.Text = "";
                this.Show();
            };

            this.Hide();
            dashboard.Show();
        }
        catch (Exception ex)
        {
            _lblStatus.ForeColor = ModernTheme.DangerRose;
            _lblStatus.Text = "Database connection error. Check SQL Server.";
            MessageBox.Show($"Unable to authenticate with database:\n{ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnLogin.Enabled = true;
            _btnLogin.Text = "Sign In to Dashboard";
        }
    }
}
