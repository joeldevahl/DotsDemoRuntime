Shader "ScreenBlit"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            StructuredBuffer<float4> _buffer;

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float2 uv = float2((v.vertexID << 1) & 2, v.vertexID & 2);
                o.vertex = float4(uv * 2.0 - 1.0, UNITY_NEAR_CLIP_VALUE, 1.0);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                uint index = uint(i.vertex.y) * uint(_ScreenParams.x) + uint(i.vertex.x);
                return _buffer[index];
            }
            ENDHLSL
        }
    }
}

