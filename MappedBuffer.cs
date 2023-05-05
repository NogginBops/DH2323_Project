using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DH2323_Project
{
    internal unsafe struct MappedBuffer<T> where T : unmanaged
    {
        public T* Ptr;
        public int Size;

        public int ByteSize => Size * sizeof(T);

        public int Buffer;

        // FIXME: We can simplify this API substantially.
        public static MappedBuffer<T> CreateMappedBuffer(string name, int size, BufferStorageFlags flags, BufferAccessMask mask)
        {
            GL.CreateBuffers(1, out int buffer);
            GL.ObjectLabel(ObjectLabelIdentifier.Buffer, buffer, -1, name);

            int sizeBytes = size * sizeof(T);

            GL.NamedBufferStorage(buffer, sizeBytes, IntPtr.Zero, flags);

            T* ptr = (T*)GL.MapNamedBufferRange(buffer, IntPtr.Zero, sizeBytes, mask);

            return new MappedBuffer<T>(ptr, size, buffer);
        }

        public MappedBuffer(T* ptr, int size, int buffer)
        {
            Ptr = ptr;
            Size = size;
            Buffer = buffer;
        }

        // FIXME: Bounds check!
        public ref T this[int i]
        {
            get => ref Ptr[i];
        }

        public void Flush()
        {
            GL.FlushMappedNamedBufferRange(Buffer, 0, ByteSize);
        }

        public void FlushElementRange(int offset, int count)
        {
            GL.FlushMappedNamedBufferRange(Buffer, offset * sizeof(T), count * sizeof(T));
        }
    }
}
