Shader "Custom/LowPolyPlanet"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        [Header(Sun Lighting)]
        _SunColor ("Sun Color", Color) = (1,1,1,1)
        _AmbientMin ("Ambient Minimum", Range(0,1)) = 0.2
        _ToonSteps ("Toon Shading Steps", Range(1,10)) = 5

        [Header(Emission)]
        [Toggle] _UseEmissionOnly ("Use Emission Only", Float) = 0
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _EmissionStrength ("Emission Strength", Range(0,10)) = 1

        [Header(Eclipse Shadows)]
        [Toggle] _EnableEclipseShadows ("Enable Eclipse Shadows", Float) = 1
        _ShadowDarkness ("Shadow Darkness", Range(0,1)) = 0.25
        _EclipsePenumbra ("Eclipse Penumbra", Float) = 6.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"

            // --------------------------------------------------
            // Properties
            // --------------------------------------------------
            float4 _BaseColor;
            float4 _SunDirection;   // w is unused; xyz = direction toward sun
            float4 _SunColor;
            float  _AmbientMin;
            float  _ToonSteps;

            float  _UseEmissionOnly;
            float4 _EmissionColor;
            float  _EmissionStrength;

            sampler2D _MainTex;
            float4    _MainTex_ST;

            // Eclipse variables
            float _EnableEclipseShadows;
            float _ShadowDarkness;
            float _EclipsePenumbra;

            // Global variables for eclipse shadows (set via script)
            uniform float4 _EclipseShadowCasters[32];
            uniform int _EclipseShadowCasterCount;
            uniform float4 _EclipseSunPosition;

            // --------------------------------------------------
            // Structs
            // --------------------------------------------------
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex    : SV_POSITION;
                float2 uv        : TEXCOORD0;
                // nointerpolation keeps the flat low-poly look —
                // the value is taken from the provoking vertex only.
                nointerpolation float4 color       : COLOR;
                nointerpolation float3 worldNormal : TEXCOORD1;
            };

            // --------------------------------------------------
            // Vertex
            // --------------------------------------------------
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex      = UnityObjectToClipPos(v.vertex);
                o.uv          = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);

                // Start with original vertex color
                float4 vertexColor = v.color;

                if (_EnableEclipseShadows > 0.5)
                {
                    float3 vertexWorldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    float3 sunPos = _EclipseSunPosition.xyz;
                    float3 toVertex = vertexWorldPos - sunPos;
                    float toVertexSqrMag = dot(toVertex, toVertex);
                    
                    float maxShadowFactor = 0.0;
                    float3 targetCenter = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;
                    
                    for (int j = 0; j < _EclipseShadowCasterCount; j++)
                    {
                        float4 caster = _EclipseShadowCasters[j]; // xyz = pos, w = radius
                        float3 pCenter = caster.xyz;
                        
                        // Self-exclusion check: don't let a planet cast an eclipse shadow on itself
                        float3 diff = pCenter - targetCenter;
                        if (dot(diff, diff) < 1.0) continue;

                        float pRadius = caster.w;
                        
                        // Vector from sun to shadow caster center
                        float3 toCaster = pCenter - sunPos;
                        
                        // Projection parameter of caster center onto the ray from sun to vertex
                        float u = dot(toCaster, toVertex) / (toVertexSqrMag + 0.0001);
                        
                        // We check if the caster is between the sun and the vertex
                        if (u > 0.0 && u < 1.0)
                        {
                            float3 closestPoint = sunPos + toVertex * u;
                            float3 offset = pCenter - closestPoint;
                            float distSq = dot(offset, offset);
                            
                            float rMinSq = pRadius * pRadius;
                            
                            if (distSq < rMinSq)
                            {
                                maxShadowFactor = 1.0;
                            }
                            else if (_EclipsePenumbra > 0.001)
                            {
                                float rMax = pRadius + _EclipsePenumbra;
                                float rMaxSq = rMax * rMax;
                                if (distSq < rMaxSq)
                                {
                                    float dist = sqrt(distSq);
                                    float shadowFactor = 1.0 - ((dist - pRadius) / _EclipsePenumbra);
                                    if (shadowFactor > maxShadowFactor)
                                    {
                                        maxShadowFactor = shadowFactor;
                                    }
                                }
                            }
                        }
                    }
                    
                    float shadowMultiplier = 1.0 - maxShadowFactor * (1.0 - _ShadowDarkness);
                    vertexColor.rgb *= shadowMultiplier;
                }

                o.color = vertexColor;
                return o;
            }

            // --------------------------------------------------
            // Fragment
            // --------------------------------------------------
            fixed4 frag (v2f i) : SV_Target
            {
                // ---- Emission-only mode (e.g. star / sun mesh) ----
                if (_UseEmissionOnly > 0.5)
                {
                    // Clamp strength to [0,10] so it can't blow out
                    // uncontrollably; use HDR-safe gamma-correct multiply.
                    float3 emission = _EmissionColor.rgb * clamp(_EmissionStrength, 0.0, 10.0);
                    return float4(emission, 1.0);
                }

                // ---- Diffuse lighting ----
                // _SunDirection.xyz is expected to point FROM the surface TOWARD the sun.
                float3 lightDir  = normalize(_SunDirection.xyz);
                float3 normal    = normalize(i.worldNormal);

                float  NdotL     = dot(normal, lightDir);           // [-1 .. 1]
                float  NdotL01   = max(NdotL, 0.0);                 // [0  .. 1]

                // Toon / stepped shading — number of steps driven by property
                float steps      = max(1.0, _ToonSteps);
                float toon       = floor(NdotL01 * steps + 0.5) / steps;

                // Clamp to ambient floor so dark side is never pitch black
                float light      = max(toon, _AmbientMin);

                // ---- Base color = vertex color × material color × texture ----
                float4 texColor  = tex2D(_MainTex, i.uv);
                float4 baseColor = i.color * _BaseColor * texColor;

                // ---- Tint by sun color ----
                float3 finalColor = baseColor.rgb * light * _SunColor.rgb;

                return float4(finalColor, 1.0);
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
