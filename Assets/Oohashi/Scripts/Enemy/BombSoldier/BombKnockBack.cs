using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombKnockBack : EnemyKnockBack
{
    [SerializeField,Header("爆発を実行するスクリプト")]
    private BombProtocol _bombProtocol = default;


    /// <summary>
    /// 吹き飛ぶメソッド
    /// </summary>
    public override void KnockBackMethod()
    {
        //落下中でなければ実行
        if (_enemyMove.EnemyState != EnemyState.fall)
        {
            //吹き飛び力が0より上であればノックバックさせ続ける
            if (_force > 0)
            {
                //ステートをノックバック中にして別の敵に触れたときに爆発するフラグをtrueにする
                _enemyMove.EnemyState = EnemyState.knockback;
                _bombProtocol.IsBlowing = true;
                _force -= _decelerationValue;
            }
            else
            {
                //吹き飛びのフラグをfalseにして吹き飛ばなくする
                _canKnockBack = false;
                //別の敵に触れた時に爆発しなくする
                _bombProtocol.IsBlowing = false;
                //ステートを移動にする
                _enemyMove.EnemyState = EnemyState.move;
            }
        }
        else
        {
            //落下中は吹き飛び力を0にする
            _force =0f;
            _bombProtocol.IsBlowing = false;
        }
        transform.position += (Vector3)_blowAwayDirection * _force * Time.fixedDeltaTime;

    }
}
