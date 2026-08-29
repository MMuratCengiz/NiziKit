struct PSInput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};

cbuffer Model : register(b0, space2)
{
    float4x4 model;
}

cbuffer ViewProjection : register(b1, space0)
{
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
