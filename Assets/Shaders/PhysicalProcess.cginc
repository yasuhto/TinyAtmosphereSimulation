//  �n���ɓ͂����z�G�l���M�[�� �ܓx���A���]�ʒu���Ƃ̓����G�l���M�[�̑���
//  ��C��n�\���ł̔��˂͍l�����Ă��Ȃ�
//  �쒆���x�̒Ⴂ�A���ܓx�œ~�̎����́A�Ⴂ
inline float CalcSolarRadiation(float latitude)
{
    //  ���˔M�̋��x = sin(�쒆���x) = sin( 90 - latitude + ���z���x�̓����̕ω���) �ɔ��
    //half R_ = sin((90 + solarDegree - latitude) * Deg2Rad);
    float R_ = sin((90 - latitude) * Deg2Rad);

    //  R_ < 0 �͑��z�����������ĂȂ� �� �ɖ�
    return 1366 * max(0.2, R_);  //   0.2�͓K��
}

//  ���z�G�l���M�[����C���ɕϊ�����
inline float GetBaseTemperature(float solarRadiation)
{
    //  �C�� = �ܓx���Ƃ́A�n������ + ��������
    //  �n�����˂́A�n���i��C�{�n�\�j���z���������z�G�l���M�[�Ɠ����i7���z���A3�����ˁj
    //  ��C�{�n�\�ɓ͂����z�G�l���M�[ E�́A���z���������ɓ������ꍇ�A�i���z�萔�j��1/4�~0.7��2.4�~10^2 J�Es^-1�Em^-2
    //  ����Ɋe�n�_�̓��ˊp�x���l������ Ep = E*sin(�쒆���x) 
    //  �G�l���M�[(E)�ƃP���r�����x(T)�̊֌W�́A�X�e�t�@���E�{���c�}���̖@���ɂ��AEp���Ђs^4
    //  T = pow(Ep/��, 1/4)
    //  ���ϋC���� -18�x�ɂȂ�

    //  infinate�΍�� pow��2�ɕ�����
    float earthR = pow(solarRadiation * 0.7 * 0.25 / 5.67, 0.25) * pow(100000000, 0.25);  // solor * 0.7 * 0.25 / 5.67 * 10^8

    //  �������ʂ��������A�C��0m�ł̍ő�C����55�x�ɂȂ�悤�ɂ����グ
    return earthR + 73;
}

//  �C���iK�j
inline half CalcTemperature(uint3 id, float temperature)
{
    float rate = 0.05;  //   ���M���i�K���j
    float2 latlng = GetLatLng(id);
    float solarRadiation = CalcSolarRadiation(latlng.x);
    float temperature0 = (1 - rate) * temperature + rate * GetBaseTemperature(solarRadiation);

    return temperature0;
}
