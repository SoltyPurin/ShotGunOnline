Shader "UI/ModernMetallicButton"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Metallic Gradient)]
        _GradTopColor ("Gradient Top Multiplier", Color) = (1, 1, 1, 1)
        _GradBottomColor ("Gradient Bottom Multiplier", Color) = (1, 1, 1, 1)

        [Header(Diagonal Shine)]
        _ShineColor ("Shine Color", Color) = (1.0, 0.88, 0.58, 1)
        _ShineProgress ("Shine Progress", Range(-0.5, 1.5)) = -0.5
        _ShineWidth ("Shine Width", Range(0.02, 0.5)) = 0.18
        _ShineIntensity ("Shine Intensity", Range(0, 2)) = 0.0

        [Header(Edge Rim Glow)]
        _RimColor ("Rim Color", Color) = (0.0, 0.0, 0.0, 1)
        _RimIntensity ("Rim Intensity", Range(0, 1)) = 0.0
        _RimThickness ("Rim Thickness (UV)", Range(0.005, 0.15)) = 0.035

        [Header(Focus Color Boost)]
        _FocusAdd ("Focus Self-Add", Range(0, 1)) = 0.0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            fixed4 _GradTopColor;
            fixed4 _GradBottomColor;
            fixed4 _ShineColor;
            float _ShineProgress;
            float _ShineWidth;
            float _ShineIntensity;
            fixed4 _RimColor;
            float _RimIntensity;
            float _RimThickness;
            float _FocusAdd;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // 縦方向のメタリックグラデーション
                fixed4 grad = lerp(_GradBottomColor, _GradTopColor, saturate(IN.texcoord.y));
                color.rgb *= grad.rgb;

                // 元の明るさ（輝度）を判定：暗いボタンは加算して鮮やかに、白い/明るいボタンは少し暗くして差別化
                float luma = dot(color.rgb, float3(0.299, 0.587, 0.114));
                float brightMask = smoothstep(0.60, 0.90, luma);

                // 選択中：暗いボタンは元色加算、白いボタンは少し暗く＋淡いアクセント色
                if (_FocusAdd > 0.001)
                {
                    fixed3 darkened = color.rgb * (1.0 - _FocusAdd * 0.42);
                    fixed3 brightened = color.rgb + color.rgb * _FocusAdd;
                    color.rgb = lerp(brightened, darkened, brightMask);
                }

                // 斜めに走る金属光沢（シマー）
                if (_ShineIntensity > 0.001)
                {
                    float diag = IN.texcoord.x * 0.75 + IN.texcoord.y * 0.25;
                    float dist = abs(diag - _ShineProgress);
                    float shine = smoothstep(_ShineWidth, 0.0, dist);
                    float core = smoothstep(_ShineWidth * 0.35, 0.0, dist);
                    float shineStrength = (shine * 0.65 + core * 0.85) * _ShineIntensity * color.a;

                    // 白いボタン上でも光沢が視認できるよう、明るい面ではわずかに色味＋コントラストを乗せる
                    fixed3 brightShine = (_ShineColor.rgb - fixed3(0.18, 0.18, 0.18)) * shineStrength;
                    fixed3 darkShine = _ShineColor.rgb * shineStrength;
                    color.rgb += lerp(darkShine, brightShine, brightMask);
                }

                // 外枠ボーダー（半透明ボタンの中身は白くせず、フチだけに白/黒の枠を描く）
                if (_RimIntensity > 0.001)
                {
                    float edgeX = min(IN.texcoord.x, 1.0 - IN.texcoord.x);
                    float edgeY = min(IN.texcoord.y, 1.0 - IN.texcoord.y);
                    float edge = min(edgeX, edgeY);
                    float rim = smoothstep(_RimThickness, _RimThickness * 0.65, edge);
                    float borderAlpha = saturate(rim * _RimIntensity);
                    color.rgb = lerp(color.rgb, _RimColor.rgb, borderAlpha);
                    color.a = max(color.a, borderAlpha * _RimColor.a);
                }

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
