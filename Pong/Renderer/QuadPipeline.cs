using DenOfIz;
using NiziKit.Graphics;

namespace Pong.Renderer;

public class QuadPipeline : IDisposable
{
    private readonly RootSignature _rootSignature;
    private readonly InputLayout _inputLayout;
    private readonly ShaderProgram _shaderProgram;

    public BindGroupLayout DrawBindGroupLayout { get; }

    public BindGroupLayout ViewProjectionBindGroupLayout { get; }

    public BindGroupLayout AlbedoBindGroupLayout { get; }

    public Pipeline Instance { get; }

    private const uint ViewProjectionSpace = 0;
    private const uint AlbedoSpace = 1;
    private const uint ModelSpace = 2;

    private static BindGroupLayoutDesc LayoutForSpace(BindGroupLayoutDesc[] descs, uint registerSpace)
    {
        foreach (var desc in descs)
        {
            if (desc.RegisterSpace == registerSpace)
            {
                return desc;
            }
        }

        throw new InvalidOperationException($"Shader reflection has no bind group for register space {registerSpace}");
    }

    public QuadPipeline()
    {
        var vsDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Vertex,
            Path = StringView.Create("Assets/Shaders/Quad.vs.hlsl"),
            EntryPoint = StringView.Create("VSMain")
        };

        var psDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Pixel,
            Path = StringView.Create("Assets/Shaders/Quad.ps.hlsl"),
            EntryPoint = StringView.Create("PSMain")
        };

        _shaderProgram = new ShaderProgram(new ShaderProgramDesc()
        {
            ShaderStages = ShaderStageDescArray.Create([vsDesc, psDesc])
        });

        var reflection = _shaderProgram.Reflect();
        _inputLayout = GraphicsContext.Device.CreateInputLayout(reflection.InputLayout);
        var bindGroupLayoutDescs = reflection.BindGroupLayouts.ToArray();
        ViewProjectionBindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(LayoutForSpace(bindGroupLayoutDescs, ViewProjectionSpace));
        AlbedoBindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(LayoutForSpace(bindGroupLayoutDescs, AlbedoSpace));
        DrawBindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(LayoutForSpace(bindGroupLayoutDescs, ModelSpace));
        
        _rootSignature = GraphicsContext.Device.CreateRootSignature(new RootSignatureDesc
        {
            BindGroupLayouts =
                BindGroupLayoutArray.Create([ViewProjectionBindGroupLayout, AlbedoBindGroupLayout, DrawBindGroupLayout,]),
        });
        var blendDesc = new BlendDesc
        {
            Enable = false,
            RenderTargetWriteMask = 0x0F
        };

        var renderTarget = new RenderTargetDesc
        {
            Format = GraphicsContext.BackBufferFormat,
            Blend = blendDesc
        };

        using var renderTargets = RenderTargetDescArray.Create([renderTarget]);
        Instance = GraphicsContext.Device.CreatePipeline(
            new PipelineDesc
            {
                InputLayout = _inputLayout,
                RootSignature = _rootSignature,
                BindPoint = BindPoint.Graphics,
                Graphics = new GraphicsPipelineDesc
                {
                    PrimitiveTopology = PrimitiveTopology.Triangle,
                    CullMode = CullMode.None,
                    FillMode = FillMode.Solid,
                    DepthTest = new DepthTest
                    {
                        Enable = false,
                        CompareOp = CompareOp.Always,
                        Write = false
                    },
                    RenderTargets = renderTargets,
                    DepthStencilAttachmentFormat = GraphicsContext.DepthBufferFormat
                },
                ShaderProgram = _shaderProgram,
            });
    }

    public void Dispose()
    {
        Instance.Dispose();
        _inputLayout.Dispose();
        _rootSignature.Dispose();
        DrawBindGroupLayout.Dispose();
        ViewProjectionBindGroupLayout.Dispose();
        AlbedoBindGroupLayout.Dispose();
        _shaderProgram.Dispose();
    }
}