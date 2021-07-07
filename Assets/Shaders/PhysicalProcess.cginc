//  地球に届く太陽エネルギーの 緯度毎、公転位置ごとの日中エネルギーの総量
//  大気や地表等での反射は考慮していない
//  南中高度の低い、高緯度で冬の時期は、低い
inline float CalcSolarRadiation(float latitude)
{
    //  入射熱の強度 = sin(南中高度) = sin( 90 - latitude + 太陽高度の日毎の変化量) に比例
    //half R_ = sin((90 + solarDegree - latitude) * Deg2Rad);
    float R_ = sin((90 - latitude) * Deg2Rad);

    //  R_ < 0 は太陽光が当たってない ＝ 極夜
    return 1366 * max(0.2, R_);  //   0.2は適当
}

//  太陽エネルギーを基準気温に変換する
inline float GetBaseTemperature(float solarRadiation)
{
    //  気温 = 緯度ごとの、地球放射 + 温室効果
    //  地球放射は、地球（大気＋地表）が吸収した太陽エネルギーと等価（7割吸収、3割反射）
    //  大気＋地表に届く太陽エネルギー Eは、太陽光が垂直に入った場合、（太陽定数）の1/4×0.7＝2.4×10^2 J・s^-1・m^-2
    //  これに各地点の入射角度を考慮して Ep = E*sin(南中高度) 
    //  エネルギー(E)とケルビン温度(T)の関係は、ステファン・ボルツマンの法則により、Ep＝σＴ^4
    //  T = pow(Ep/σ, 1/4)
    //  平均気温は -18度になる

    //  infinate対策で powを2つに分けた
    float earthR = pow(solarRadiation * 0.7 * 0.25 / 5.67, 0.25) * pow(100000000, 0.25);  // solor * 0.7 * 0.25 / 5.67 * 10^8

    //  温室効果を加味し、海抜0mでの最大気温が55度になるようにかさ上げ
    return earthR + 73;
}

//  気温（K）
inline half CalcTemperature(uint3 id, float temperature)
{
    float rate = 0.05;  //   加熱率（適当）
    float2 latlng = GetLatLng(id);
    float solarRadiation = CalcSolarRadiation(latlng.x);
    float temperature0 = (1 - rate) * temperature + rate * GetBaseTemperature(solarRadiation);

    return temperature0;
}
