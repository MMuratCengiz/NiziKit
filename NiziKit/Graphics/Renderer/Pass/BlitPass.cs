using System.Text;
using DenOfIz;
using NiziKit.Graphics.Resources;

namespace NiziKit.Graphics.Renderer.Pass;

public class BlitPass : IDisposable
{
    private static readonly Format[] CommonFormats = [Format.B8G8R8A8Unorm, Format.R8G8B8A8Unorm];

    private readonly ShaderProgram _program;
    private readonly RootSignature _rootSignature;
    private readonly InputLayout _inputLayout;
    private readonly BindGroupLayout _bindGroupLayout;
    private readonly Sampler _linearSampler;
    private readonly Dictionary<Format, Pipeline> _pipelines = new();
    private readonly Lock _pipelineLock = new();
    private readonly BindGroup[] _bindGroups;
    private readonly Texture?[] _boundTextures;

    public BlitPass()
    {
        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = ShaderStageDescArray.Create([
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("VSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(PassShaders.FullScreenQuad)),
                    Stage = (uint)ShaderStageFlagBits.Vertex
                },
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("PSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(PassShaders.CopyTexture)),
                    Stage = (uint)ShaderStageFlagBits.Pixel
                }
            ])
        };
        _program = new ShaderProgram(programDesc);

        var reflection = _program.Reflect();
        var bindGroupLayoutDescs = reflection.BindGroupLayouts.ToArray();
        _bindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(bindGroupLayoutDescs[0]);

        var rootSigDesc = new RootSignatureDesc
        {
            BindGroupLayouts = BindGroupLayoutArray.Create([_bindGroupLayout]),
        };
        _rootSignature = GraphicsContext.Device.CreateRootSignature(rootSigDesc);
        _inputLayout = GraphicsContext.Device.CreateInputLayout(reflection.InputLayout);

        _linearSampler = GraphicsContext.Device.CreateSampler(new SamplerDesc
        {
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            MipmapMode = MipmapMode.Nearest
        });

        var numFrames = (int)GraphicsContext.NumFrames;
        _bindGroups = new BindGroup[numFrames];
        _boundTextures = new Texture?[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            _bindGroups[i] = GraphicsContext.Device.CreateBindGroup(new BindGroupDesc
            {
                Layout = _bindGroupLayout
            });
        }

        Parallel.ForEach(CommonFormats, format => GetOrCreatePipeline(format));
    }

    public void Execute(GraphicsPass pass, Texture source, CycledTexture dest)
    {
        var frameIndex = GraphicsContext.FrameIndex;
        UpdateBindGroup(frameIndex, source);

        pass.SetRenderTarget(0, dest, LoadOp.Load);
        pass.Begin();
        pass.TransitionTexture(source, (uint)ResourceUsageFlagBits.ShaderResource);
        pass.BeginRendering();
        pass.BindPipeline(GetOrCreatePipeline(dest.Format));
        pass.Bind(_bindGroups[frameIndex]);
        pass.Draw(3);
        pass.End();
    }

    private Pipeline GetOrCreatePipeline(Format format)
    {
        lock (_pipelineLock)
        {
            if (_pipelines.TryGetValue(format, out var existing))
            {
                return existing;
            }

            var blendDesc = new BlendDesc
            {
                Enable = true,
                SrcBlend = Blend.One,
                DstBlend = Blend.InvSrcAlpha,
                BlendOp = BlendOp.Add,
                SrcBlendAlpha = Blend.One,
                DstBlendAlpha = Blend.InvSrcAlpha,
                BlendOpAlpha = BlendOp.Add,
                RenderTargetWriteMask = 0x0F
            };

            var renderTarget = new RenderTargetDesc
            {
                Format = format,
                Blend = blendDesc
            };

            using var renderTargets = RenderTargetDescArray.Create([renderTarget]);

            var pipelineDesc = new PipelineDesc
            {
                RootSignature = _rootSignature,
                InputLayout = _inputLayout,
                ShaderProgram = _program,
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
                    RenderTargets = renderTargets
                }
            };

            var pipeline = GraphicsContext.Device.CreatePipeline(pipelineDesc);
            _pipelines[format] = pipeline;
            return pipeline;
        }
    }

    private void UpdateBindGroup(int frameIndex, Texture sourceTexture)
    {
        if (_boundTextures[frameIndex] == sourceTexture)
        {
            return;
        }

        var bindGroup = _bindGroups[frameIndex];
        bindGroup.BeginUpdate();
        bindGroup.SrvTexture(0, sourceTexture);
        bindGroup.Sampler(0, _linearSampler);
        bindGroup.EndUpdate();

        _boundTextures[frameIndex] = sourceTexture;
    }

    public void Dispose()
    {
        foreach (var bindGroup in _bindGroups)
        {
            bindGroup.Dispose();
        }

        foreach (var pipeline in _pipelines.Values)
        {
            pipeline.Dispose();
        }

        _pipelines.Clear();

        _linearSampler.Dispose();
        _inputLayout.Dispose();
        _rootSignature.Dispose();
        _bindGroupLayout.Dispose();
        _program.Dispose();
    }
}
