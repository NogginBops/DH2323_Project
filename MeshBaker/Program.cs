using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using DH2323_Project;
using Mikktspace.NET;

namespace MeshBaker
{
    internal class Program
    {
        static Vertex[] Vertices;
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine($"Invalid number of arguments. {args.Length}");
                return;
            }

            NativeLibrary.SetDllImportResolver(typeof(MikkGenerator).Assembly, (string libraryName, Assembly assembly, DllImportSearchPath? searchPath) =>
            {
                if (libraryName == "mikktspace")
                {
                    return NativeLibrary.Load("Mikktspace.NET.1.0/Windows x64/mikktspace.dll");
                }

                return IntPtr.Zero;
            });

            var model = ModelLoader.LoadObjModel(args[0]);

            // Unindex the model
            Vertices = new Vertex[model.Indices.Length];
            for (int i = 0; i < model.Indices.Length; i++)
            {
                int index = model.Indices[i];
                Vertices[i] = model.Vertices[index];
            }
            
            var context = new MikktspaceContext(Vertices.Length / 3, VerticesPerFace, VertexPosition, VertexNormalHandler, VertexUVHandler, BasicTangent);
            MikkGenerator.GenerateTangentSpace(context);

            // Now we have tangents!
            // For now, we just write this out as is!
            BinaryWriter writer = new BinaryWriter(File.OpenWrite("result.bin"));
            writer.Write(Vertices.Length);
            writer.Write(MemoryMarshal.Cast<Vertex, byte>(Vertices.AsSpan()));
            writer.Write(model.Objects.Length);
            foreach (var obj in model.Objects)
            {
                writer.Write(obj.StartIndex);
                writer.Write(obj.EndIndex);
                writer.Write(obj.Name);
                writer.Write(obj.MaterialName);
            }

            writer.Write(model.Groups.Length);
            foreach (var group in model.Groups)
            {
                writer.Write(group.StartIndex);
                writer.Write(group.EndIndex);
                writer.Write(group.Name);
                writer.Write(group.MaterialName);
            }

            Console.WriteLine($"Wrote {Vertices.Length} vertices.");
            Console.WriteLine($"Wrote {model.Objects.Length} objects.");
            Console.WriteLine($"Wrote {model.Groups.Length} groups.");

            writer.Write(model.MtlLib);

            writer.Close();
        }

        private static void BasicTangent(int face, int vertex, float x, float y, float z, float sign)
        {
            Vertices[face * 3 + vertex].Tangent = (x, y, z, sign);
        }

        private static int VerticesPerFace(int face) => 3;

        private static void VertexPosition(int face, int vertex, out float x, out float y, out float z)
        {
            (x, y, z) = Vertices[face * 3 + vertex].Position;
        }

        private static void VertexNormalHandler(int face, int vertex, out float x, out float y, out float z)
        {
            (x, y, z) = Vertices[face * 3 + vertex].Normal;
        }

        private static void VertexUVHandler(int face, int vertex, out float u, out float v)
        {
            (u, v) = Vertices[face * 3 + vertex].UV;
        }
    }
}