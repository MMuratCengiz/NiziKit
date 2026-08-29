using DenOfIz;
using NiziKit.Graphics;

namespace Pong;

public class QuadPipeline
{
    private ShaderProgram _shaderProgram;
    private Pipeline _instance;

    public QuadPipeline()
    {
        var vsDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Vertex,
            Path = StringView.Create("Shaders/Quad.vert.hlsl"),
            EntryPoint = StringView.Create("VSMain")
        };

        var psDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Pixel,
            Path = StringView.Create("Shaders/Quad.ps.hlsl"),
            EntryPoint = StringView.Create("VSMain")
        };

        _shaderProgram = new ShaderProgram(new ShaderProgramDesc()
        {
            ShaderStages = ShaderStageDescArray.Create([vsDesc, psDesc])
        });
        
        _instance = GraphicsContext.Device.CreatePipeline(
            new PipelineDesc
            {
                BindPoint = BindPoint.Graphics,
                Graphics = new GraphicsPipelineDesc()
                {
                    
                },
                ShaderProgram = _shaderProgram,
            });
    }
}