using DenOfIz;

namespace NiziKit.Graphics.Binding;

public abstract class ShaderBinding<TTarget> : IShaderBinding
{
    public Type TargetType => typeof(TTarget);
    public BindGroupLayout Layout { get; }
    public bool RequiresCycling { get; }

    protected int NumBindGroups => BindGroups.Length;
    protected BindGroup[] BindGroups { get; }

    public BindGroup BindGroup => BindGroups[RequiresCycling ? GraphicsContext.FrameIndex : 0];

    protected ShaderBinding(BindGroupLayout layout, bool cycled = false)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        RequiresCycling = cycled;
        BindGroups = new BindGroup[cycled ? (int)GraphicsContext.NumFrames : 1];

        var bindGroupDesc = new BindGroupDesc
        {
            Layout = Layout
        };

        for (var i = 0; i < NumBindGroups; i++)
        {
            BindGroups[i] = GraphicsContext.Device.CreateBindGroup(bindGroupDesc);
        }
    }

    public void Update(object target)
    {
        OnUpdate((TTarget)target);
    }

    protected abstract void OnUpdate(TTarget target);

    public virtual void Dispose()
    {
        foreach (var bindGroup in BindGroups)
        {
            bindGroup.Dispose();
        }
    }
}
