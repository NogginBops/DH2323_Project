#version 460 core

in vec3 worldPosition;
in vec3 worldNormal;
in vec2 uv;

out vec4 out_color;

layout(binding = 0) uniform sampler2D albedo;

struct PointLight
{
    vec4 Position;
    vec4 Color;
};

layout(std140, row_major, binding = 0) buffer ssbo_PointLights
{
    PointLight PointLights[];
};

void main()
{
    vec4 albedo = texture(albedo, uv);
    if (albedo.a < 0.5) discard;
    
    // FIXME: Two sided?
    vec3 normal = normalize(worldNormal);
    
    vec3 color = vec3(0);
    for (int i = 0; i < PointLights.length(); i++)
    {
        PointLight light = PointLights[i];

        vec3 l = light.Position.xyz - worldPosition;
        float distance = length(l);
        l = normalize(l);
        
        float attenuation = 1.0 / (distance * distance);

        float diff = max(dot(normal, l), 0.0);

        color += diff * attenuation * light.Color.rgb * albedo.rgb;
    }

    out_color = vec4(color, 1);
}