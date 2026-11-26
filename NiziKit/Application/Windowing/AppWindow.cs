using DenOfIz;

namespace NiziKit.Application.Windowing;

public sealed class AppWindow : IDisposable
{
    private string _title;

    public uint Width { get; private set; }
    public uint Height { get; private set; }
    public float DisplayScale { get; private set; } = 1f;
    public bool IsMinimized { get; private set; }
    public bool HasFocus { get; private set; } = true;
    public GraphicsWindowHandle GraphicsHandle => NativeWindow.GetGraphicsWindowHandle();

    public Window NativeWindow { get; }

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            using var title = new StringView.Pinned(value);
            NativeWindow.SetTitle(title);
        }
    }

    public AppWindow(string title, uint width, uint height, WindowFlags flags)
    {
        _title = title;
        NativeWindow = new Window(new WindowDesc
        {
            Width = (int)width,
            Height = (int)height,
            Title = StringView.Create(title),
            Position = WindowPosition.Centered,
            Flags = flags
        });

        NativeWindow.Sync();

        Refresh();
    }

    internal void Refresh()
    {
        var pixelSize = NativeWindow.GetSizeInPixels();
        if (pixelSize is { Width: > 0, Height: > 0 })
        {
            Width = (uint)pixelSize.Width;
            Height = (uint)pixelSize.Height;
        }

        IsMinimized = NativeWindow.IsMinimized();

        var displayScale = NativeWindow.GetDisplayScale();
        if (displayScale > 0f)
        {
            DisplayScale = displayScale;
        }
    }

    public void Dispose()
    {
        NativeWindow.Dispose();
    }

    public void Show()
    {
        NativeWindow.Show();
    }

    public void Hide()
    {
        NativeWindow.Hide();
    }

    internal void HandleWindowEvent(WindowEventType eventType)
    {
        switch (eventType)
        {
            case WindowEventType.PixelSizeChanged:
                Refresh();
                break;
            case WindowEventType.Minimized:
                IsMinimized = true;
                break;
            case WindowEventType.Restored:
                IsMinimized = false;
                break;
            case WindowEventType.FocusGained:
                HasFocus = true;
                break;
            case WindowEventType.FocusLost:
                HasFocus = false;
                break;
        }
    }
}
