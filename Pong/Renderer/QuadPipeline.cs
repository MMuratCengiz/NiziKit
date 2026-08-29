using DenOfIz;
using NiziKit.Graphics;

namespace Pong.Renderer;

public class QuadPipeline : IDisposable
{
    private readonly RootSignature _rootSignature;
    private readonly InputLayout _inputLayout;
    private readonly BindGroupLayout _modelBindGroupLayout;
    private readonly BindGroupLayout _viewProjectionBindGroupLayout;
    private readonly BindGroupLayout _albedoBindGroupLayout;
    private readonly ShaderProgram _shaderProgram;
    private readonly Pipeline _instance;

    public BindGroupLayout ModelBindGroupLayout => _modelBindGroupLayout;
    public BindGroupLayout ViewProjectionBindGroupLayout => _viewProjectionBindGroupLayout;
    public BindGroupLayout AlbedoBindGroupLayout => _albedoBindGroupLayout;
    
    public Pipeline Instance => _instance;

    // Register spaces used by Assets/Shaders/Quad.*.hlsl. Keep these as regular
    // descriptor-table spaces: DenOfIz's root-level buffer space (30) is not
    // bound at the correct root parameter index on DX12.
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
        _viewProjectionBindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(LayoutForSpace(bindGroupLayoutDescs, ViewProjectionSpace));
        _albedoBindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(LayoutForSpace(bindGroupLayoutDescs, AlbedoSpace));
        _modelBindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(LayoutForSpace(bindGroupLayoutDescs, ModelSpace));
        
        _rootSignature = GraphicsContext.Device.CreateRootSignature(new RootSignatureDesc
        {
            BindGroupLayouts =
                BindGroupLayoutArray.Create([_viewProjectionBindGroupLayout, _albedoBindGroupLayout, _modelBindGroupLayout,]),
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
        _instance = GraphicsContext.Device.CreatePipeline(
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
        _instance.Dispose();
        _inputLayout.Dispose();
        _rootSignature.Dispose();
        _modelBindGroupLayout.Dispose();
        _viewProjectionBindGroupLayout.Dispose();
        _albedoBindGroupLayout.Dispose();
        _shaderProgram.Dispose();
    }
}