Shader "Custom/TrueFlatIsometricCube"
{
    Properties
    {
        _MainTex ("Number Texture", 2D) = "white" {}
        _TopColor ("Top/Base Color", Color) = (0.95, 0.95, 0.95, 1)
        _LeftColor ("Left Face Shade", Color) = (0.82, 0.82, 0.86, 1)
        _RightColor ("Right Face Shade", Color) = (0.66, 0.66, 0.72, 1)
        _LineColor ("Number/Line Color", Color) = (0.2, 0.15, 0.25, 1)
        _ShadeInset ("White Edge Size", Range(0, 0.45)) = 0.035
        _ShadeSoftness ("Shade Edge Softness", Range(0.001, 0.08)) = 0.001
        _CubeHalfSize ("Cube Half Size", Range(0.1, 2)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 localPos : TEXCOORD1;
                float3 localNormal : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TopColor;
            fixed4 _LeftColor;
            fixed4 _RightColor;
            fixed4 _LineColor;
            float _ShadeInset;
            float _ShadeSoftness;
            float _CubeHalfSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex.xyz;
                o.localNormal = v.normal;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float3 SnapCubeNormal(float3 normal)
            {
                float3 normalAbs = abs(normal);

                if (normalAbs.y >= normalAbs.x && normalAbs.y >= normalAbs.z)
                {
                    return float3(0, sign(normal.y), 0);
                }

                if (normalAbs.x >= normalAbs.z)
                {
                    return float3(sign(normal.x), 0, 0);
                }

                return float3(0, 0, sign(normal.z));
            }

            float2 GetFaceUV(float3 localPos, float3 cubeNormal)
            {
                float3 normalizedPos = localPos / max(_CubeHalfSize * 2.0, 0.0001) + 0.5;

                if (abs(cubeNormal.y) > 0.5)
                {
                    return normalizedPos.xz;
                }

                if (abs(cubeNormal.x) > 0.5)
                {
                    return normalizedPos.zy;
                }

                return normalizedPos.xy;
            }

            fixed4 GetShadeColor(float3 cubeNormal)
            {
                if (abs(cubeNormal.y) > 0.5)
                {
                    return _TopColor;
                }

                if (cubeNormal.z > 0.5 || cubeNormal.x < -0.5)
                {
                    return _LeftColor;
                }

                return _RightColor;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 cubeNormal = SnapCubeNormal(normalize(i.localNormal));
                fixed4 shadeColor = GetShadeColor(cubeNormal);

                float2 faceUV = saturate(GetFaceUV(i.localPos, cubeNormal));
                float2 edgeDistance = min(faceUV, 1.0 - faceUV);
                float shadeMask = smoothstep(_ShadeInset, _ShadeInset + _ShadeSoftness, edgeDistance.x) *
                                  smoothstep(_ShadeInset, _ShadeInset + _ShadeSoftness, edgeDistance.y);

                fixed4 faceColor = lerp(_TopColor, shadeColor, shadeMask);

                fixed4 tex = tex2D(_MainTex, i.uv);
                float numberMask = (1.0 - tex.r) * tex.a;

                return lerp(faceColor, _LineColor, numberMask);
            }
            ENDCG
        }
    }
}
