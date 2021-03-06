struct VS_POS
{
	float3 pos : POSITION;
};

struct VS_POS_TEX
{
	float3 pos : POSITION;
	float2 uv  : TEXCOORD;
};

struct VS_POS_NORM
{
	float3 pos : POSITION;
	float3 norm : NORMAL;
};

struct VS_POS_NORM_TEX
{
	float3 pos : POSITION;
	float3 norm : NORMAL;
	float2 uv : TEXCOORD;
};

struct PS_TEX
{
	float4 pos   : SV_POSITION;
	float2 uv    : TEXCOORD;
};

struct PS_NORM
{
	float4 pos : SV_POSITION;
	float3 norm : NORMAL;
};

struct PS_NORM_TEX
{
	float4 pos : SV_POSITION;
	float3 norm : NORMAL;
	float2 uv : TEXCOORD;
};