Shader "Maj/Notes/AirshiftOverlappingStarSplit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _SplitSide ("Left -1 / Right +1", Float) = -1
        _SplitCenter ("Local Split Center", Float) = 0
        _DividerHalfWidth ("Yellow Divider Half Width", Float) = 0.03
        _DividerColor ("Divider Color", Color) = (1, 0.85, 0.08, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "AirshiftOverlappingStarSplit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Assets/Plugins/MajdataPlay/Rendering/RenderPipelines/MajRenderPipeline/Library/MajCore.hlsl"

            sampler2D _MainTex;
            float _SplitSide;
            float _SplitCenter;
            float _DividerHalfWidth;
            half4 _DividerColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float localX : TEXCOORD1;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                output.localX = input.positionOS.x;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float delta = input.localX - _SplitCenter;
                // Left and right renderers draw complementary halves.
                // Using the vertex's local X makes the split rotate with the STAR.
                if (_SplitSide < 0)
                {
                    clip(-delta);
                }
                else
                {
                    clip(delta);
                }

                half4 color = tex2D(_MainTex, input.uv) * input.color;
                if (abs(delta) <= _DividerHalfWidth)
                {
                    color.rgb = _DividerColor.rgb;
                }
                return color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
