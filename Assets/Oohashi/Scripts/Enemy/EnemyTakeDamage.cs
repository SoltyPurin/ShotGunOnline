using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTakeDamage : MonoBehaviour
{
    protected int _enemyHP = 1000;
    public int EnemyHP
    {
        get { return _enemyHP; }
    }

    [SerializeField, Header("ダメージに乗算する値")]
    protected int _damageMultiplier = 499;
    [SerializeField, Header("ノーチャージの時の最低ダメージ")]
    protected int _minDamage = 200;
    [SerializeField, Header("爆発の時の乗算値")]
    protected int _explosionMultiplier = 1000;
    [SerializeField, Header("死ぬまでのタイムラグ")]
    protected float _deathTimeMultiplier = 2.5f;
    [SerializeField, Header("移動のスクリプト")]
    protected EnemyMove _enemyMove = default;
    [SerializeField, Header("HPを表示するUI")]
    protected EnemyDamageUI _hpUI = default;
    private PlayerCoinDrop _coinControl = default;
    [SerializeField, Header("アニメーションのオブジェクト")]
    protected GameObject _deathObject = default;
    [SerializeField, Header("ダメージエフェクトのアニメーター")]
    protected Animator _damageAnimator = default;
    [Header("スプライトアニメ管理")]
    public DamageMaterial _damageMaterial = default;

    //プレイヤーのオブジェクト及びそれに付随するスクリプト達
    protected GameObject _playerObject = default;
    protected SoulKeep _coinKeep = default;
    private readonly string PLAYERTAGNAME = "Player";
    //SEのスクリプト
    protected SEManager _playTheSEManager = default;

    protected readonly string COMBOCOUNTER_TAGNAME = "ComboCounter";

    protected bool _isDead = false;
    public bool IsDead
    {
        get { return _isDead; }
    }

    public virtual void Start()
    {
        JsonSaver.Instance.LoadAllConfigs();
        _playerObject = GameObject.FindWithTag(PLAYERTAGNAME);
        _coinKeep = _playerObject.GetComponent<SoulKeep>();
        GameObject seManager = GameObject.FindWithTag("SEManager");
        _playTheSEManager = seManager.GetComponent<SEManager>();
        _coinControl = GetComponent<PlayerCoinDrop>();
    }
    /// <summary>
    /// ダメージを喰らったときのメソッド、ウルトと通常で挙動が変わる
    /// </summary>
    /// <param name="chargeTime">チャージ時間</param>
    /// <param name="state">現在のプレイヤーのステート</param>
    public virtual void SetTakeDamege(float chargeTime, PlayerState state)
    {
        //ダメージのアニメーション再生
        _damageAnimator.SetTrigger("Damage");
        _damageMaterial.Damage();

        //プレイヤーのステートがウルトの時に実行
        if (state == PlayerState.Ultimate)
        {
            //体力を速攻0にする
            _isDead = true;
            _hpUI.UpdateHP(0);
            float delayInSeconds = 1.0f;
            float fakeCharge = delayInSeconds * _deathTimeMultiplier;
            StartCoroutine(DeathProtocol(fakeCharge));
        }
        else
        {
            //ダメージはチャージ時間と乗算値を掛けて求める
            float damage = chargeTime * _damageMultiplier;
            //ダメージが低すぎた場合、最低保証をする
            damage = Mathf.Max(damage, _minDamage);
            //hpを更新
            _enemyHP = (_enemyHP - (int)damage);
            _hpUI.UpdateHP(_enemyHP);
            //被弾音を再生
            _playTheSEManager.PlayEnemyDamageSound();
            //体力が0になったらコルーチン起動
            if (_enemyHP <= 0)
            {
                _isDead = true;
                StartCoroutine(DeathProtocol(chargeTime));
            }
        }
    }

    /// <summary>
    /// 反射弾を喰らったときのダメージ
    /// </summary>
    /// <param name="chargeTime"></param>
    public virtual void SetMirrorDamage(float chargeTime)
    {
        //ダメージの代入、HPの書き換え
        float damage = chargeTime * _damageMultiplier;
        damage = Mathf.Max(damage, _minDamage);
        _enemyHP = (_enemyHP - (int)damage);
        _hpUI.UpdateHP(_enemyHP);
        _playTheSEManager.PlayEnemyDamageSound();
        if (_enemyHP <= 0)
        {
            StartCoroutine(DeathProtocol(chargeTime));
        }
    }

    /// <summary>
    /// 落下のダメージ、基本は一発で死ぬ
    /// </summary>
    public virtual void FallDamage()
    {
        if (_isDead)
        {
            return;
        }
        _isDead = true;
        _enemyMove.EnemyState = EnemyState.fall;
        _enemyHP -= _enemyHP;
        if (this.gameObject.activeInHierarchy)
        {
            _playTheSEManager.PlayDropSound();
            StartCoroutine(FallDeathProtocol());
        }
    }
    /// <summary>
    /// 落下の演出及びコンボ加算
    /// </summary>
    /// <returns></returns>
    private IEnumerator FallDeathProtocol()
    {
        if (!this.gameObject.scene.isLoaded)
        {
            yield break;
        }
        _isDead = true;
        ComboCounter counter = GameObject.FindWithTag(COMBOCOUNTER_TAGNAME).GetComponent<ComboCounter>();
        if (counter != null && this.gameObject.activeInHierarchy)
        {
            counter.ComboPlus();
            //落下のアニメーション再生が終わるまで待つ
            yield return new WaitForSeconds(1.8f);
            _playTheSEManager.PlayMoneyDropSound();
            Instantiate(_deathObject, transform.position, Quaternion.identity);
            //ソウルが落ちるアニメーション再生
            _coinControl.CoinDrop(1);

            //滅殺
            Destroy(this.gameObject);
        }

    }

    /// <summary>
    /// 爆発のダメージ、渡された仮の値に爆発の乗算値を掛ける
    /// </summary>
    /// <param name="chargeTime">爆発のプロトコルから渡される仮のチャージ値</param>
    public virtual void SetExplosionDamgage(float chargeTime)
    {
        float damage = chargeTime * _explosionMultiplier;
        _enemyHP = (_enemyHP - (int)damage);
        _hpUI.UpdateHP(_enemyHP);
        if (_enemyHP <= 0)
        {
            _isDead = true;
            StartCoroutine(DeathProtocol(chargeTime));
        }
    }

    /// <summary>
    /// 死亡処理を行うコルーチン
    /// </summary>
    /// <param name="chargeTime">チャージ値、これによって死ぬまでの時間が変わる</param>
    /// <returns></returns>
    public virtual IEnumerator DeathProtocol(float chargeTime)
    {
        if (!this.gameObject.scene.isLoaded)
        {
            yield break;
        }
        //チャージ時間を死ぬまでの乗算値で割って死亡時間を出す
        float timeLeftUntilDeath = chargeTime / _deathTimeMultiplier;
        _playTheSEManager.PlayEnemyDeathSound();
        //死亡時間待つ
        yield return new WaitForSeconds(timeLeftUntilDeath);
        //ComboCounter counter = GameObject.FindWithTag(COMBOCOUNTER_TAGNAME).GetComponent<ComboCounter>();
        //コンボカウンターにプラス
        //counter.ComboPlus();
        _playTheSEManager.PlayMoneyDropSound();
        Instantiate(_deathObject, transform.position, Quaternion.identity);
        _coinControl.CoinDrop(1);

        Destroy(this.gameObject);
    }

    /// <summary>
    /// 岩に接触したときのメソッド、惨たらしい轢殺
    /// </summary>
    public virtual void ContactStoneMethod()
    {
        if (!this.gameObject.scene.isLoaded)
        {
            return;
        }

        _isDead = true;
        //轢殺音再生
        _playTheSEManager.PlayRoadKill();
        Instantiate(_deathObject, transform.position, Quaternion.identity);
        _coinControl.CoinDrop(1);
        Destroy(this.gameObject);
    }
}
