#version 460 core

layout(location = 0) in vec3 in_position;
layout(location = 1) in vec3 in_normal;
layout(location = 2) in vec4 in_tangent;
layout(location = 3) in vec2 in_uv;

out vec3 worldPosition;
out vec3 worldNormal;
out vec3 worldTangent;
out vec3 worldBitangent;
out vec2 uv;

uniform mat4 ModelMatrix;
uniform mat4 VP;
uniform mat3 NormalMatrix;

void main()
{
    vec4 world = vec4(in_position, 1) * ModelMatrix;
    worldPosition = world.xyz;
    gl_Position = world * VP;

    worldNormal = in_normal * NormalMatrix;
    worldTangent = in_tangent.xyz * NormalMatrix;
    worldBitangent = cross(worldNormal, worldTangent) * in_tangent.w;

    uv = in_uv;
}
