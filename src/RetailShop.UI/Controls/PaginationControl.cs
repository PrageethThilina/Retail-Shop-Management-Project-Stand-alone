using System.Drawing;
using System.Windows.Forms;
using RetailShop.Core.Common;
using RetailShop.UI.Styles;

namespace RetailShop.UI.Controls;

public class PaginationControl : UserControl
{
    private Label _lblInfo = null!;
    private Label _lblPageSize = null!;
    private ComboBox _cmbPageSize = null!;
    private Button _btnFirst = null!;
    private Button _btnPrev = null!;
    private Label _lblPageStatus = null!;
    private Button _btnNext = null!;
    private Button _btnLast = null!;

    private int _currentPage = 1;
    private int _totalPages = 1;
    private bool _isUpdating = false;

    public event EventHandler<int>? PageChanged;
    public event EventHandler<int>? PageSizeChanged;

    public int CurrentPage => _currentPage;
    public int SelectedPageSize => _cmbPageSize.SelectedItem is int size ? size : 10;

    public PaginationControl()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.Height = 46;
        this.BackColor = ModernTheme.SurfaceWhite;
        this.Padding = new Padding(12, 6, 12, 6);
        this.Font = ModernTheme.BodyFont;

        // Custom top border paint
        this.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernTheme.BorderColor, 1);
            e.Graphics.DrawLine(pen, 0, 0, this.Width, 0);
        };

        var flowLeft = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0)
        };

        _lblInfo = new Label
        {
            Text = "Showing 0 of 0 records",
            ForeColor = ModernTheme.TextMuted,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 4, 16, 0)
        };

        _lblPageSize = new Label
        {
            Text = "Page Size:",
            ForeColor = ModernTheme.TextMuted,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 4, 4, 0)
        };

        _cmbPageSize = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 65,
            Height = 28,
            FlatStyle = FlatStyle.Flat
        };
        _cmbPageSize.Items.AddRange(new object[] { 10, 25, 50, 100 });
        _cmbPageSize.SelectedIndex = 0;
        _cmbPageSize.SelectedIndexChanged += (s, e) =>
        {
            if (!_isUpdating && _cmbPageSize.SelectedItem is int size)
            {
                PageSizeChanged?.Invoke(this, size);
            }
        };

        flowLeft.Controls.Add(_lblInfo);
        flowLeft.Controls.Add(_lblPageSize);
        flowLeft.Controls.Add(_cmbPageSize);

        var flowRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        _btnFirst = CreateNavButton("⏮ First", () => OnNavigate(1));
        _btnPrev = CreateNavButton("◀ Prev", () => OnNavigate(_currentPage - 1));

        _lblPageStatus = new Label
        {
            Text = "Page 1 of 1",
            ForeColor = ModernTheme.TextPrimary,
            Font = ModernTheme.BodyBoldFont,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(8, 7, 8, 0)
        };

        _btnNext = CreateNavButton("Next ▶", () => OnNavigate(_currentPage + 1));
        _btnLast = CreateNavButton("Last ⏭", () => OnNavigate(_totalPages));

        flowRight.Controls.Add(_btnFirst);
        flowRight.Controls.Add(_btnPrev);
        flowRight.Controls.Add(_lblPageStatus);
        flowRight.Controls.Add(_btnNext);
        flowRight.Controls.Add(_btnLast);

        this.Controls.Add(flowLeft);
        this.Controls.Add(flowRight);
    }

    private Button CreateNavButton(string text, Action onClick)
    {
        var btn = new Button
        {
            Text = text,
            Height = 32,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = ModernTheme.BgLight,
            ForeColor = ModernTheme.TextPrimary,
            Cursor = Cursors.Hand,
            Margin = new Padding(2, 2, 2, 2)
        };
        btn.FlatAppearance.BorderColor = ModernTheme.BorderColor;
        btn.FlatAppearance.BorderSize = 1;
        btn.Click += (s, e) => onClick();
        return btn;
    }

    private void OnNavigate(int targetPage)
    {
        if (targetPage < 1 || targetPage > _totalPages || targetPage == _currentPage)
            return;

        _currentPage = targetPage;
        PageChanged?.Invoke(this, _currentPage);
    }

    public void BindPagedResult<T>(PagedResult<T> result)
    {
        _isUpdating = true;
        try
        {
            _currentPage = result.PageNumber;
            _totalPages = Math.Max(1, result.TotalPages);

            _lblInfo.Text = result.TotalCount == 0 
                ? "No records found" 
                : $"Showing {result.FirstItemIndex:N0} - {result.LastItemIndex:N0} of {result.TotalCount:N0} records";

            _lblPageStatus.Text = $"Page {_currentPage} of {_totalPages}";

            _btnFirst.Enabled = result.HasPreviousPage;
            _btnPrev.Enabled = result.HasPreviousPage;
            _btnNext.Enabled = result.HasNextPage;
            _btnLast.Enabled = result.HasNextPage;

            if (_cmbPageSize.SelectedItem is not int currentSelected || currentSelected != result.PageSize)
            {
                if (_cmbPageSize.Items.Contains(result.PageSize))
                {
                    _cmbPageSize.SelectedItem = result.PageSize;
                }
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }
}
