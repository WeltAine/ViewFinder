Shader "MyShader/Screen"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _MaskTex ("Mask", 2D) = "white" {}

        [Enum(UnityEngine.Rendering.BlendMode)]
        _SrcFactor ("Src Fractor", Float) = 5

        [Enum(UnityEngine.Rendering.BlendMode)]
        _DstFactor ("Dst Factor", Float) = 10

        [Enum(UnityEngine.Rendering.BlendOp)]
        _Op ("Operation", Float) = 0

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Blend [_SrcFactor] [_DstFactor]
        BlendOp [_Op]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD1;
                float4 screenPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _MaskTex;
            float4 _MaskTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MaskTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w; 
                // sample the texture
                fixed4 col = tex2D(_MainTex, screenUV);
                fixed4 maskCol = tex2D(_MaskTex, i.uv);
                // apply fog
                return fixed4(col.rgb, maskCol.a);
            }
            ENDCG
        }
    }
}
