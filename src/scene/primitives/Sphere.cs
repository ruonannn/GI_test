using System;

namespace RayTracer
    {
    /// <summary>
    /// Class to represent an (infinite) plane in a scene.
    /// </summary>
    public class Sphere : SceneEntity
    {
        private Vector3 center;
        private double radius;
        private Material material;

        /// <summary>
        /// Construct a sphere given its center point and a radius.
        /// </summary>
        /// <param name="center">Center of the sphere</param>
        /// <param name="radius">Radius of the spher</param>
        /// <param name="material">Material assigned to the sphere</param>
        public Sphere(Vector3 center, double radius, Material material)
        {
            this.center = center;
            this.radius = radius;
            this.material = material;
        }

        /// <summary>
        /// Determine if a ray intersects with the sphere, and if so, return hit data.
        /// </summary>
        /// <param name="ray">Ray to check</param>
        /// <returns>Hit data (or null if no intersection)</returns>
        public RayHit Intersect(Ray ray)
        {
            // calculate the intersection of the ray with the sphere
            // ray equation: P = O + tD (O=origin, D=direction, t=parameter)
            // sphere equation: |P - C|² = r² (C=center, r=radius)
            // substitute: |O + tD - C|² = r²            
            var ro = ray.Origin;
            var rd = ray.Direction;

            // a = rd . rd
            double a = rd.Dot(rd);

            // b = 2 * rd . (ro - center)
            double b = 2 * rd.Dot(ro - center);

            // c = (ro - center) . (ro - center) - radius^2
            double c = (ro - center).Dot(ro - center) - radius * radius;

            // discriminant = b^2 - 4ac
            double discriminant = b * b - 4 * a * c;

            if (discriminant < 0)
            {
                // no intersection
                return null;
            }

            // calculate the two intersection points
            double t0 = (-b - Math.Sqrt(discriminant)) / (2 * a);
            double t1 = (-b + Math.Sqrt(discriminant)) / (2 * a);

            // calculate the closest intersection point
            double t = -1;
            const double EPSILON = 1e-9;    
            if(t0 > EPSILON && t1 > EPSILON)
            {
                t = Math.Min(t0, t1);
            }
            else if(t0 > EPSILON)
            {
                t = t0;
            }
            else if(t1 > EPSILON)
            {
                t = t1;
            }

            // if the intersection point is behind the camera, return null
            if(t <= EPSILON)
            {
                return null;
            }

            // calculate the hit point and normal
            Vector3 hitPoint = ray.Origin + ray.Direction * t;
            Vector3 normal = (hitPoint - center).Normalized();

            return new RayHit(hitPoint, normal, ray.Direction, material);
        }

        /// <summary>
        /// The material of the sphere.
        /// </summary>
        public Material Material { get { return this.material; } }
    }

}
