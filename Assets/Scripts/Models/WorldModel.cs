using System;
using UnityEngine;

[Serializable]
public class WorldModel
{
    /// <summary>1フレーム毎に経過する時間(sec)</summary>
    public float DeltaTime;
    /// <summary>1グリッドの3方向のサイズ(km)</summary>
    public Vector3 GridSize;
    /// <summary>重力加速度(m/s2)</summary>
    public float GForces;
    /// <summary>自転角速度(rad/s)</summary>
    public float RotationRate;
}
