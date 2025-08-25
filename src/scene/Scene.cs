using System;
using System.Collections.Generic;
using System.Numerics;

namespace RayTracer
{
    /// <summary>
    /// Class to represent a ray traced scene, including the objects,
    /// light sources, and associated rendering logic.
    /// </summary>
    public class Scene
    {
        private SceneOptions options;
        private Camera camera;
        private Color ambientLightColor;
        private ISet<SceneEntity> entities;
        private ISet<PointLight> lights;
        private ISet<Animation> animations;

        private struct CameraParams
        {
            public readonly double d;
            public readonly double halfW;
            public readonly double halfH;
            public CameraParams(double d, double halfW, double halfH)
            { this.d = d; this.halfW = halfW; this.halfH = halfH; }
        }

        /// <summary>
        /// Construct a new scene with provided options.
        /// </summary>
        /// <param name="options">Options data</param>
        public Scene(SceneOptions options = new SceneOptions())
        {
            this.options = options;
            this.camera = new Camera(Transform.Identity);
            this.ambientLightColor = new Color(0, 0, 0);
            this.entities = new HashSet<SceneEntity>();
            this.lights = new HashSet<PointLight>();
            this.animations = new HashSet<Animation>();
        }

        /// <summary>
        /// Set the camera for the scene.
        /// </summary>
        /// <param name="camera">Camera object</param>
        public void SetCamera(Camera camera)
        {
            this.camera = camera;
        }

        /// <summary>
        /// Set the ambient light color for the scene.
        /// </summary>
        /// <param name="color">Color object</param>
        public void SetAmbientLightColor(Color color)
        {
            this.ambientLightColor = color;
        }

        /// <summary>
        /// Add an entity to the scene that should be rendered.
        /// </summary>
        /// <param name="entity">Entity object</param>
        public void AddEntity(SceneEntity entity)
        {
            this.entities.Add(entity);
        }

        /// <summary>
        /// Add a point light to the scene that should be computed.
        /// </summary>
        /// <param name="light">Light structure</param>
        public void AddPointLight(PointLight light)
        {
            this.lights.Add(light);
        }

        /// <summary>
        /// Add an animation to the scene.
        /// </summary>
        /// <param name="animation">Animation object</param>
        public void AddAnimation(Animation animation)
        {
            this.animations.Add(animation);
        }

        /// <summary>
        /// Render the scene to an output image. This is where the bulk
        /// of your ray tracing logic should go... though you may wish to
        /// break it down into multiple functions as it gets more complex!
        /// </summary>
        /// <param name="outputImage">Image to store render output</param>
        /// <param name="time">Time since start in seconds</param>
        public void Render(Image outputImage, double time = 0)
        {
            // Begin writing your code here...

            // Stage 1.1 set background color to white
            // for(int y = 0; y < outputImage.Height; y++)
            // {
            //     for(int x = 0; x < outputImage.Width; x++)
            //     {
            //         outputImage.SetPixel(x, y, new Color(1, 1, 1));
            //     }
            // }

            // Stage 1.5 - Ray tracing with object intersection
            int width = outputImage.Width, height = outputImage.Height;

            // Build camera parameters
            var cam = BuildCameraParams(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Make a primary ray for the current pixel
                    var ray = MakePrimaryRay(x, y, width, height, cam);

                    // trace the ray, starting with the depth 0
                    Color pixelColor = TraceRayWhitted(ray, 0);

                    // ensure the color is in the valid range.
                    pixelColor = ClampColor(pixelColor);
                    outputImage.SetPixel(x, y, pixelColor);
                }
            }
        }

        /// <summary>
        /// Build camera parameters
        /// </summary>
        /// <param name="image">Image to store render output</param>
        /// <returns>Camera parameters</returns>
        private CameraParams BuildCameraParams(int width, int height)
        {
            const double FOVxDeg = 60.0;
            double FOVx = Math.PI * FOVxDeg / 180.0;
            double d = 1.0;
            double halfW = Math.Tan(FOVx / 2.0) * d;
            double aspect = (double)width / (double)height;
            double halfH = halfW / aspect;
            return new CameraParams(d, halfW, halfH);
        }

        /// <summary>
        /// Make a primary ray for a given pixel
        /// </summary>
        /// <param name="x">X coordinate of the pixel</param>
        /// <param name="y">Y coordinate of the pixel</param>
        /// <param name="width">Width of the image</param>
        /// <param name="height">Height of the image</param>
        /// <param name="cam">Camera parameters</param>
        /// <returns>Primary ray</returns>
        private Ray MakePrimaryRay(int x, int y, int width, int height, CameraParams cam)
        {
            // Convert pixel coordinates to normalized device coordinates [0,1]
            double u = (x + 0.5) / (double)width;
            double v = (y + 0.5) / (double)height;

            // Convert NDC to camera space
            double sx = (2.0 * u - 1.0) * cam.halfW;
            double sy = (1.0 - 2.0 * v) * cam.halfH;

            // Convert camera space to world space
            var origin = new Vector3(0, 0, 0);
            var direction = new Vector3(sx, sy, cam.d).Normalized();
            return new Ray(origin, direction);
        }

        /// <summary>
        /// Trace a ray through the scene using the Whitted-style ray tracing algorithm.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        private Color TraceRayWhitted(Ray ray, int depth = 0)
        {
            const int MAX_DEPTH = 8;

            // Terminate recursion if max depth is reached
            if (depth >= MAX_DEPTH)
            {
                return new Color(0, 0, 0);
            }

            // 1.Find the closest intersection
            RayHit closestHit = FindClosestIntersection(ray);

            // if no hit was found, return black
            if (closestHit == null)
            {
                return new Color(0, 0, 0);
            }

            // 2. Calculate local color (diffuse + specular + ambient + shadow)
            Color localColor = CalculateLocalIllumination(closestHit, ray);

            // 3. Calculate the global illumination effect (Reflection contribution + refraction contribution)
            Color globalColor = new Color(0, 0, 0);

            // 3.a Calculate Reflection contribution
            if (closestHit.Material.Reflectivity > 0.01)
            {
                Color reflectedColor = CalculateReflectionContribution(closestHit, ray, depth);
                globalColor += reflectedColor;
            }

            // Calculate refraction color
            if (closestHit.Material.Transmissivity > 0.01)
            {
                Color refractedColor = CalculateRefractionContribution(closestHit, ray, depth);
                globalColor += refractedColor;
            }

            // 4. Combine local and global color (Whitted Model)
            Color finalColor = localColor + globalColor;
            return finalColor;
        }

        /// <summary>
        /// Find the closest intersection of a ray with the scene.
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        private RayHit FindClosestIntersection(Ray ray)
        {
            RayHit closestHit = null;
            double closestDistance = double.MaxValue;

            foreach (SceneEntity entity in this.entities)
            {
                RayHit hit = entity.Intersect(ray);
                if (hit != null)
                {
                    double distance = (hit.Position - ray.Origin).Length();
                    if (distance < closestDistance && distance > 1e-9)
                    {
                        closestDistance = distance;
                        closestHit = hit;
                    }
                }
            }

            return closestHit;
        }

        /// <summary>
        /// Calculate the local illumination at a hit point.
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="ray"></param>
        /// <returns></returns>
        private Color CalculateLocalIllumination(RayHit hit, Ray ray)
        {
            Material material = hit.Material;
            Vector3 hitPoint = hit.Position;
            Vector3 normal = hit.Normal;
            Vector3 viewDirection = (-ray.Direction).Normalized();

            // 1. ambient light
            Color ambient = material.AmbientColor * this.ambientLightColor;

            // 2. initialize diffuse color and specular color
            Color directLighting = new Color(0, 0, 0);

            // 3. calculate lighting for each light source (with shadow)
            foreach (PointLight light in this.lights)
            {
                Vector3 lightDirection = (light.Position - hitPoint).Normalized();
                double lightDistance = (light.Position - hitPoint).Length();

                // shadow check
                bool inShadow = IsInShadow(hitPoint, light.Position, lightDistance, normal);

                if (!inShadow)
                {
                    // diffuse reflection calculation
                    double diffuseIntensity = Math.Max(0, normal.Dot(lightDirection));
                    Color diffuse = material.DiffuseColor * light.Color * diffuseIntensity;

                    // specular reflection 
                    Color specular = new Color(0, 0, 0);
                    if (diffuseIntensity > 0)
                    {
                        // calculate reflection direction: R = 2(N * L)N - L
                        Vector3 reflectionDirection = (2 * normal.Dot(lightDirection) * normal - lightDirection).Normalized();
                        double specularIntensity = Math.Max(0, reflectionDirection.Dot(viewDirection));
                        specularIntensity = Math.Pow(specularIntensity, material.Shininess);
                        specular = material.SpecularColor * light.Color * specularIntensity;
                    }

                    directLighting += diffuse + specular;
                }
            }

            // 4. combine all lighting components
            return ambient + directLighting;
        }

        /// <summary>
        /// Check if a point is in shadow of a light source.
        /// </summary>
        /// <param name="hitPoint"></param>
        /// <param name="lightPosition"></param>
        /// <param name="lightDistance"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        private bool IsInShadow(Vector3 hitPoint, Vector3 lightPosition, double lightDistance, Vector3 normal)
        {
            // calculate the direction from the hit point to the light source
            Vector3 shadowRayDirection = (lightPosition - hitPoint).Normalized();

            // slightly offset the starting point to avoid self-intersection issues.
            const double EPSILON = 1e-9;
            Vector3 shadowRayOrigin = hitPoint + normal * EPSILON;

            // if the light source is behind the face, return false
            if (normal.Dot(shadowRayDirection) <= 0) return false;

            // create a shadow ray
            Ray shadowRay = new Ray(shadowRayOrigin, shadowRayDirection);

            // check for intersection with any objects in the scene
            foreach (SceneEntity entity in this.entities)
            {
                RayHit shadowHit = entity.Intersect(shadowRay);
                if (shadowHit != null)
                {
                    double hitDistance = (shadowHit.Position - shadowRayOrigin).Length();

                    if (hitDistance < lightDistance - EPSILON * 2)
                    {
                        return true; // in shadow
                    }
                }

            }
            return false; // not in shadow
        }

        /// <summary>
        /// Calculate the reflection contribution at a hit point.
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="incomingRay"></param>
        /// <param name="currentDepth"></param>
        /// <returns></returns>
        private Color CalculateReflectionContribution(RayHit hit, Ray incomingRay, int currentDepth)
        {
            Vector3 hitPoint = hit.Position;
            Vector3 normal = hit.Normal;
            Vector3 incomingDirection = incomingRay.Direction;
            double reflectivity = hit.Material.Reflectivity;

            if (normal.Dot(incomingDirection) > 0)
            {
                normal = -normal; // flip normal if it's facing the wrong way
            }

            // calculate reflection direction: R = D - 2(D · N)N
            Vector3 reflectionDirection = incomingDirection - 2.0 * incomingDirection.Dot(normal) * normal;
            reflectionDirection = reflectionDirection.Normalized();

            // offset the starting point to avoid self-intersection issues.
            const double EPSILON = 1e-9;
            Vector3 reflectionOrigin = hitPoint + normal * EPSILON;

            // check if the reflection direction is below the surface
            if (reflectionDirection.Dot(normal) < 0)
            {
                // if the reflection direction is below the surface, return black
                return new Color(0, 0, 0);
            }

            // create a reflection ray
            Ray reflectionRay = new Ray(reflectionOrigin, reflectionDirection);

            // trace the reflection ray
            Color reflectedColor = TraceRayWhitted(reflectionRay, currentDepth + 1);

            // apply the reflectivity
            return reflectedColor * reflectivity;
        }

        /// <summary>
        /// Calculate the refraction contribution at a hit point.
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="incomingRay"></param>
        /// <param name="currentDepth"></param>
        /// <returns></returns>
        private Color CalculateRefractionContribution(RayHit hit, Ray incomingRay, int currentDepth)
        {
            Vector3 hitPoint = hit.Position;
            Vector3 normal = hit.Normal;
            Vector3 incomingDirection = incomingRay.Direction;
            double transmissivity = hit.Material.Transmissivity;
            double materialRefractiveIndex = hit.Material.RefractiveIndex;

            // determine if the ray is entering or exiting the material
            bool entering = incomingDirection.Dot(normal) < 0;

            double n1, n2; // refractive indices of the incident medium and the refracted medium
            Vector3 surfaceNormal = normal;

            if (entering)
            {
                // entering the material: air to material
                n1 = 1.0;
                n2 = materialRefractiveIndex;
            }
            else
            {
                // exiting the material: material to air
                n1 = materialRefractiveIndex;
                n2 = 1.0;
                surfaceNormal = -normal; // flip normal if exiting
            }

            // calculate refraction direction using Snell's law
            Vector3 refractionDirection;
            bool totalInternalReflection = false;

            if (!CalculateRefractionDirection(incomingDirection, surfaceNormal, n1, n2, out refractionDirection))
            {
                // total internal reflection occurs, reverting to reflection
                totalInternalReflection = true;
                refractionDirection = CalculateReflectionDirection(incomingDirection, surfaceNormal);

            }

            const double EPSILON = 1e-9;

            if (totalInternalReflection)
            {
                // for total internal reflection, offset along the normal
                Vector3 reflectionDirection = incomingDirection - surfaceNormal * (2.0 * incomingDirection.Dot(surfaceNormal));
                Vector3 refractionOrigin = hitPoint + surfaceNormal * EPSILON;
                Ray reflectionRay = new Ray(refractionOrigin, reflectionDirection.Normalized());

                Color totalInternalReflectedColor = TraceRayWhitted(reflectionRay, currentDepth + 1);
                return totalInternalReflectedColor * transmissivity;
            }
            else
            {
                // ordinary refraction, offset along the direction of refraction
                Vector3 refractionOrigin = hitPoint - surfaceNormal * EPSILON;
                Ray refractionRay = new Ray(refractionOrigin, refractionDirection);

                Color refractedColor = TraceRayWhitted(refractionRay, currentDepth + 1);
                return refractedColor * transmissivity;
            }
        }

        /// <summary>
        /// Calculate the refraction direction at a hit point.
        /// </summary>
        /// <param name="incident"></param>
        /// <param name="normal"></param>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <param name="refractedDirection"></param>
        /// <returns></returns>
        private bool CalculateRefractionDirection(Vector3 incident, Vector3 normal, double n1, double n2, out Vector3 refractedDirection)
        {
            refractedDirection = Vector3.Zero;

            double eta = n1 / n2; // refractive index ratio
            double cosI = -incident.Dot(normal); // cos(incident angle)

            // check for total internal reflection
            double discriminant = 1.0 - eta * eta * (1.0 - cosI * cosI);

            if (discriminant < 0)
            {
                // total internal reflection occurs, can't calculate refraction
                return false;
            }

            double cosT = Math.Sqrt(discriminant);
            refractedDirection = eta * incident + (eta * cosI - cosT) * normal;
            refractedDirection = refractedDirection.Normalized();

            return true;
        }

        /// <summary>
        /// Calculate the reflection direction at a hit point.
        /// </summary>
        /// <param name="incident"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        private Vector3 CalculateReflectionDirection(Vector3 incident, Vector3 normal)
        {
            return (incident - 2.0 * incident.Dot(normal) * normal).Normalized();
        }

        /// <summary>
        /// Calculate the clamped color value.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private Color ClampColor(Color color)
        {
            return new Color(
                Math.Max(0, Math.Min(1, color.R)),
                Math.Max(0, Math.Min(1, color.G)),
                Math.Max(0, Math.Min(1, color.B))
            );
        }
    }
}
