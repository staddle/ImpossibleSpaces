Shader "Custom/InvisibleInsideRoomShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RoomVertexCount ("Room Vertex Count", Int) = 0
    }
    SubShader
    {
        Tags 
        { 
          "RenderType"="Transparent"
          "Queue"="Transparent" 
        }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct v2f
            {
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (float4 vertex : POSITION, float2 uv : TEXCOORD0)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(vertex);
                o.uv = TRANSFORM_TEX(uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float2 toFloat2(float3 input)
            {
                return float2(input.x, input.z);
            }

            float2 toFloat42(float4 input)
            {
                return float2(input.x, input.z);
            }

            // https://stackoverflow.com/questions/217578/how-can-i-determine-whether-a-2d-point-is-within-a-polygon
            int areIntersecting(float4 v1P13D, float4 v1P23D, float3 v2P13D, float3 v2P23D)
            {
                // only need 2D represenation, so scrap the y coordinates
                float2 v1P1 = toFloat42(v1P13D);
                float2 v1P2 = toFloat42(v1P23D);
                float2 v2P1 = toFloat2(v2P13D);
                float2 v2P2 = toFloat2(v2P23D);

                float a1 = v1P2.y - v1P1.y;
                float b1 = v1P1.x - v1P2.x;
                float c1 = (v1P2.x * v1P1.y) - (v1P1.x * v1P2.y);

                float d1 = (a1 * v2P1.x) + (b1 * v2P1.y) + c1;
                float d2 = (a1 * v2P2.x) + (b1 * v2P2.y) + c1;

                if (d1 > 0 && d2 > 0)
                {
                    return 0; // not intersecting
                }
                if (d1 < 0 && d2 < 0)
                {
                    return 0; // not intersecting
                }

                float a2 = v2P2.y - v2P1.y;
                float b2 = v2P1.x - v2P2.x;
                float c2 = (v2P2.x * v2P1.y) - (v2P1.x * v2P2.y);

                d1 = (a2 * v1P1.x) + (b2 * v1P1.y) + c2;
                d2 = (a2 * v1P2.x) + (b2 * v1P2.y) + c2;

                if (d1 > 0 && d2 > 0)
                {
                    return 0; // not intersecting
                }
                if (d1 < 0 && d2 < 0)
                {
                    return 0; // not intersecting
                }

                if ((a1 * b2) - (a2 * b1) == 0.0f)
                {
                    return 2; // colinear
                }

                return 1; // intersecting
            }

            int _RoomVertexCount = 0;
            float4 _RoomVertices[1000];

            fixed4 frag (v2f input) : SV_Target
            {
                float3 pointOutside = float3(-1, 0, -1);
                int counter = 0;
                /*if(_RoomVertexCount == 4)
                  return fixed4(_RoomVertices[1].x*25, _RoomVertices[1].y*25, _RoomVertices[1].z*25, 255);*/
                for (int i = 0; i < _RoomVertexCount; i++)
                {
                    counter += areIntersecting(_RoomVertices[i], _RoomVertices[(i + 1) % _RoomVertexCount], pointOutside, input.worldPos);
                }
                if (counter % 2 == 1) // inside
                {
                    return fixed4(0, 0, 0, 0);
                }

                // sample the texture
                fixed4 col = tex2D(_MainTex, input.uv);
                // apply fog
                UNITY_APPLY_FOG(input.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
