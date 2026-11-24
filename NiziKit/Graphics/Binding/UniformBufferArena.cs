using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace NiziKit.Graphics.Binding;

public sealed class UniformBufferArena : IDisposable
{
    private const int ChunkSize = 65536;
    private const int ConstantBufferAlignment = 256;

    private readonly LogicalDevice _device;
    private readonly List<Buffer> _buffers = [];
    private readonly Dictionary<int, Stack<GpuBufferView>> _freeViews = new();
    private Buffer _currentChunk;
    private int _currentOffset;

    public UniformBufferArena(LogicalDevice device)
    {
        _device = device;
        _currentChunk = CreateBuffer(ChunkSize);
    }

    public GpuBufferView Request(int size)
    {
        var alignedSize = AlignUp(size, ConstantBufferAlignment);
        if (_freeViews.TryGetValue(alignedSize, out var freeViews) && freeViews.TryPop(out var view))
        {
            view.NumBytes = (uint)size;
            return view;
        }

        if (alignedSize > ChunkSize)
        {
            return new GpuBufferView { Buffer = CreateBuffer(alignedSize), NumBytes = (uint)size, Offset = 0 };
        }

        if (_currentOffset + alignedSize > ChunkSize)
        {
            _currentChunk = CreateBuffer(ChunkSize);
            _currentOffset = 0;
        }

        var offset = (uint)_currentOffset;
        _currentOffset += alignedSize;
        return new GpuBufferView { Buffer = _currentChunk, NumBytes = (uint)size, Offset = offset };
    }

    public void Release(GpuBufferView view)
    {
        var alignedSize = AlignUp((int)view.NumBytes, ConstantBufferAlignment);
        if (!_freeViews.TryGetValue(alignedSize, out var freeViews))
        {
            freeViews = new Stack<GpuBufferView>();
            _freeViews[alignedSize] = freeViews;
        }

        freeViews.Push(view);
    }

    private static int AlignUp(int value, int alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    private Buffer CreateBuffer(int numBytes)
    {
        var buffer = _device.CreateBuffer(new BufferDesc
        {
            Usage = (uint)BufferUsageFlagBits.Uniform,
            NumBytes = (uint)numBytes,
            HeapType = HeapType.CpuGpu,
            DebugName = StringView.Create("Uniform Buffer Arena Chunk #" + _buffers.Count)
        });
        buffer.MapMemory();
        _buffers.Add(buffer);
        return buffer;
    }

    public void Dispose()
    {
        foreach (var buffer in _buffers)
        {
            buffer.UnmapMemory();
            buffer.Dispose();
        }

        _buffers.Clear();
        _freeViews.Clear();
    }
}
