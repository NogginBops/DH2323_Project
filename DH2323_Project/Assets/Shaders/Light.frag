#version 460 core

in vec3 worldPosition;
in vec2 uv;

out vec4 out_color;

uniform vec4 ColorAndIntensity = vec4(1, 1, 1, 1);

void main()
{
    out_color = vec4(ColorAndIntensity.rgb * ColorAndIntensity.w, 1);
}