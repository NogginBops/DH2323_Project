using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DH2323_Project
{
    internal class ShaderProgram
    {
        public string Name;
        public int Program;

        public int ModelMatrixLocation;
        public int VPLocation;
        public int NormalMatrixLocation;

        public ShaderProgram(string name, string vertexPath, string fragmentPath)
        {
            Name = name;

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

                Program = GL.CreateProgram();
                GL.ObjectLabel(ObjectLabelIdentifier.Program, Program, -1, $"Program: {name}");

                GL.AttachShader(Program, vertexShader);
                GL.AttachShader(Program, fragmentShader);

                GL.LinkProgram(Program);

                GL.DetachShader(Program, vertexShader);
                GL.DetachShader(Program, fragmentShader);

                GL.DeleteShader(vertexShader);
                GL.DeleteShader(fragmentShader);

                GL.GetProgram(Program, GetProgramParameterName.LinkStatus, out success);
                if (success == 0)
                {
                    string log = GL.GetProgramInfoLog(Program);

                    Console.WriteLine($"{name} program link errror:\n{log}");
                }
            }

            ModelMatrixLocation = GL.GetUniformLocation(Program, "ModelMatrix");
            VPLocation = GL.GetUniformLocation(Program, "VP");
            NormalMatrixLocation = GL.GetUniformLocation(Program, "NormalMatrix");
        }
    }
}
