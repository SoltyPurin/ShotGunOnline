using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StoneState
{
    Normal,
    KnockBack
}
public class SetKnockBackStone : EnemyKnockBack
{
    [SerializeField] private float _forceDivision = 0.6f;
    private StoneState _state = StoneState.Normal;
    public StoneState State
    {
        get { return _state; }
    }
    public override void SetDirectionAndForce(Vector2 playerPos, float chargeTime, bool isCrit, bool isUlt)
    {
        _blowAwayDirection = ((playerPos - (Vector2)this.transform.position) * -1).normalized;
        //チャージ時間の乗を求める
        _powValue = Mathf.Pow(chargeTime, _forceMultiplier);
        //最低6からpowValueまでの間で線形補正を行い、チャージ値ごとの吹き飛ばしをなめらかに
        //敵のノックバックは初速最速で瞬間的に最高速度から抵抗に負けて減速するみたいなシステムにする
        //長距離飛ぶわけではないけど沢山飛ぶわけではない
        //3つプレイヤーに気づかせることでプレイヤーは面白いと感じる
        //最後まで気づかれないのは絶対ダメ
        _force = Mathf.Lerp(_minForce, _powValue, 0.5f);
        _canKnockBack = true;

    }
    public override void KnockBackMethod()
    {
        if (_force > 0)
        {
            _state = StoneState.KnockBack;
            _force -= _forceDivision;
        }
        else
        {
            _state = StoneState.Normal;
            _force = 0;
        }
        transform.position += (Vector3)_blowAwayDirection * _force * Time.fixedDeltaTime;
    }
}
