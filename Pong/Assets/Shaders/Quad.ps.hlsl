struct PSInput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};

Texture2D albedo : register(t0, space1);

cbuffer colorBuffer : register(b1, space1)
{
    float4 color;
}

SamplerState samplerState : register(s1, space1);

float4 PSMain(PSInput input) : SV_TARGET
{
    if (color.a > 0.0f)
    {
        return color;
    }
    return albedo.Sample(samplerState, input.texCoord);
}
