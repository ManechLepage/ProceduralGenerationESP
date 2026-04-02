// Shader generated with the assistance of Claude AI (Anthropic)
Shader "Custom/UI/GreyscaleEXR"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.worldPos = v.vertex;
                o.uv       = v.uv;
                o.color    = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample only red channel, output as greyscale
                float r = tex2D(_MainTex, i.uv).r;
                fixed4 col = fixed4(r, r, r, 1.0);

                // Multiply by vertex color (carries RawImage alpha/tint)
                col *= i.color;

                // Apply RectMask2D clipping
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                return col;
            }
            ENDCG
        }
    }
}