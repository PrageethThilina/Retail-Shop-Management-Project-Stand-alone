using System.Drawing;
using System.Windows.Forms;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Security;
using RetailShop.UI.Styles;

namespace RetailShop.UI.Views;

public class ChangePasswordDialog : Form
{
    private readonly IUserRepository _userRepository;

    private TextBox _txtCurrentPassword = null!;
    private TextBox _txtNewPassword = null!;
    private TextBox _txtConfirmPassword = null!;
    private Button _btnSave = null!;
    private Label _lblStatus = null!;

    public ChangePasswordDialog(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.Text = "Change Account Password";
        this.Size = new Size(380, 360);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = ModernTheme.SurfaceWhite;
        this.Padding = new Padding(24);
        this.Font = ModernTheme.BodyFont;

        var lblCurrent = new Label { Text = "Current Password:", Location = new Point(24, 15), AutoSize = true, Font = ModernTheme.SmallFont };
        _txtCurrentPassword = new TextBox { Location = new Point(24, 35), Size = new Size(316, 26), PasswordChar = '●', Font = ModernTheme.BodyFont };

        var lblNew = new Label { Text = "New Password (min 8 characters):", Location = new Point(24, 75), AutoSize = true, Font = ModernTheme.SmallFont };
        _txtNewPassword = new TextBox { Location = new Point(24, 95), Size = new Size(316, 26), PasswordChar = '●', Font = ModernTheme.BodyFont };

        var lblConfirm = new Label { Text = "Confirm New Password:", Location = new Point(24, 135), AutoSize = true, Font = ModernTheme.SmallFont };
        _txtConfirmPassword = new TextBox { Location = new Point(24, 155), Size = new Size(316, 26), PasswordChar = '●', Font = ModernTheme.BodyFont };

        _lblStatus = new Label
        {
            Text = "",
            ForeColor = ModernTheme.DangerRose,
            Location = new Point(24, 195),
            Size = new Size(316, 20),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = ModernTheme.SmallFont
        };

        _btnSave = new Button { Text = "Update Password", Location = new Point(24, 225), Size = new Size(316, 42) };
        ModernTheme.ApplyPrimaryButtonStyle(_btnSave);
        _btnSave.Click += async (s, e) => await HandleChangePasswordAsync();

        this.Controls.AddRange(new Control[] {
            lblCurrent, _txtCurrentPassword,
            lblNew, _txtNewPassword,
            lblConfirm, _txtConfirmPassword,
            _lblStatus, _btnSave
        });
    }

    private async Task HandleChangePasswordAsync()
    {
        var currentPass = _txtCurrentPassword.Text;
        var newPass = _txtNewPassword.Text;
        var confirmPass = _txtConfirmPassword.Text;

        if (string.IsNullOrWhiteSpace(currentPass) || string.IsNullOrWhiteSpace(newPass))
        {
            _lblStatus.Text = "Please enter all fields.";
            return;
        }

        if (newPass.Length < 8)
        {
            _lblStatus.Text = "New password must be at least 8 characters.";
            return;
        }

        if (newPass != confirmPass)
        {
            _lblStatus.Text = "New password and confirmation do not match.";
            return;
        }

        var userId = UserSession.Current.UserId;
        if (!userId.HasValue)
        {
            _lblStatus.Text = "No active user session.";
            return;
        }

        _btnSave.Enabled = false;
        try
        {
            var success = await _userRepository.ChangePasswordAsync(userId.Value, currentPass, newPass);
            if (success)
            {
                MessageBox.Show("Your password was updated successfully!", "Password Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                _lblStatus.Text = "Current password was incorrect.";
            }
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error updating password: {ex.Message}";
        }
        finally
        {
            _btnSave.Enabled = true;
        }
    }
}
