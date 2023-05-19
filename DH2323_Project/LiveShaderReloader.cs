using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DH2323_Project
{
    static class LiveShaderLoader
    {
        public class TrackedFile
        {
            public FileInfo File;
            public DateTime LastUpdate;

            public TrackedFile(FileInfo file)
            {
                File = file;
                LastUpdate = file.LastWriteTimeUtc;
            }
        }

        public static List<TrackedFile> TrackedFiles = new List<TrackedFile>();

        public static Dictionary<FileInfo, List<ShaderProgram>> FileDependencies = new Dictionary<FileInfo, List<ShaderProgram>>();

        public static DirtyList<ShaderProgram> TrackedShaders = new DirtyList<ShaderProgram>();

        public static void TrackShader(ShaderProgram shader)
        {
            TrackedShaders.Add(shader);

            TrackFile(new FileInfo(shader.VertexPath), shader);
            TrackFile(new FileInfo(shader.FragmentPath), shader);

            static void TrackFile(FileInfo file, ShaderProgram program)
            {
                // FIXME: More efficient test!!
                // Here we go through and try to find a file with the same path as this file
                // this means we want to use the instance of FileInfo to do the rest of the
                // operations in this function.
                bool found = false;
                foreach (var tracked in TrackedFiles)
                {
                    if (tracked.File.FullName == file.FullName)
                    {
                        file = tracked.File;
                        break;
                    }
                }

                if (found == false)
                    TrackedFiles.Add(new TrackedFile(file));

                if (TrackedFiles.Any(t => t.File.FullName == file.FullName) == false)
                    TrackedFiles.Add(new TrackedFile(file));

                if (FileDependencies.TryGetValue(file, out var list) == false)
                {
                    list = new List<ShaderProgram>();
                    FileDependencies.Add(file, list);
                }

                if (list.Contains(program) == false)
                    list.Add(program);
            }
        }

        public static void RecompileShadersIfNeeded()
        {
            foreach (var file in TrackedFiles)
            {
                file.File.Refresh();
                if (file.LastUpdate < file.File.LastWriteTimeUtc)
                {
                    Debug.WriteLine($"File changed '{file.File.FullName}' ({file.LastUpdate} -> {file.File.LastWriteTimeUtc})");

                    foreach (var shader in FileDependencies[file.File])
                    {
                        TrackedShaders.MarkDirty(shader);
                    }

                    file.LastUpdate = file.File.LastWriteTimeUtc;
                }
            }

            // Go through all dirty shaders and try to recompile them
            for (int i = 0; i < TrackedShaders.Count; i++)
            {
                if (TrackedShaders.IsDirty(i))
                {
                    var dirtyShader = TrackedShaders[i];

                    try
                    {
                        dirtyShader.Recompile();
                    }
                    catch(IOException)
                    {
                        // Just ignore this program for now, we can recompile it the next frame.
                        continue;
                    }
                    
                    Debug.WriteLine($"Recompiled program '{dirtyShader.Name}'");

                    TrackedShaders.MarkClean(i);
                }
            }
        }
    }

    public class DirtyList<T>
    {
        public T[] Elements;
        public bool[] DirtyFlags;
        public int Count;

        public DirtyList(int initialSize = 16)
        {
            Elements = new T[initialSize];
            DirtyFlags = new bool[initialSize];
            Count = 0;
        }

        public void EnsureSize(int size)
        {
            if (Elements.Length < size)
            {
                int newSize = Elements.Length + Elements.Length / 2;
                if (newSize < size) newSize = size;
                Array.Resize(ref Elements, newSize);
                Array.Resize(ref DirtyFlags, newSize);
            }
        }

        public bool Add(T element)
        {
            for (int i = 0; i < Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(element, Elements[i]))
                {
                    return false;
                }
            }

            EnsureSize(Count + 1);

            Elements[Count] = element;
            DirtyFlags[Count] = false;
            Count++;
            return true;
        }

        public bool Remove(T element)
        {
            for (int i = 0; i < Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(element, Elements[i]))
                {
                    Elements[i] = Elements[Count - 1];
                    DirtyFlags[i] = DirtyFlags[Count - 1];
                    Count--;
                    return true;
                }
            }

            return false;
        }

        public bool IsDirty(int i)
        {
            if (i >= Count) throw new IndexOutOfRangeException();
            return DirtyFlags[i];
        }

        public void MarkDirty(int i)
        {
            if (i >= Count) throw new IndexOutOfRangeException();
            DirtyFlags[i] = true;
        }

        public void MarkDirty(T element)
        {
            for (int i = 0; i < Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(element, Elements[i]))
                {
                    DirtyFlags[i] = true;
                    return;
                }
            }

            throw new InvalidOperationException();
        }

        public void MarkClean(int i)
        {
            if (i >= Count) throw new IndexOutOfRangeException();
            DirtyFlags[i] = false;
        }

        public T this[int i]
        {
            get
            {
                if (i >= Count) throw new IndexOutOfRangeException();
                return Elements[i];
            }
        }
    }
}
