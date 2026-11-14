using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BombTakeDamage : EnemyTakeDamage
{
    [SerializeField, Header("自爆プロトコルのスクリプト")]
    private BombProtocol _bombProtocol = default;

    private bool _hasExploted = false;

    public override void Start()
    {
        base.Start();
        SetMaxHP(JsonSaver.Instance.EnemyJson.BombHP);
        if (_bombProtocol == null)
        {
            _bombProtocol = GetComponent<BombProtocol>();
        }
    }

    /// <summary>
    /// 爆発のダメージ設定
    /// </summary>
    /// <param name="chargeTime">チャージ時間</param>
    public override void SetExplosionDamgage(float chargeTime)
    {
        if (!IsServer || _hasExploted) return;
        float damage = chargeTime * _explosionMultiplier;
        _enemyHP.Value = (_enemyHP.Value - (int)damage);
        if (_hpUI != null) _hpUI.UpdateHP(_enemyHP.Value);
        if (_enemyHP.Value <= 0)
        {
            _isDead = true;
            _hasExploted = true;
            if (_bombProtocol != null)
            {
                _bombProtocol.BombCircleCheck();
            }
            NormalDeath();
        }
    }

    /// <summary>
    /// 死亡時のコルーチン
    /// </summary>
    /// <param name="chargeTime">チャージ時間</param>
    /// <returns></returns>
    public override IEnumerator DeathProtocol(float chargeTime)
    {
        if (!IsServer) yield break;
        if (!gameObject.scene.isLoaded) yield break;

        _isDead = true;
        float timeLeftUntilDeath = chargeTime / _deathTimeMultiplier;

        GameObject counterObj = GameObject.FindWithTag(COMBOCOUNTER_TAGNAME);
        if (counterObj != null)
        {
            ComboCounter counter = counterObj.GetComponent<ComboCounter>();
            if (counter != null && gameObject.activeInHierarchy)
            {
                counter.ComboPlus();
            }
        }

        PlayDeathEffectsClientRpc(_shouldDropCoin);

        yield return new WaitForSeconds(timeLeftUntilDeath);

        if (_bombProtocol != null)
        {
            _bombProtocol.BombCircleCheck();
        }

        NormalDeath();
    }

    /// <summary>
    /// プレイヤーに接触したときの死亡
    /// </summary>
    public void PlayerCollision()
    {
        NormalDeath();
    }

    /// <summary>
    /// 爆発・自爆による死亡（安全なネットワークDespawn）
    /// </summary>
    public void NormalDeath()
    {
        if (!IsServer) return;

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
}
