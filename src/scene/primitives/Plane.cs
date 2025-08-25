using System;
using System.Numerics;

namespace RayTracer
{
    /// <summary>
    /// Class to represent an (infinite) plane in a scene.
    /// </summary>
    public class Plane : SceneEntity
    {
        private Vector3 center;
        private Vector3 normal;
        private Material material;

        /// <summary>
        /// Construct an infinite plane object.
        /// </summary>
        /// <param name="center">Position of the center of the plane</param>
        /// <param name="normal">Direction that the plane faces</param>
        /// <param name="material">Material assigned to the plane</param>
        public Plane(Vector3 center, Vector3 normal, Material material)
        {
            this.center = center;
            this.normal = normal.Normalized();
            this.material = material;
        }

        /// <summary>
        /// Determine if a ray intersects with the plane, and if so, return hit data.
        /// </summary>
        /// <param name="ray">Ray to check</param>
        /// <returns>Hit data (or null if no intersection)</returns>
        public RayHit Intersect(Ray ray)
        {
            // calculate the intersection of the ray with the plane
            // plane equation: (P - C) · N = 0 (P=point, C=center, N=normal)
            // ray equation: P = O + tD (O=origin, D=direction, t=parameter)
            // substitute: (O + tD - C) · N = 0
            // solve for t: t = - (O - C) · N / (D · N)            
            
            double denominator = ray.Direction.Dot(this.normal);

            // check if the ray is parallel to the plane
            if(Math.Abs(denominator) < 1e-8)
            {
                // no intersection
                return null;
            }

            Vector3 oc = this.center - ray.Origin;
            double t = oc.Dot(this.normal) / denominator;

            // check if the intersection point is behind the camera
            const double EPSILON = 1e-6;
            if(t <= EPSILON)
            {
                // no intersection
                return null;
            }

            // calculate the hit point and normal
            Vector3 hitPoint = ray.Origin + ray.Direction * t;
            Vector3 normal = this.normal;
            if(ray.Direction.Dot(normal) > 0)
            {
                normal = -normal;
            }

            return new RayHit(hitPoint, normal, ray.Direction, material);
        }

        /// <summary>
        /// The material of the plane.
        /// </summary>
        public Material Material { get { return this.material; } }
    }

}
