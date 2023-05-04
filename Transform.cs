using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DH2323_Project
{
    internal class Transform
    {
        public string Name;

        public Transform? Parent;

        private Vector3 position;
        private Quaternion rotation;
        private Vector3 scale;

        public Matrix4 LocalToParent;

        public Matrix4 LocalToWorld => CalculateLocalToWorld();

        public Vector3 LocalPosition
        {
            get => position;
            set
            {
                position = value;
                LocalToParent = CalculateLocalToParent();
            }
        }
        public Quaternion LocalRotation 
        {
            get => rotation;
            set
            {
                rotation = value;
                LocalToParent = CalculateLocalToParent();
            }
        }
        public Vector3 LocalScale 
        {
            get => scale;
            set
            {
                scale = value;
                LocalToParent = CalculateLocalToParent();
            }
        }

        public Vector3 Forward => Vector3.TransformVector(-Vector3.UnitZ, LocalToWorld);
        public Vector3 Right => Vector3.TransformVector(Vector3.UnitX, LocalToWorld);
        public Vector3 Up => Vector3.TransformVector(Vector3.UnitY, LocalToWorld);

        public Transform(string name) : this(name, Vector3.Zero, Quaternion.Identity, Vector3.One)
        {
        }

        public Transform(string name, Vector3 position) : this(name, position, Quaternion.Identity, Vector3.One)
        {
        }

        public Transform(string name, Vector3 position, Quaternion rotation) : this(name, position, rotation, Vector3.One)
        {
        }

        public Transform(string name, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Name = name;

            this.position = position;
            this.rotation = rotation;
            this.scale = scale;

            LocalToParent = CalculateLocalToParent();
        }

        public Matrix4 CalculateLocalToWorld()
        {
            Matrix4 matrix = LocalToParent;

            Transform? parent = Parent;
            while (parent != null)
            {
                Matrix4 parentMatrix = parent.LocalToParent;

                Matrix4.Mult(in matrix, in parentMatrix, out matrix);
            }

            return matrix;
        }

        public Matrix4 CalculateLocalToParent()
        {
            Matrix3.CreateFromQuaternion(rotation, out Matrix3 rotationMatrix);

            Matrix4 matrix;
            matrix.Row0 = new Vector4(rotationMatrix.Row0 * scale.X, 0);
            matrix.Row1 = new Vector4(rotationMatrix.Row1 * scale.Y, 0);
            matrix.Row2 = new Vector4(rotationMatrix.Row2 * scale.Z, 0);
            matrix.Row3 = new Vector4(position, 1);

            return matrix;
        }
    }
}
