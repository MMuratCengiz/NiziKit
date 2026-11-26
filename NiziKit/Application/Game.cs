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
    private const float ResumeDeltaTime = 1f / 60;
    private const float AcquireRetrySeconds = 1f / 60;

    private static Game? _instance;
    private static bool _engineInitialized;
    private static int _frameRequested;

    public static Game Instance => _instance ?? throw new InvalidOperationException("Game not initialized");

    private readonly FixedTimestep _fixedTimestep;
    private readonly Time _time;
    private readonly GraphicsContext _graphics;
    private readonly GpuBinding _gpuBinding;
    private readonly RenderMode _renderMode;
    private bool _settle = true;
    private bool _retryAcquire;
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
        _renderMode = desc.RenderMode;
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
        _graphics = new GraphicsContext(desc.Graphics, Window.NativeWindow, _renderMode == RenderMode.OnDemand ? PresentMode.Fifo : PresentMode.Immediate);
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

        if (_renderMode == RenderMode.OnDemand)
        {
            while (IsRunning)
            {
                RunOnDemandFrame();
            }
        }
        else
        {
            while (IsRunning)
            {
                RunFrame();
            }
        }

        GraphicsContext.WaitIdle();
        OnShutdown();
    }

    public void Quit()
    {
        IsRunning = false;
    }

    public static void RequestFrame()
    {
        if (_instance is { _renderMode: RenderMode.OnDemand } && Interlocked.Exchange(ref _frameRequested, 1) == 0)
        {
            InputSystem.Wake();
        }
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

    internal static void InitializeEngine()
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

    internal static void ShutdownEngine()
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

    private void RunOnDemandFrame()
    {
        var wait = Window.IsMinimized || (!_settle && Volatile.Read(ref _frameRequested) == 0 && !Ui.IsAnimating && !UiDebug.HasPending);
        _settle = false;
        if (wait || _retryAcquire)
        {
            WaitForEvent(wait ? float.PositiveInfinity : AcquireRetrySeconds);
        }

        Time.Tick();
        if (wait)
        {
            Ui.AddIdleTime(Time.ClampDeltaTime(ResumeDeltaTime));
            _fixedTimestep.Reset();
        }

        while (IsRunning && InputSystem.PollEvent(out var ev))
        {
            DispatchEvent(ref ev);
        }

        if (!IsRunning)
        {
            return;
        }

        Window.Refresh();

        if (StopOnDeviceLost() || Window.IsMinimized)
        {
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

        Interlocked.Exchange(ref _frameRequested, 0);
        Update(Time.DeltaTime);
        if (canRender)
        {
            Render(Time.DeltaTime);
        }

        _settle |= !canRender;
        _retryAcquire = !canRender;
    }

    private void WaitForEvent(float maxSeconds)
    {
        var timeout = Window.IsMinimized ? float.PositiveInfinity : MathF.Min(Ui.NextTimer, maxSeconds);
        var received = float.IsPositiveInfinity(timeout)
            ? InputSystem.WaitEvent(out var ev)
            : InputSystem.WaitEventTimeout(out ev, (int)Math.Ceiling(Math.Min(timeout * 1000.0, int.MaxValue)));
        if (received)
        {
            DispatchEvent(ref ev);
        }
    }

    private void DispatchEvent(ref Event ev)
    {
        _settle = true;
        switch (ev.Type)
        {
            case EventType.Wake:
                return;
            case EventType.Quit:
                IsRunning = false;
                return;
            case EventType.WindowEvent:
                Window.HandleWindowEvent(ev.Window.Event);
                break;
        }

        if (!Ui.HandleEvent(in ev))
        {
            OnEvent(ref ev);
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

            if (!Ui.HandleEvent(in ev))
            {
                OnEvent(ref ev);
            }
        }
    }
}