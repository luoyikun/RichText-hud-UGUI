
Shader "UI/RichText"
{
    Properties
    {
        _MainTex("Font Texture", 2D) = "white" {} //����ͼ
        _SpriteTex("Sprite Texture", 2D) = "white" {}
        _Color("Text Color", Color) = (1, 1, 1, 1)
		 //��߿���ɫ
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
		_IsText ("IsText",Int) = 0
		//[MaterialEnum(Common,4,AlwaysUp,8)]_Ztest("Ztest", Int) = 4  //����ztest
		_Ztest("Ztest", Int) = 4  //����ztest
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
		ZTest [_Ztest]
        //ZTest LEqual //��ȼ�⣬Always��Զд�룬�Ḳ�����У��������ʾ��LEqual��������ȼ��
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
				float2 uv2  : TEXCOORD2;
				//UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex; //����������Ӧ����2d����
            float4 _MainTex_ST; //�̶�д������Ҫ��_ST
			float4 _MainTex_TexelSize;

            sampler2D _SpriteTex;
            float4 _SpriteTex_ST;

            uniform fixed4 _Color;
			int _IsText;
			//�߿���ɫ
            fixed4 _OutlineColor;      

			 #define BIAS 0.5

            static const float2 dirList[9] = {
                float2(-BIAS, -BIAS),
                float2(0, -BIAS),
                float2(BIAS, -BIAS),
                float2(-BIAS, 0),
                float2(0, 0),
                float2(BIAS, 0),
                float2(-BIAS, BIAS),
                float2(0, BIAS),
                float2(BIAS, BIAS)
            };

            //�õ�ÿ�����ص��alphaֵ
            half getDirPosAlpha(float index, float2 xy)
            {
                float2 curPos = xy;
                float2 dir = dirList[index];
                float2 dirPos = curPos + dir * _MainTex_TexelSize.xy;
                return tex2D(_MainTex, dirPos).a;
            };

            //
            float mixAlpha(float2 xy)
            {
                float a = 0;
                float index = 0;
                a += getDirPosAlpha(index, xy);
                a += getDirPosAlpha(index++, xy);
                a += getDirPosAlpha(index++, xy);
                a += getDirPosAlpha(index++, xy);
                a += getDirPosAlpha(index++, xy);
                a += getDirPosAlpha(index++, xy);
                a += getDirPosAlpha(index++, xy);
                a += getDirPosAlpha(index++, xy);
                a += getDirPosAlpha(index++, xy);
                a = step(a, 9) * a;
                a = clamp(0, 1, a);
                return a;
            }


            //������ɫ����, �ο�lerp
            fixed4 blendColor(fixed4 colorBottom, fixed4 colorTop)
            {
                float a = colorTop.a + colorBottom.a * (1 - colorTop.a);
                fixed4 newCol = fixed4(0, 0, 0, a);
                newCol.rgb = (colorTop.rgb * colorTop.a + colorBottom.rgb * colorBottom.a * (1 - colorTop.a)) / a;
                return newCol;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); //�̶�д����ģ�Ϳռ䶥������ת�ü��ռ�
				//����1Ϊuv������2Ϊ������
				//ʵ����������ź�ƽ��Ч��
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex); //�������UV������ݲ��ʵ�Tiling��Offset���Խ��б任����UnityCG.cginc �� #define TRANSFORM_TEX(tex,name) (tex.xy * name##_ST.xy + name##_ST.zw)

                o.uv1 = TRANSFORM_TEX(v.uv1, _SpriteTex);
				o.uv2 = v.uv0;
                o.color = v.color;// *_Color;

                return o;
            }

            half4 frag(v2f i) : COLOR
            {
				if (i.uv1.y> 0)
                {
					//�õ���ͼƬ
					half4 result = i.color * i.uv1.x;
					//���ֵ�͸��
					result.a *= (tex2D(_MainTex, i.uv0)).a;
					result += i.uv1.y * i.color * tex2D(_SpriteTex, i.uv0);
					return result;
				}
				else
				{
					
					//��Ⱦ������ߣ����ְ嶼����ߣ���ֻ��ָ����һ����ɫ���������ı����������һ����ɫ��
					float uvAlpha = tex2D(_MainTex, i.uv0).a;
                
					fixed4 col = i.color;
					col.a = uvAlpha * i.color.a;

					 float4 outlineCol = _OutlineColor;
					// ����һ��alphaϵ����Ϊ�����ο�ʱ���ڱ�Եƽ��
					outlineCol.a = lerp(1 - uvAlpha, outlineCol.a, smoothstep(0, 1, i.color.a - 0.65));
					// ����߿��alphaֵ��Ϊ������ߵ����Եƽ��
					outlineCol.a = mixAlpha(i.uv0) * _OutlineColor.a * outlineCol.a;
					col = blendColor(outlineCol, col);
					clip(col.a - 0.001);
				
					col += i.uv1.y * i.color * tex2D(_SpriteTex, i.uv0);
					return col;
				}
            }
            ENDCG
        }
    }
}
