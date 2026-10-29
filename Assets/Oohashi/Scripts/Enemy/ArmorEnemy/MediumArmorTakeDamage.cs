using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MediumArmorTakeDamage : ArmorTakeDamage
{
    #region Serialize変数
    [SerializeField, Header("岩接触時のダメージ")]
    private float _stoneTakeDamage = 700;
    [SerializeField, Header("アルティメット時のダメージ")]
    private float _ultDamage = 1500;
    [SerializeField, Header("落下時のダメージ")]
    private float _fallTakeDamage = 700;
    [SerializeField, Header("ステージに復帰するスクリプト")]
    private BossFallToCliffAndHole _bossFall = default;
    #endregion

    #region 定数
    private readonly string DAMAGEEFFECTTRIGERNAME = "Damage";
    private readonly string FALLCOROUTINENAME = "HoleFallMethod";
    #endregion

    #region 変数
    private Vector2 _returnPosition = new Vector2(0, 10);
    private MidiumCameraSW _cameraSW = default;
    private bool _isFalling = false;
    #endregion

    public override void Start()
    {
        base.Start();
        _cameraSW = GetComponent<MidiumCameraSW>();
        SetMaxHP(JsonSaver.Instance.EnemyJson.ArmorHP);
    }

    /// <summary>
    /// 被弾ダメージ処理、ウルト時は即死ダメージ
    /// </summary>
    /// <param name="chargeTime">チャージ時間</param>
    /// <param name="state">プレイヤーステート</param>
    public override void SetTakeDamege(float chargeTime, PlayerState state)
    {
        if (!IsServer || _isDead) return;

        // プレイヤーステートがウルトならウルトダメージに切り替え
        if (state == PlayerState.Ultimate)
        {
            _damage = _ultDamage;
        }

        bool isGuard = _armorKnockbackScript.IsFromFlont;
        // ガード時の処理
        if (isGuard)
        {
            _damage = chargeTime * _guardMultiplier;
            _damage = Mathf.Max(_damage, _guardMinDamage);
            TakeDamageEffectClientRpc(true);
        }
        else // 正面以外から攻撃を受けた場合の処理
        {
            _damage = chargeTime * _damageMultiplier;
            _damage = Mathf.Max(_damage, _minDamage);
            TakeDamageEffectClientRpc(false);
        }

        // HPからダメージを減算しHPバー更新
        _enemyHP.Value = (_enemyHP.Value - (int)_damage);
        _hpUI.UpdateHP(_enemyHP.Value);
        if (_enemyHP.Value <= 0)
        {
            _isDead = true;
            StartCoroutine(DeathProtocol(chargeTime));
        }
    }

    /// <summary>
    /// 被弾時の演出を再生するメソッド
    /// </summary>
    /// <param name="isGuard">ガード成功か</param>
    [ClientRpc]
    private void TakeDamageEffectClientRpc(bool isGuard)
    {
        if (isGuard)
        {
            _guardEffect.SetTrigger(DAMAGEEFFECTTRIGERNAME);
        }
        else
        {
            _damageAnimator.SetTrigger(DAMAGEEFFECTTRIGERNAME);
            _damageMaterial.Damage();
        }
    }

    /// <summary>
    /// 岩に接触したときのメソッド
    /// </summary>
    public override void ContactStoneMethod()
    {
        if (!IsServer || _isDead) return;

        _damage = _stoneTakeDamage;
        _enemyHP.Value = (_enemyHP.Value - (int)_damage);
        _hpUI.UpdateHP(_enemyHP.Value);

        if (_enemyHP.Value <= 0)
        {
            _isDead = true;
            StartCoroutine(DeathProtocol(1));
        }
    }

    /// <summary>
    /// 落下時のダメージ処理（EnemyUnderRay等の毎フレーム重複呼び出しを防止）
    /// </summary>
    public override void FallDamage()
    {
        if (!IsServer || _isDead || _isFalling) return;

        _isFalling = true;
        _enemyMove.EnemyState = EnemyState.fall;
        _enemyHP.Value -= (int)_fallTakeDamage;
        _hpUI.UpdateHP(_enemyHP.Value);

        FallSoundClientRpc();

        if (_enemyHP.Value <= 0)
        {
            _isDead = true;
            StartCoroutine(DeathProtocol(1));
        }
        else
        {
            // 生存時のみ穴からの復帰演出・復帰処理を行う
            if (_bossFall != null)
            {
                _bossFall.StartCoroutine(FALLCOROUTINENAME);
            }
            StartCoroutine(ReturnStage());
        }
    }

    [ClientRpc]
    private void FallSoundClientRpc()
    {
        if (_playTheSEManager != null)
        {
            _playTheSEManager.PlayDropSound();
        }
    }

    /// <summary>
    /// ステージに復帰するコルーチン
    /// </summary>
    private IEnumerator ReturnStage()
    {
        yield return new WaitForSeconds(2);
        _enemyMove.EnemyState = EnemyState.move;
        transform.position = _returnPosition;
        _isFalling = false;
    }

    public override IEnumerator DeathProtocol(float chargeTime)
    {
        if (_cameraSW != null)
        {
            _cameraSW.SwitchOff();
        }
        return base.DeathProtocol(chargeTime);
    }
}
