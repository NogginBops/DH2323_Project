#version 460 core

in vec3 worldPosition;
in vec3 worldNormal;
in vec3 worldTangent;
in vec3 worldBitangent;
in vec2 uv;

out vec4 out_color;

layout(location = 0) uniform vec3 cameraPosition;
layout(location = 1) uniform float metallic;
layout(location = 2) uniform float reflectance;
layout(location = 3) uniform float roughness;


layout(binding = 0) uniform sampler2D albedo;
layout(binding = 1) uniform sampler2D normalTex;
layout(binding = 1) uniform sampler2D roughnessTex;

// Linearly Transformed Cosine LUTs
layout(binding = 10) uniform sampler2D LTCMat;
layout(binding = 11) uniform sampler2D LTCAmp;

struct PointLight
{
    vec4 PositionAndInvRadius;
    vec4 Color;
};

struct AreaLight 
{
    vec4 P[4];
    vec4 ColorAndIntensity;
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

    vec3 position;

    vec3 viewDirection;

    vec3 normal;

    float roughness;
    float NoV;
};

layout(std140, row_major, binding = 0) buffer ssbo_PointLights
{
    PointLight PointLights[];
};

layout(std140, row_major, binding = 1) buffer ssbo_AreaLights
{
    AreaLight AreaLights[];
};

const float PI = 3.141592653589793238;
const float OneOverPI = 1.0 / 3.141592653589793238;

vec2 LTC_Coords(float cosTheta, float roughness)
{
    float theta = acos(cosTheta);
    vec2 coords = vec2(roughness, theta * (2 / PI));

    // FIXME: Figure out what the biasing and stuff was about??
    
    const float LUT_SIZE = 32.0;
    // scale and bias coordinates, for correct filtered lookup
    coords = coords*(LUT_SIZE - 1.0)/LUT_SIZE + 0.5/LUT_SIZE;
    
    return coords;
}

mat3 LTC_Matrix(sampler2D LTCMat, vec2 coord)
{
    vec4 t = texture(LTCMat, coord);
    mat3 Minv = mat3(
        vec3(1, 0, t.y),
        vec3(0, t.z, 0),
        vec3(t.w, 0, t.x)
    );

    return Minv;
}

// This code is taken from the LTC demo project by Eric Heitz
void ClipQuadToHorizon(inout vec3 L[5], out int n)
{
    // detect clipping config
    int config = 0;
    if (L[0].z > 0.0) config += 1;
    if (L[1].z > 0.0) config += 2;
    if (L[2].z > 0.0) config += 4;
    if (L[3].z > 0.0) config += 8;

    // clip
    n = 0;

    if (config == 0)
    {
        // clip all
    }
    else if (config == 1) // V1 clip V2 V3 V4
    {
        n = 3;
        L[1] = -L[1].z * L[0] + L[0].z * L[1];
        L[2] = -L[3].z * L[0] + L[0].z * L[3];
    }
    else if (config == 2) // V2 clip V1 V3 V4
    {
        n = 3;
        L[0] = -L[0].z * L[1] + L[1].z * L[0];
        L[2] = -L[2].z * L[1] + L[1].z * L[2];
    }
    else if (config == 3) // V1 V2 clip V3 V4
    {
        n = 4;
        L[2] = -L[2].z * L[1] + L[1].z * L[2];
        L[3] = -L[3].z * L[0] + L[0].z * L[3];
    }
    else if (config == 4) // V3 clip V1 V2 V4
    {
        n = 3;
        L[0] = -L[3].z * L[2] + L[2].z * L[3];
        L[1] = -L[1].z * L[2] + L[2].z * L[1];
    }
    else if (config == 5) // V1 V3 clip V2 V4) impossible
    {
        n = 0;
    }
    else if (config == 6) // V2 V3 clip V1 V4
    {
        n = 4;
        L[0] = -L[0].z * L[1] + L[1].z * L[0];
        L[3] = -L[3].z * L[2] + L[2].z * L[3];
    }
    else if (config == 7) // V1 V2 V3 clip V4
    {
        n = 5;
        L[4] = -L[3].z * L[0] + L[0].z * L[3];
        L[3] = -L[3].z * L[2] + L[2].z * L[3];
    }
    else if (config == 8) // V4 clip V1 V2 V3
    {
        n = 3;
        L[0] = -L[0].z * L[3] + L[3].z * L[0];
        L[1] = -L[2].z * L[3] + L[3].z * L[2];
        L[2] =  L[3];
    }
    else if (config == 9) // V1 V4 clip V2 V3
    {
        n = 4;
        L[1] = -L[1].z * L[0] + L[0].z * L[1];
        L[2] = -L[2].z * L[3] + L[3].z * L[2];
    }
    else if (config == 10) // V2 V4 clip V1 V3) impossible
    {
        n = 0;
    }
    else if (config == 11) // V1 V2 V4 clip V3
    {
        n = 5;
        L[4] = L[3];
        L[3] = -L[2].z * L[3] + L[3].z * L[2];
        L[2] = -L[2].z * L[1] + L[1].z * L[2];
    }
    else if (config == 12) // V3 V4 clip V1 V2
    {
        n = 4;
        L[1] = -L[1].z * L[2] + L[2].z * L[1];
        L[0] = -L[0].z * L[3] + L[3].z * L[0];
    }
    else if (config == 13) // V1 V3 V4 clip V2
    {
        n = 5;
        L[4] = L[3];
        L[3] = L[2];
        L[2] = -L[1].z * L[2] + L[2].z * L[1];
        L[1] = -L[1].z * L[0] + L[0].z * L[1];
    }
    else if (config == 14) // V2 V3 V4 clip V1
    {
        n = 5;
        L[4] = -L[0].z * L[3] + L[3].z * L[0];
        L[0] = -L[0].z * L[1] + L[1].z * L[0];
    }
    else if (config == 15) // V1 V2 V3 V4
    {
        n = 4;
    }
    
    if (n == 3)
        L[3] = L[0];
    if (n == 4)
        L[4] = L[0];
}

float IntegrateEdge(vec3 v1, vec3 v2)
{
    float cosTheta = dot(v1, v2);
    cosTheta = clamp(cosTheta, -0.9999, 0.9999);

    float theta = acos(cosTheta);    
    float res = cross(v1, v2).z * theta / sin(theta);

    return res;
}

mat3 mat3_from_rows(vec3 r0, vec3 r1, vec3 r2)
{
    return mat3(r0.x, r1.x, r2.x,
                r0.y, r1.y, r2.y,
                r0.z, r1.z, r2.z);
}

vec3 LTC_Evaluate(const Surface surface, mat3 Minv, vec4 points[4])
{
    vec3 V = surface.viewDirection;
    vec3 N = surface.normal;

    vec3 T1, T2;
    T1 = normalize(V - N * dot(V, N));
    T2 = cross(N, T1);

    Minv = Minv * mat3_from_rows(T1, T2, N);

    vec3 L[5];
    L[0] = Minv * (points[0].xyz - surface.position);
    L[1] = Minv * (points[1].xyz - surface.position);
    L[2] = Minv * (points[2].xyz - surface.position);
    L[3] = Minv * (points[3].xyz - surface.position);
    L[4] = L[3];

    int n;
    ClipQuadToHorizon(L, n);

    if (n == 0)
        return vec3(0, 0, 0);

    L[0] = normalize(L[0]);
    L[1] = normalize(L[1]);
    L[2] = normalize(L[2]);
    L[3] = normalize(L[3]);
    L[4] = normalize(L[4]);

    float sum = 0.0;

    sum += IntegrateEdge(L[0], L[1]);
    sum += IntegrateEdge(L[1], L[2]);
    sum += IntegrateEdge(L[2], L[3]);
    if (n >= 4)
        sum += IntegrateEdge(L[3], L[4]);
    if (n == 5)
        sum += IntegrateEdge(L[4], L[0]);

    const bool twoSided = false;
    sum = twoSided ? abs(sum) : max(0.0, -sum);

    return vec3(sum, sum, sum);

    // FIXME: Here we could do textured lights.
    //vec3 Lo_i = vec3(sum, sum, sum);
}

// See: https://google.github.io/filament/Filament.html#materialsystem/parameterization
// https://github.com/google/filament/blob/main/shaders/src/shading_model_standard.fs
// https://github.com/google/filament/blob/main/shaders/src/brdf.fs

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

float D_GGXFixed(float NoH, float roughness)
{
    float oneMinusNoHSquared = 1.0 - NoH * NoH;
    float a = NoH * roughness;
    float k = roughness / (oneMinusNoHSquared + a * a);
    float d = k * k * (1.0 / PI);
    return clamp(d, 0, 1);
}

float V_SmithGGXCorrelated(float NoV, float NoL, float roughness)
{
    float a2 = roughness * roughness;
    float GGXV = NoL * sqrt(NoV * NoV * (1.0 - a2) + a2);
    float GGXL = NoV * sqrt(NoL * NoL * (1.0 - a2) + a2);
    return clamp(0.5 / (GGXV + GGXL), 0, 1);
}

float V_SmithGGXCorrelatedFast(float NoV, float NoL, float roughness)
{
    float a = roughness;
    float GGXV = NoL * (NoV * (1.0 - a) + a);
    float GGXL = NoV * (NoL * (1.0 - a) + a);
    return clamp(0.5 / (GGXV + GGXL), 0, 1);
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

    float D = D_GGXFixed(NoH, surface.roughness);
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

vec3 evalSurface(const Surface surface, const AreaLight light)
{
    vec2 coords = LTC_Coords(dot(surface.normal, surface.viewDirection), surface.roughness);
    mat3 Minv = LTC_Matrix(LTCMat, coords);

    vec3 lightColor = light.ColorAndIntensity.rgb * light.ColorAndIntensity.w;

    // Evaluate both the diffuse and specular lighting.
    vec3 Lo_d = LTC_Evaluate(surface, mat3(1.0), light.P);
    Lo_d *= surface.diffuseColor;
    Lo_d *= lightColor;
    
    vec3 Lo_f = LTC_Evaluate(surface, Minv, light.P);
    vec2 schlick = texture(LTCAmp, coords).xy;
    Lo_f *= surface.f0 * schlick.x + (1.0 - surface.f0) * schlick.y;
    Lo_f *= lightColor;
    

    return (Lo_f + Lo_d) / 2.0 * PI;
}

void main()
{
    vec4 albedo = texture(albedo, uv);
    if (albedo.a < 0.5) discard;
    
    vec3 mappedNormal = texture(normalTex, uv).rgb;
    // Compensate for DirectX normal maps.
    mappedNormal.y = 1 - mappedNormal.y;
    mappedNormal = mappedNormal * 2 - 1;

    // FIXME: Two sided?
    vec3 normal = normalize(worldNormal);
    mappedNormal = normalize(mappedNormal.x * normalize(worldTangent) + mappedNormal.y * normalize(worldBitangent) + mappedNormal.z * worldNormal);

    Surface surface;
    surface.position = worldPosition;
    surface.viewDirection = normalize(cameraPosition - worldPosition);
    surface.normal = mappedNormal;
    surface.roughness = texture(roughnessTex, uv).r * roughness;
    surface.NoV = abs(dot(surface.normal, surface.viewDirection));
    surface.diffuseColor = (1.0 - metallic) * albedo.rgb;
    surface.f0 = 0.16 * reflectance * reflectance * (1.0 - metallic) + albedo.rgb * metallic;

    vec3 color = vec3(0);
    for (int i = 0; i < 0/*PointLights.length()*/; i++)
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

    for (int i = 0; i < AreaLights.length(); i++)
    {
        AreaLight light = AreaLights[i];

        color += evalSurface(surface, light);
    }

    out_color = vec4(color, 1);
}