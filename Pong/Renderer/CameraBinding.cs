using System.Numerics;
using NiziKit.Graphics;
using DenOfIz;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Buffers;

namespace Pong.Renderer;

public class CameraBinding(BindGroupLayout layout) : ShaderBinding<Camera>(layout)
{
    private readonly ConstantBuffer<Matrix4x4> _dataBuffer = new();

    public override bool RequiresCycling => true;

    protected override void OnCreated()
    {
        for (var i = 0; i < NumBindGroups; i++)
        {
            var dataView = _dataBuffer[i];

            var bg = BindGroups[i];
            bg.BeginUpdate();
            bg.CbvWithDesc(new BindBufferDesc
            {
                Binding = 1,
                Resource = dataView.Buffer,
                ResourceOffset = dataView.Offset
            });
            bg.EndUpdate();
        }
    }

    protected override void OnUpdate(Camera target)
    {
        _dataBuffer.Write(target.ViewProjectionMatrix);
    }
}
