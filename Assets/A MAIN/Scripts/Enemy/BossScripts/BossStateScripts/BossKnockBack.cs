using UnityEngine;

/// <summary>
/// ボスののけ反りを管理するメソッド
/// </summary>
public class BossKnockBack : EnemyKnockBack
{
    [SerializeField, Header("ボスの現在のステートを取得するためのスクリプト")]
    private BossStateManagement _stateManagement = default;
    
    public override void SetDirectionAndForce(Vector2 playerPos, float chargeTime,bool isCrit,bool isUlt)
    {
        bool cantKnockBack = false;
        if (_stateManagement._currentState == BossStateManagement.BossState.JumpAtack ||
            _stateManagement._currentState == BossStateManagement.BossState.Punch ||
            !_stateManagement.isActiveAndEnabled)
        {
            cantKnockBack=true;
        }
            
        if (cantKnockBack)
        {
            return; //ジャンプ中とかだったら吹き飛ばしの処理をしない
        }
        _blowAwayDirection = ((playerPos - (Vector2)this.transform.position) * -1).normalized;
        //チャージ時間の乗を求める
        _powValue = Mathf.Pow(chargeTime, 5f);
        //最低3からpowValueまでの間で線形補正を行い、チャージ値ごとの吹き飛ばしをなめらかに
        _force = Mathf.Lerp(3, _powValue, 0.5f);
        //敵のノックバックは初速最速で瞬間的に最高速度から抵抗に負けて減速するみたいなシステムにする
        //長距離飛ぶわけではないけど沢山飛ぶわけではない
        //3つプレイヤーに気づかせることでプレイヤーは面白いと感じる
        //最後まで気づかれないのは絶対ダメ
        _canKnockBack = true;
    }
}
