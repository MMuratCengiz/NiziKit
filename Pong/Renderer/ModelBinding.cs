using System.Numerics;
using DenOfIz;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Buffers;

namespace Pong.Renderer;

public class ModelBinding : ShaderBinding<RenderObject>
{
    private readonly ConstantBuffer<Matrix4x4> _dataBuffer = new();

    public override BindGroupLayout Layout { get; }

    public override bool RequiresCycling => true;


    public ModelBinding(BindGroupLayout layout)
    {
        Layout = layout;
        for (var i = 0; i < NumBindGroups; i++)
        {
            var dataView = _dataBuffer[i];

            var bg = BindGroups[i];
            bg.BeginUpdate();
            bg.CbvWithDesc(new BindBufferDesc
            {
                Binding = 0,
                Resource = dataView.Buffer,
                ResourceOffset = dataView.Offset
            });
            bg.EndUpdate();
        }
    }

    protected override void OnUpdate(RenderObject target)
    {
        var model = Matrix4x4.CreateTranslation(target.Position.X, target.Position.Y, 1);
        _dataBuffer.Write(model);
    }
}