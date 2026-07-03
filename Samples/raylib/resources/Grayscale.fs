#version 330

// Grayscale post-process for a render-target container (issue #3465). Bound by the raylib renderer
// for the single composite blit of a container whose SourceShaderFile points here, so it recolors
// the whole baked container. Uses raylib's default-shader interface (texture0 / colDiffuse /
// fragColor / fragTexCoord).

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 finalColor;

uniform sampler2D texture0;
uniform vec4 colDiffuse;

void main()
{
    vec4 texel = texture(texture0, fragTexCoord);
    float gray = dot(texel.rgb, vec3(0.299, 0.587, 0.114));
    finalColor = vec4(gray, gray, gray, texel.a) * fragColor * colDiffuse;
}
