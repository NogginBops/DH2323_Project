using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DH2323_Project
{
    internal class Texture
    {
        public string Name;
        public int GLTexture;

        public bool HasMips;
        public bool IsSRGB;
        public float AnisoLevel;

        public static int LoadTexture(string path, bool generateMipmap, bool srgb, float anisoLevel)
        {
            StbImage.stbi_set_flip_vertically_on_load(1);

            ImageResult result = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);

            GL.CreateTextures(TextureTarget.Texture2D, 1, out int texture);

            int mipmapLevels = generateMipmap ?
               MathF.ILogB(Math.Max(result.Width, result.Height)) :
               1;

            SizedInternalFormat internalFormat = srgb ? SizedInternalFormat.Srgb8Alpha8 : SizedInternalFormat.Rgba8;

            GL.TextureStorage2D(texture, mipmapLevels, internalFormat, result.Width, result.Height);

            GL.TextureSubImage2D(texture, 0, 0, 0, result.Width, result.Height, PixelFormat.Rgba, PixelType.UnsignedByte, result.Data);

            GL.TextureParameter(texture, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TextureParameter(texture, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            if (generateMipmap)
            {
                GL.GenerateTextureMipmap(texture);

                GL.TextureParameter(texture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TextureParameter(texture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            }
            else
            {
                GL.TextureParameter(texture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TextureParameter(texture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            }
            
            GL.TextureParameter(texture, TextureParameterName.TextureMaxAnisotropy, anisoLevel);

            return texture;
        }

        public Texture(string name, string path, bool generateMips, bool srgb, float anisoLevel)
        {
            Name = name;

            HasMips = generateMips;
            IsSRGB = srgb;
            AnisoLevel = anisoLevel;

            GLTexture = LoadTexture(path, generateMips, srgb, anisoLevel);
        }
    }
}
