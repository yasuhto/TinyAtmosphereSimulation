float _DeltaTime;

float4 _Size;               //  バッファサイズ
float3 _GridSize;           //  グリッドの寸法(km)
float3 _InverseGridSize;    //  グリッドサイズの逆数

RWTexture3D<float4> _Write;
Texture3D<float4> _Read;

Texture3D<float4> _Velocity;
Texture3D<float4> _Atmosphere;

RWTexture2D<float4> _DebugTexture;
RWStructuredBuffer<float> _DebugBuffer;
uint _DebugBufferType;
uint _DebugSizeIndex;
bool _DebugIsZYField;
float3 _DebugPick;

//  東西：Repeat 上下：Clamp 南北：Mirror
SamplerState _Linear_RepeatU_ClampV_MirrorW;

inline float4 SampleTexture(Texture3D<float4> source, float3 origin, float3 offset)
{
    float w = _Size.x;
    float h = _Size.y;
    float d = _Size.z;
    float3 pos = origin + offset;

    //  ボクセルの中心座標を求める
    float3 px = float3(1.0 / w, 1.0 / h, 1.0 / d);
    float3 uvw = float3(pos.x / w, pos.y / h, pos.z / d) + px * 0.5;

    return source.SampleLevel(_Linear_RepeatU_ClampV_MirrorW, uvw, 0);
}

//  右手系で変換する
inline float2 GetLatLng(uint3 id) { return id.xz / _Size.xz * float2(360, 180) + float2(-180, -90); } //  経度、緯度

inline void DrawDebugTextureImpl(uint3 id, float4 color)
{
    //  2Dなので、目的の高さ以外はスキップ
    if (_DebugIsZYField)
    {
        if (id.x != _DebugSizeIndex) return;
        _DebugTexture[id.zy] = color;
    }
    else
    {
        if (id.y != _DebugSizeIndex) return;
        _DebugTexture[id.zx] = color;
    }
}

inline void DrawDebugTexture(uint3 id)
{
    float4 color = 0;
    float4 ratio = 0;
    switch (_DebugBufferType)
    {
    case BufferType_Velocity:
        ratio = float4 (0.5, 0.5, 1, 1);
        color = lerp(float4(0, 0, abs(_Velocity[id].y), 1.0), float4(_Velocity[id].y, 0, 0, 1.0), step(0, _Velocity[id].y)) * ratio;
        break;
    case BufferType_Atmosphere:
        ratio = float4 (0.005, 1, 1, 1);
        color = _Atmosphere[id] * ratio;
        break;
    default:
        return;
    }

    DrawDebugTextureImpl(id, color);
}

inline void GenerateDebugData(uint3 id)
{
    DrawDebugTexture(id);

    //  デバッグ用グリッドデータ書き出し
    if (length(id - _DebugPick) == 0)
    {
        switch (_DebugBufferType)
        {
        case BufferType_Velocity:
            _DebugBuffer[0] = _Velocity[id].x;
            _DebugBuffer[1] = _Velocity[id].y;
            _DebugBuffer[2] = _Velocity[id].z;
            _DebugBuffer[3] = _Velocity[id].w;
            break;
        case BufferType_Atmosphere:
            _DebugBuffer[0] = _Atmosphere[id].x;
            _DebugBuffer[1] = _Atmosphere[id].y;
            _DebugBuffer[2] = _Atmosphere[id].z;
            _DebugBuffer[3] = _Atmosphere[id].w;
            break;
        default:
            return;
        }
    }
}