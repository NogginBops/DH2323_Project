using ImGuiNET;
using Microsoft.VisualBasic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DH2323_Project
{
    struct PointLight
    {
        public Vector3 Position;
        public float InvRadius;
        public Vector3 Color;
        public float padding1;
    }

    struct AreaLight
    {
        public Vector4 P0, P1, P2, P3;
        public Vector4 ColorAndIntensity;
    }

    internal class Program : GameWindow
    {
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory("../../../Assets");
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

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
                Vsync = VSyncMode.On,
            };

#if DEBUG
            nws.Flags |= ContextFlags.Debug;
#endif

            Program program = new Program(gws, nws);
            program.Run();
        }

        public Program(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
            _keyboardState = new ToggledKeyboardState(this);
            _mouseState = new ToggledMouseState(this);
        }

        ImGuiController ImguiController;

        private ToggledKeyboardState _keyboardState;
        private ToggledMouseState _mouseState;

        new ToggledKeyboardState KeyboardState => _keyboardState;
        new ToggledMouseState MouseState => _mouseState;

        RefList<AreaLight> AreaLights;
        MappedBuffer<AreaLight> AreaLightsBuffer;
        List<Mesh> AreaLightMeshes = new List<Mesh>();
        List<Transform> AreaLightTransforms = new List<Transform>();
        Texture LTCAmp;
        Texture LTCMat;

        Texture DefaultNormal;
        Texture Black;
        Texture White;

        MappedBuffer<PointLight> PointLights;

        Framebuffer HDRFramebuffer;

        ShaderProgram HDRToLDRShader;

        ShaderProgram BasicShader;
        ShaderProgram LightShader;
        Mesh Mesh;
        Transform Transform;

        Camera Camera;

        float metallic = 0.2f;
        float reflecance = 0.2f;
        float roughness = 0.1f;

        // FIXME: Live shader reload!

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

            DefaultNormal = new Texture("Default Normal", new Color4(0.5f, 0.5f, 1f, 1f));
            Black = new Texture("Default Normal", Color4.Black);
            White = new Texture("Default Normal", Color4.White);

            LTCMat = new Texture("LTC Mat", "ltc_mat.dds", false, false, 0);
            LTCAmp = new Texture("LTC Amp", "ltc_amp.dds", false, false, 0);

            AreaLights = new RefList<AreaLight>();
            AreaLights.Add(new AreaLight()
            {
                P0 = new Vector4(-5, -5, 0, 1),
                P1 = new Vector4(+5, -5, 0, 1),
                P2 = new Vector4(+5, +5, 0, 1),
                P3 = new Vector4(-5, +5, 0, 1),
                ColorAndIntensity = new Vector4(((Vector4)Color4.CornflowerBlue).Xyz, 10f),
            });
            AreaLights.Add(new AreaLight()
            {
                P0 = new Vector4(-5, -5, 0, 1),
                P1 = new Vector4(+5, -5, 0, 1),
                P2 = new Vector4(+5, +5, 0, 1),
                P3 = new Vector4(-5, +5, 0, 1),
                ColorAndIntensity = new Vector4(((Vector4)Color4.White).Xyz, 10f),
            });
            AreaLightsBuffer = MappedBuffer<AreaLight>.CreateMappedBuffer("Area Lights", AreaLights.Count, BufferStorageFlags.MapPersistentBit | BufferStorageFlags.MapWriteBit, BufferAccessMask.MapPersistentBit | BufferAccessMask.MapWriteBit | BufferAccessMask.MapFlushExplicitBit);
            AreaLightsBuffer[0] = AreaLights[0];
            AreaLightsBuffer[1] = AreaLights[1];
            AreaLightsBuffer.Flush();
            AreaLightMeshes.Add(new Mesh("Area Light 1", AreaLightsBuffer[0]));
            AreaLightMeshes.Add(new Mesh("Area Light 2", AreaLightsBuffer[0]));
            AreaLightTransforms.Add(new Transform("Area Light 1"));
            AreaLightTransforms.Add(new Transform("Area Light 2"));
            AreaLightTransforms[0].LocalPosition += (0, 10, 0);
            AreaLightTransforms[0].LocalRotation = Quaternion.FromAxisAngle(Vector3.UnitY, +90 * Util.D2R);
            AreaLightTransforms[1].LocalPosition += (0, 10, 0);
            AreaLightTransforms[1].LocalRotation = Quaternion.FromAxisAngle(Vector3.UnitY, -90 * Util.D2R);

            PointLights = MappedBuffer<PointLight>.CreateMappedBuffer("Point Lights", 0, BufferStorageFlags.MapPersistentBit | BufferStorageFlags.MapWriteBit, BufferAccessMask.MapPersistentBit | BufferAccessMask.MapWriteBit | BufferAccessMask.MapFlushExplicitBit);
            Random rand = new Random();
            for (int i = 0; i < PointLights.Size; i++)
            {
                float x = MathHelper.MapRange(rand.NextSingle(), 0, 1, -100, 100);
                float y = MathHelper.MapRange(rand.NextSingle(), 0, 1,  0.1f, 30);
                float z = MathHelper.MapRange(rand.NextSingle(), 0, 1, -15, 15);

                PointLights[i].Position = (x, y, z);

                PointLights[i].InvRadius = 1 / 1000f;

                Color4 color = Color4.FromHsv((rand.NextSingle(), 1, 1, 1));
                PointLights[i].Color = new Vector3(color.R, color.G, color.B) * 400.0f;
                //PointLights[i].Color = Vector3.One * 400.0f;
            }
            PointLights.Flush();

            HDRFramebuffer = new Framebuffer("HDR", Size.X, Size.Y, ColorFormat.RGBA16F, DepthFormat.Depth24Stencil8);
            Screen.RegisterFramebuffer(HDRFramebuffer);

            Camera = new Camera("Camera", 90, 0.1f, 1000f, Color4.Coral);

            HDRToLDRShader = new ShaderProgram("HDR to LDR", "Shaders/FullscreenTri.vert", "Shaders/HDRToLDR.frag");

            BasicShader = new ShaderProgram("Basic Shader", "Shaders/Basic.vert", "Shaders/Lighting.frag");
            LightShader = new ShaderProgram("Light Shader", "Shaders/Light.vert", "Shaders/Light.frag");

            LiveShaderLoader.TrackShader(HDRToLDRShader);
            LiveShaderLoader.TrackShader(BasicShader);
            LiveShaderLoader.TrackShader(LightShader);

            //var meshData = ModelLoader.LoadObjModel("Sponza/sponza.obj");
            var meshData = ModelLoader.LoadBinModel("Sponza/sponza.bin");

            //Mesh = new Mesh("TestModel", ModelLoader.LoadObjModel("TestModel.obj"));

            Dictionary<string, Material> Materials = new Dictionary<string, Material>();
            if (meshData.MtlLib != null)
            {
                RefList<ModelLoader.MtlMaterial> materials = ModelLoader.LoadMtlLib(Path.Combine("Sponza", meshData.MtlLib));

                Texture uvtest = new Texture($"UV test", "uvtest.png", true, true, 16f);

                for (int i = 0; i < materials.Count; i++)
                {
                    ModelLoader.MtlMaterial material = materials[i];

                    Material mat = new Material(material.Name);

                    if (material.MapKd != null)
                    {
                        mat.Albedo = new Texture($"{material.Name}: Albedo", material.MapKd, true, true, 16f);
                    }

                    if (material.MapDisp != null)
                    {
                        mat.Normal = new Texture($"{material.Name}: Normal", material.MapDisp, true, false, 16f);

                        string roughness = material.MapDisp.Replace("ddn", "roughness");
                        roughness = Path.ChangeExtension(roughness, "png");
                        if (File.Exists(roughness))
                        {
                            mat.Roughness = new Texture($"{material.Name}: Roughtness", roughness, true, false, 16f);
                        }
                        else
                        {
                            Console.WriteLine($"Could not find: {roughness}");
                        }
                    }

                    

                    Materials.Add(material.Name, mat);
                }
            }

            Mesh = new Mesh("Sponza", meshData, Materials);

            Transform = new Transform(Mesh.Name);
            Transform.LocalScale *= 0.1f;

            ImguiController = new ImGuiController(Size.X, Size.Y);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            if (KeyboardState.IsKeyPressed(Keys.Escape))
            {
                Close();
                return;
            }

            if (KeyboardState.IsKeyPressed(Keys.F11))
            {
                if (WindowState == WindowState.Fullscreen)
                {
                    WindowState = WindowState.Normal;
                }
                else
                {
                    WindowState = WindowState.Fullscreen;
                }
            }

            float deltaTime = (float)args.Time;
            
            Screen.NewFrame();

            ImguiController.Update(this, deltaTime);
            var io = ImGui.GetIO();
            KeyboardState.InputEnabled = io.WantCaptureKeyboard == false;
            MouseState.InputEnabled = io.WantCaptureMouse == false;

            ShowUI(deltaTime);

            UpdateCamera(Camera, deltaTime);

            // Rotate light
            {
                //AreaLightTransforms[0].LocalRotation *= Quaternion.FromAxisAngle(Vector3.UnitY, MathF.PI * deltaTime);
                for (int i = 0; i < AreaLightsBuffer.Size; i++)
                {
                    AreaLightsBuffer[i].P0 = AreaLights[i].P0 * AreaLightTransforms[i].LocalToWorld;
                    AreaLightsBuffer[i].P1 = AreaLights[i].P1 * AreaLightTransforms[i].LocalToWorld;
                    AreaLightsBuffer[i].P2 = AreaLights[i].P2 * AreaLightTransforms[i].LocalToWorld;
                    AreaLightsBuffer[i].P3 = AreaLights[i].P3 * AreaLightTransforms[i].LocalToWorld;
                    AreaLightsBuffer[i].ColorAndIntensity = AreaLights[i].ColorAndIntensity;
                }

                AreaLightsBuffer.Flush();
            }

            LiveShaderLoader.RecompileShadersIfNeeded();

            Screen.ResizeToScreenSizeIfNecessary(HDRFramebuffer);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, HDRFramebuffer.GLFramebuffer);

            GL.ClearColor(Camera.ClearColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            GL.UseProgram(BasicShader.Program);
            GL.BindVertexArray(Mesh.VAO);

            Matrix4 model = Transform.LocalToWorld;
            Matrix3 normal = Matrix3.Invert(Matrix3.Transpose(new Matrix3(model)));
            Camera.CalcViewProjection(out Matrix4 VP);

            GL.Uniform3(GL.GetUniformLocation(BasicShader.Program, "cameraPosition"), Camera.Transform.WorldPosition);
            GL.Uniform1(1, metallic);
            GL.Uniform1(2, reflecance);
            GL.Uniform1(3, roughness);

            GL.UniformMatrix4(BasicShader.ModelMatrixLocation, true, ref model);
            GL.UniformMatrix4(BasicShader.VPLocation, true, ref VP);
            GL.UniformMatrix3(BasicShader.NormalMatrixLocation, true, ref normal);

            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, PointLights.Buffer);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, AreaLightsBuffer.Buffer);

            GL.BindTextureUnit(10, LTCMat.GLTexture);
            GL.BindTextureUnit(11, LTCAmp.GLTexture);

            foreach (var submesh in Mesh.Submeshes)
            {
                GL.BindTextureUnit(0, submesh.Material.Albedo?.GLTexture ?? 0);
                GL.BindTextureUnit(1, submesh.Material.Normal?.GLTexture ?? DefaultNormal.GLTexture);
                GL.BindTextureUnit(2, submesh.Material.Roughness?.GLTexture ?? White.GLTexture);

                GL.DrawElements(PrimitiveType.Triangles, submesh.IndexCount, DrawElementsType.UnsignedInt, submesh.StartIndex * sizeof(int));
            }

            GL.UseProgram(LightShader.Program);

            for (int i = 0; i < AreaLightMeshes.Count; i++)
            {
                var light = AreaLightMeshes[i];

                Matrix4 lightMatrix = AreaLightTransforms[i].LocalToWorld;
                GL.UniformMatrix4(LightShader.ModelMatrixLocation, true, ref lightMatrix);
                GL.UniformMatrix4(LightShader.VPLocation, true, ref VP);

                GL.Uniform4(GL.GetUniformLocation(LightShader.Program, "ColorAndIntensity"), AreaLights[i].ColorAndIntensity);

                GL.BindVertexArray(light.VAO);
                GL.DrawElements(PrimitiveType.Triangles, light.Submeshes[0].IndexCount, DrawElementsType.UnsignedInt, light.Submeshes[0].StartIndex * sizeof(int));
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(HDRToLDRShader.Program);
            GL.BindTextureUnit(0, HDRFramebuffer.ColorTexture);

            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            ImguiController.Render();

            SwapBuffers();
        }

        private void ShowUI(float deltaTime)
        {
            if (ImGui.Begin("Controls"))
            {
                ImGui.Text($"Frame time: {(deltaTime * 1000):0.0000}ms");

                ImGui.DragFloat("Metallic", ref metallic, 0.01f, 0.0f, 1.0f);
                ImGui.DragFloat("Reflecance", ref reflecance, 0.01f, 0.0f, 1.0f);
                ImGui.DragFloat("Roughness", ref roughness, 0.01f, 0.045f, 1.0f);

                ImGui.PushID(1);
                if (ImGui.CollapsingHeader("Light 1 Transform##1"))
                {
                    Vector3 pos = AreaLightTransforms[0].LocalPosition;
                    if (ImGui.DragFloat3("Position", ref Unsafe.As<Vector3, System.Numerics.Vector3>(ref pos)))
                        AreaLightTransforms[0].LocalPosition = pos;

                    Vector3 rot = AreaLightTransforms[0].LocalRotation.ToEulerAngles();
                    if (ImGui.DragFloat3("Rotation", ref Unsafe.As<Vector3, System.Numerics.Vector3>(ref rot), deltaTime))
                        AreaLightTransforms[0].LocalRotation = Quaternion.FromEulerAngles(rot);

                    Vector3 scale = AreaLightTransforms[0].LocalScale;
                    if (ImGui.DragFloat3("Scale", ref Unsafe.As<Vector3, System.Numerics.Vector3>(ref scale), deltaTime))
                        AreaLightTransforms[0].LocalScale = scale;

                    Vector3 color = AreaLights[0].ColorAndIntensity.Xyz;
                    if (ImGui.ColorEdit3("Color", ref Unsafe.As<Vector3, System.Numerics.Vector3>(ref color)))
                        AreaLights[0].ColorAndIntensity.Xyz = color;

                    float intensity = AreaLights[0].ColorAndIntensity.W;
                    if (ImGui.DragFloat("Intensity", ref intensity, deltaTime, 0, 1000))
                        AreaLights[0].ColorAndIntensity.W = intensity;
                }
                ImGui.PopID();

                ImGui.PushID(2);
                if (ImGui.CollapsingHeader("Light 2 Transform##2"))
                {
                    Vector3 pos = AreaLightTransforms[1].LocalPosition;
                    if (ImGui.DragFloat3("Position", ref Unsafe.As<Vector3, System.Numerics.Vector3>(ref pos)))
                        AreaLightTransforms[1].LocalPosition = pos;

                    Vector3 rot = AreaLightTransforms[1].LocalRotation.ToEulerAngles();
                    if (ImGui.DragFloat3("Rotation", ref Unsafe.As<Vector3, System.Numerics.Vector3>(ref rot), deltaTime))
                        AreaLightTransforms[1].LocalRotation = Quaternion.FromEulerAngles(rot);

                    Vector3 scale = AreaLightTransforms[1].LocalScale;
                    if (ImGui.DragFloat3("Scale", ref Unsafe.As<Vector3, System.Numerics.Vector3>(ref scale), deltaTime))
                        AreaLightTransforms[1].LocalScale = scale;

                    Vector3 color = AreaLights[1].ColorAndIntensity.Xyz;
                    if (ImGui.ColorEdit3("Color", ref Unsafe.As<Vector3, System.Numerics.Vector3>(ref color)))
                        AreaLights[1].ColorAndIntensity.Xyz = color;

                    float intensity = AreaLights[1].ColorAndIntensity.W;
                    if (ImGui.DragFloat("Intensity", ref intensity, deltaTime, 0, 1000))
                        AreaLights[1].ColorAndIntensity.W = intensity;
                }
                ImGui.PopID();
            }
            ImGui.End();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            Screen.UpdateScreenSize(e.Size);

            GL.Viewport(0, 0, e.Width, e.Height);

            ImguiController.WindowResized(e.Width, e.Height);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            ImguiController.PressChar((char)e.Unicode);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            ImguiController.MouseScroll(e.Offset);
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

            static void UpdateCameraMovement(Camera camera, ToggledKeyboardState keyboard, ToggledMouseState mouse, float deltaTime)
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

            static void UpdateCameraDirection(Camera camera, ToggledMouseState mouse, float deltaTime)
            {
                if (mouse.IsButtonDown(MouseButton.Left))
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

    internal class ToggledKeyboardState
    {
        public bool InputEnabled = true;
        public GameWindow Window;

        public ToggledKeyboardState(GameWindow window)
        {
            Window = window;
        }

        public bool this[Keys key]
        {
            get => IsKeyDown(key);
        }

        public bool IsKeyDown(Keys key)
        {
            if (InputEnabled) return Window.KeyboardState.IsKeyDown(key);
            else return false;
        }

        public bool WasKeyDown(Keys key)
        {
            if (InputEnabled) return Window.KeyboardState.WasKeyDown(key);
            else return false;
        }

        public bool IsKeyPressed(Keys key)
        {
            if (InputEnabled) return Window.KeyboardState.IsKeyPressed(key);
            else return false;
        }

        public bool IsKeyReleased(Keys key)
        {
            if (InputEnabled) return Window.KeyboardState.IsKeyReleased(key);
            else return false;
        }
    }

    internal class ToggledMouseState
    {
        public bool InputEnabled = true;
        public GameWindow Window;

        public ToggledMouseState(GameWindow window)
        {
            Window = window;
        }

        public Vector2 Position => Window.MouseState.Position;

        public Vector2 PreviousPosition => Window.MouseState.PreviousPosition;

        public Vector2 Delta => Window.MouseState.Delta;

        public Vector2 Scroll => InputEnabled ? Window.MouseState.Scroll : Vector2.Zero;

        public Vector2 PreviousScroll => InputEnabled ? Window.MouseState.PreviousScroll : Vector2.Zero;

        public Vector2 ScrollDelta => InputEnabled ? Window.MouseState.ScrollDelta : Vector2.Zero;

        public bool this[MouseButton button]
        {
            get => InputEnabled ? Window.MouseState[button] : false;
        }

        public bool IsButtonDown(MouseButton button)
        {
            return InputEnabled ? Window.MouseState.IsButtonDown(button) : false;
        }

        public bool WasButtonDown(MouseButton button)
        {
            return InputEnabled ? Window.MouseState.WasButtonDown(button) : false;
        }

        public bool IsButtonPressed(MouseButton button)
        {
            return InputEnabled ? Window.MouseState.IsButtonPressed(button) : false;
        }

        public bool IsButtonReleased(MouseButton button)
        {
            return InputEnabled ? Window.MouseState.IsButtonReleased(button) : false;
        }
    }
}