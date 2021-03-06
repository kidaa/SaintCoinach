#include "Structures.fxh"

Texture2D<float4> g_Normal      : register(t1);
Texture2D<float4> g_Mask        : register(t2);

sampler g_NormalSampler         : register(s1)
{
    AddressU = Wrap;
    AddressV = Wrap;
    Filter = MIN_MAG_MIP_LINEAR;
};
sampler g_MaskSampler           : register(s2)
{
    AddressU = Wrap;
    AddressV = Wrap;
    Filter = MIN_MAG_MIP_LINEAR;
};

#include "Common.fxh"
#include "Lighting.fxh"

cbuffer g_CustomizeParameters : register(b2)
{
    float3 m_LeftColor      : packoffset(c0);
    float3 m_RightColor     : packoffset(c1);
};
/*
float4 ComputeCommon(VSOutput pin, float4 diffuse, float4 specular)
{
float4 texNormal = g_Normal.Sample(g_NormalSampler, pin.UV);

float3 normal = CalculateNormal(pin.WorldNormal, pin.WorldTangent, pin.WorldBinormal, texNormal.xyz);

float3 eyeVector = normalize(m_EyePosition - pin.PositionWS);
LightResult light = ComputeLights(eyeVector, normal, 3);

float4 color = float4(diffuse.rgb, texNormal.a);

color.rgb *= light.Diffuse.rgb;
color.rgb += light.Specular.rgb * specular.rgb * color.a;

return color;
};*/

float4 ComputeCommon(VSOutput pin, float4 diffuse, float4 specular)
{
    float4 texNormal = g_Normal.Sample(g_NormalSampler, pin.UV.xy);
    float a = texNormal.a;
    clip(a <= 0.5 ? -1 : 1);
    float3 bump = (texNormal.xyz - 0.5) * 2.0;

    float3 binorm = cross(pin.NormalWS.xyz, pin.Tangent1WS.xyz);
    float3 bumpNormal = (bump.x * pin.Tangent1WS) + (bump.y * binorm) + (bump.z * pin.NormalWS);
    bumpNormal = normalize(bumpNormal);

    float3 eyeVector = normalize(m_EyePosition - pin.PositionWS);
    Lighting light = GetLight(m_EyePosition, eyeVector, bumpNormal);

    float4 color = float4(diffuse.rgb, a);

    color.rgb *= light.Diffuse.rgb;
    color.rgb += light.Specular.rgb * specular.rgb * color.a;

    return color;
};

float4 PSIris(VSOutput pin) : SV_Target0
{
    float4 texMask = g_Mask.Sample(g_MaskSampler, pin.UV.xy);
    float4 texNormal = g_Normal.Sample(g_NormalSampler, pin.UV.xy);

    float4 diffuse = float4(0, 0, 0, 1);
    diffuse.rgb = texMask.x * (pin.Color.x * m_LeftColor.rgb + pin.Color.y * m_RightColor.rgb);
    float4 specular = (0.75).xxxx;

    return ComputeCommon(pin, diffuse, specular);
}

technique11 Iris
{
    pass P0 {
        SetGeometryShader(0);
        SetVertexShader(CompileShader(vs_4_0, VSCommon()));
        SetPixelShader(CompileShader(ps_4_0, PSIris()));
    }
}