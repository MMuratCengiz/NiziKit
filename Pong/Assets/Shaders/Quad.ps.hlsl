struct PSInput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};

Texture2D albedo : register(t0, space1);
SamplerState samplerState : register(s1, space1);

float4 PSMain(PSInput input) : SV_TARGET
{
    return albedo.Sample(samplerState, input.texCoord);
}
