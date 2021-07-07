float _AngularVelocityOfRotation;
float _GForces;

//  NOTE:���W�n�͍���n���g���iy=�����j�BUnity�̍��W�n�ƍ��킹��

//  Calc Pressure [hPa]
inline float CalcPressure(float temperature, float density)
{
    // p = ��RT * 0.01[hPa]
    // R:������C�̋C�̒萔 287 J/kg K
    return 2.87 * density * temperature;   
}

inline float CalcDensity(float temperature, float pressure)
{
    return pressure * 100 / (287 * temperature);
}

inline float ToSeaLevelPressure(float pressure, float altitude, float temperature)
{
    return pressure * pow(1 - (0.0065 * altitude) / (0.0065 * altitude + temperature), -5.257);
}

inline float CalcTemperatureWithPressure(float pressure, float density)
{
    // T = 100*p/�i��R�j     [Pa]
    // R:������C�̋C�̒萔 287 J/kg K
    return 100 * pressure / (287 * density);
}

inline float CalcAltitudeWithPressure(float p0, float ps, float temperature)
{
    return temperature * (pow(p0 / ps, 1 / 5.257) - 1) / 0.0065;
}

inline float CalcDeltaAltitudeWithTemperature(float temperature)
{
    return -1 * temperature * 287 / _GForces * log(_GridSize.y * 100); // m
}

inline float3 CalcPressureGradientWithP(uint3 id, Texture3D<float4> _Atmosphere)
{
    //  P���W�n�ł̒P�ʎ���1kg�̋C���X�x�́im/s2�j= g * ��z/��n
    //      g�F�d�͉����x�im/s2�j9.81
    //      n�F�����im�j

    float4 aL = SampleTexture(_Atmosphere, id, int3(-1, 0, 0));
    float4 aR = SampleTexture(_Atmosphere, id, int3(1, 0, 0));
    float4 aB = SampleTexture(_Atmosphere, id, int3(0, -1, 0));
    float4 aT = SampleTexture(_Atmosphere, id, int3(0, 1, 0));
    float4 aD = SampleTexture(_Atmosphere, id, int3(0, 0, -1));
    float4 aU = SampleTexture(_Atmosphere, id, int3(0, 0, 1));

    // left, right, bottom, and top x samples
    float zL = aL.w;
    float zR = aR.w;
    float zB = aB.w;
    float zT = aT.w;
    float zD = aD.w;
    float zU = aU.w;

    return float3(
        -_GForces * (zR - zL) * _InverseGridSize.x * 0.001,
        0,
        -_GForces * (zU - zD) * _InverseGridSize.z * 0.001);
}

inline float3 CalcCoriolis(float3 velocity, float latitude)
{
    //  �P�ʎ���1kg�̃R���I���́im/s2�j = V * 2��sin��
    //      ���F���]�̊p���x[rad/s]
    //       V�F���̂̑��x[m/s]
    //      ���F�ܓx

    return float3(
        velocity.z * 2 * _AngularVelocityOfRotation * sin(latitude * Deg2Rad),
        0,
        velocity.x * -2 * _AngularVelocityOfRotation * sin(latitude * Deg2Rad));
}

inline float3 CalcForce(uint3 id, Texture3D<float4> atmosphere, float3 velocity)
{
    //  �^��������
    //  ���������̉����x = -�C���X�x�� + �R���I��
    //  ���������̉����x = -�C���X�x�� - �d�͉����x

    float3 pressureGradient = CalcPressureGradientWithP(id, atmosphere);
    float3 coriolis = CalcCoriolis(velocity, GetLatLng(id).y);

    return pressureGradient + coriolis;
}

