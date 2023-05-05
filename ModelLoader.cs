using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace DH2323_Project
{
    internal static class ModelLoader
    {
        public struct ObjData
        {
            public Vertex[] Vertices;
            public int[] Indices;
            public ObjObject[] Objects;
            public ObjGroup[] Groups;

            public string? MtlLib;

            public ObjData(Vertex[] vertices, int[] indices, ObjObject[] objects, ObjGroup[] groups, string mtllib)
            {
                Vertices = vertices;
                Indices = indices;
                Objects = objects;
                Groups = groups;
                MtlLib = mtllib;
            }
        }

        public struct Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 UV;

            public Vertex(Vector3 position, Vector3 normal, Vector2 uV)
            {
                Position = position;
                Normal = normal;
                UV = uV;
            }
        }

        public static ObjData LoadObjModel(string path)
        {
            string[] lines = File.ReadAllLines(path);

            RefList<VertexRef> faces = new RefList<VertexRef>();

            RefList<ObjObject> objects = new RefList<ObjObject>();
            RefList<ObjGroup> groups = new RefList<ObjGroup>();

            RefList<Vector3> positions = new RefList<Vector3>();
            RefList<Vector3> normals = new RefList<Vector3>();
            RefList<Vector2> uvs = new RefList<Vector2>();

            string? mtllib = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                // Figure out what kind of statement we are dealing with.

                if (line[0] == 'v')
                {
                    switch (line[1])
                    {
                        case ' ':
                            {
                                // v x y z
                                var span = line.AsSpan()[2..];
                                int spaceIdx = span.IndexOf(' ');
                                float x = float.Parse(span[..spaceIdx]);

                                span = span[(spaceIdx + 1)..];
                                spaceIdx = span.IndexOf(' ');
                                float y = float.Parse(span[..spaceIdx]);

                                span = span[(spaceIdx + 1)..];
                                float z = float.Parse(span[..^0]);

                                positions.Add(new Vector3(x, y, z));
                                break;
                            }
                        case 'n':
                            {
                                // vn x y z
                                var span = line.AsSpan()[3..];
                                int spaceIdx = span.IndexOf(' ');
                                float x = float.Parse(span[..spaceIdx]);

                                span = span[(spaceIdx + 1)..];
                                spaceIdx = span.IndexOf(' ');
                                float y = float.Parse(span[..spaceIdx]);

                                span = span[(spaceIdx + 1)..];
                                float z = float.Parse(span[..^0]);

                                normals.Add(new Vector3(x, y, z));
                                break;
                            }
                        case 't':
                            {
                                // vt u v
                                var span = line.AsSpan()[3..];
                                int spaceIdx = span.IndexOf(' ');
                                float x = float.Parse(span[..spaceIdx]);

                                span = span[(spaceIdx + 1)..];
                                spaceIdx = span.IndexOf(' ');
                                if (spaceIdx == -1) spaceIdx = span.Length - 1;
                                float y = float.Parse(span[..spaceIdx]);

                                uvs.Add(new Vector2(x, y));
                                break;
                            }
                        default: break;
                    }
                }
                else if (line[0] == 'f')
                {
                    // FIXME: N-gons

                    // f v/vt/vn v/vt/vn v/vt/vn
                    var span = line.AsSpan()[2..];

                    int spaceIdx = span.IndexOf(' ');
                    VertexRef v0 = ParseVertexRef(span[..spaceIdx]);

                    span = span[(spaceIdx + 1)..];
                    spaceIdx = span.IndexOf(' ');
                    VertexRef v1 = ParseVertexRef(span[..spaceIdx]);

                    span = span[(spaceIdx + 1)..];
                    VertexRef v2 = ParseVertexRef(span[..^0]);

                    faces.Add(v0);
                    faces.Add(v1);
                    faces.Add(v2);

                    static VertexRef ParseVertexRef(ReadOnlySpan<char> span)
                    {
                        int slashIdx = span.IndexOf('/');
                        if (slashIdx == -1) slashIdx = span.Length - 1;
                        int v = int.Parse(span[..slashIdx]);
                        span = span[(slashIdx + 1)..];
                        if (span.Length == 0) return new VertexRef(v, -1, -1);

                        slashIdx = span.IndexOf('/');
                        if (slashIdx == -1) slashIdx = span.Length - 1;
                        int vt = int.Parse(span[..slashIdx]);
                        span = span[(slashIdx + 1)..];
                        if (span.Length == 0) return new VertexRef(v, -vt, -1);

                        int vn = int.Parse(span[..^0]);
                        return new VertexRef(v, vt, vn);
                    }
                }
                else if (line[0] == 'o')
                {
                    // o name

                    // FIXME: End the current group if there is one!
                    if (groups.Count > 0 && groups[^1].EndIndex == 0)
                    {
                        groups[^1].EndIndex = faces.Count - 1;
                    }

                    if (objects.Count > 0)
                    {
                        objects[^1].EndIndex = faces.Count - 1;
                    }

                    ref ObjObject obj = ref objects.RefAdd();
                    obj.Name = line[2..];

                    obj.StartIndex = faces.Count;

                }
                else if (line[0] == 'g')
                {
                    // g name

                    if (groups.Count > 0 && groups[^1].EndIndex == 0)
                    {
                        groups[^1].EndIndex = faces.Count - 1;
                    }

                    ref ObjGroup group = ref groups.RefAdd();

                    group.Name = line[2..];
                    group.Object = objects.Count - 1;
                    group.StartIndex = faces.Count;
                }
                else if (line.StartsWith("mtllib "))
                {
                    mtllib = line["mtllib ".Length..];
                }
                else if (line.StartsWith("usemtl "))
                {
                    objects[^1].MaterialName = line["usemtl ".Length..];
                    groups[^1].MaterialName = line["usemtl ".Length..];
                }
            }

            // End the last object and group
            if (objects.Count > 0 && objects[^1].EndIndex == 0)
            {
                objects[^1].EndIndex = faces.Count - 1;
            }

            if (groups.Count > 0 && groups[^1].EndIndex == 0)
            {
                groups[^1].EndIndex = faces.Count - 1;
            }

            // Now we have all of the data

            Dictionary<VertexRef, int> indexDict = new Dictionary<VertexRef, int>();
            RefList<Vertex> vertices = new RefList<Vertex>();
            RefList<int> indices = new RefList<int>();

            for (int i = 0; i < faces.Count; i++)
            {
                VertexRef @ref = faces[i];
                if (indexDict.TryGetValue(@ref, out int index) == false)
                {
                    index = vertices.Count;
                    indexDict.Add(@ref, index);

                    vertices.Add(new Vertex(positions[@ref.vIdx - 1], normals[@ref.vnIdx - 1], uvs[@ref.vtIdx - 1]));
                }

                indices.Add(index);
            }

            return new ObjData(vertices.ToArray(), indices.ToArray(), objects.ToArray(), groups.ToArray(), mtllib);
        }

        public static RefList<MtlMaterial> LoadMtlLib(string? mtllib)
        {
            if (mtllib == null) return new RefList<MtlMaterial>();

            RefList<MtlMaterial> materials = new RefList<MtlMaterial>();
            ref MtlMaterial currentMat = ref Unsafe.NullRef<MtlMaterial>();

            string dir = Path.GetDirectoryName(mtllib)!;

            string[] lines = File.ReadAllLines(mtllib);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                var lineSpan = line.AsSpan();
                if (string.IsNullOrEmpty(line)) continue;

                if (line.StartsWith("newmtl "))
                {
                    materials.Add(new MtlMaterial());
                    currentMat = ref materials[^1];
                    currentMat.Name = line["newmtl ".Length..];
                }
                else if (line.StartsWith("Ns "))
                {
                    float ns = float.Parse(lineSpan["Ns ".Length..]);
                    currentMat.Ns = ns;
                }
                else if (line.StartsWith("Ka "))
                {
                    int index1 = line.IndexOf(' ') + 1;
                    int index2 = line.IndexOf(' ', index1) + 1;
                    int index3 = line.IndexOf(' ', index2) + 1;

                    float ka1 = float.Parse(lineSpan[index1..(index2 - 1)]);
                    float ka2 = float.Parse(lineSpan[index2..(index3 - 1)]);
                    float ka3 = float.Parse(lineSpan[index3..]);

                    currentMat.Ka = new Color4(ka1, ka2, ka3, 1f);
                }
                else if (line.StartsWith("Kd "))
                {
                    int index1 = line.IndexOf(' ') + 1;
                    int index2 = line.IndexOf(' ', index1) + 1;
                    int index3 = line.IndexOf(' ', index2) + 1;

                    float kd1 = float.Parse(lineSpan[index1..(index2 - 1)]);
                    float kd2 = float.Parse(lineSpan[index2..(index3 - 1)]);
                    float kd3 = float.Parse(lineSpan[index3..]);

                    currentMat.Kd = new Color4(kd1, kd2, kd3, 1f);
                }
                else if (line.StartsWith("Ks "))
                {
                    int index1 = line.IndexOf(' ') + 1;
                    int index2 = line.IndexOf(' ', index1) + 1;
                    int index3 = line.IndexOf(' ', index2) + 1;

                    float ks1 = float.Parse(lineSpan[index1..(index2 - 1)]);
                    float ks2 = float.Parse(lineSpan[index2..(index3 - 1)]);
                    float ks3 = float.Parse(lineSpan[index3..]);

                    currentMat.Ks = new Color4(ks1, ks2, ks3, 1f);
                }
                else if (line.StartsWith("d "))
                {
                    float d = float.Parse(lineSpan["d ".Length..]);
                    currentMat.d = d;
                }
                else if (line.StartsWith("map_Kd "))
                {
                    string name = line["map_Kd ".Length..];
                    currentMat.MapKd = Path.Combine(dir, name);
                }
                else continue;
            }

            return materials;
        }

        public struct MtlMaterial
        {
            public string Name;

            public Color4 Ka;
            public Color4 Kd;
            public Color4 Ks;
            public float Ns;
            public float d;

            public int Illum;

            public string MapKa;
            public string MapKd;
            public string MapDisp;
            public string Map_d;
        }

        public struct ObjObject
        {
            public string Name;
            public int StartIndex;
            public int EndIndex;

            public string MaterialName;
        }

        public struct ObjGroup
        {
            public string Name;

            public int Object;

            public int StartIndex;
            public int EndIndex;

            public string MaterialName;
        }

        struct VertexRef : IEquatable<VertexRef>
        {
            public int vIdx;
            public int vtIdx;
            public int vnIdx;

            public VertexRef(int vIdx, int vtIdx, int vnIdx)
            {
                this.vIdx = vIdx;
                this.vtIdx = vtIdx;
                this.vnIdx = vnIdx;
            }

            public override bool Equals(object? obj)
            {
                return obj is VertexRef vert && Equals(vert);
            }

            public bool Equals(VertexRef other)
            {
                return vIdx == other.vIdx &&
                       vtIdx == other.vtIdx &&
                       vnIdx == other.vnIdx;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(vIdx, vtIdx, vnIdx);
            }

            public static bool operator ==(VertexRef left, VertexRef right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(VertexRef left, VertexRef right)
            {
                return !(left == right);
            }

            public override string? ToString()
            {
                return $"{vIdx}/{vtIdx}/{vnIdx}";
            }
        }
    }
}
