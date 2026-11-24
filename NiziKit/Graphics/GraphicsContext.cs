using DenOfIz;
using NiziKit.Graphics.Binding;
using NiziKit.UI;

namespace NiziKit.Graphics;

public sealed class GraphicsContext : IDisposable
{
    private static GraphicsContext? _instance;
    public static GraphicsContext Instance => _instance ?? throw new InvalidOperationException("GraphicsContext not initialized");

    public static Window Window => Instance._window!;
    public static GraphicsApi GraphicsApi => Instance._graphicsApi;
    public static LogicalDevice Device => Instance._logicalDevice;
    public static SwapChain SwapChain => Instance._swapChain;
    public static ResourceTracking ResourceTracking => Instance._resourceTracking;
    public static CommandQueue GraphicsCommandQueue => Instance._graphicsQueue;
    public static CommandQueue ComputeCommandQueue => Instance._computeQueue;
    public static CommandQueue CopyCommandQueue => Instance._copyQueue;
    public static uint NumFrames => Instance._numFrames;
    public static Format BackBufferFormat => Instance._backBufferFormat;
    public static Format DepthBufferFormat => Instance._depthBufferFormat;
    public static int FrameIndex => Instance._currentFrame;
    public static uint Width => Instance._width;
    public static uint Height => Instance._height;
    public static UniformBufferArena UniformBufferArena => Instance._uniformBufferArena;
    public static void BeginFrame() => Instance._BeginFrame();
    public static void WaitIdle() => Instance._WaitIdle();
    public static DisplaySize GetWindowPixelSize() => Instance._window?.GetSizeInPixels() ?? new DisplaySize();

    public static event Action<uint, uint>? OnResize;

    internal static event Action<uint, uint>? ResizeCompleted;
    public static bool IsDeviceLost => _instance != null && _instance._IsDeviceLost();

    public static bool AcquireFrame() => Instance._AcquireFrame();

    internal static Texture AcquireSwapChainImage() => Instance._AcquireSwapChainImage();

    internal static void Present() => Instance._Present();

    private readonly GraphicsApi _graphicsApi;
    private readonly LogicalDevice _logicalDevice;
    private readonly SwapChain _swapChain;
    private readonly CommandQueue _graphicsQueue;
    private readonly CommandQueue _computeQueue;
    private readonly CommandQueue _copyQueue;
    private readonly ResourceTracking _resourceTracking = new();
    private readonly Window? _window;

    public static Lock TransferLock { get; } = new();

    private readonly uint _numFrames;
    private readonly Format _backBufferFormat;
    private readonly Format _depthBufferFormat;
    private int _currentFrame = 0;
    private int _nextFrame = 0;
    private uint _width;
    private uint _height;
    private readonly UniformBufferArena _uniformBufferArena;

    private Texture? _backBuffer;
    private bool _deviceLost;

    public GraphicsContext(Window window, GraphicsDesc? desc = null) : this(desc ?? new GraphicsDesc(), window)
    {
    }

    public GraphicsContext(GraphicsDesc? desc = null) : this(desc ?? new GraphicsDesc(), null)
    {
    }

    private GraphicsContext(GraphicsDesc desc, Window? window)
    {
        _numFrames = desc.NumFrames;
        _backBufferFormat = desc.BackBufferFormat;
        _depthBufferFormat = desc.DepthBufferFormat;
        _window = window;

        _graphicsApi = new GraphicsApi(desc.ApiPreference);
        _logicalDevice = _graphicsApi.CreateAndLoadOptimalLogicalDevice(new LogicalDeviceDesc
        {
#if DEBUG
            EnableValidationLayers = true
#endif
        });

        _graphicsQueue = _logicalDevice.CreateCommandQueue(new CommandQueueDesc { QueueType = QueueType.Graphics });
        _computeQueue = _logicalDevice.CreateCommandQueue(new CommandQueueDesc { QueueType = QueueType.Compute });
        _copyQueue = _logicalDevice.CreateCommandQueue(new CommandQueueDesc { QueueType = QueueType.Copy });

        if (window != null)
        {
            _swapChain = _logicalDevice.CreateSwapChain(new SwapChainDesc
            {
                PresentMode = desc.PresentMode,
                BackBufferFormat = desc.BackBufferFormat,
                DepthBufferFormat = Format.Undefined,
                CommandQueue = _graphicsQueue,
                WindowHandle = window.GetGraphicsWindowHandle(),
                NumBuffers = desc.NumFrames,
                ColorSpace = ColorSpace.Srgb
            });

            var swapChainDesc = _swapChain.GetDesc();
            _width = swapChainDesc.Width;
            _height = swapChainDesc.Height;
        }
        else
        {
            _swapChain = null!;
        }

        _uniformBufferArena = new UniformBufferArena(_logicalDevice);
        _instance = this;

        if (window != null)
        {
            Ui.Initialize();
        }
    }

    private bool _AcquireFrame()
    {
        if (_deviceLost)
        {
            return false;
        }

        if (_backBuffer != null)
        {
            return true;
        }

        var result = _swapChain.AcquireNextImage(out var backBuffer);
        if (result == Result.ErrorDeviceLost)
        {
            _deviceLost = true;
        }

        if (result != Result.Success || backBuffer == null)
        {
            return false;
        }

        _backBuffer = backBuffer;
        _resourceTracking.TrackTexture(backBuffer, QueueType.Graphics);

        var desc = _swapChain.GetDesc();
        if (desc.Width != _width || desc.Height != _height)
        {
            _width = desc.Width;
            _height = desc.Height;
            OnResize?.Invoke(_width, _height);
            ResizeCompleted?.Invoke(_width, _height);
        }

        return true;
    }

    private Texture _AcquireSwapChainImage()
    {
        if (!_AcquireFrame())
        {
            throw new InvalidOperationException("No swap chain image is available this frame.");
        }

        return _backBuffer!;
    }

    private void _Present()
    {
        if (_backBuffer == null)
        {
            return;
        }

        var backBuffer = _backBuffer;
        _backBuffer = null;
        if (_swapChain.Present() == Result.ErrorDeviceLost)
        {
            _deviceLost = true;
        }

        _resourceTracking.UntrackTexture(backBuffer);
    }

    private bool _IsDeviceLost()
    {
        if (!_deviceLost && _logicalDevice.IsDeviceLost())
        {
            _deviceLost = true;
        }

        return _deviceLost;
    }

    public void _BeginFrame()
    {
        _currentFrame = _nextFrame;
        _nextFrame = (_nextFrame + 1) % (int)NumFrames;
    }

    private void _WaitIdle()
    {
        _logicalDevice.WaitIdle();
        _graphicsQueue.WaitIdle();
        _computeQueue.WaitIdle();
        _copyQueue.WaitIdle();
    }

    public void Dispose()
    {
        _WaitIdle();
        Ui.Shutdown();
        _resourceTracking.Dispose();
        _uniformBufferArena.Dispose();
        _swapChain?.Dispose();
        _copyQueue.Dispose();
        _computeQueue.Dispose();
        _graphicsQueue.Dispose();
        _logicalDevice.Dispose();
        _graphicsApi.Dispose();
        GraphicsApi.ReportLiveObjects();
        _instance = null;
    }
}
