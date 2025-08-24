using System;

namespace RayTracer
{
    /// <summary>
    /// Class to represent a triangle in a scene represented by three vertices.
    /// </summary>
    public class Triangle : SceneEntity
    {
        private Vector3 v0, v1, v2;
        private Material material;

        /// <summary>
        /// Construct a triangle object given three vertices.
        /// </summary>
        /// <param name="v0">First vertex position</param>
        /// <param name="v1">Second vertex position</param>
        /// <param name="v2">Third vertex position</param>
        /// <param name="material">Material assigned to the triangle</param>
        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2, Material material)
        {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;
            this.material = material;
        }

        /// <summary>
        /// Determine if a ray intersects with the triangle, and if so, return hit data.
        /// </summary>
        /// <param name="ray">Ray to check</param>
        /// <returns>Hit data (or null if no intersection)</returns>
        public RayHit Intersect(Ray ray)
        {
            // use the Moller-Trumbore algorithm to check for intersection
            // reference: https://en.wikipedia.org/wiki/Möller–Trumbore_intersection_algorithm

            const double EPSILON = 1e-9;

            Vector3 e1 = v1 - v0;
            Vector3 e2 = v2 - v0;
            Vector3 h = ray.Direction.Cross(e2);
            double a = e1.Dot(h);

            // check if the ray is parallel to the triangle
            if (a > -EPSILON && a < EPSILON)
            {
                // no intersection
                return null;
            }

            double f = 1.0 / a;
            Vector3 s = ray.Origin - v0;
            double u = f * s.Dot(h);
            if (u < 0.0 || u > 1.0)
            {
                return null;
            }

            Vector3 q = s.Cross(e1);
            double v = f * ray.Direction.Dot(q);
            if (v < 0.0 || u + v > 1.0)
            {
                return null;
            }
            
            double t = f * e2.Dot(q);
            if (t > EPSILON) // check if the intersection point is behind the camera
            {
                Vector3 hitPoint = ray.Origin + ray.Direction * t;
                Vector3 normal = e1.Cross(e2).Normalized();
                if(normal.Dot(ray.Direction) > 0)
                {
                    normal = -normal;
                }
                return new RayHit(hitPoint, normal, ray.Direction, material);
            }
            else
            {
                // the line segments intersect but the rays do not
                return null;
            }
        }

        /// <summary>
        /// The material of the triangle.
        /// </summary>
        public Material Material { get { return this.material; } }
    }
}
