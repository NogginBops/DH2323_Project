#version 460 core

layout(location = 0) in vec3 in_position;
layout(location = 1) in vec3 in_normal;
layout(location = 2) in vec2 in_uv;

out vec3 worldPosition;
out vec3 worldNormal;
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

    uv = in_uv;
}
