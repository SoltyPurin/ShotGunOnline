using Unity.Netcode;
using UnityEngine;

public class ArmorTakeDamage : EnemyTakeDamage
{
    [Header("ガード判定用スクリプト")]
    [SerializeField] protected ArmorKnockBack _armorKnockbackScript = default;
    [Header("ガード時の被ダメージ乗算用")]
    [SerializeField] protected float _guardMultiplier = 250;
    [SerializeField, Header("ガード時エフェクト")]
    protected Animator _guardEffect = default;

    [SerializeField, Header("ガード時最低ダメージ")]
    protected float _guardMinDamage = 100;
    // ダメージ受信用変数
    protected float _damage = 0;

    public override void Start()
    {
        base.Start();
        SetMaxHP(JsonSaver.Instance.EnemyJson.ArmorHP);
    }

    /// <summary>
    /// 被弾ダメージ処理、ウルト時は即死
    /// </summary>
    /// <param name="chargeTime">チャージ時間</param>
    /// <param name="state">プレイヤーステート</param>
    public override void SetTakeDamege(float chargeTime, PlayerState state)
    {
        if (!IsServer || _isDead) return;

        // ウルト時は即死
        if (state == PlayerState.Ultimate)
        {
            _isDead = true;
            _enemyHP.Value = 0;
            if (_hpUI != null) _hpUI.UpdateHP(0);
            PlayArmorDamageVisualClientRpc(false);
            StartCoroutine(DeathProtocol(2));
            return;
        }

        // 前方からの攻撃（ガード）判定
        bool isGuard = _armorKnockbackScript != null && _armorKnockbackScript.IsFromFlont;
        if (isGuard)
        {
            _damage = chargeTime * _guardMultiplier;
            _damage = Mathf.Max(_damage, _guardMinDamage);
            PlayArmorDamageVisualClientRpc(true);
        }
        else
        {
            _damage = chargeTime * _damageMultiplier;
            _damage = Mathf.Max(_damage, _minDamage);
            PlayArmorDamageVisualClientRpc(false);
            if (_playTheSEManager != null) _playTheSEManager.PlayEnemyDamageSound();
        }

        // HP更新
        _enemyHP.Value = (_enemyHP.Value - (int)_damage);
        if (_hpUI != null) _hpUI.UpdateHP(_enemyHP.Value);
        if (_enemyHP.Value <= 0)
        {
            _isDead = true;
            StartCoroutine(DeathProtocol(chargeTime));
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    protected void PlayArmorDamageVisualClientRpc(bool isGuard)
    {
        if (isGuard)
        {
            if (_guardEffect != null) _guardEffect.SetTrigger("Damage");
        }
        else
        {
            if (_damageAnimator != null) _damageAnimator.SetTrigger("Damage");
            if (_damageMaterial != null) _damageMaterial.Damage();
        }
    }
}
