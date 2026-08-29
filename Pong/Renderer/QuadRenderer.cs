using DenOfIz;
using NiziKit.Graphics;
using NiziKit.Graphics.Renderer;
using NiziKit.Graphics.Resources;
using Pong.Entities;

namespace Pong.Renderer;

public class QuadRenderer : IDisposable
{
    private readonly QuadPipeline _pipeline = new();
    private readonly RenderFrame _renderFrame;

    private readonly CycledTexture _sceneColor;
    private readonly CycledTexture _sceneDepth;

    private readonly CameraBinding _cameraBinding;

    private readonly List<DrawBinding> _drawBindings = [];
    private readonly List<AlbedoBinding> _albedoBindings = [];

    private readonly BatchResourceCopy _batchCopy = new(new BatchResourceCopyDesc
    {
        Device = GraphicsContext.Device
    });

    private readonly QuadVertexBuffer _vertexBuffer = new();
    private readonly TextureStore _textureStore = new();
    private readonly Scene _scene;

    private readonly Sampler _sampler = GraphicsContext.Device.CreateSampler(new SamplerDesc
    {
        AddressModeU = SamplerAddressMode.Repeat,
        AddressModeV = SamplerAddressMode.Repeat,
        AddressModeW = SamplerAddressMode.Repeat,
        MinFilter = Filter.Linear,
        MagFilter = Filter.Linear,
        MipmapMode = MipmapMode.Linear
    });

    private Camera _camera = new();

    public QuadRenderer(Scene scene)
    {
        _scene = scene;
        _renderFrame = new RenderFrame();
        _sceneColor = CycledTexture.ColorAttachment("SceneColor2D");
        _sceneDepth = CycledTexture.DepthAttachment("SceneDepth2D");

        _cameraBinding = new CameraBinding(_pipeline.ViewProjectionBindGroupLayout);
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

        foreach (var renderObject in _scene)
        {
            EnsureBindings(renderObject.Id);

            var drawBinding = _drawBindings[renderObject.Id];
            var albedoBinding = _albedoBindings[renderObject.Id];

            drawBinding.Update(renderObject);
            albedoBinding.Update(renderObject);

            pass.Bind(drawBinding);
            pass.Bind(albedoBinding);
            pass.Draw(QuadVertexBuffer.VertexCount);
        }

        pass.End();
        
        _renderFrame.Submit();
        _renderFrame.Present(_sceneColor);
    }

    private void EnsureBindings(int id)
    {
        while (_drawBindings.Count <= id)
        {
            _drawBindings.Add(new DrawBinding(_pipeline.DrawBindGroupLayout));
            _albedoBindings.Add(new AlbedoBinding(_pipeline.AlbedoBindGroupLayout, _textureStore, _sampler));
        }
    }

    public void SetCamera(Camera camera)
    {
        _camera = camera;
    }

    public void Dispose()
    {
        _renderFrame.Dispose();

        foreach (var binding in _albedoBindings)
        {
            binding.Dispose();
        }

        foreach (var binding in _drawBindings)
        {
            binding.Dispose();
        }

        _cameraBinding.Dispose();
        _sampler.Dispose();

        _sceneDepth.Dispose();
        _sceneColor.Dispose();
        _textureStore.Dispose();
        _vertexBuffer.Dispose();
        _batchCopy.Dispose();
        _pipeline.Dispose();
    }
}
