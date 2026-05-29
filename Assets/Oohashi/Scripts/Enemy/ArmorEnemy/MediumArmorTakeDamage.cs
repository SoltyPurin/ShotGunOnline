using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public class MediumArmorTakeDamage : ArmorTakeDamage
{
    #region Serialize変数
    [SerializeField, Header("岩で喰らうダメージ")]
    private float _stoneTakeDamage = 700;
    [SerializeField, Header("ウルトで喰らうダメージ")]
    private float _ultDamage = 1500;
    [SerializeField, Header("落下で喰らうダメージ")]
    private float _fallTakeDamage = 700;
    [SerializeField, Header("ステージに戻るスクリプト")]
    private BossFallToCliffAndHole _bossFall = default;
    #endregion

    #region 定数

    private readonly string DAMAGEEFFECTTRIGERNAME = "Damage";
    private readonly string FALLCOROUTINENAME = "HoleFallMethod";
    #endregion

    #region 変数

    private Vector2 _returnPosition = new Vector2(0, 10);

    private MidiumCameraSW _cameraSW = default;

    #endregion

    public override void Start()
    {
        base.Start();
        _cameraSW = GetComponent<MidiumCameraSW>();
        _enemyHP = JsonSaver.Instance.EnemyJson.ArmorHP;
        if (_hpUI != null)
        {
            //最初にHP表示のバーに最大hpを設定
            _hpUI.Initialize(_enemyHP);
        }

    }
    /// <summary>
    /// 中ボスはウルト死なないので処理が別
    /// </summary>
    /// <param name="chargeTime">チャージ時間</param>
    /// <param name="state">プレイヤーのステート</param>
    public override void SetTakeDamege(float chargeTime, PlayerState state)
    {
        //プレイヤーのステートがウルトだったらダメージをウルトダメージに切り替える
        //これはガード不可能
        if (state == PlayerState.Ultimate)
        {
            _damage = _ultDamage;
        }
        bool isGuard = _armorKnockbackScript.IsFromFlont;
        //ガードした場合の処理
        if (isGuard)
        {
            _damage = chargeTime * _guardMultiplier;
            _damage = Mathf.Max(_damage, _guardMinDamage);
            _guardEffect.SetTrigger(DAMAGEEFFECTTRIGERNAME);
        }
        else //側面などから攻撃した場合の処理
        {
            _damage = chargeTime * _damageMultiplier;
            _damage = Mathf.Max(_damage, _minDamage);
            _damageAnimator.SetTrigger(DAMAGEEFFECTTRIGERNAME);
            _damageMaterial.Damage();

        }
        //HPからダメージ分引いて代入、HPバーを更新
        _enemyHP = (_enemyHP - (int)_damage);
        _hpUI.UpdateHP(_enemyHP);
        if (_enemyHP <= 0)
        {
            StartCoroutine(DeathProtocol(chargeTime));
        }

    }

    /// <summary>
    /// 岩に接触したときのメソッド、通常と違い一撃では死なない
    /// </summary>
    public override void ContactStoneMethod()
    {
        //ダメージに岩の接触ダメージを代入
        _damage = _stoneTakeDamage;
        //hpを減らしてhpバーを更新
        _enemyHP = (_enemyHP - (int)_damage);
        _hpUI.UpdateHP(_enemyHP);
        //体力が0になったら1秒後に死亡のコルーチン実行
        if (_enemyHP <= 0)
        {
            StartCoroutine(DeathProtocol(1));
        }
    }

    /// <summary>
    /// 落下のダメージ
    /// </summary>
    public override void FallDamage()
    {
        //ステートを落下中にする
        _enemyMove.EnemyState = EnemyState.fall;
        //hpから落下ダメージを引いて体力バーを更新
        _enemyHP -= (int)_fallTakeDamage;
        _hpUI.UpdateHP(_enemyHP);
        //落下の効果音鳴らす
        _playTheSEManager.PlayDropSound();
        //復帰のコルーチン起動
        _bossFall.StartCoroutine(FALLCOROUTINENAME);
        //ステージに戻る
        StartCoroutine(ReturnStage());
        if (_enemyHP <= 0)
        {
            StartCoroutine(DeathProtocol(1));
        }
    }

    /// <summary>
    /// ステージに戻ってくるコルーチン
    /// </summary>
    /// <returns>アニメーションの再生時間分待つ</returns>
    private IEnumerator ReturnStage()
    {
        yield return new WaitForSeconds(2);
        //移動のステートに変更
        _enemyMove.EnemyState = EnemyState.move;
        //復活地点に戻す
        this.transform.position = _returnPosition;

    }

    public override IEnumerator DeathProtocol(float chargeTime)
    {
        _cameraSW.SwitchOff();
        return base.DeathProtocol(chargeTime);
    }

}
