using UnityEngine;

/// <summary>
/// 座標系操作のヘルパークラス
/// </summary>
public static class CoordinateHelper
{
    /// <summary>
    /// 右手系を左手系座標系に変換します
    /// </summary>
    public static Vector3 RightToLeft(Vector3 position) => new Vector3(position.y, position.z, position.x);

    /// <summary>
    /// 左手系を右手系座標系に変換します
    /// </summary>
    public static Vector3 LeftToRight(Vector3 position) => new Vector3(position.z, position.x, position.y);
}
