using DenOfIz;
using NiziKit.Graphics.Renderer.Pass;
using NiziKit.Graphics.Resources;
using Semaphore = DenOfIz.Semaphore;

namespace NiziKit.Graphics.Renderer;

public partial class RenderFrame
{
    private const int MaxBlitPassesPerFrame = 8;

    private readonly BlitPass?[] _blitPasses = new BlitPass?[MaxBlitPassesPerFrame];
    private int _blitPassIndex;

    public GraphicsPass AlphaBlit(CycledTexture source, CycledTexture dest, params ReadOnlySpan<RenderPass> dependencies)
    {
        return AlphaBlit(source[_currentFrame], dest, null, dependencies);
    }

    public GraphicsPass AlphaBlit(Texture source, CycledTexture dest, Semaphore? waitSemaphore = null,
        params ReadOnlySpan<RenderPass> dependencies)
    {
        if (_blitPassIndex >= MaxBlitPassesPerFrame)
        {
            throw new InvalidOperationException($"Too many blits this frame, the limit is {MaxBlitPassesPerFrame}");
        }

        var blitPass = GetOrCreateBlitPass(_blitPassIndex++);
        var pass = BeginGraphicsPassInternal(-1);
        var passIndex = _passCount - 1;
        _passes[passIndex].ExternalWaitSemaphore = waitSemaphore;
        foreach (var dependency in dependencies)
        {
            AddDependency(passIndex, FindPassIndex(dependency));
        }

        blitPass.Execute(pass, source, dest);
        return pass;
    }

    private BlitPass GetOrCreateBlitPass(int index)
    {
        return _blitPasses[index] ??= new BlitPass();
    }

    private void ResetBlitPassIndex()
    {
        _blitPassIndex = 0;
    }

    private void DisposeBlitResources()
    {
        for (var i = 0; i < MaxBlitPassesPerFrame; i++)
        {
            _blitPasses[i]?.Dispose();
        }
    }
}
