Shader "Custom/RayTracing"
{
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // shader vars
            uniform float3 ViewParams;
            uniform float4x4 CamLocalToWorldMatrix;

            // shader structs
            struct Ray {
                float3 origin;
                float3 dir;
            };

            struct objMaterial {
                float4 colour;
                float4 emissionColour;
                float emissionStrength;
            };

            struct HitInfo {
                bool didHit;
                float distance;
                float3 hitPoint;
                float3 hitNormal;
                objMaterial material;
            };


            struct Sphere {
                float3 position;
                float radius;
                objMaterial material;
            };

            // buffers
            StructuredBuffer<Sphere> Spheres;
            int NumSpheres;

            HitInfo hitSphere(Ray ray, float3 sphereCentre, float sphereRadius)
            {
                // initialize a new HitInfo with default values
                HitInfo hitInfo = (HitInfo)0;

                // we offset the centre so spheres can be drawn away from origin.
                float3 offsetCentre = ray.origin - sphereCentre;

                // calculate sphere intersection quadratic values
                float a = dot(ray.dir, ray.dir);
                float b = 2.0 * dot(ray.dir, offsetCentre);
                float c = dot(offsetCentre , offsetCentre ) - sphereRadius * sphereRadius;
                float discriminant = b * b - 4 * a * c;

                // if discriminant < 0 then the ray missed, hitInfo stays empty.
                if (discriminant >= 0) {
                    
                    // distance to nearest sphere ray intersection
                    float distance = (-b - sqrt(discriminant)) / (2 * a);
                    
                    // update hitinfo if sphere intersection is forward facing
                    if (distance >= 0) {
                        hitInfo.didHit = true;
                        hitInfo.distance = distance;
                        hitInfo.hitPoint = ray.origin + ray.dir * distance;
                        hitInfo.hitNormal = -normalize(hitInfo.hitPoint - sphereCentre);
                    }
                    

                }

                return hitInfo;

            }



            HitInfo hitGround(Ray ray) {

                HitInfo hitInfo = (HitInfo)0;

                // Calculate ray distance to infinite ground at y=0
                float distance = -ray.origin.y / ray.dir.y;
                if (distance > 0 && distance < 1.#INF) {
                    hitInfo.didHit = true;
                    hitInfo.distance = distance;
                    hitInfo.hitPoint = ray.origin + distance * ray.dir;
                    hitInfo.hitNormal = float3(0,1,0); // Pointing up in the y axis.
                }

                return hitInfo;
            }

            HitInfo CalculateRayCollisions(Ray ray)
            {
                HitInfo closestHit = (HitInfo)0;
                // assume closesthit is a miss (infinitely far)
                closestHit.distance = 1.#INF;

                for (int i = 0; i < NumSpheres; i++) {
                    Sphere sphere = Spheres[i];
//🤷‍♂️
                    HitInfo hitInfo = hitSphere(ray, sphere.position, sphere.radius);

                    if (hitInfo.didHit && hitInfo.distance < closestHit.distance) {
                        closestHit =  hitInfo;
                        closestHit.material = sphere.material;
                    }
                }

                return closestHit;
            }

            // handle random ray bounces for diffuse reflection

            float RandomValue(inout uint state)
            {
                state = state * 747796405 + 2891336453;
                uint result = ((state >> ((state >> 28) + 4)) ^ state) * 277803737;
                result = (result >> 22) ^ result;
                return result / 4294967295;
            }

            float RandomValueNormalDistribution(inout uint state)
            {
                float theta = 2 * 3.1415926 * RandomValue(state);
                float rho = sqrt(-2 * log(RandomValue(state)));
                return rho * cos(theta);
            }

            // randomise direction via spherically symetric normal distribution
            float3 RandomDirection(inout uint state)
            {
                float x = RandomValueNormalDistribution(state);
                float y = RandomValueNormalDistribution(state);
                float z = RandomValueNormalDistribution(state);
                return normalize(float3(x,y,z));

            }

            // ensure random directions are exiting sphere not entering
            float3 RandomHemisphereDirection(float3 normal, inout uint rngState)
            {
                float3 dir = RandomDirection(rngState);
                return dir * sign(dot(normal, dir));
            }

            float3 Trace(Ray ray, inout uint rngState)
            {

                float3 incomingLight = 0;
                // light is initially white
                float3 rayColour = 1;

                float MaxBounceCount = 1;

                // trace rays as they reflect around the scene.
                for (int i = 0; i <= MaxBounceCount; i++) {
                    HitInfo hitInfo = CalculateRayCollisions(ray);
                    if (hitInfo.didHit) {

                        ray.origin = hitInfo.hitPoint;
                        ray.dir = RandomHemisphereDirection(hitInfo.hitNormal, rngState);

                        objMaterial material = hitInfo.material;
                        // for calculate colour of emmitted light
                        float3 emittedLight = material.emissionColour * material.emissionStrength;
                        incomingLight += emittedLight * rayColour;
                        rayColour *= material.colour;
                    }
                    else {
                        break;
                    }
                }
                return incomingLight;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // initialize camera perspective in scene
                float3 viewPointLocal = float3(i.uv - 0.5, 1) * ViewParams;
                float3 viewPoint = mul(CamLocalToWorldMatrix, float4(viewPointLocal, 1));

                // initalize ray and ray properties for each pixel
                Ray ray;
                ray.origin = _WorldSpaceCameraPos;
                ray.dir = normalize(viewPoint - ray.origin);

                // calculate random diffuse reflection seed
                uint2 numPixels = _ScreenParams.xy;
                uint2 pixelCoord = i.uv * numPixels;
                uint pixelIndex = pixelCoord.y * numPixels.x + pixelCoord.x;
                uint rngState = pixelIndex;


                // initial tracing output
                float3 pixelColour = Trace(ray, rngState);
                return float4(pixelColour, 1);

            }
            ENDCG
        }
    }
}
