using DenOfIz;
using NiziKit.Graphics.Renderer;
using NiziKit.Graphics.Resources;

namespace Pong.Renderer;

public class QuadRenderer : IDisposable
{
    private QuadPipeline _pipeline = new();
    private CommandListAllocator _commandListAllocator = new();
    private RenderFrame _renderFrame;
    
    private readonly CycledTexture _sceneColor;
    private readonly CycledTexture _sceneDepth;
    
    public QuadRenderer()
    {
        _renderFrame = new RenderFrame();
        _sceneColor = CycledTexture.ColorAttachment("SceneColor2D");
        _sceneDepth = CycledTexture.DepthAttachment("SceneDepth2D");
    }

    public void Render(float deltaTime)
    {
        var pass = _renderFrame.BeginGraphicsPass();
        pass.SetRenderTarget(0, _sceneColor, LoadOp.Clear);
        pass.SetDepthTarget(_sceneDepth, LoadOp.Clear);
        pass.Begin();
        
        pass.BindPipeline(_pipeline.Instance);
        
        pass.End();
    }


    public void Dispose()
    {
        _pipeline.Dispose();
        _commandListAllocator.Dispose();
    }
}