Shader "Unlit/ImageFade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _StartColor ("Start Color", Color) = (1,1,1,1)
        _EndColor ("End Color", Color) = (0,0,0,1)

        _FadeStart ("Fade Start", Range(0,1)) = 0
        _FadeEnd ("Fade End", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            sampler2D _MainTex;

            float4 _StartColor;
            float4 _EndColor;

            float _FadeStart;
            float _FadeEnd;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 texColor = tex2D(_MainTex, i.uv);

                // Chuyển đổi sang Linear space để gradient màu pha trộn chuẩn và mịn hơn
                float3 startRGB = GammaToLinearSpace(_StartColor.rgb);
                float3 endRGB = GammaToLinearSpace(_EndColor.rgb);

                float3 gradientRGB = lerp(startRGB, endRGB, i.uv.x);
                float gradientAlpha = lerp(_StartColor.a, _EndColor.a, i.uv.x);

                // Tạo hiệu ứng fade mượt theo chiều ngang (smoothstep)
                float fade = 1.0 - smoothstep(_FadeStart, _FadeEnd, i.uv.x);

                // Áp dụng fade vào cả alpha lẫn RGB để tránh viền tối cứng nhắc
                gradientAlpha *= fade;
                gradientRGB *= fade;

                // Chuyển trả về Gamma space để hiển thị đúng trên màn hình
                gradientRGB = LinearToGammaSpace(gradientRGB);

                float4 finalColor = float4(gradientRGB, gradientAlpha);

                // Kết hợp với Alpha của Sprite/Image và màu Vertex của UI
                finalColor.a *= texColor.a;
                finalColor *= i.color;

                return finalColor;
            }

            ENDCG
        }
    }
}