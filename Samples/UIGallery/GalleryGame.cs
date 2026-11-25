using System.Diagnostics;
using System.Numerics;
using NiziKit.UI.Widgets;
using Semaphore = DenOfIz.Semaphore;

namespace UIGallery;

internal sealed class GalleryGame() : Game(new GameDesc { Title = "NiziKit UI Gallery", Width = 1360, Height = 900 })
{
    private readonly bool _renderToTexture = Environment.GetEnvironmentVariable("UIGALLERY_TEXTURE") == "1";
    private readonly bool _printStats = Environment.GetEnvironmentVariable("UIGALLERY_STATS") == "1";
    private readonly int _script = int.TryParse(Environment.GetEnvironmentVariable("UIGALLERY_SCRIPT"), out var script) ? script : 0;
    private readonly int _page = int.TryParse(Environment.GetEnvironmentVariable("UIGALLERY_PAGE"), out var page) ? page : 0;
    private readonly Stopwatch _frameTimer = new();
    private RenderFrame _frame = null!;
    private CycledTexture _color = null!;
    private Visualizer _visualizer = null!;
    private Texture _pattern = null!;
    private Gallery _gallery = null!;
    private bool _patternInShaderState;
    private int _frameCount;
    private Vector2? _scriptPointer;
    private long _statsAllocated;
    private int _statsFrames;
    private double _statsTime;

    protected override void Load()
    {
        _frame = new RenderFrame();
        _color = CycledTexture.ColorAttachment("GalleryColor");
        _visualizer = new Visualizer();
        _pattern = PatternTexture.Create(Environment.GetEnvironmentVariable("UIGALLERY_AUTOALIGN") == "1");
        _gallery = new Gallery(() => _visualizer.Texture, _pattern);
        if (_page is >= 1 and <= 6)
        {
            _gallery.Tabs.SelectedIndex = _page - 1;
        }

        _statsAllocated = GC.GetAllocatedBytesForCurrentThread();
    }

    protected override void Render(float dt)
    {
        _frameTimer.Restart();
        _frameCount++;
        ApplyScript();

        _frame.BeginFrame();
        var (_, visualizerReady) = _visualizer.Render(dt);

        Ui.BeginFrame(dt);
        _gallery.Draw();
        Ui.EndFrame();

        if (_renderToTexture)
        {
            var clear = _frame.BeginGraphicsPass();
            clear.SetRenderTarget(0, _color, LoadOp.Clear);
            clear.Begin();
            PreparePattern(clear.CommandList);
            clear.BeginRendering();
            clear.End();

            var (ui, uiReady) = Ui.RenderToTexture([visualizerReady]);
            _frame.AlphaBlit(ui, _color, uiReady, clear);
        }
        else
        {
            var pass = _frame.BeginGraphicsPass(visualizerReady);
            pass.SetRenderTarget(0, _color, LoadOp.Clear);
            pass.Begin();
            PreparePattern(pass.CommandList);
            pass.BeginRendering();
            Ui.Render(pass.CommandList);
            pass.End();
        }

        _frame.Submit();
        _frame.Present(_color);
        UpdateStats();
    }

    protected override void OnShutdown()
    {
        GraphicsContext.WaitIdle();
        _visualizer.Dispose();
        GraphicsContext.ResourceTracking.UntrackTexture(_pattern);
        _pattern.Dispose();
        _color.Dispose();
        _frame.Dispose();
    }

    protected override void OnEvent(ref Event ev)
    {
        if (ev is { Type: EventType.KeyDown, Key.KeyCode: KeyCode.Escape })
        {
            Quit();
        }
    }

    private void PreparePattern(CommandList commandList)
    {
        if (_patternInShaderState)
        {
            return;
        }

        GraphicsContext.ResourceTracking.TransitionTexture(commandList, _pattern, (uint)ResourceUsageFlagBits.ShaderResource, QueueType.Graphics);
        _patternInShaderState = true;
    }

    private void UpdateStats()
    {
        _gallery.FrameMilliseconds = (float)_frameTimer.Elapsed.TotalMilliseconds;
        _statsFrames++;
        _statsTime += _frameTimer.Elapsed.TotalSeconds;
        if (_statsFrames < 120)
        {
            return;
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread();
        _gallery.AllocatedPerFrame = (allocated - _statsAllocated) / _statsFrames;
        if (_printStats)
        {
            Console.WriteLine(
                $"frame {_frameCount}: {_statsTime * 1000 / _statsFrames:0.000} ms cpu, {_gallery.AllocatedPerFrame} B/frame, gen0 {GC.CollectionCount(0)}, total {GC.GetTotalMemory(false) / 1024} KB");
        }

        _statsAllocated = GC.GetAllocatedBytesForCurrentThread();
        _statsFrames = 0;
        _statsTime = 0;
    }

    private void ApplyScript()
    {
        if (_script == 0)
        {
            return;
        }

        if (_frameCount == 6)
        {
            switch (_page)
            {
                case 1:
                    _gallery.QualityDropdown.Open();
                    _scriptPointer = Center(_gallery.PrimaryButton);
                    break;
                case 2:
                    _gallery.NameBox.Text = "Ada Lovelace";
                    _gallery.NotesArea.Focus();
                    _gallery.NotesArea.Select(52, 73);
                    break;
                case 3:
                    _gallery.Files.SelectedIndex = 500;
                    _gallery.Files.ScrollToSelected();
                    break;
                case 4 when _script == 2:
                    _gallery.Dialog.ShowCentered();
                    break;
                case 4:
                    _gallery.InfoPopup.Open(_gallery.InfoButton);
                    if (Ui.TryGetRect(_gallery.InfoButton.Id, out var button))
                    {
                        _gallery.OpenContextMenuAt(new Vector2(button.X + 560, button.Y + 60));
                    }

                    break;
            }
        }

        if (_scriptPointer is { } pointer)
        {
            var ev = new Event { Type = EventType.MouseMotion };
            ev.MouseMotion.X = pointer.X * Ui.PixelsPerPoint;
            ev.MouseMotion.Y = pointer.Y * Ui.PixelsPerPoint;
            Ui.HandleEvent(in ev);
        }
    }

    private static Vector2? Center(Widget widget)
    {
        return widget.TryGetBounds(out var rect) ? new Vector2(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f) : null;
    }
}
