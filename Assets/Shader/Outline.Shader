Shader "Custom/InvertedHullOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _Thickness ("Outline Thickness", Float) = 0.01
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+1"
        }

        Pass
        {
            Name "Outline"
            // Bước sống còn: Chỉ vẽ mặt sau, ẩn mặt trước
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float4 _OutlineColor;
            float _Thickness;

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Đẩy các đỉnh dọc theo hướng Normal (vector pháp tuyến) để làm dày viền
                float3 extrudedPositionOS = input.positionOS.xyz + (input.normalOS * _Thickness);

                // Chuyển không gian sang Clip Space để hiển thị lên màn hình
                output.positionCS = TransformObjectToHClip(extrudedPositionOS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Trả về một màu phẳng duy nhất cho đường viền
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}