Shader "Custom/URP/DiceOutline"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _LineColor ("Line Color", Color) = (0, 0, 0, 1)
        _Thickness ("Thickness", Range(0.001, 0.1)) = 0.02
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 bary : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 bary : TEXCOORD0;
            };

            float4 _BaseColor;
            float4 _LineColor;
            float _Thickness;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.bary = IN.bary;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float edge = min(IN.bary.x, min(IN.bary.y, IN.bary.z));

                if(edge < _Thickness)
                    return _LineColor;

                return _BaseColor;
            }
            ENDHLSL
        }
    }
}