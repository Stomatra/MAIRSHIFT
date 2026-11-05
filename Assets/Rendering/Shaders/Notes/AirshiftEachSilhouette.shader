Shader "Maj/Notes/AirshiftEachSilhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Skin Sprite", 2D) = "white" {}
        _BorderColor ("Each Border Color", Color) = (1, 0.85, 0.08, 1)
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
            Name "AirshiftEachSilhouette"
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Assets/Plugins/MajdataPlay/Rendering/RenderPipelines/MajRenderPipeline/Library/MajCore.hlsl"

            sampler2D _MainTex;
            half4 _BorderColor;

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
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // Flat yellow derived only from the imported sprite's alpha.
                // Skin RGB is deliberately ignored; the original, separately
                // tinted sprite is rendered in front of this silhouette.
                half alpha = tex2D(_MainTex, input.uv).a;
                return half4(_BorderColor.rgb, alpha * _BorderColor.a * input.color.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
