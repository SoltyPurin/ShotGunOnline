Shader "Sprites/FlashWhite"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _FlashAmount ("Flash Amount", Range(0,1)) = 0
    }
    SubShader
    {
        Tags{ "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _FlashAmount;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };

            v2f vert (appdata v){
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color; return o;
            }

            fixed4 frag (v2f i):SV_Target{
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                float3 white = float3(1,1,1);
                c.rgb = lerp(c.rgb, white, saturate(_FlashAmount));
                return c;
            }
            ENDCG
        }
    }
}
