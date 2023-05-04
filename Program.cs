using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace DH2323_Project
{
    internal class Program : GameWindow
    {
        static void Main(string[] args)
        {
            GameWindowSettings gws = new GameWindowSettings()
            {

            };

            NativeWindowSettings nws = new NativeWindowSettings()
            {
                Title = "Linearly Transformed Cosines",
                Size = (1600, 900),
                API = ContextAPI.OpenGL,
                APIVersion = new Version(4, 6),
                Profile = ContextProfile.Core,
                Flags = ContextFlags.ForwardCompatible,
            };

#if DEBUG
            nws.Flags |= ContextFlags.Debug;
#endif

            Program program = new Program(gws, nws);
            program.Run();
        }

        public Program(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.ClearColor(Color4.Coral);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);



            SwapBuffers();
        }
    }
}