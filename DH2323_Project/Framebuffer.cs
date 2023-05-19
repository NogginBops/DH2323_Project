using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DH2323_Project
{
    enum ColorFormat
    {
        RGBA16F,
        RGBA8,
    }

    enum DepthFormat
    {
        Depth32f,
        Depth24Stencil8,
    }

    internal class Framebuffer
    {
        public string Name;

        public int GLFramebuffer;

        public ColorFormat ColorFormat;
        public DepthFormat DepthFormat;

        public int ColorTexture;
        public int DepthStencilTexture;

        public Framebuffer(string name, int width, int height, ColorFormat colorFormat, DepthFormat depthFormat)
        {
            Name = name;

            ColorFormat = colorFormat;
            DepthFormat = depthFormat;

            GL.CreateFramebuffers(1, out GLFramebuffer);
            GL.ObjectLabel(ObjectLabelIdentifier.Framebuffer, GLFramebuffer, -1, $"Framebuffer: {name}");

            (ColorTexture, DepthStencilTexture) = CreateTextures(name, width, height, colorFormat, depthFormat);

            GL.NamedFramebufferTexture(GLFramebuffer, FramebufferAttachment.ColorAttachment0, ColorTexture, 0);
            GL.NamedFramebufferTexture(GLFramebuffer, FramebufferAttachment.DepthStencilAttachment, DepthStencilTexture, 0);

            FramebufferStatus status = GL.CheckNamedFramebufferStatus(GLFramebuffer, FramebufferTarget.Framebuffer);
            if (status != FramebufferStatus.FramebufferComplete)
            {
                throw new Exception(status.ToString());
            }
        }

        public static (int color, int depth) CreateTextures(string name, int width, int height, ColorFormat colorFormat, DepthFormat depthFormat)
        {
            int colorTexture;
            int depthTexture;

            GL.CreateTextures(TextureTarget.Texture2D, 1, out colorTexture);
            GL.ObjectLabel(ObjectLabelIdentifier.Texture, colorTexture, -1, $"Texture: FBO {name} color");
            GL.TextureParameter(colorTexture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TextureParameter(colorTexture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            GL.CreateTextures(TextureTarget.Texture2D, 1, out depthTexture);
            GL.ObjectLabel(ObjectLabelIdentifier.Texture, depthTexture, -1, $"Texture: FBO {name} depth");
            GL.TextureParameter(depthTexture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TextureParameter(depthTexture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            SizedInternalFormat glColorFormat = colorFormat switch
            {
                ColorFormat.RGBA16F => SizedInternalFormat.Rgba16f,
                ColorFormat.RGBA8 => SizedInternalFormat.Rgba8,
                _ => throw new Exception(),
            };

            GL.TextureStorage2D(colorTexture, 1, glColorFormat, width, height);

            SizedInternalFormat glDepthStencilFormat = depthFormat switch
            {
                DepthFormat.Depth32f => SizedInternalFormat.DepthComponent32f,
                DepthFormat.Depth24Stencil8 => SizedInternalFormat.Depth24Stencil8,
                _ => throw new Exception(),
            };

            GL.TextureStorage2D(depthTexture, 1, glDepthStencilFormat, width, height);

            return (colorTexture, depthTexture);
        }

        public void Resize(int width, int height)
        {
            var (colorTexture, depthStencilTexture) = CreateTextures(Name, width, height, ColorFormat, DepthFormat);

            GL.NamedFramebufferTexture(GLFramebuffer, FramebufferAttachment.ColorAttachment0, colorTexture, 0);
            GL.NamedFramebufferTexture(GLFramebuffer, FramebufferAttachment.DepthStencilAttachment, depthStencilTexture, 0);

            GL.DeleteTexture(ColorTexture);
            GL.DeleteTexture(DepthStencilTexture);

            ColorTexture = colorTexture;
            DepthStencilTexture = depthStencilTexture;

            FramebufferStatus status = GL.CheckNamedFramebufferStatus(GLFramebuffer, FramebufferTarget.Framebuffer);
            if (status != FramebufferStatus.FramebufferComplete)
            {
                throw new Exception(status.ToString());
            }
        }
    }
}
