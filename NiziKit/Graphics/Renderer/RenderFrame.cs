using System.Runtime.CompilerServices;
using DenOfIz;
using NiziKit.Graphics.Resources;
using Semaphore = DenOfIz.Semaphore;
using RenderPass = NiziKit.Graphics.Renderer.Pass.RenderPass;
using GraphicsPass = NiziKit.Graphics.Renderer.Pass.GraphicsPass;
using ComputePass = NiziKit.Graphics.Renderer.Pass.ComputePass;
using RaytracingPass = NiziKit.Graphics.Renderer.Pass.RaytracingPass;

namespace NiziKit.Graphics.Renderer;

public partial class RenderFrame : IDisposable
{
    private const int MaxGraphicsPassesPerFrame = 32;
    private const int MaxComputePassesPerFrame = 8;
    private const int MaxRaytracingPassesPerFrame = 8;
    private const int MaxPassesPerFrame = MaxGraphicsPassesPerFrame + MaxComputePassesPerFrame + MaxRaytracingPassesPerFrame;
    private const int MaxDependenciesPerPass = 8;

    private readonly Fence[] _frameFences;
    private readonly CommandListAllocator _commandListAllocator;

    private readonly GraphicsPass[][] _graphicsPasses;
    private readonly Semaphore[][] _graphicsSemaphores;
    private readonly ComputePass[][] _computePasses;
    private readonly Semaphore[][] _computeSemaphores;
    private readonly RaytracingPass[][] _raytracingPasses;
    private readonly Semaphore[][] _raytracingSemaphores;

    private readonly Pass.PresentPass[] _presentPasses;
    private readonly Semaphore[] _presentSemaphores;
    private readonly CommandList[] _presentCommandLists;

    private readonly PassData[] _passes;
    private int _passCount;
    private int _graphicsPassCount;
    private int _computePassCount;
    private int _raytracingPassCount;

    private readonly CommandList[] _submitCommandLists;
    private readonly Semaphore[] _submitSignalSemaphores;
    private readonly Semaphore[] _submitWaitSemaphores;

    private int _currentFrame;
    
    public int FrameIndex => _currentFrame;

    [InlineArray(MaxDependenciesPerPass)]
    private struct DependencyList
    {
        private int _element0;
    }

    private struct PassData
    {
        public RenderPass Pass;
        public Semaphore SignalSemaphore;
        public QueueType QueueType;
        public Semaphore? ExternalWaitSemaphore;
        public int DependencyCount;
        public DependencyList Dependencies;
    }

    public RenderFrame()
    {
        var numFrames = (int)GraphicsContext.NumFrames;

        _frameFences = new Fence[numFrames];
        for (var i = 0; i < numFrames; i++)
        {
            _frameFences[i] = GraphicsContext.Device.CreateFence();
        }

        _commandListAllocator = new CommandListAllocator();
        _passes = new PassData[MaxPassesPerFrame];

        _graphicsPasses = new GraphicsPass[numFrames][];
        _graphicsSemaphores = new Semaphore[numFrames][];
        _computePasses = new ComputePass[numFrames][];
        _computeSemaphores = new Semaphore[numFrames][];
        _raytracingPasses = new RaytracingPass[numFrames][];
        _raytracingSemaphores = new Semaphore[numFrames][];
        _presentPasses = new Pass.PresentPass[numFrames];
        _presentSemaphores = new Semaphore[numFrames];
        _presentCommandLists = new CommandList[numFrames];

        for (var frame = 0; frame < numFrames; frame++)
        {
            _graphicsPasses[frame] = new GraphicsPass[MaxGraphicsPassesPerFrame];
            _graphicsSemaphores[frame] = new Semaphore[MaxGraphicsPassesPerFrame];
            _computePasses[frame] = new ComputePass[MaxComputePassesPerFrame];
            _computeSemaphores[frame] = new Semaphore[MaxComputePassesPerFrame];
            _raytracingPasses[frame] = new RaytracingPass[MaxRaytracingPassesPerFrame];
            _raytracingSemaphores[frame] = new Semaphore[MaxRaytracingPassesPerFrame];

            for (var i = 0; i < MaxGraphicsPassesPerFrame; i++)
            {
                var (gfxCmd, gfxSem) = _commandListAllocator.GetCommandList(QueueType.Graphics, (uint)frame);
                _graphicsPasses[frame][i] = new GraphicsPass(gfxCmd);
                _graphicsSemaphores[frame][i] = gfxSem;
            }

            for (var i = 0; i < MaxComputePassesPerFrame; i++)
            {
                var (compCmd, compSem) = _commandListAllocator.GetCommandList(QueueType.Compute, (uint)frame);
                _computePasses[frame][i] = new ComputePass(compCmd);
                _computeSemaphores[frame][i] = compSem;
            }

            for (var i = 0; i < MaxRaytracingPassesPerFrame; i++)
            {
                var (rtCmd, rtSem) = _commandListAllocator.GetCommandList(QueueType.Graphics, (uint)frame);
                _raytracingPasses[frame][i] = new RaytracingPass(rtCmd);
                _raytracingSemaphores[frame][i] = rtSem;
            }

            var (presentCmd, presentSem) = _commandListAllocator.GetCommandList(QueueType.Graphics, (uint)frame);
            _presentPasses[frame] = new Pass.PresentPass();
            _presentSemaphores[frame] = presentSem;
            _presentCommandLists[frame] = presentCmd;
        }

        _submitCommandLists = new CommandList[1];
        _submitSignalSemaphores = new Semaphore[1];
        _submitWaitSemaphores = new Semaphore[MaxPassesPerFrame + 1];
    }

    public void BeginFrame()
    {
        GraphicsContext.BeginFrame();
        _currentFrame = GraphicsContext.FrameIndex;
        _frameFences[_currentFrame].Wait();
        _passCount = 0;
        _graphicsPassCount = 0;
        _computePassCount = 0;
        _raytracingPassCount = 0;
        ResetBlitPassIndex();
    }

    public GraphicsPass BeginGraphicsPass()
    {
        return BeginGraphicsPassInternal(-1);
    }

    public GraphicsPass BeginGraphicsPass(RenderPass dependency)
    {
        var depIndex = FindPassIndex(dependency);
        return BeginGraphicsPassInternal(depIndex);
    }

    public GraphicsPass BeginGraphicsPass(RenderPass dependency1, RenderPass dependency2)
    {
        var pass = BeginGraphicsPassInternal(FindPassIndex(dependency1));
        AddDependency(_passCount - 1, FindPassIndex(dependency2));
        return pass;
    }
    
    public GraphicsPass BeginGraphicsPass(Semaphore? externalWaitSemaphore)
    {
        var pass = BeginGraphicsPassInternal(-1);
        _passes[_passCount - 1].ExternalWaitSemaphore = externalWaitSemaphore;
        return pass;
    }

    public ComputePass BeginComputePass()
    {
        return BeginComputePassInternal(-1);
    }

    public ComputePass BeginComputePass(RenderPass dependency)
    {
        var depIndex = FindPassIndex(dependency);
        return BeginComputePassInternal(depIndex);
    }

    public RaytracingPass BeginRaytracingPass()
    {
        return BeginRaytracingPassInternal(-1);
    }

    public RaytracingPass BeginRaytracingPass(RenderPass dependency)
    {
        var depIndex = FindPassIndex(dependency);
        return BeginRaytracingPassInternal(depIndex);
    }

    private GraphicsPass BeginGraphicsPassInternal(int dependencyIndex)
    {
        if (_graphicsPassCount >= MaxGraphicsPassesPerFrame)
        {
            throw new InvalidOperationException(
                $"Too many graphics passes this frame, the limit is {MaxGraphicsPassesPerFrame} (blits count as graphics passes)");
        }

        var pass = _graphicsPasses[_currentFrame][_graphicsPassCount];
        var semaphore = _graphicsSemaphores[_currentFrame][_graphicsPassCount];
        _graphicsPassCount++;

        RegisterPass(pass, semaphore, QueueType.Graphics, dependencyIndex);
        return pass;
    }

    private ComputePass BeginComputePassInternal(int dependencyIndex)
    {
        if (_computePassCount >= MaxComputePassesPerFrame)
        {
            throw new InvalidOperationException($"Too many compute passes this frame, the limit is {MaxComputePassesPerFrame}");
        }

        var pass = _computePasses[_currentFrame][_computePassCount];
        var semaphore = _computeSemaphores[_currentFrame][_computePassCount];
        _computePassCount++;

        RegisterPass(pass, semaphore, QueueType.Compute, dependencyIndex);
        return pass;
    }

    private RaytracingPass BeginRaytracingPassInternal(int dependencyIndex)
    {
        if (_raytracingPassCount >= MaxRaytracingPassesPerFrame)
        {
            throw new InvalidOperationException($"Too many raytracing passes this frame, the limit is {MaxRaytracingPassesPerFrame}");
        }

        var pass = _raytracingPasses[_currentFrame][_raytracingPassCount];
        var semaphore = _raytracingSemaphores[_currentFrame][_raytracingPassCount];
        _raytracingPassCount++;

        RegisterPass(pass, semaphore, QueueType.Graphics, dependencyIndex);
        return pass;
    }

    private void RegisterPass(RenderPass pass, Semaphore semaphore, QueueType queueType, int dependencyIndex)
    {
        pass.Reset();

        _passes[_passCount] = new PassData
        {
            Pass = pass,
            SignalSemaphore = semaphore,
            QueueType = queueType
        };

        if (dependencyIndex >= 0)
        {
            AddDependency(_passCount, dependencyIndex);
        }

        _passCount++;
    }

    private void AddDependency(int passIndex, int dependencyIndex)
    {
        ref var passData = ref _passes[passIndex];
        if (passData.DependencyCount >= MaxDependenciesPerPass)
        {
            throw new InvalidOperationException("Too many dependencies for pass");
        }

        passData.Dependencies[passData.DependencyCount++] = dependencyIndex;
    }

    private int FindPassIndex(RenderPass pass)
    {
        for (var i = 0; i < _passCount; i++)
        {
            if (_passes[i].Pass == pass)
            {
                return i;
            }
        }

        throw new InvalidOperationException("Pass not found in frame");
    }

    public void Submit()
    {
        for (var i = 0; i < _passCount; i++)
        {
            ref var passData = ref _passes[i];

            _submitCommandLists[0] = passData.Pass.CommandList;
            _submitSignalSemaphores[0] = passData.SignalSemaphore;

            var waitCount = 0;
            for (var j = 0; j < passData.DependencyCount; j++)
            {
                _submitWaitSemaphores[waitCount++] = _passes[passData.Dependencies[j]].SignalSemaphore;
            }

            if (passData.ExternalWaitSemaphore != null)
            {
                _submitWaitSemaphores[waitCount++] = passData.ExternalWaitSemaphore;
            }

            var queue = passData.QueueType == QueueType.Compute
                ? GraphicsContext.ComputeCommandQueue
                : GraphicsContext.GraphicsCommandQueue;

            queue.ExecuteCommandLists(
                new ReadOnlySpan<CommandList>(_submitCommandLists, 0, 1),
                null,
                new ReadOnlySpan<Semaphore>(_submitWaitSemaphores, 0, waitCount),
                new ReadOnlySpan<Semaphore>(_submitSignalSemaphores, 0, 1));
        }
    }

    public void Present(Texture sourceTexture)
    {
        var commandList = _presentCommandLists[_currentFrame];
        var semaphore = _presentSemaphores[_currentFrame];

        _presentPasses[_currentFrame].Execute(commandList, sourceTexture);

        _submitCommandLists[0] = commandList;
        _submitSignalSemaphores[0] = semaphore;

        var waitCount = 0;
        for (var i = 0; i < _passCount; i++)
        {
            _submitWaitSemaphores[waitCount++] = _passes[i].SignalSemaphore;
        }

        GraphicsContext.GraphicsCommandQueue.ExecuteCommandLists(
            new ReadOnlySpan<CommandList>(_submitCommandLists, 0, 1),
            _frameFences[_currentFrame],
            new ReadOnlySpan<Semaphore>(_submitWaitSemaphores, 0, waitCount),
            new ReadOnlySpan<Semaphore>(_submitSignalSemaphores, 0, 1));

        GraphicsContext.Present();
    }

    public void Present(CycledTexture sourceTexture)
    {
        Present(sourceTexture[GraphicsContext.FrameIndex]);
    }

    public void Dispose()
    {
        GraphicsContext.GraphicsCommandQueue.WaitIdle();
        GraphicsContext.ComputeCommandQueue.WaitIdle();

        DisposeBlitResources();
        
        var numFrames = (int)GraphicsContext.NumFrames;
        for (var frame = 0; frame < numFrames; frame++)
        {
            for (var i = 0; i < MaxGraphicsPassesPerFrame; i++)
            {
                _graphicsPasses[frame][i].Dispose();
            }

            for (var i = 0; i < MaxComputePassesPerFrame; i++)
            {
                _computePasses[frame][i].Dispose();
            }

            for (var i = 0; i < MaxRaytracingPassesPerFrame; i++)
            {
                _raytracingPasses[frame][i].Dispose();
            }

            _presentPasses[frame].Dispose();
        }

        foreach (var fence in _frameFences)
        {
            fence.Dispose();
        }

        _commandListAllocator.Dispose();
    }
}
