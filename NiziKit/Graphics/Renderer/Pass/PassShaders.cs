namespace NiziKit.Graphics.Renderer.Pass;

public class PassShaders
{
    public const string FullScreenQuad =
        """
        struct VSOutput
        {
            float4 Position : SV_POSITION;
            float2 TexCoord : TEXCOORD0;
        };

        VSOutput VSMain(uint vertexId : SV_VertexID)
        {
            VSOutput output;
            output.TexCoord = float2((vertexId << 1) & 2, vertexId & 2);
            output.Position = float4(output.TexCoord * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);
            return output;
        }
        """;

    public const string CopyTexture =
        """
        Texture2D<float4> SourceTexture : register(t0);
        SamplerState LinearSampler : register(s0);
        
        struct PSInput
        {
            float4 Position : SV_POSITION;
            float2 TexCoord : TEXCOORD0;
        };
        
        float4 PSMain(PSInput input) : SV_TARGET
        {
            return SourceTexture.Sample(LinearSampler, input.TexCoord);
        }
        """;
}