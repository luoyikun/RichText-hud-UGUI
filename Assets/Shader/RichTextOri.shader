
Shader "UI/RichTextOri"
{
    Properties
    {
        _MainTex("Font Texture", 2D) = "white" {} //����ͼ
        _SpriteTex("Sprite Texture", 2D) = "white" {}
        _Color("Text Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane" //������Ϊ"Plane"ʱ��Unity����һ��ƽ������ʾ����Ԥ������ͨ������2D���ʻ�����Ҫ��ƽ���ϲ鿴Ч���Ĳ��ʡ�
        }

        Lighting Off
        Cull Off
        ZTest LEqual //��ȼ�⣬Always��Զд�룬�Ḳ�����У��������ʾ��LEqual��������ȼ��
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			//#pragma multi_compile_instancing
			//#pragma multi_compile ��Unity Shader����е�һ����Ҫָ������������ڱ���ʱ���ɶ����ɫ������
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "UnityCG.cginc"

			// ʹ��һ���ṹ�������嶥����ɫ��������
            struct appdata_t
            {
                float4 vertex : POSITION;  // POSITION���壬��ģ�Ϳռ�Ķ����������vertex����
                half4 color : COLOR;
                float2 uv0 	: TEXCOORD0;  // TEXCOORD0���壬��ģ�͵ĵ�һ�������������texcoord����
                float2 uv1 	: TEXCOORD1;
				//UNITY_VERTEX_INPUT_INSTANCE_ID
            };

			//ʹ��һ���ṹ�������嶥����ɫ�������
            struct v2f
            {
                float4 vertex : POSITION; //�ü��ռ��¶���
                half4 color : COLOR;
                float2 uv0 	: TEXCOORD0;
                float2 uv1 	: TEXCOORD1;
				//UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex; //����������Ӧ����2d����
            float4 _MainTex_ST; //�̶�д������Ҫ��_ST

            sampler2D _SpriteTex;
            float4 _SpriteTex_ST;

            uniform fixed4 _Color;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); //�̶�д����ģ�Ϳռ䶥������ת�ü��ռ�
				//����1Ϊuv������2Ϊ������
				//ʵ����������ź�ƽ��Ч��
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex); //�������UV������ݲ��ʵ�Tiling��Offset���Խ��б任����UnityCG.cginc �� #define TRANSFORM_TEX(tex,name) (tex.xy * name##_ST.xy + name##_ST.zw)

                o.uv1 = TRANSFORM_TEX(v.uv1, _SpriteTex);
                o.color = v.color;// *_Color;

                return o;
            }

            half4 frag(v2f i) : COLOR
            {
                half4 result = i.color * i.uv1.x;
				//���ֵ�͸��
                result.a *= (tex2D(_MainTex, i.uv0)).a;
                result += i.uv1.y * i.color * tex2D(_SpriteTex, i.uv0);

                return result;
            }
            ENDCG
        }
    }
}
