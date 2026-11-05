Shader "Maj/Notes/AirshiftOverlappingTouchSplit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _SplitSide ("Left -1 / Right +1", Float) = -1
        _SplitOrigin ("Split Origin (World)", Vector) = (0,0,0,0)
        _SplitNormal ("Split Normal (World)", Vector) = (1,0,0,0)
        _DividerHalfWidth ("Yellow Divider Half Width", Float) = 0.022
        _DividerColor ("Divider Color", Color) = (1,0.85,0.08,1)
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
            Name "AirshiftOverlappingTouchSplit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Assets/Plugins/MajdataPlay/Rendering/RenderPipelines/MajRenderPipeline/Library/MajCore.hlsl"

            sampler2D _MainTex;
            float _SplitSide;
            float4 _SplitOrigin;
            float4 _SplitNormal;
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
                float3 positionWS : TEXCOORD1;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = mul(unity_ObjectToWorld, input.positionOS).xyz;
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // Consistent global half through the moving fans, point, border.
                // Preserve SpriteRenderer's animated alpha and original texture.
                float delta = dot(input.positionWS - _SplitOrigin.xyz, _SplitNormal.xyz);
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
