using Microsoft.VisualBasic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DH2323_Project
{
    internal class Program : GameWindow
    {
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory("../../../Assets");

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

        DebugProc DebugCallback;

        ShaderProgram BasicShader;
        Mesh Mesh;
        Transform Transform;

        Camera Camera;

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.DebugMessageCallback(DebugProcCallback, 0);
            GL.Enable(EnableCap.DebugOutput);
#if DEBUG
            GL.Enable(EnableCap.DebugOutputSynchronous);
#endif

            GL.Enable(EnableCap.DepthTest);
            GL.CullFace(CullFaceMode.Back);
            GL.Enable(EnableCap.CullFace);


            Camera = new Camera("Camera", 90, 0.1f, 1000f, Color4.Coral);

            BasicShader = new ShaderProgram("Basic Shader", "Shaders/Basic.vert", "Shaders/Basic.frag");
            var meshData = ModelLoader.LoadObjModel("Sponza/sponza.obj");
            
            //Mesh = new Mesh("TestModel", ModelLoader.LoadObjModel("TestModel.obj"));

            Dictionary<string, Material> Materials = new Dictionary<string, Material>();
            if (meshData.MtlLib != null)
            {
                RefList<ModelLoader.MtlMaterial> materials = ModelLoader.LoadMtlLib(Path.Combine("Sponza", meshData.MtlLib));

                for (int i = 0; i < materials.Count; i++)
                {
                    ModelLoader.MtlMaterial material = materials[i];

                    Material mat = new Material(material.Name);

                    if (material.MapKd != null)
                    {
                        mat.Albedo = new Texture($"{material.Name}: Albedo", material.MapKd, true, true, 16f);
                    }

                    Materials.Add(material.Name, mat);
                }
            }

            Mesh = new Mesh("Sponza", meshData, Materials);

            Transform = new Transform(Mesh.Name);
            Transform.LocalScale *= 0.1f;
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            if (KeyboardState.IsKeyPressed(Keys.Escape))
            {
                Close();
                return;
            }

            Screen.NewFrame();

            float deltaTime = (float)args.Time;

            UpdateCamera(Camera, deltaTime);

            GL.ClearColor(Camera.ClearColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            GL.UseProgram(BasicShader.Program);
            GL.BindVertexArray(Mesh.VAO);

            Matrix4 model = Transform.LocalToWorld;
            Matrix3 normal = Matrix3.Invert(Matrix3.Transpose(new Matrix3(model)));
            Camera.CalcViewProjection(out Matrix4 VP);

            GL.UniformMatrix4(BasicShader.ModelMatrixLocation, true, ref model);
            GL.UniformMatrix4(BasicShader.VPLocation, true, ref VP);
            GL.UniformMatrix3(BasicShader.NormalMatrixLocation, true, ref normal);

            foreach (var submesh in Mesh.Submeshes)
            {
                GL.BindTextureUnit(0, submesh.Material.Albedo?.GLTexture ?? 0);

                GL.DrawElements(PrimitiveType.Triangles, submesh.IndexCount, DrawElementsType.UnsignedInt, submesh.StartIndex * sizeof(int));
            }

            SwapBuffers();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            Screen.UpdateScreenSize(e.Size);

            GL.Viewport(0, 0, e.Width, e.Height);
        }

        public static float CameraSpeed = 20;
        public static float MouseSpeedX = 1.5f;
        public static float MouseSpeedY = 1.5f;
        public static float CameraMinY = -80f;
        public static float CameraMaxY = 80f;
        public static float CameraPanSpeed = 100f;
        public void UpdateCamera(Camera camera, float deltaTime)
        {
            UpdateCameraMovement(camera, KeyboardState, MouseState, deltaTime);
            UpdateCameraDirection(camera, MouseState, deltaTime);

            static void UpdateCameraMovement(Camera camera, KeyboardState keyboard, MouseState mouse, float deltaTime)
            {
                {
                    Vector3 direction = Vector3.Zero;

                    if (keyboard.IsKeyDown(Keys.W))
                    {
                        direction += camera.Transform.Forward;
                    }

                    if (keyboard.IsKeyDown(Keys.S))
                    {
                        direction += -camera.Transform.Forward;
                    }

                    if (keyboard.IsKeyDown(Keys.A))
                    {
                        direction += -camera.Transform.Right;
                    }

                    if (keyboard.IsKeyDown(Keys.D))
                    {
                        direction += camera.Transform.Right;
                    }

                    if (keyboard.IsKeyDown(Keys.Space))
                    {
                        direction += Vector3.UnitY;
                    }

                    if (keyboard.IsKeyDown(Keys.LeftShift) |
                        keyboard.IsKeyDown(Keys.RightShift))
                    {
                        direction += -Vector3.UnitY;
                    }

                    float speed = CameraSpeed;
                    if (keyboard.IsKeyDown(Keys.LeftControl))
                    {
                        speed /= 4f;
                    }

                    camera.Transform.LocalPosition += direction * speed * deltaTime;
                }

                if (mouse.IsButtonDown(MouseButton.Middle))
                {
                    var delta = mouse.Delta;

                    var offsetDirection = camera.Transform.Up * delta.Y + camera.Transform.Right * -delta.X;
                    if (offsetDirection.LengthSquared > 0.001f)
                        offsetDirection = Vector3.Normalize(offsetDirection);
                    else
                        offsetDirection = Vector3.Zero;

                    var offset = offsetDirection * CameraPanSpeed * deltaTime;
                    Console.WriteLine($"Delta: {delta:0.000}, Offset = {offsetDirection:0.000}");
                    camera.Transform.LocalPosition += offset;
                }
            }

            static void UpdateCameraDirection(Camera camera, MouseState mouse, float deltaTime)
            {
                if (mouse.IsButtonDown(MouseButton.Right))
                {
                    var delta = mouse.Delta;

                    camera.YAxisRotation += -delta.X * MouseSpeedX * deltaTime;
                    camera.XAxisRotation += -delta.Y * MouseSpeedY * deltaTime;
                    camera.XAxisRotation = MathHelper.Clamp(camera.XAxisRotation, CameraMinY * Util.D2R, CameraMaxY * Util.D2R);
                    camera.Transform.LocalRotation =
                        Quaternion.FromAxisAngle(Vector3.UnitY, camera.YAxisRotation) *
                        Quaternion.FromAxisAngle(Vector3.UnitX, camera.XAxisRotation);
                }
            }
        }

        private readonly static DebugProc DebugProcCallback = Window_DebugProc;

        private static void Window_DebugProc(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr messagePtr, IntPtr userParam)
        {
            string message = Marshal.PtrToStringAnsi(messagePtr, length);

            bool showMessage;
            switch (source)
            {
                case DebugSource.DebugSourceApplication:
                    showMessage = false;
                    break;
                case DebugSource.DontCare:
                case DebugSource.DebugSourceApi:
                case DebugSource.DebugSourceWindowSystem:
                case DebugSource.DebugSourceShaderCompiler:
                case DebugSource.DebugSourceThirdParty:
                case DebugSource.DebugSourceOther:
                default:
                    showMessage = true;
                    break;
            }

            if (showMessage)
            {
                switch (severity)
                {
                    case DebugSeverity.DontCare:
                        Debug.Print($"[DontCare] {message}");
                        break;
                    case DebugSeverity.DebugSeverityNotification:
                        Debug.Print($"[Notification] [{source}] {message}");
                        break;
                    case DebugSeverity.DebugSeverityHigh:
                        Debug.Print($"[Error] [{source}] {message}");
                        //Debug.Break();
                        break;
                    case DebugSeverity.DebugSeverityMedium:
                        Debug.Print($"[Warning] [{source}] {message}");
                        break;
                    case DebugSeverity.DebugSeverityLow:
                        Debug.Print($"[Info] [{source}] {message}");
                        break;
                    default:
                        Debug.Print($"[default] {message}");
                        break;
                }
            }
        }
    }
}