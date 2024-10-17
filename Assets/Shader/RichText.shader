
Shader "UI/RichText"
{
    Properties
    {
        _MainTex("Font Texture", 2D) = "white" {} //字体图
        _SpriteTex("Sprite Texture", 2D) = "white" {}
        _Color("Text Color", Color) = (1, 1, 1, 1)
		 //外边框颜色
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
		_IsText ("IsText",Int) = 0
		//[MaterialEnum(Common,4,AlwaysUp,8)]_Ztest("Ztest", Int) = 4  //设置ztest
		_Ztest("Ztest", Int) = 4  //设置ztest
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane" //当设置为"Plane"时，Unity会在一个平面上显示材质预览，这通常用于2D材质或者需要在平面上查看效果的材质。
        }

        Lighting Off
        Cull Off
		ZTest [_Ztest]
        //ZTest LEqual //深度检测，Always永远写入，会覆盖所有，在最顶层显示。LEqual正常的深度检测
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			//#pragma multi_compile_instancing
			//#pragma multi_compile 是Unity Shader编程中的一个重要指令，它允许开发者在编译时生成多个着色器变体
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "UnityCG.cginc"

			// 使用一个结构体来定义顶点着色器的输入
            struct appdata_t
            {
                float4 vertex : POSITION;  // POSITION语义，用模型空间的顶点坐标填充vertex变量
                half4 color : COLOR;
                float2 uv0 	: TEXCOORD0;  // TEXCOORD0语义，用模型的第一套纹理坐标填充texcoord变量
                float2 uv1 	: TEXCOORD1;
				//UNITY_VERTEX_INPUT_INSTANCE_ID
            };

			//使用一个结构体来定义顶点着色器的输出
            struct v2f
            {
                float4 vertex : POSITION; //裁剪空间下顶点
                half4 color : COLOR;
                float2 uv0 	: TEXCOORD0;
                float2 uv1 	: TEXCOORD1;
				float2 uv2  : TEXCOORD2;
				//UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex; //采样器，对应上面2d属性
            float4 _MainTex_ST; //固定写法，需要加_ST
			float4 _MainTex_TexelSize;

            sampler2D _SpriteTex;
            float4 _SpriteTex_ST;

            uniform fixed4 _Color;
			int _IsText;
			//边框颜色
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

            //得到每个像素点的alpha值
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


            //两个颜色叠加, 参考lerp
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
                o.vertex = UnityObjectToClipPos(v.vertex); //固定写法，模型空间顶点坐标转裁剪空间
				//参数1为uv，参数2为采样器
				//实现纹理的缩放和平移效果
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex); //将顶点的UV坐标根据材质的Tiling和Offset属性进行变换。在UnityCG.cginc 中 #define TRANSFORM_TEX(tex,name) (tex.xy * name##_ST.xy + name##_ST.zw)

                o.uv1 = TRANSFORM_TEX(v.uv1, _SpriteTex);
				o.uv2 = v.uv0;
                o.color = v.color;// *_Color;

                return o;
            }

            half4 frag(v2f i) : COLOR
            {
				if (i.uv1.y> 0)
                {
					//用到了图片
					half4 result = i.color * i.uv1.x;
					//文字的透明
					result.a *= (tex2D(_MainTex, i.uv0)).a;
					result += i.uv1.y * i.color * tex2D(_SpriteTex, i.uv0);
					return result;
				}
				else
				{
					
					//渲染文字描边，名字板都加描边，且只能指定加一种颜色。（所有文本都加描边且一种颜色）
					float uvAlpha = tex2D(_MainTex, i.uv0).a;
                
					fixed4 col = i.color;
					col.a = uvAlpha * i.color.a;

					 float4 outlineCol = _OutlineColor;
					// 计算一个alpha系数，为了让镂空时的内边缘平滑
					outlineCol.a = lerp(1 - uvAlpha, outlineCol.a, smoothstep(0, 1, i.color.a - 0.65));
					// 推算边框的alpha值，为了让描边的外边缘平滑
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
