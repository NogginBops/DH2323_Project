using DH2323_Project;
using OpenTK.Core;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DH2323_Project
{
    enum ProjectionType
    {
        Perspective,
        Orthographic,
    }

    class Camera
    {
        public Transform Transform;

        public Color4 ClearColor;

        public ProjectionType ProjectionType;

        public float VerticalFov;
        public Box2 Viewport;
        public float NearPlane;
        public float FarPlane;

        // The vertical orthograpic size
        public float OrthograpicSize;

        public float Aspect => (Viewport.Size.X * Screen.Width) / (Viewport.Size.Y * Screen.Height);

        // Used by the mouse controls for cameras
        public float YAxisRotation, XAxisRotation;

        public Camera(string name, float verticalFov, float near, float far, Color4 clear)
        {
            Transform = new Transform(name);
            ClearColor = clear;
            VerticalFov = verticalFov;
            Viewport = new Box2((0, 0), (1, 1));
            NearPlane = near;
            FarPlane = far;
        }

        public void CalcViewProjection(out Matrix4 vp)
        {
            CalcViewMatrix(out var view);
            CalcProjectionMatrix(out var proj);
            Matrix4.Mult(view, proj, out vp);
        }

        public void CalcViewMatrix(out Matrix4 viewMatrix)
        {
            Matrix4.Invert(Transform.LocalToWorld, out viewMatrix);
        }

        public void CalcProjectionMatrix(out Matrix4 projection)
        {
            switch (ProjectionType)
            {
                case ProjectionType.Perspective:
                    Matrix4.CreatePerspectiveFieldOfView(
                        VerticalFov * Util.D2R,
                        Aspect, NearPlane, FarPlane, out projection);
                    break;
                case ProjectionType.Orthographic:
                    Matrix4.CreateOrthographic(
                        OrthograpicSize * Aspect, OrthograpicSize,
                        0, FarPlane, out projection);
                    break;
                default:
                    throw new Exception();
            }
        }

        /*public Ray RayFromPixel(Vector2 pixel, Vector2i resolution)
        {
            Vector3 pixelVec = new Vector3(pixel.X, resolution.Y - pixel.Y, NearPlane);
            CalcViewProjection(out var vp);
            vp.Invert();
            var res = Vector3.Unproject(pixelVec, 0, 0, resolution.X, resolution.Y, NearPlane, FarPlane, vp);
            var pos = Transform.WorldPosition;
            var direction = Vector3.Normalize(res - pos);
            return new Ray(pos, direction);
        }*/
    }
}