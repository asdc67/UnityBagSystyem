Shader "Custom/Game/UniversalItemShader"
{
    Properties
    {
        // 基础设置
        [PerRendererData] _MainTex ("Item Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _Saturation ("Saturation", Range(0, 2)) = 1.0
        
        // 轮廓发光
        [Toggle(OUTLINE_ON)] _OutlineOn ("Enable Outline", Float) = 1
        _OutlineColor ("Outline Color", Color) = (1,0.8,0.2,1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02
        _OutlineIntensity ("Outline Intensity", Range(0, 5)) = 1.5
        
        // 能量光晕
        [Toggle(GLOW_ON)] _GlowOn ("Enable Glow", Float) = 0
        _GlowColor ("Glow Color", Color) = (0.2,0.6,1,1)
        _GlowSpeed ("Glow Speed", Range(0, 2)) = 0.5
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 1.0
        _GlowFrequency ("Glow Frequency", Range(1, 20)) = 8.0
        
        // 品质边框
        [Toggle(BORDER_ON)] _BorderOn ("Enable Border", Float) = 1
        _BorderColor ("Border Color", Color) = (0.8,0.8,0.8,1)
        _BorderWidth ("Border Width", Range(0, 0.15)) = 0.03
        _BorderPattern ("Border Pattern", Range(1, 20)) = 4
        
        // 动态效果
        [Toggle(PULSE_ON)] _PulseOn ("Enable Pulse", Float) = 0
        _PulseSpeed ("Pulse Speed", Range(0, 3)) = 1.0
        _PulseAmount ("Pulse Amount", Range(0, 0.3)) = 0.1
        
        // 旋转特效
        [Toggle(ROTATE_ON)] _RotateOn ("Enable Rotation", Float) = 0
        _RotateSpeed ("Rotate Speed", Range(-3, 3)) = 1.0
        _RotateRadius ("Rotate Radius", Range(0, 0.5)) = 0.2
        _RotateCount ("Particle Count", Range(1, 8)) = 3
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
            #pragma shader_feature OUTLINE_ON
            #pragma shader_feature GLOW_ON
            #pragma shader_feature BORDER_ON
            #pragma shader_feature PULSE_ON
            #pragma shader_feature ROTATE_ON
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            
            float _Brightness;
            float _Saturation;
            
            // 轮廓发光
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineIntensity;
            
            // 能量光晕
            fixed4 _GlowColor;
            float _GlowSpeed;
            float _GlowIntensity;
            float _GlowFrequency;
            
            // 品质边框
            fixed4 _BorderColor;
            float _BorderWidth;
            float _BorderPattern;
            
            // 动态效果
            float _PulseSpeed;
            float _PulseAmount;
            
            // 旋转特效
            float _RotateSpeed;
            float _RotateRadius;
            float _RotateCount;
            
            // 噪声函数
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123);
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y;
                
                // 基础纹理采样
                half4 color = tex2D(_MainTex, uv);
                color = (color + _TextureSampleAdd) * i.color;
                
                // 初始颜色
                float3 finalColor = color.rgb;
                
                // 1. 轮廓发光效果
                #ifdef OUTLINE_ON
                {
                    float2 texelSize = float2(1.0/512.0, 1.0/512.0);
                    
                    float s00 = tex2D(_MainTex, uv + float2(-1,-1) * texelSize).a;
                    float s10 = tex2D(_MainTex, uv + float2( 0,-1) * texelSize).a;
                    float s20 = tex2D(_MainTex, uv + float2( 1,-1) * texelSize).a;
                    float s01 = tex2D(_MainTex, uv + float2(-1, 0) * texelSize).a;
                    float s21 = tex2D(_MainTex, uv + float2( 1, 0) * texelSize).a;
                    float s02 = tex2D(_MainTex, uv + float2(-1, 1) * texelSize).a;
                    float s12 = tex2D(_MainTex, uv + float2( 0, 1) * texelSize).a;
                    float s22 = tex2D(_MainTex, uv + float2( 1, 1) * texelSize).a;
                    
                    float sobelX = (s00 + 2.0 * s01 + s02) - (s20 + 2.0 * s21 + s22);
                    float sobelY = (s00 + 2.0 * s10 + s20) - (s02 + 2.0 * s12 + s22);
                    float edge = sqrt(sobelX * sobelX + sobelY * sobelY);
                    
                    float outline = smoothstep(0.0, _OutlineWidth, edge);
                    finalColor = lerp(finalColor, _OutlineColor.rgb * _OutlineIntensity, outline);
                }
                #endif
                
                // 2. 能量光晕效果
                #ifdef GLOW_ON
                {
                    float2 center = float2(0.5, 0.5);
                    float dist = length(uv - center);
                    
                    float pulse = sin(time * _GlowSpeed) * 0.5 + 0.5;
                    float ring = abs(dist - 0.3);
                    float glow = 1.0 - smoothstep(0.0, 0.2, ring + pulse * 0.1);
                    
                    glow *= sin(dist * _GlowFrequency + time) * 0.5 + 0.5;
                    
                    finalColor += _GlowColor.rgb * glow * _GlowIntensity;
                }
                #endif
                
                // 3. 品质边框效果
                #ifdef BORDER_ON
                {
                    float2 center = float2(0.5, 0.5);
                    float2 distToEdge = abs(uv - center) * 2.0;
                    float maxDist = max(distToEdge.x, distToEdge.y);
                    
                    float border = smoothstep(1.0 - _BorderWidth, 1.0, maxDist);
                    float pattern = sin(uv.x * _BorderPattern + time) * 
                                   sin(uv.y * _BorderPattern + time) * 0.5 + 0.5;
                    border *= pattern;
                    
                    finalColor += _BorderColor.rgb * border;
                }
                #endif
                
                // 4. 脉动效果
                #ifdef PULSE_ON
                {
                    float2 center = float2(0.5, 0.5);
                    float dist = length(uv - center);
                    
                    float pulse = sin(time * _PulseSpeed) * _PulseAmount * 0.5 + 1.0;
                    float pulseMask = 1.0 - smoothstep(0.0, 0.5, dist);
                    
                    finalColor *= pulse * pulseMask;
                }
                #endif
                
                // 5. 旋转粒子效果
                #ifdef ROTATE_ON
                {
                    float3 particleColor = float3(0,0,0);
                    int count = int(_RotateCount);
                    
                    for(int j = 0; j < count; j++)
                    {
                        float angle = time * _RotateSpeed + (6.28318 / _RotateCount) * j;
                        float2 particlePos = float2(0.5, 0.5) + float2(cos(angle), sin(angle)) * _RotateRadius;
                        
                        float dist = length(uv - particlePos);
                        float particle = 1.0 - smoothstep(0.0, 0.05, dist);
                        
                        float3 pColor = float3(
                            sin(angle) * 0.5 + 0.5,
                            cos(angle) * 0.5 + 0.5,
                            sin(angle + 2.094) * 0.5 + 0.5
                        );
                        
                        particleColor += pColor * particle;
                    }
                    
                    finalColor += particleColor;
                }
                #endif
                
                // 6. 颜色调整
                {
                    finalColor *= _Brightness;
                    float luminance = dot(finalColor, float3(0.299, 0.587, 0.114));
                    finalColor = lerp(float3(luminance, luminance, luminance), finalColor, _Saturation);
                }
                
                // 最终输出
                color.rgb = finalColor;
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                
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