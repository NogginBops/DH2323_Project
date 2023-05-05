#version 460 core

in vec3 worldPosition;
in vec3 worldNormal;
in vec2 uv;

out vec4 out_color;

layout(location = 0) uniform vec3 cameraPosition;
layout(location = 1) uniform float metallic;
layout(location = 2) uniform float reflectance;
layout(location = 3) uniform float roughness;


layout(binding = 0) uniform sampler2D albedo;

struct PointLight
{
    vec4 PositionAndInvRadius;
    vec4 Color;
};

struct PointLightData
{
    vec3 lightDirection;
    float attenuation;
};

struct Surface
{
    vec3 f0;
    vec3 diffuseColor;

    vec3 viewDirection;
    vec3 normal;
    
    float roughness;
    float NoV;
};

layout(std140, row_major, binding = 0) buffer ssbo_PointLights
{
    PointLight PointLights[];
};

const float PI = 3.141592653589793238;
const float OneOverPI = 1.0 / 3.141592653589793238;

float D_GGX(float NoH, float roughness)
{
    float a2 = roughness * roughness;
    float f = (NoH * a2 - NoH) * NoH + 1.0;
    return a2 / (PI * f * f);
}

float D_GGX2(float NoH, const vec3 n, const vec3 h, float roughness)
{
    vec3 NxH = cross(n, h);
    float a = NoH * roughness;
    float k = roughness / (dot(NxH, NxH) + a * a);
    return k * k * OneOverPI;
}

float V_SmithGGXCorrelated(float NoV, float NoL, float roughness)
{
    float a2 = roughness * roughness;
    float GGXV = NoL * sqrt(NoV * NoV * (1.0 - a2) + a2);
    float GGXL = NoV * sqrt(NoL * NoL * (1.0 - a2) + a2);
    return 0.5 / (GGXV + GGXL);
}

float V_SmithGGXCorrelatedFast(float NoV, float NoL, float roughness)
{
    float a = roughness;
    float GGXV = NoL * (NoV * (1.0 - a) + a);
    float GGXL = NoV * (NoL * (1.0 - a) + a);
    return 0.5 / (GGXV + GGXL);
}

vec3 F_Schlick(vec3 f0, float f90, float VoH) 
{
    return f0 + (vec3(f90) - f0) * pow(1.0 - VoH, 5.0);
}

vec3 F_Schlick(vec3 f0, float VoH)
{
    return f0 * (1.0 - f0) + pow(1.0 - VoH, 5.0);
}

float F_Schlick(float f0, float f90, float VoH)
{
    return f0 + (f90 - f0) * pow(1.0 - VoH, 5.0);
}

float Fd_Burley(float NoV, float NoL, float LoH, float roughness)
{
    float f90 = 0.5 + 2.0 * roughness * LoH * LoH;
    float lightScatter = F_Schlick(NoL, 1.0, f90);
    float viewScatter = F_Schlick(NoV, 1.0, f90);
    return lightScatter * viewScatter * OneOverPI;
}

vec3 evalSurface(const Surface surface, const PointLight light, const PointLightData data)
{
    vec3 h = normalize(surface.viewDirection + data.lightDirection);

    float NoV = surface.NoV;
    float NoL = clamp(dot(surface.normal, data.lightDirection), 0.0, 1.0);
    float NoH = clamp(dot(surface.normal, h), 0.0, 1.0);
    float LoH = clamp(dot(data.lightDirection, h), 0.0, 1.0);

    float D = D_GGX(NoH, surface.roughness);
    float V = V_SmithGGXCorrelated(NoV, NoH, surface.roughness);
    float f90 = clamp(dot(surface.f0, vec3(50.0 * 0.33)), 0.0, 1.0);
    vec3 F = F_Schlick(surface.f0, f90, LoH);
    vec3 Fr = D * V * F;

    vec3 Fd = surface.diffuseColor * Fd_Burley(NoV, NoL, NoH, surface.roughness);

    // FIXME: Correction?
    vec3 color = Fr + Fd;

    // FIXME: attenuation!
    return (color * light.Color.rgb) * data.attenuation;
}

void main()
{
    vec4 albedo = texture(albedo, uv);
    if (albedo.a < 0.5) discard;
    
    // FIXME: Two sided?
    vec3 normal = normalize(worldNormal);
    
    Surface surface;
    surface.viewDirection = normalize(cameraPosition - worldPosition);
    surface.normal = normal;
    surface.roughness = roughness * roughness;
    surface.NoV = clamp(dot(surface.normal, surface.viewDirection), 0.0, 1.0);
    surface.diffuseColor = (1.0 - metallic) * albedo.rgb;
    surface.f0 = 0.16 * reflectance * reflectance * (1.0 - metallic) + albedo.rgb * metallic;

    vec3 color = vec3(0);
    for (int i = 0; i < PointLights.length(); i++)
    {
        PointLight light = PointLights[i];
        PointLightData data;

        vec3 l = light.PositionAndInvRadius.xyz - worldPosition;
        float distanceSquare = dot(l, l);
        float factor = distanceSquare * light.PositionAndInvRadius.w * light.PositionAndInvRadius.w;
        float smoothFactor = max(1.0 - factor * factor, 0.0);
        l = normalize(l);

        data.lightDirection = l;
        data.attenuation = (smoothFactor * smoothFactor) / max(distanceSquare, 1e-4);
        
        color += evalSurface(surface, light, data);
    }

    out_color = vec4(color, 1);
}