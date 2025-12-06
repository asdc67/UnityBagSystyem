// UI_FlowEffect.shader 完整内容
Shader "Custom/UI/FlowEffect"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // 流光效果参数
        _FlowColor ("流光颜色", Color) = (0.2, 0.6, 1, 1)
        _FlowSpeed ("流光速度", Range(0, 5)) = 1.5
        _FlowWidth ("流光宽度", Range(0, 0.5)) = 0.15
        _FlowIntensity ("流光强度", Range(0, 3)) = 1.5
        _FlowDirection ("流光方向", Vector) = (1, 1, 0, 0)
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
            float2 _FlowDirection;
            
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
                
                // 创建流光遮罩
                float flowTime = _Time.y * _FlowSpeed;
                float2 flowDir = normalize(_FlowDirection);
                float flowDot = dot(IN.texcoord, flowDir);
                float flowValue = sin(flowDot * 6.2831 - flowTime) * 0.5 + 0.5;
                
                // 计算流光强度
                float flowMask = smoothstep(0.0, _FlowWidth, flowValue) - 
                                smoothstep(_FlowWidth, _FlowWidth * 2.0, flowValue);
                
                // 应用流光
                color.rgb += _FlowColor.rgb * flowMask * _FlowIntensity * color.a;
                
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