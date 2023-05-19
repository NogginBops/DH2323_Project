using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DH2323_Project
{
    internal class Texture
    {
        public string Name;
        public int GLTexture;

        public bool HasMips;
        public bool IsSRGB;
        public float AnisoLevel;

        public static int LoadTexture(string name, string path, bool generateMipmap, bool srgb, float anisoLevel)
        {
            StbImage.stbi_set_flip_vertically_on_load(1);

            ImageResult result = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);

            GL.CreateTextures(TextureTarget.Texture2D, 1, out int texture);
            GL.ObjectLabel(ObjectLabelIdentifier.Texture, texture, -1, $"Texture: {name} ({Path.GetRelativePath(Directory.GetCurrentDirectory(), path)})");

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

        public static unsafe int LoadDDSTexture(string name, string path, bool generateMipmap, bool srgb, float anisoLevel)
        {
            DDSData result = DDSReader.ReadDDSFile(path);

            GL.CreateTextures(TextureTarget.Texture2D, 1, out int texture);
            GL.ObjectLabel(ObjectLabelIdentifier.Texture, texture, -1, $"Texture: {name} ({Path.GetRelativePath(Directory.GetCurrentDirectory(), path)})");

            int mipmapLevels = generateMipmap ?
               MathF.ILogB(Math.Max(result.Width, result.Height)) :
               1;

            SizedInternalFormat internalFormat;
            PixelFormat pixelFormat;
            PixelType pixelType;
            switch (result.Format)
            {
                case DDSFormat.RGBA32F:
                    internalFormat = SizedInternalFormat.Rgba32f;
                    pixelFormat = PixelFormat.Rgba;
                    pixelType = PixelType.Float;
                    break;
                case DDSFormat.RG32F:
                    internalFormat = SizedInternalFormat.Rg32f;
                    pixelFormat = PixelFormat.Rg;
                    pixelType = PixelType.Float;
                    break;
                default:
                    throw new Exception();
            }

            GL.TextureStorage2D(texture, mipmapLevels, internalFormat, result.Width, result.Height);

            GL.TextureSubImage2D(texture, 0, 0, 0, result.Width, result.Height, pixelFormat, pixelType, result.Data);

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

            if (Path.GetExtension(path) == ".dds")
            {
                GLTexture = LoadDDSTexture(name, path, generateMips, srgb, anisoLevel);
            }
            else
            {
                GLTexture = LoadTexture(name, path, generateMips, srgb, anisoLevel);
            }
        }

        public Texture(string name, Color4 color)
        {
            Name = name;

            HasMips = false;
            // ??
            IsSRGB = false;
            AnisoLevel = 1;

            {
                GL.CreateTextures(TextureTarget.Texture2D, 1, out int texture);
                GL.ObjectLabel(ObjectLabelIdentifier.Texture, texture, -1, $"Texture: {name}");

                SizedInternalFormat internalFormat = IsSRGB ? SizedInternalFormat.Srgb8Alpha8 : SizedInternalFormat.Rgba8;

                GL.TextureStorage2D(texture, 1, internalFormat, 1, 1);

                GL.TextureSubImage2D(texture, 0, 0, 0, 1, 1, PixelFormat.Rgba, PixelType.Float, ref color);

                GL.TextureParameter(texture, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TextureParameter(texture, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                GL.TextureParameter(texture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TextureParameter(texture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                GLTexture = texture;
            }
        }
    }
}
