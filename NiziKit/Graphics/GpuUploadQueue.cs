using DenOfIz;
using Semaphore = DenOfIz.Semaphore;

namespace NiziKit.Graphics;

public sealed class GpuUploadQueue : IDisposable
{
    private readonly record struct InFlight(BatchResourceCopy Batch, Semaphore Semaphore, ulong RetireAtFrame);

    private readonly Queue<InFlight> _inFlight = new();
    private readonly Stack<Semaphore> _semaphorePool = new();
    private ulong _frame;

    public BatchResourceCopy Begin()
    {
        var batch = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = GraphicsContext.Device
        });
        batch.Begin();
        return batch;
    }

    public Semaphore Submit(BatchResourceCopy batch)
    {
        var semaphore = _semaphorePool.Count > 0 ? _semaphorePool.Pop() : GraphicsContext.Device.CreateSemaphore();
        batch.Submit(semaphore);
        _inFlight.Enqueue(new InFlight(batch, semaphore, _frame + GraphicsContext.NumFrames));
        return semaphore;
    }

    public void Tick()
    {
        _frame++;
        while (_inFlight.Count > 0 && _inFlight.Peek().RetireAtFrame <= _frame)
        {
            var retired = _inFlight.Dequeue();
            retired.Batch.Dispose();
            _semaphorePool.Push(retired.Semaphore);
        }
    }

    public void Dispose()
    {
        GraphicsContext.WaitIdle();

        while (_inFlight.Count > 0)
        {
            var inFlight = _inFlight.Dequeue();
            inFlight.Batch.Dispose();
            inFlight.Semaphore.Dispose();
        }

        while (_semaphorePool.Count > 0)
        {
            _semaphorePool.Pop().Dispose();
        }
    }
}
