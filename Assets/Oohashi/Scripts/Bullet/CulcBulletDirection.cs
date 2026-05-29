using UnityEngine;
using UnityEngine.UIElements;

public class CulcBulletDirection : MonoBehaviour
{
    /// <summary>
    /// 銃弾の飛んでいく方向を計算
    /// </summary>
    /// <param name="direction">プレイヤーの向いている方向</param>
    /// <param name="chargeValue">チャージ時間</param>
    /// <param name="isPlus">ばらけさせるためのフラグ</param>
    /// <returns></returns>
    public Vector3 CulcDirection(Vector3 direction, float chargeValue, bool isPlus)
    {
        //初期地点をチャージ時間によって変動させる
        float startDirection = Random.Range(0, 2.1f-chargeValue);
        //右上だった場合計算が狂うので右上と左下を判定する
        bool isRightUp = (direction.x >= 0 && direction.x <= 1 && direction.y >= 0 && direction.y <= 1);
        bool isLeftDown = (direction.x < 0 && direction.x >= -1 && direction.y < 0 && direction.y >= -1);

        float angle = Mathf.Lerp(50, 15, chargeValue);
        float randomAngle = Random.Range(-angle, angle);
        Vector3 newDirection = Quaternion.Euler(0, 0, randomAngle) * direction;
        //そして出た方向をreturnする
        return newDirection;
    }

}
