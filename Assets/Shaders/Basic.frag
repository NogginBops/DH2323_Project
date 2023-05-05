#version 460 core

in vec3 worldPosition;
in vec3 worldNormal;
in vec2 uv;

out vec4 out_color;

layout(binding = 0) uniform sampler2D albedo;

void main()
{
    vec4 albedo = texture(albedo, uv);
    if (albedo.a < 0.5) discard;

    out_color = vec4(albedo.rgb, 1);
    //out_color = vec4(uv, 0, 1.0);
    //out_color = vec4(abs(normalize(worldNormal)), 1.0);
}