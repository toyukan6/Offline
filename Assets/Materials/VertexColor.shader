Shader "Custom/VertexColor" {
	Properties{
		_PointSize("PointSize", Float) = 1
	}
		SubShader{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 200
		Pass{
		Cull Off ZWrite On Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM

#pragma exclude_renderers flash
#pragma vertex vert
#pragma fragment frag


	struct appdata {
		float4 pos : POSITION;
		fixed4 color : COLOR;
	};


	struct v2f {
		float4 pos : SV_POSITION;
		float size : PSIZE;
		fixed4 color : COLOR;
	};
	float _PointSize;

	v2f vert(appdata v) {
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.pos);
		o.size = _PointSize;
		o.color = v.color;
		return o;
	}

	half4 frag(v2f i) : COLOR0
	{
		return i.color;
	}
		ENDCG
	}
	}
}