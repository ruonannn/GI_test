using System;
using System.Collections.Generic;

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

                    // Find the closest intersection
                    Color pixelColor = TraceRay(ray);
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
            double aspect = (double)width / height;
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
            double u = (x + 0.5) / width;
            double v = (y + 0.5) / height;

            // Convert NDC to camera space
            double sx = (2.0 * u - 1.0) * cam.halfW;
            double sy = (1.0 - 2.0 * v) * cam.halfH;

            // Convert camera space to world space
            var origin = new Vector3(0, 0, 0);
            var direction = new Vector3(sx, sy, cam.d).Normalized();
            return new Ray(origin, direction);
        }

        /// <summary>
        /// Trace a ray through the scene
        /// </summary>
        /// <param name="ray">Ray to trace</param>
        /// <returns>Color of the pixel</returns>
        private Color TraceRay(Ray ray)
        {
            RayHit closestHit = null;
            double closestT = double.MaxValue;

            // check intersection with all entities in the scene
            foreach (SceneEntity entity in this.entities)
            {
                RayHit hit = entity.Intersect(ray);
                if (hit != null)
                {
                    // calculate distance from ray origin to hit point
                    double t = (hit.Position - ray.Origin).Length();

                    // Keep trace of the closet hit
                    if (t < closestT)
                    {
                        closestT = t;
                        closestHit = hit;
                    }
                }
            }

            if (closestHit == null)
            {
                return new Color(0, 0, 0);
            }

            return CalculateLighting(closestHit, ray);
        }
        
        private Color CalculateLighting(RayHit hit, Ray ray)
        {
            Material material = hit.Material;
            Vector3 hitPoint = hit.Position;
            Vector3 normal = hit.Normal;
            Vector3 viewDirection = (-ray.Direction).Normalized(); 

            // 1. ambient light
            Color ambient = material.AmbientColor * this.ambientLightColor;

            // 2. initialize diffuse color and specular color
            Color diffuse = new Color(0, 0, 0);
            Color specular = new Color(0, 0, 0);

            // 3. calculate lighting for each light source
            foreach(PointLight light in this.lights) 
            {
                Vector3 lightDirection = (light.Position - hitPoint).Normalized();

                // diffuse reflection calculation
                double diffuseIntensity = Math.Max(0, normal.Dot(lightDirection));

                Color diffuseContribution = material.DiffuseColor * light.Color * diffuseIntensity;
                diffuse += diffuseContribution;

                // specular reflection calculation
                if(diffuseIntensity > 0)
                {   
                    // calculate reflection direction: R = 2(N * L)N - L
                    Vector3 reflectionDirection = (2 * normal.Dot(lightDirection) * normal - lightDirection).Normalized();
                    double specularIntensity = Math.Max(0, reflectionDirection.Dot(viewDirection));
                    specularIntensity = Math.Pow(specularIntensity, material.Shininess);

                    Color specularContribution = material.SpecularColor * light.Color * specularIntensity;
                    specular += specularContribution;
                }

            }

            // 4. combine all lighting components
            Color finalColor = ambient + diffuse + specular;
            return finalColor;
        }
    }
}
