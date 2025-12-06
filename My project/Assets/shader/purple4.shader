Shader "Custom/UI/FlowEffect_Diagonal45"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // 流光效果参数
        _FlowColor ("流光颜色", Color) = (0.85, 0.55, 1, 1)      // 亮紫色
        _FlowSpeed ("流光速度", Range(0, 3)) = 1.0
        _FlowWidth ("流光宽度", Range(0, 0.3)) = 0.06
        _FlowIntensity ("流光强度", Range(0, 5)) = 2.5
        
        // 对角控制参数
        _DiagonalWidth ("对角区域宽度", Range(0.1, 0.5)) = 0.15   // 控制对角条带宽度
        _EdgeFade ("边缘衰减", Range(0, 1)) = 0.3                 // 边缘淡出程度
        _FlowLength ("流光长度", Range(0.1, 1)) = 0.4             // 流光条长度
    }
    
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            
            // 流光参数
            fixed4 _FlowColor;
            float _FlowSpeed;
            float _FlowWidth;
            float _FlowIntensity;
            
            // 对角控制参数
            float _DiagonalWidth;
            float _EdgeFade;
            float _FlowLength;
            
            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }
            
            fixed4 frag(v2f IN) : SV_Target
            {
                // 基础纹理采样
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                
                float2 uv = IN.texcoord;
                
                // ====== 45度对角线遮罩计算 ======
                
                // 方法1：主对角线（从左上到右下）
                float diagonal1 = uv.x - uv.y;  // 45度线：x - y = 常数
                
                // 方法2：副对角线（从左下到右上）
                float diagonal2 = (1.0 - uv.x) - uv.y;  // 45度线：1-x - y = 常数
                
                // 创建两条45度线的遮罩
                float mask1 = 1.0 - smoothstep(0.0, _DiagonalWidth, abs(diagonal1));
                float mask2 = 1.0 - smoothstep(0.0, _DiagonalWidth, abs(diagonal2));
                
                // 合并两条线的遮罩
                float diagonalMask = max(mask1, mask2);
                
                // ====== 边缘衰减 ======
                // 让靠近边缘的效果减弱
                float2 edgeDist = min(uv, 1.0 - uv);
                float edgeMask = smoothstep(0.0, _EdgeFade, min(edgeDist.x, edgeDist.y));
                diagonalMask *= edgeMask;
                
                // ====== 动态流光效果 ======
                float flowTime = _Time.y * _FlowSpeed;
                
                // 沿对角线方向的流动
                // 使用45度方向的投影：x+y 或 x-y
                float flowPos1 = (uv.x + uv.y) * 2.0;  // 沿副对角线
                float flowPos2 = (uv.x - uv.y + 1.0) * 2.0;  // 沿主对角线，+1让值在0-2
                
                // 创建流动波
                float flowWave1 = sin(flowPos1 * 6.0 - flowTime) * 0.5 + 0.5;
                float flowWave2 = sin(flowPos2 * 6.0 - flowTime + 1.57) * 0.5 + 0.5; // 相位偏移
                
                // 创建流光条带
                float flowBand1 = smoothstep(0.0, _FlowWidth, flowWave1) - 
                                 smoothstep(_FlowWidth, _FlowWidth * 2.0, flowWave1);
                float flowBand2 = smoothstep(0.0, _FlowWidth, flowWave2) - 
                                 smoothstep(_FlowWidth, _FlowWidth * 2.0, flowWave2);
                
                // 控制流光长度：只在一定距离内显示
                float lengthMask1 = 1.0 - smoothstep(_FlowLength, _FlowLength + 0.1, frac(flowPos1));
                float lengthMask2 = 1.0 - smoothstep(_FlowLength, _FlowLength + 0.1, frac(flowPos2));
                
                flowBand1 *= lengthMask1;
                flowBand2 *= lengthMask2;
                
                // 合并两条流光的遮罩
                float flowMask = (flowBand1 + flowBand2) * 0.5;
                
                // ====== 最终效果合成 ======
                
                // 只在对角线区域显示流光
                flowMask *= diagonalMask;
                
                // 应用流光效果
                float3 glowColor = _FlowColor.rgb * flowMask * _FlowIntensity;
                
                // 屏幕混合模式，在深色背景上更明显
                color.rgb = color.rgb + glowColor * (1.0 - color.rgb);
                
                // UI裁剪
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                
                return color;
            }
            ENDCG
        }
    }
    
    Fallback "UI/Default"
}