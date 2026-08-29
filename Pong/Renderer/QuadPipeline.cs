using DenOfIz;
using NiziKit.Graphics;

namespace Pong.Renderer;

public class QuadPipeline : IDisposable
{
    private readonly RootSignature _rootSignature;
    private readonly InputLayout _inputLayout;
    private readonly BindGroupLayout _mvpBindGroupLayout;
    private readonly BindGroupLayout _albedoBindGroupLayout;
    private readonly BindGroupLayout _samplerBindGroupLayout;
    private readonly ShaderProgram _shaderProgram;
    private readonly Pipeline _instance;

    public BindGroupLayout MVPBindGroupLayout => _mvpBindGroupLayout;
    public BindGroupLayout AlbedoBindGroupLayout => _albedoBindGroupLayout;
    
    public Pipeline Instance => _instance;


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
        _albedoBindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(bindGroupLayoutDescs[0]);
        _samplerBindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(bindGroupLayoutDescs[1]);
        _mvpBindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(bindGroupLayoutDescs[2]);
        
        _rootSignature = GraphicsContext.Device.CreateRootSignature(new RootSignatureDesc
        {
            BindGroupLayouts =
                BindGroupLayoutArray.Create([_albedoBindGroupLayout, _samplerBindGroupLayout, _mvpBindGroupLayout,]),
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
        _mvpBindGroupLayout.Dispose();
        _albedoBindGroupLayout.Dispose();
        _samplerBindGroupLayout.Dispose();
        _shaderProgram.Dispose();
    }
}