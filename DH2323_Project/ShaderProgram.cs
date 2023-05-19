using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DH2323_Project
{
    internal class ShaderProgram
    {
        public string Name;
        public int Program;

        public string VertexPath;
        public string FragmentPath;

        public int ModelMatrixLocation;
        public int VPLocation;
        public int NormalMatrixLocation;

        public ShaderProgram(string name, string vertexPath, string fragmentPath)
        {
            Name = name;

            VertexPath = vertexPath;
            FragmentPath = fragmentPath;

            Program = Compile(Name, vertexPath, fragmentPath);

            UpdateUniformLocations();
        }

        public void Recompile()
        {
            // If we fail to compile (throw an exception) we don't want to have deleted the old program.
            int oldProgram = Program;
            Program = Compile(Name, VertexPath, FragmentPath);
            GL.DeleteProgram(oldProgram);

            UpdateUniformLocations();
        }

        private int Compile(string name, string vertexPath, string fragmentPath)
        {
            {
                string vertexSource = File.ReadAllText(vertexPath);

                int vertexShader = GL.CreateShader(ShaderType.VertexShader);
                GL.ObjectLabel(ObjectLabelIdentifier.Shader, vertexShader, -1, $"Vertex Shader: {name}");
                GL.ShaderSource(vertexShader, vertexSource);

                GL.CompileShader(vertexShader);

                GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success);
                if (success == 0)
                {
                    string log = GL.GetShaderInfoLog(vertexShader);

                    Console.WriteLine($"{name} vertex shader compile errror:\n{log}");
                }

                string fragmentSource = File.ReadAllText(fragmentPath);

                int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
                GL.ObjectLabel(ObjectLabelIdentifier.Shader, fragmentShader, -1, $"Fragment Shader: {name}");
                GL.ShaderSource(fragmentShader, fragmentSource);

                GL.CompileShader(fragmentShader);

                GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out success);
                if (success == 0)
                {
                    string log = GL.GetShaderInfoLog(fragmentShader);

                    Console.WriteLine($"{name} fragment shader compile errror:\n{log}");
                }

                int program = GL.CreateProgram();
                GL.ObjectLabel(ObjectLabelIdentifier.Program, program, -1, $"Program: {name}");

                GL.AttachShader(program, vertexShader);
                GL.AttachShader(program, fragmentShader);

                GL.LinkProgram(program);

                GL.DetachShader(program, vertexShader);
                GL.DetachShader(program, fragmentShader);

                GL.DeleteShader(vertexShader);
                GL.DeleteShader(fragmentShader);

                GL.GetProgram(program, GetProgramParameterName.LinkStatus, out success);
                if (success == 0)
                {
                    string log = GL.GetProgramInfoLog(program);

                    Console.WriteLine($"{name} program link errror:\n{log}");
                }

                return program;
            }
        }

        private void UpdateUniformLocations()
        {
            ModelMatrixLocation = GL.GetUniformLocation(Program, "ModelMatrix");
            VPLocation = GL.GetUniformLocation(Program, "VP");
            NormalMatrixLocation = GL.GetUniformLocation(Program, "NormalMatrix");
        }
    }
}
