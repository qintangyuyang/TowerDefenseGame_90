Shader "TD90/SkillRing"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Progress ("Progress", Range(0, 1)) = 0
        _RingWidth ("Ring Width", Range(0.001, 0.5)) = 0.12
        _Intensity ("Intensity", Float) = 1
        _Alpha ("Alpha", Range(0, 1)) = 1
        _NoiseScale ("Noise Scale", Float) = 8
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _Progress;
            float _RingWidth;
            float _Intensity;
            float _Alpha;
            float _NoiseScale;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                float d = length(p);

                float edge =
                    smoothstep(_Progress - _RingWidth, _Progress, d) -
                    smoothstep(_Progress, _Progress + _RingWidth, d);

                float angle = atan2(p.y, p.x);
                float noise = 0.5 + 0.5 * sin(angle * _NoiseScale + _Time.y * 6.0);

                float alpha = edge * _Alpha * _Intensity * (0.65 + 0.35 * noise);

                return fixed4(_Color.rgb * _Intensity, alpha);
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}