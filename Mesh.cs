using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DH2323_Project
{
    internal class Mesh
    {
        public string Name;

        public int VAO;
        public int VertexBuffer;
        public int IndexBuffer;

        public int VertexCount;
        public int IndexCount;

        public SubMesh[] Submeshes;

        public ModelLoader.ObjObject[] Objects;
        public ModelLoader.ObjGroup[] Groups;

        public unsafe Mesh(string name, ModelLoader.ObjData data, Dictionary<string, Material> materials)
        {
            Name = name;

            Objects = data.Objects;
            Groups = data.Groups;

            VertexCount = data.Vertices.Length;
            IndexCount = data.Indices.Length;

            {
                GL.CreateVertexArrays(1, out VAO);
                GL.ObjectLabel(ObjectLabelIdentifier.VertexArray, VAO, name.Length, name);

                GL.CreateBuffers(1, out VertexBuffer);
                GL.CreateBuffers(1, out IndexBuffer);

                GL.NamedBufferStorage(VertexBuffer, sizeof(ModelLoader.Vertex) * data.Vertices.Length, data.Vertices, BufferStorageFlags.None);
                // FIXME: Maybe do short indices?
                GL.NamedBufferStorage(IndexBuffer, sizeof(int) * data.Indices.Length, data.Indices, BufferStorageFlags.None);

                GL.VertexArrayElementBuffer(VAO, IndexBuffer);

                GL.VertexArrayVertexBuffer(VAO, 0, VertexBuffer, 0, sizeof(ModelLoader.Vertex));

                GL.VertexArrayAttribFormat(VAO, 0, 3, VertexAttribType.Float, false, 0);
                GL.VertexArrayAttribFormat(VAO, 1, 3, VertexAttribType.Float, false, sizeof(Vector3));
                GL.VertexArrayAttribFormat(VAO, 2, 2, VertexAttribType.Float, false, sizeof(Vector3) * 2);

                GL.VertexArrayAttribBinding(VAO, 0, 0);
                GL.VertexArrayAttribBinding(VAO, 1, 0);
                GL.VertexArrayAttribBinding(VAO, 2, 0);

                GL.EnableVertexArrayAttrib(VAO, 0);
                GL.EnableVertexArrayAttrib(VAO, 1);
                GL.EnableVertexArrayAttrib(VAO, 2);
            }

            Submeshes = new SubMesh[Objects.Length];
            for (int i = 0; i < Objects.Length; i++)
            {
                var obj = Objects[i];

                Submeshes[i].Name = obj.Name;

                if (materials.TryGetValue(obj.MaterialName, out Material? material))
                {
                    Submeshes[i].Material = material;
                }

                Submeshes[i].StartIndex = obj.StartIndex;
                Submeshes[i].IndexCount = (obj.EndIndex - obj.StartIndex) + 1;
            }
        }
    }

    struct SubMesh
    {
        public string Name;

        public Material Material;

        public int StartIndex;
        public int IndexCount;

        public SubMesh(string name, Material material, int startIndex, int indexCount)
        {
            Name = name;
            Material = material;
            StartIndex = startIndex;
            IndexCount = indexCount;
        }
    }
}
