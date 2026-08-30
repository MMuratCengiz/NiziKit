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

    public const string BlitPsShader =
        """
        Texture2D<float4> Tex1 : register(t0);
        Texture2D<float4> Tex2 : register(t1);
        SamplerState LinearSampler : register(s0);

        struct PSInput
        {
            float4 Position : SV_POSITION;
            float2 TexCoord : TEXCOORD0;
        };

        float4 PSMain(PSInput input) : SV_TARGET
        {
            float4 tex1 = Tex1.Sample(LinearSampler, input.TexCoord);
            float4 tex2 = Tex2.Sample(LinearSampler, input.TexCoord);
            return lerp(tex1, tex2, tex2.a);
        }
        """;

    public const string PresentShader =
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