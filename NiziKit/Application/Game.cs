using System.Runtime.CompilerServices;
using DenOfIz;
using Microsoft.Extensions.Logging;
using NiziKit.Application.Timing;
using NiziKit.Application.Windowing;
using NiziKit.Core;
using NiziKit.Graphics;
using NiziKit.Graphics.Binding;
using NiziKit.UI;

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
        GraphicsContext.ResizeCompleted += OnResize;
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

    protected virtual void OnDeviceLost()
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

        GraphicsContext.WaitIdle();
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

        GraphicsContext.ResizeCompleted -= OnResize;
        GraphicsContext.WaitIdle();

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

        Window.Refresh();
        Ui.SetDisplayScale(Window.DisplayScale);

        if (StopOnDeviceLost())
        {
            return;
        }

        if (Window.IsMinimized)
        {
            Thread.Sleep(16);
            return;
        }

        var fixedSteps = _fixedTimestep.Accumulate(Time.UnscaledDeltaTime);
        for (var i = 0; i < fixedSteps; i++)
        {
            FixedUpdate((float)_fixedTimestep.FixedDeltaTime);
        }

        var canRender = GraphicsContext.AcquireFrame();
        if (StopOnDeviceLost())
        {
            return;
        }

        Update(Time.DeltaTime);
        if (canRender)
        {
            Render(Time.DeltaTime);
        }
    }

    private bool StopOnDeviceLost()
    {
        if (!GraphicsContext.IsDeviceLost)
        {
            return false;
        }

        Log.Get<Game>().LogCritical("GPU device lost, shutting down.");
        IsRunning = false;
        OnDeviceLost();
        return true;
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
                Window.HandleWindowEvent(ev.Window.Event);
            }

            OnEvent(ref ev);
        }
    }
}