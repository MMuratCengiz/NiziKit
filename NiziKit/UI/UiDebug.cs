using System.Buffers.Binary;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using DenOfIz;
using NiziKit.Application;
using NiziKit.Graphics;

namespace NiziKit.UI;

// Inspection and remote control of the UI for tools, tests and agents: snapshots of the laid out widget tree, PNG captures, synthetic input,
// and a headless mode that runs the UI on the GPU without a window. Coordinates are in points, like every UI query.
public static partial class UiDebug
{
    public const float FrameTime = 1f / 60;

    private readonly record struct Queued(Event Event, byte[]? Text);

    private static readonly Queue<List<Queued>> Pending = new();
    private static HeadlessSession? _session;
    private static float _displayScale = 1;
    private static Vector2 _pointer = new(-1);

    public static bool IsHeadless => _session != null;

    public static bool IsAnimating => Ui.IsInitialized && (Ui.Native.IsAnimating || Images.Loading || Pending.Count > 0);

    internal static bool HasPending => Pending.Count > 0;

    public static IDisposable Headless(float width = 1280, float height = 800, float displayScale = 1)
    {
        if (Ui.IsInitialized)
        {
            throw new InvalidOperationException("The UI is already running; headless mode creates its own device and cannot run next to a window.");
        }

        Game.InitializeEngine();
        var graphics = new GraphicsContext();
        Ui.Headless = true;
        Ui.Initialize();
        _session = new HeadlessSession(graphics);
        _displayScale = displayScale;
        Resize(width, height);
        return _session;
    }

    public static void Resize(float width, float height, float? displayScale = null)
    {
        RequireHeadless();
        _displayScale = displayScale is > 0 ? displayScale.Value : _displayScale;
        Ui.Native.SetDisplayScale(_displayScale);
        Ui.Native.SetViewportSize((uint)MathF.Round(width * _displayScale), (uint)MathF.Round(height * _displayScale));
    }

    public static void Step(int frames = 1, float deltaTime = FrameTime)
    {
        RequireHeadless();
        for (var i = 0; i < frames; i++)
        {
            GraphicsContext.BeginFrame();
            Ui.Update(deltaTime);
        }
    }

    public static void Wait(float seconds, float deltaTime = FrameTime)
    {
        Step(Math.Max(1, (int)MathF.Ceiling(seconds / deltaTime)), deltaTime);
    }

    public static int Settle(int maxFrames = 600, float deltaTime = FrameTime)
    {
        RequireHeadless();
        var frames = 0;
        do
        {
            if (Images.Loading)
            {
                Thread.Sleep(2);
            }

            Step(1, deltaTime);
            frames++;
        } while (frames < maxFrames && IsAnimating);

        return frames;
    }

    public static UiSnapshot Snapshot()
    {
        return UiSnapshot.Take();
    }

    public static Widget? Find(string query)
    {
        return Snapshot().Find(query)?.Widget;
    }

    public static T? Find<T>(Func<T, bool>? match = null) where T : Widget
    {
        return Ui.Built.OfType<T>().FirstOrDefault(w => match == null || match(w));
    }

    public static Widget? WidgetAt(Vector2 point)
    {
        return Snapshot().At(point)?.Widget;
    }

    public static void Send(in Event ev)
    {
        Pending.Enqueue([new Queued(ev, null)]);
    }

    public static void MoveTo(Vector2 point)
    {
        Post([Motion(point)]);
        Complete();
    }

    public static void Click(Vector2 point, MouseButton button = MouseButton.Left, int clicks = 1)
    {
        Post([Motion(point), Button(point, button, true, 1)], [Button(point, button, false, 1)]);
        for (var i = 2; i <= clicks; i++)
        {
            Post([Button(point, button, true, (uint)i)], [Button(point, button, false, (uint)i)]);
        }

        Complete();
    }

    public static bool Click(Widget widget, MouseButton button = MouseButton.Left, int clicks = 1)
    {
        if (Target(widget) is not { } point)
        {
            return false;
        }

        Click(point, button, clicks);
        return true;
    }

    public static bool Click(string query, MouseButton button = MouseButton.Left, int clicks = 1)
    {
        if (Snapshot().Locate(query) is not { Rect: { Width: > 0, Height: > 0 } rect })
        {
            return false;
        }

        Click(rect.Center, button, clicks);
        return true;
    }

    public static void Drag(Vector2 from, Vector2 to, int steps = 8)
    {
        Post([Motion(from)], [Button(from, MouseButton.Left, true, 1)]);
        for (var i = 1; i <= steps; i++)
        {
            Post([Motion(Vector2.Lerp(from, to, i / (float)steps))]);
        }

        Post([Button(to, MouseButton.Left, false, 1)]);
        Complete();
    }

    public static void Scroll(Vector2 point, float lines, float horizontal = 0)
    {
        var wheel = new Event { Type = EventType.MouseWheel };
        wheel.MouseWheel.X = horizontal;
        wheel.MouseWheel.Y = -lines;
        Post([Motion(point)], [new Queued(wheel, null)]);
        Complete();
    }

    public static void Type(string text)
    {
        Post([new Queued(new Event { Type = EventType.TextInput }, Encoding.UTF8.GetBytes(text))]);
        Complete();
    }

    public static void Key(KeyCode key, KeyMod mods = 0)
    {
        Post([KeyEvent(key, mods, true)], [KeyEvent(key, mods, false)]);
        Complete();
    }

    public static void Leave()
    {
        var leave = new Event { Type = EventType.WindowEvent };
        leave.Window.Event = WindowEventType.Leave;
        _pointer = new Vector2(-1);
        Post([new Queued(leave, null)]);
        Complete();
    }

    public static void Focus(Widget widget)
    {
        widget.Focus();
        Complete();
    }

    public static byte[] Capture(out int width, out int height)
    {
        var (w, h) = Ui.Native.ViewportPixelSize;
        width = (int)w;
        height = (int)h;
        var rgba = new byte[width * height * 4];
        if (!Ui.Native.Capture((uint)GraphicsContext.FrameIndex, rgba))
        {
            throw new InvalidOperationException("The UI could not be captured, see the DenOfIz log.");
        }

        return rgba;
    }

    public static void Capture(string path, Rectangle? region = null)
    {
        var rgba = Capture(out var width, out var height);
        var scale = Ui.PixelsPerPoint;
        var x0 = 0;
        var y0 = 0;
        var x1 = width;
        var y1 = height;
        if (region is { } r)
        {
            x0 = Math.Clamp((int)MathF.Floor(r.X * scale), 0, width);
            y0 = Math.Clamp((int)MathF.Floor(r.Y * scale), 0, height);
            x1 = Math.Clamp((int)MathF.Ceiling(r.Right * scale), x0, width);
            y1 = Math.Clamp((int)MathF.Ceiling(r.Bottom * scale), y0, height);
        }

        CreateDirectory(path);
        WritePng(path, rgba, width, x0, y0, Math.Max(1, x1 - x0), Math.Max(1, y1 - y0));
    }

    public static string[] Save(string pathWithoutExtension, bool nodes = false)
    {
        var snapshot = Snapshot();
        string[] paths = [pathWithoutExtension + ".txt", pathWithoutExtension + ".json", pathWithoutExtension + ".png"];
        CreateDirectory(pathWithoutExtension);
        File.WriteAllText(paths[0], snapshot.ToString());
        File.WriteAllText(paths[1], snapshot.ToJson(nodes));
        Capture(paths[2]);
        return paths;
    }

    internal static void DispatchQueued()
    {
        if (!Pending.TryDequeue(out var batch))
        {
            return;
        }

        foreach (var item in batch)
        {
            Dispatch(item);
        }
    }

    private static unsafe void Dispatch(Queued item)
    {
        var ev = item.Event;
        if (item.Text == null)
        {
            Ui.HandleEvent(in ev);
            return;
        }

        fixed (byte* chars = item.Text)
        {
            ev.Text.Text = new StringView { Chars = (IntPtr)chars, NumChars = (ulong)item.Text.Length };
            Ui.HandleEvent(in ev);
        }
    }

    private static void Post(params List<Queued>[] frames)
    {
        foreach (var frame in frames)
        {
            Pending.Enqueue(frame);
        }
    }

    // Headless runs the queued frames, one more for the widgets to handle what they reported, then waits for animations to finish.
    // With a window the app's own frames deliver the input.
    private static void Complete()
    {
        if (!IsHeadless)
        {
            return;
        }

        while (Pending.Count > 0)
        {
            Step();
        }

        Step();
        Settle();
    }

    private static Vector2? Target(Widget widget)
    {
        var info = Snapshot().Info(widget);
        return info is { Visible: true } ? info.VisibleRect.Center : null;
    }

    private static Queued Motion(Vector2 point)
    {
        var ev = new Event { Type = EventType.MouseMotion };
        var pixels = point * Ui.PixelsPerPoint;
        var previous = _pointer.X >= 0 ? _pointer * Ui.PixelsPerPoint : pixels;
        ev.MouseMotion.X = pixels.X;
        ev.MouseMotion.Y = pixels.Y;
        ev.MouseMotion.RelX = pixels.X - previous.X;
        ev.MouseMotion.RelY = pixels.Y - previous.Y;
        _pointer = point;
        return new Queued(ev, null);
    }

    private static Queued Button(Vector2 point, MouseButton button, bool down, uint clicks)
    {
        var ev = new Event { Type = down ? EventType.MouseButtonDown : EventType.MouseButtonUp };
        ev.MouseButton.Button = button;
        ev.MouseButton.Clicks = clicks;
        ev.MouseButton.X = point.X * Ui.PixelsPerPoint;
        ev.MouseButton.Y = point.Y * Ui.PixelsPerPoint;
        return new Queued(ev, null);
    }

    private static Queued KeyEvent(KeyCode key, KeyMod mods, bool down)
    {
        var ev = new Event { Type = down ? EventType.KeyDown : EventType.KeyUp };
        ev.Key.KeyCode = key;
        ev.Key.Mod = (uint)mods;
        return new Queued(ev, null);
    }

    private static void RequireHeadless()
    {
        if (!IsHeadless)
        {
            throw new InvalidOperationException("Only in headless mode, see UiDebug.Headless; with a window the app drives the frames.");
        }
    }

    private static void CreateDirectory(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory != null)
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static void WritePng(string path, byte[] rgba, int stride, int x0, int y0, int width, int height)
    {
        using var file = File.Create(path);
        file.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8;
        header[9] = 6;
        WriteChunk(file, "IHDR"u8, header);

        using var data = new MemoryStream();
        using (var zlib = new ZLibStream(data, CompressionLevel.Fastest, true))
        {
            for (var y = 0; y < height; y++)
            {
                zlib.WriteByte(0);
                zlib.Write(rgba, ((y0 + y) * stride + x0) * 4, width * 4);
            }
        }

        WriteChunk(file, "IDAT"u8, data.GetBuffer().AsSpan(0, (int)data.Length));
        WriteChunk(file, "IEND"u8, []);
    }

    private static void WriteChunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> word = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(word, data.Length);
        stream.Write(word);
        stream.Write(type);
        stream.Write(data);
        var crc = Crc(Crc(0xFFFFFFFFu, type), data) ^ 0xFFFFFFFFu;
        BinaryPrimitives.WriteUInt32BigEndian(word, crc);
        stream.Write(word);
    }

    private static uint Crc(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++)
            {
                crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
            }
        }

        return crc;
    }

    private sealed class HeadlessSession(GraphicsContext graphics) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Pending.Clear();
            Ui.Root = null;
            graphics.Dispose();
            Ui.Headless = false;
            _session = null;
            _pointer = new Vector2(-1);
            Game.ShutdownEngine();
        }
    }
}
