using System.Numerics;
using DenOfIz;
using NiziKit.Graphics;
using NiziKit.Graphics.Renderer;
using NiziKit.Graphics.Resources;

namespace Pong.Renderer;

public class QuadRenderer : IDisposable
{
    private readonly QuadPipeline _pipeline = new();
    private readonly RenderFrame _renderFrame;

    private readonly CycledTexture _sceneColor;
    private readonly CycledTexture _sceneDepth;

    private readonly ModelBinding _modelBinding;
    private readonly CameraBinding _cameraBinding;
    private readonly AlbedoBinding _albedoBinding;

    private readonly List<RenderObject> _renderObjects = [];

    private readonly BatchResourceCopy _batchCopy = new(new BatchResourceCopyDesc
    {
        Device = GraphicsContext.Device
    });

    private readonly QuadVertexBuffer _vertexBuffer = new();
    private readonly TextureStore _textureStore = new();

    private Camera _camera = new()
    {
        ViewMatrix = Matrix4x4.Identity,
        ProjectionMatrix = Matrix4x4.Identity,
        ViewProjectionMatrix = Matrix4x4.Identity
    };

    public QuadRenderer()
    {
        _renderFrame = new RenderFrame();
        _sceneColor = CycledTexture.ColorAttachment("SceneColor2D");
        _sceneDepth = CycledTexture.DepthAttachment("SceneDepth2D");

        _modelBinding = new ModelBinding(_pipeline.ModelBindGroupLayout);
        _cameraBinding = new CameraBinding(_pipeline.ViewProjectionBindGroupLayout);
        _albedoBinding = new AlbedoBinding(_pipeline.AlbedoBindGroupLayout, _textureStore);
    }

    public void AddTexture(string path)
    {
        _textureStore.Add(path);
    }

    public void Load()
    {
        _batchCopy.Begin();
        _vertexBuffer.Load(_batchCopy);
        _textureStore.Load(_batchCopy);
        _batchCopy.Submit(null);
    }

    public void Render(float deltaTime)
    {
        _renderFrame.BeginFrame();
        _cameraBinding.Update(_camera);

        var pass = _renderFrame.BeginGraphicsPass();
        pass.SetRenderTarget(0, _sceneColor, LoadOp.Clear);
        pass.SetDepthTarget(_sceneDepth, LoadOp.Clear);
        pass.Begin();

        pass.BindPipeline(_pipeline.Instance);
        pass.Bind(_cameraBinding);

        pass.BindVertexBuffer(_vertexBuffer.Buffer, 0, 0);

        foreach (var renderObject in _renderObjects)
        {
            _modelBinding.Update(renderObject);
            _albedoBinding.Update(renderObject);

            pass.Bind(_modelBinding);
            pass.Bind(_albedoBinding);
            pass.Draw(QuadVertexBuffer.VertexCount);
        }

        pass.End();

        _renderFrame.Submit();
        _renderFrame.Present(_sceneColor);
    }

    public void SetCamera(Camera camera)
    {
        _camera = camera;
    }

    public void AddRenderObject(RenderObject renderObject)
    {
        _renderObjects.Add(renderObject);
    }

    public void ClearRenderObjects()
    {
        _renderObjects.Clear();
    }

    public void Dispose()
    {
        _renderFrame.Dispose();

        _albedoBinding.Dispose();
        _cameraBinding.Dispose();
        _modelBinding.Dispose();

        _sceneDepth.Dispose();
        _sceneColor.Dispose();
        _textureStore.Dispose();
        _vertexBuffer.Dispose();
        _batchCopy.Dispose();
        _pipeline.Dispose();
    }
}