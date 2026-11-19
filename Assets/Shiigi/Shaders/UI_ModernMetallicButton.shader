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
        _RimColor ("Rim Color", Color) = (1.0, 0.96, 0.72, 1)
        _RimIntensity ("Rim Intensity", Range(0, 1)) = 0.0
        _RimThickness ("Rim Thickness (Px)", Float) = 5.0
        _RectSize ("Rect Size (Px)", Vector) = (160, 60, 0, 0)
        [Toggle] _UseSpriteAlpha ("Use Sprite Alpha Outline", Float) = 0
        _SpriteUVBounds ("Sprite UV Bounds", Vector) = (0, 0, 1, 1)

        [Header(Focus Color Boost)]
        _FocusAdd ("Focus Self-Add", Range(0, 1)) = 0.0

        [Toggle] _UseLocalUV ("Use Local UV (TEXCOORD1)", Float) = 0

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
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex    : POSITION;
                float4 color     : COLOR;
                float2 texcoord  : TEXCOORD0;
                float3 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float3 localUV       : TEXCOORD2;
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
            float4 _RectSize;
            float _UseSpriteAlpha;
            float4 _SpriteUVBounds;
            float _FocusAdd;
            float _UseLocalUV;

            float SampleSpriteAlpha(float2 uv)
            {
                if (uv.x < _SpriteUVBounds.x || uv.x > _SpriteUVBounds.z ||
                    uv.y < _SpriteUVBounds.y || uv.y > _SpriteUVBounds.w)
                {
                    return 0.0;
                }
                return (tex2Dlod(_MainTex, float4(uv, 0.0, 0.0)) + _TextureSampleAdd).a;
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.localUV = v.texcoord1;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 texSample = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;
                float texAlpha = texSample.a;
                half4 color = texSample * IN.color;

                // 動的分岐の外でスクリーン空間のUV微分を計算しておく（9スライス/Simple両対応）
                float2 uvPerPx = float2(
                    length(float2(ddx(IN.texcoord.x), ddy(IN.texcoord.x))),
                    length(float2(ddx(IN.texcoord.y), ddy(IN.texcoord.y)))
                );

                // ボタン矩形内の正規化座標 (0〜1)。MultipleスプライトやTiledでも端から端まで均一に演出する
                float2 effectUV = (_UseLocalUV > 0.5) ? IN.localUV.xy : IN.texcoord;
                // Shadow / Outline コンポーネントによって複製された頂点には強調表示を適用しない
                float effectMask = (_UseLocalUV > 0.5) ? step(0.5, IN.localUV.z) : 1.0;

                if (effectMask > 0.5)
                {
                    // 縦方向のメタリックグラデーション
                    fixed4 grad = lerp(_GradBottomColor, _GradTopColor, saturate(effectUV.y));
                    color.rgb *= grad.rgb;

                    // 元の明るさ（輝度）を判定：白に近い明るいボタンだけ少し抑え、黄色や暗いボタンは明るく鮮やかに浮き立たせる
                    float luma = dot(color.rgb, float3(0.299, 0.587, 0.114));
                    float brightMask = smoothstep(0.78, 0.96, luma);

                    // 選択中：暗い/有彩色ボタンは明るくブースト、ほぼ白のボタンは少しトーンを落として差別化
                    if (_FocusAdd > 0.001)
                    {
                        fixed3 darkened = color.rgb * (1.0 - _FocusAdd * 0.35);
                        fixed3 brightened = color.rgb + max(color.rgb, fixed3(0.18, 0.15, 0.10)) * _FocusAdd;
                        color.rgb = lerp(brightened, darkened, brightMask);
                    }

                    // 斜めに走る金属光沢（シマー）
                    if (_ShineIntensity > 0.001)
                    {
                        float diag = effectUV.x * 0.75 + effectUV.y * 0.25;
                        float dist = abs(diag - _ShineProgress);
                        float shine = smoothstep(_ShineWidth, 0.0, dist);
                        float core = smoothstep(_ShineWidth * 0.35, 0.0, dist);
                        float shineStrength = (shine * 0.65 + core * 0.85) * _ShineIntensity * color.a;

                        // 白いボタン上でも光沢が視認できるよう、明るい面ではわずかに色味＋コントラストを乗せる
                        fixed3 brightShine = (_ShineColor.rgb - fixed3(0.18, 0.18, 0.18)) * shineStrength;
                        fixed3 darkShine = _ShineColor.rgb * shineStrength;
                        color.rgb += lerp(darkShine, brightShine, brightMask);
                    }

                    // 外枠ボーダー（スプライトの透過シルエットに沿った輪郭強調＋矩形フォールバック）
                    if (_RimIntensity > 0.001)
                    {
                        float2 rectSize = max(_RectSize.xy, float2(1.0, 1.0));
                        float2 pixelDist = min(effectUV, 1.0 - effectUV) * rectSize;
                        float edgePx = min(pixelDist.x, pixelDist.y);
                        float thicknessPx = max(_RimThickness, 1.0);
                        float rectRim = 1.0 - smoothstep(thicknessPx * 0.72, thicknessPx, edgePx);
                        float rectGlow = (1.0 - smoothstep(thicknessPx, thicknessPx * 2.8, edgePx)) * 0.32;

                        float rim = rectRim;
                        float innerGlow = rectGlow;

                        if (_UseSpriteAlpha > 0.5 && texAlpha > 0.05)
                        {
                            float2 uvSize = max(_SpriteUVBounds.zw - _SpriteUVBounds.xy, float2(0.0001, 0.0001));
                            float2 offset = min(uvPerPx * thicknessPx, uvSize * 0.25);
                            float2 diagOffset = offset * 0.7071;

                            float minA = 1.0;
                            minA = min(minA, SampleSpriteAlpha(IN.texcoord + float2(offset.x, 0.0)));
                            minA = min(minA, SampleSpriteAlpha(IN.texcoord - float2(offset.x, 0.0)));
                            minA = min(minA, SampleSpriteAlpha(IN.texcoord + float2(0.0, offset.y)));
                            minA = min(minA, SampleSpriteAlpha(IN.texcoord - float2(0.0, offset.y)));
                            minA = min(minA, SampleSpriteAlpha(IN.texcoord + float2(diagOffset.x, diagOffset.y)));
                            minA = min(minA, SampleSpriteAlpha(IN.texcoord + float2(-diagOffset.x, diagOffset.y)));
                            minA = min(minA, SampleSpriteAlpha(IN.texcoord + float2(diagOffset.x, -diagOffset.y)));
                            minA = min(minA, SampleSpriteAlpha(IN.texcoord + float2(-diagOffset.x, -diagOffset.y)));

                            float spriteRim = 1.0 - smoothstep(0.1, 0.85, minA);

                            float2 glowOffset = min(uvPerPx * (thicknessPx * 2.2), uvSize * 0.35);
                            float minGlowA = minA;
                            minGlowA = min(minGlowA, SampleSpriteAlpha(IN.texcoord + float2(glowOffset.x, 0.0)));
                            minGlowA = min(minGlowA, SampleSpriteAlpha(IN.texcoord - float2(glowOffset.x, 0.0)));
                            minGlowA = min(minGlowA, SampleSpriteAlpha(IN.texcoord + float2(0.0, glowOffset.y)));
                            minGlowA = min(minGlowA, SampleSpriteAlpha(IN.texcoord - float2(0.0, glowOffset.y)));

                            float spriteGlow = (1.0 - smoothstep(0.1, 0.85, minGlowA)) * 0.32;

                            rim = max(rectRim, spriteRim);
                            innerGlow = max(rectGlow, spriteGlow);
                        }

                        // スプライトの不透明な領域にのみアウトラインを適用し、透過部分はそのまま維持する
                        float alphaMask = smoothstep(0.05, 0.25, texAlpha);
                        float borderAlpha = saturate((rim + innerGlow) * _RimIntensity) * alphaMask;
                        color.rgb = lerp(color.rgb, _RimColor.rgb, borderAlpha);
                    }
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
