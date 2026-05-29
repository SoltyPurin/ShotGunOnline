using UnityEngine;

/// <summary>
/// タイトルの背景を移動させるスクリプト
/// 【作成者：髙橋英士】
/// </summary>
public class BackGroundMove : MonoBehaviour
{
    [SerializeField, Header("開始位置")]
    private float _startPos = 24.0f;

    [SerializeField, Header("終了位置")]
    private float _endPos = -24.0f;

    [SerializeField, Header("移動速度")]
    private float _moveSpeed = 1.0f;

    private void FixedUpdate()
    {
        Move();
    }

    /// <summary>
    /// 背景を移動させるメソッド
    /// </summary>
    private void Move()
    {
        //自身の座標を取得
        Vector3 pos = transform.position;

        //自身の座標から移動速度を引いた値を代入
        transform.position = new Vector3(pos.x - (_moveSpeed * Time.deltaTime), pos.y, pos.z);

        //自身の座標が終了地点以下だった場合開始位置に移動
        if(transform.position.x <= _endPos)
        {
            transform.position = new Vector3(_startPos, pos.y, pos.z);
        }
    }
}
