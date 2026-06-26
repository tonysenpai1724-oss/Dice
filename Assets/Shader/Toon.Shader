Shader "Custom/URP_CustomCubeLighting"
{
    Properties
    {
        _MainColor ("Base Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Core thư viện của URP thay cho UnityCG.cginc
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            float4 _MainColor;

            Varyings vert (Attributes input)
            {
                Varyings output;

                // Chuyển đổi vị trí đỉnh từ Object sang Clip Space
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;

                // Chuyển đổi Normal từ Object sang World Space
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;

                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // 1. Chuẩn hóa Vector pháp tuyến của Cube
                float3 N = normalize(input.normalWS);

                // 2. Lấy dữ liệu ánh sáng chính (Main Light) từ URP
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                half3 lightColor = mainLight.color;

                // 3. Tính toán cường độ sáng Diffuse (Lambert)
                float lightIntensity = max(0.0, dot(N, L));

                // 4. Cộng thêm một chút ánh sáng môi trường (Ambient) của URP
                half3 ambient = SampleSH(N) * 0.15;

                // 5. Kết hợp tất cả lại thành màu cuối cùng
                half3 finalColor = _MainColor.rgb * (lightColor * lightIntensity + ambient);

                return half4(finalColor, _MainColor.a);
            }
            ENDHLSL
        }
    }
}