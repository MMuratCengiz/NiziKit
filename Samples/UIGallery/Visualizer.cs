using DzUI = DenOfIz.UI;
using Semaphore = DenOfIz.Semaphore;

namespace UIGallery;

internal sealed class Visualizer : IDisposable
{
    public const uint Width = 320;
    public const uint Height = 180;

    private const int Bars = 14;

    private readonly DzUI _ui;
    private readonly char[] _text = new char[64];
    private float _time;

    public Visualizer()
    {
        _ui = new DzUI(new UIDesc
        {
            LogicalDevice = GraphicsContext.Device,
            ResourceTracking = GraphicsContext.ResourceTracking,
            RenderTargetFormat = Format.B8G8R8A8Unorm,
            NumFrames = GraphicsContext.NumFrames,
            Width = Width,
            Height = Height
        });
        _ui.SetDisplayScale(1);
    }

    public Texture? Texture { get; private set; }

    public (Texture Texture, Semaphore Semaphore) Render(float deltaTime)
    {
        _time += deltaTime;
        _ui.BeginFrame(deltaTime);

        var root = new UIElementDesc();
        root.Layout.Direction = UIDirection.Column;
        root.Layout.Grow = 1;
        root.Layout.Padding = UILengthEdges.All(14);
        root.Layout.Gap = 10;
        root.Style.BackgroundColor = UIColor.Hex(0x0E1424FF);
        _ui.OpenElement(in root);
        {
            var title = new UITextDesc { Color = UIColor.Hex(0xEAF0FFFF), FontSize = 15 };
            _ui.Text("Rendered by a second UI", in title);

            var bars = new UIElementDesc();
            bars.Layout.Grow = 1;
            bars.Layout.Shrink = 1;
            bars.Layout.Basis = UIValue.Points(0);
            bars.Layout.AlignItems = UIAlign.End;
            bars.Layout.Justify = UIJustify.SpaceBetween;
            _ui.OpenElement(in bars);
            for (var i = 0; i < Bars; i++)
            {
                var level = 0.15f + 0.85f * MathF.Abs(MathF.Sin(_time * (1.3f + i * 0.17f) + i * 0.7f));
                var bar = new UIElementDesc();
                bar.Layout.Width = UIValue.Percent(0.055f);
                bar.Layout.Height = UIValue.Percent(level);
                bar.Style.BackgroundColor = UIColor.Rgba((byte)(70 + i * 12), (byte)(140 + i * 6), 255);
                bar.Style.CornerRadius = new UICorners(4, 4, 0, 0);
                _ui.OpenElement(in bar);
                _ui.CloseElement();
            }

            _ui.CloseElement();

            var caption = new UITextDesc { Color = UIColor.Hex(0x8FA3C7FF), FontSize = 12 };
            _text.AsSpan().TryWrite($"{_time:0.0}s, {Width}x{Height} texture", out var written);
            _ui.Text(_text.AsSpan(0, written), in caption);
        }

        _ui.CloseElement();
        _ui.EndFrame();

        var result = _ui.RenderToTexture((uint)GraphicsContext.FrameIndex);
        Texture = result.Texture;
        return result;
    }

    public void Dispose()
    {
        _ui.Dispose();
    }
}
