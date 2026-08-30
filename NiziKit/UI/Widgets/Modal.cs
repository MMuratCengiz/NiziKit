using System.Numerics;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public class Modal : VStack
{
    private static readonly List<Modal> OpenModals = new();

    private readonly ModalBackdrop _backdrop;
    private readonly Heading _title;

    public Modal()
    {
        Width = UiSize.FitRange(320, 520);
        Padding = 16;
        Gap = 12;
        Background = UiTheme.Surface;
        BorderColor = UiTheme.Border;
        CornerRadius = UiTheme.CornerRadius;
        _title = new Heading { Wrap = false, Visible = false };
        Children.Add(_title);
        _backdrop = new ModalBackdrop(this);
        OverlayMotion.Slide(this);
    }

    public Modal(string title) : this()
    {
        Title = title;
    }

    public string Title
    {
        get => _title.Text;
        set
        {
            _title.Text = value;
            _title.Visible = value.Length > 0;
        }
    }

    public bool IsOpen { get; private set; }
    public bool CloseOnBackdropClick { get; init; } = true;
    public bool CloseOnEscape { get; set; } = true;
    public UiColor BackdropColor { get; set; } = UiColor.Rgba(0, 0, 0, 140);

    public event Action<Modal>? Opened;
    public event Action<Modal>? Closed;

    public static Modal? Topmost => OpenModals.Count > 0 ? OpenModals[^1] : null;
    public static bool AnyOpen => OpenModals.Count > 0;

    public void Show()
    {
        if (IsOpen)
        {
            return;
        }

        IsOpen = true;
        CancelExit();
        _backdrop.CancelExit();
        Visible = true;
        _backdrop.Background = BackdropColor;
        _backdrop.Floating = new UiFloating { AttachTo = UiAttachTo.Root, ZIndex = Ui.NextZIndex() };
        Floating = UiFloating.Centered(Ui.NextZIndex());
        Ui.Overlays.Add(_backdrop);
        Ui.Overlays.Add(this);
        OpenModals.Add(this);
        Ui.UnhandledKeyDown += HandleKey;
        Opened?.Invoke(this);
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        _backdrop.Background = BackdropColor.WithAlpha(0);
        _backdrop.BeginExit(OverlayMotion.ExitDuration, Vector2.Zero, null, () => Ui.Overlays.Remove(_backdrop));
        BeginExit(OverlayMotion.ExitDuration, OverlayMotion.SlideOffset, null, () => Ui.Overlays.Remove(this));
        OpenModals.Remove(this);
        Ui.UnhandledKeyDown -= HandleKey;
        Closed?.Invoke(this);
    }

    private void HandleKey(KeyboardEventData key)
    {
        if (key.KeyCode == KeyCode.Escape && CloseOnEscape && Topmost == this && !Popup.AnyOpen)
        {
            Close();
        }
    }

    private void BackdropClicked()
    {
        if (CloseOnBackdropClick && Topmost == this)
        {
            Close();
        }
    }

    private sealed class ModalBackdrop : Widget
    {
        private readonly Modal _owner;

        public ModalBackdrop(Modal owner)
        {
            _owner = owner;
            Width = UiSize.Grow;
            Height = UiSize.Grow;
            OverlayMotion.FadeBackground(this);
        }

        protected override bool TracksPointer => true;

        protected override void OnClick(UiMouseButton button)
        {
            if (button == UiMouseButton.Left)
            {
                _owner.BackdropClicked();
            }
        }
    }
}
