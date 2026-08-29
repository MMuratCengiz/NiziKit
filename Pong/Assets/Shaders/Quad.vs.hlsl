struct PSInput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};

cbuffer MVP : register(b0, space30)
{
    float4x4 model;
    float4x4 viewProjection;
}

PSInput VSMain(float2 position : POSITION0, float2 texCoord: TEXCOORD)
{
    PSInput result;
    
    float4x4 mvp = mul(model, viewProjection);

    result.position = mul(float4(position, 0.0f, 1.0f), mvp);;
    result.texCoord = texCoord;
    
    return result;
}
