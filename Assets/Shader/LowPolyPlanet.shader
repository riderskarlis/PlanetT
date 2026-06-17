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
                o.color       = v.color;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
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
