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

            struct Ray {
                float3 origin;
                float3 dir;
            };

            struct HitInfo {
                bool didHit;
                float distance;
                float3 hitPoint;
                float3 hitNormal;
            };

            HitInfo hitSphere(Ray ray, float3 sphereCentre, float sphereRadius)
            {
                // initialize a new HitInfo with default values
                HitInfo hitInfo = (HitInfo)0;

                // we offset the centre so spheres can be drawn away from origin.
                float3 offsetCentre = ray.origin - sphereCentre;

                // calculate sphere intersection quadratic values
                float a = dot(ray.dir, ray.dir);
                float b = 2.0 * dot(ray.dir, ray.origin);
                float c = dot(ray.origin, ray.origin) - sphereRadius * sphereRadius;
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
                        hitInfo.hitNormal = normalize(hitInfo.hitPoint - sphereCentre);
                    }
                    

                }

                return hitInfo;

            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewPointLocal = float3(i.uv - 0.5, 1) * ViewParams;
                float3 viewPoint = mul(CamLocalToWorldMatrix, float4(viewPointLocal, 1));

                Ray ray;
                ray.origin = _WorldSpaceCameraPos;
                ray.dir = normalize(viewPoint - ray.origin);
                return float4(hitSphere(ray, float3(2,1,0.5), 0.5).didHit,0,0,0);

            }
            ENDCG
        }
    }
}
