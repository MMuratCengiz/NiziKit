using System.Numerics;
using DenOfIz;
using NiziKit.Graphics;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Buffers;
using Pong.Entities;

namespace Pong.Renderer;

public class AlbedoBinding(BindGroupLayout layout, TextureStore textureStore, Sampler sampler) : ShaderBinding<SceneObject>(layout)
{
    private readonly ConstantBuffer<Vector4> _colorBuffer = new();

    public override bool RequiresCycling => true;

    protected override void OnUpdate(SceneObject target)
    {
        _colorBuffer.Write(target.Color);

        var texture = string.IsNullOrEmpty(target.AssetPath)
            ? GraphicsContext.NullTexture
            : textureStore.GetTexture(target.AssetPath);

        var bg = BindGroup;
        bg.BeginUpdate();
        bg.SrvTexture(0, texture);
        bg.CbvWithDesc(new BindBufferDesc
        {
            Binding = 1,
            Resource = _colorBuffer.Buffer,
            ResourceOffset = _colorBuffer.Offset
        });
        bg.Sampler(1, sampler);
        bg.EndUpdate();
    }

    public override void Dispose()
    {
        _colorBuffer.Dispose();
        base.Dispose();
    }
}
