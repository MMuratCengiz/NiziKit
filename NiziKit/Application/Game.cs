using System.Runtime.CompilerServices;
using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Application.Windowing;
using NiziKit.Core;
using NiziKit.Graphics;
using NiziKit.Graphics.Binding;

namespace NiziKit.Application;

public class Game : IDisposable
{
    private static Game? _instance;
    private static bool _engineInitialized;

    public static Game Instance => _instance ?? throw new InvalidOperationException("Game not initialized");

    private readonly FixedTimestep _fixedTimestep;
    private readonly Time _time;
    private readonly GraphicsContext _graphics;
    private readonly GpuBinding _gpuBinding;
    private bool _disposed;

    public AppWindow Window { get; }

    public InputSystem InputSystem { get; }

    public bool IsRunning { get; set; }

    public static void Run<TGame>(Func<TGame> factory) where TGame : Game
    {
        TGame game;
        try
        {
            game = factory();
        }
        catch
        {
            _instance?.Dispose();
            throw;
        }

        using (game)
        {
            game.Run();
        }
    }

    protected Game(GameDesc? desc = null)
    {
        if (_instance != null)
        {
            throw new InvalidOperationException(
                "A game is already running. Only one game instance is allowed per process.");
        }

        InitializeEngine();

        desc ??= new GameDesc();
        _fixedTimestep = new FixedTimestep(desc.FixedUpdateRate);

        var windowFlags = new WindowFlags
        {
            Resizable = desc.Resizable,
            Maximized = desc.Maximized,
            Borderless = desc.Borderless,
            Fullscreen = desc.Fullscreen
        };
        Window = new AppWindow(desc.Title, desc.Width, desc.Height, windowFlags);

        _time = new Time();
        InputSystem = new InputSystem();
        _graphics = new GraphicsContext(Window.NativeWindow, desc.Graphics);
        _gpuBinding = new GpuBinding();

        _instance = this;
    }

    protected virtual void Load()
    {
    }

    protected virtual void FixedUpdate(float fixedDt)
    {
    }

    protected virtual void Update(float dt)
    {
    }

    protected virtual void Render(float dt)
    {
    }

    protected virtual void OnResize(uint width, uint height)
    {
    }

    protected virtual void OnEvent(ref Event ev)
    {
    }

    protected virtual void OnShutdown()
    {
    }

    public void Run()
    {
        Window.Show();
        Load();

        IsRunning = true;
        Time.Start();

        while (IsRunning)
        {
            RunFrame();
        }

        OnShutdown();
    }

    public void Quit()
    {
        IsRunning = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _instance = null;

        _gpuBinding?.Dispose();
        _graphics?.Dispose();
        InputSystem?.Dispose();
        Window?.Dispose();
        ShutdownEngine();
    }

    private static void InitializeEngine()
    {
        if (_engineInitialized)
        {
            return;
        }

        Log.Initialize();
        DenOfIzRuntime.Initialize();
        Engine.Init(new EngineDesc());
        _engineInitialized = true;
    }

    private static void ShutdownEngine()
    {
        if (!_engineInitialized)
        {
            return;
        }

        _engineInitialized = false;
        Engine.Shutdown();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunFrame()
    {
        Time.Tick();
        ProcessEvents();

        if (!IsRunning)
        {
            return;
        }

        if (Window.IsMinimized)
        {
            return;
        }

        var fixedSteps = _fixedTimestep.Accumulate(Time.UnscaledDeltaTime);
        for (var i = 0; i < fixedSteps; i++)
        {
            FixedUpdate((float)_fixedTimestep.FixedDeltaTime);
        }

        Update(Time.DeltaTime);
        Render(Time.DeltaTime);
    }

    private void ProcessEvents()
    {
        while (InputSystem.PollEvent(out var ev))
        {
            if (ev.Type == EventType.Quit)
            {
                IsRunning = false;
                return;
            }

            if (ev.Type == EventType.WindowEvent)
            {
                Window.HandleWindowEvent(ev.Window.Event, ev.Window.Data1, ev.Window.Data2);

                if (ev.Window.Event == WindowEventType.SizeChanged)
                {
                    var width = (uint)ev.Window.Data1;
                    var height = (uint)ev.Window.Data2;
                    GraphicsContext.Resize(width, height);
                    OnResize(width, height);
                }
            }

            OnEvent(ref ev);
        }
    }
}