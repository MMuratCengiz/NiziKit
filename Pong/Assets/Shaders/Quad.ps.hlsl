struct PSInput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};

Texture2D albedo : register(t0, space0);
SamplerState samplerState : register(s1, space0);

float4 PSMain(PSInput input) : SV_TARGET
{
    return albedo.Sample(samplerState, input.texCoord);
}
