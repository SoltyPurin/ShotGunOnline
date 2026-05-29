using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDamageKnockBack : MonoBehaviour
{
    #region 変数
    private Vector2 _contactNormal = default;

    [Header("吹き飛ぶ力")]
    [SerializeField] private float _force = 2;

    [SerializeField, Header("盾持ちに突撃されたときの吹き飛び")]
    private float _armorRushForce = 5;
    [SerializeField, Header("盾持ちに突撃された時のヒットストップ")]
    private float _armorHitStopTime = 0.2f;

    [SerializeField, Header("ボスのミラーバレットが当たったときの吹き飛び")]
    private float _bossMirrorBulletForce = 3;
    [SerializeField, Header("ボスの必殺技オブジェクト")]
    private GameObject _bossMirrorBullet;

    [SerializeField, Header("ステートを取得")]
    private PlayerStateManager _playerState = default;
    [SerializeField,Header("レバブルのスクリプト")] 
    private ControllerVibelation _viblration = default;
    [SerializeField,Header("コインのスクリプト")]
    private SoulKeep _soulKeep = default;
    [SerializeField,Header("メーターのスクリプト")]
    private SoulMeter _soulMeter = default;
    [SerializeField, Header("ステートチェンジのスクリプト")]
    private InputChangeState _inputChangeState = default;

    [SerializeField, Header("コライダー登録")]
    private CircleCollider2D _collider = default;
    [SerializeField, Header("ダメージエフェクト")]
    private DamageVignett _damageVignette = default;
    [SerializeField, Header("無敵時間")]
    private float _invincibleTime = 0.5f;
    private ScoreData _scoreData = default;

    [SerializeField, Header("プレイヤーのアニメ管理")]
    private PlayerAnimation _playerAnim = default;

    [SerializeField, Header("ダメージを食らったときに落とすコイン")]
    private GameObject _dropCoinObject = default;

    [SerializeField,Header("表の見た目")]
    private SpriteRenderer _frontPlayerSprite = default;

    [SerializeField,Header("裏の見た目")]
    private SpriteRenderer _backPlayerSprite = default;

    [SerializeField,Header("スプライトの表裏を管理するスクリプト")]
    private SpriteDirection _spriteDirection = default;

    //被弾回数
    private int _damageCount = 0;
    public int DamageCount
    {
        get { return _damageCount; }
    }

    //無敵時間中かどうかのフラグ
    private bool _isInvincible = false;
    public bool IsInvincible
    {
        get { return _isInvincible; }
    }

    private bool _isShowingSprite = true;

    private float _originForce = default;//吹き飛ぶ力を保存する

    private bool _isKnockBacking = false; //ノックバック中かを確認する
                                          //HPのスプライト
    private HPSprite _hpSprite = default;
    //効果音のスクリプト
    private SEManager _seManager = default;
    //前のステートを保存
    private PlayerState _oldState = default;

    [SerializeField,Header("リアルタイムでスコアを表示するスクリプト")]
    private RealTimeScoreCircle _scoreCircle = default;

    private PlayerTrenble _trenble = default;

    private PlayerKnockBack _knockBack = default;
    #endregion

    #region 定数

    private readonly string ENEMYBULLETTAGNAME = "EnemyBullet";//敵のタグの名前
    private readonly string FALLHOLETAGNAME = "Hole"; //落とし穴のタグ
    private readonly string SEMANAGERTAGNAME = "SEManager";//SEManagerのタグの名前
    private readonly string STONETAGNAME = "Stone";//岩のタグの名前
    private readonly string BOMBTAGNAME = "Bomb";//爆弾のタグの名前
    private readonly string MIRRORBULLETTAGNAME = "MirrorBullet"; //反射弾のタグの名前
    private readonly string MEDIUMARMORTAGNAME = "MediumArmor";//中ボスのタグの名前
    private readonly string HPSPRITETAGNAME = "HPSprite"; //HPのスプライト管理するタグの名前
    private readonly string SCOREDATA = "WaveScore";
    private readonly string WALL_TAG_NAME = "Wall";
    #endregion


    private void Start()
    {
        //タグで探して登録
        _seManager = GameObject.FindWithTag(SEMANAGERTAGNAME).GetComponent<SEManager>();
        _hpSprite = GameObject.FindWithTag(HPSPRITETAGNAME).GetComponent<HPSprite>();
        _knockBack = GetComponent<PlayerKnockBack>();
        GameObject score = GameObject.FindWithTag(SCOREDATA);
        _trenble = GetComponent<PlayerTrenble>();
        if(score != null)
        {
            _scoreData = score.GetComponent<ScoreData>();
        }
        _originForce = _force; //最初のforceを保存
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(STONETAGNAME))
        {
            return;
        }
        else if (collision.gameObject.CompareTag(BOMBTAGNAME))
        {
            return;
        }
        else if (collision.gameObject.CompareTag(WALL_TAG_NAME))
        {
            return;
        }
        else if (collision.gameObject.CompareTag("Rock"))
        {
            return;
        }
        else if (collision.gameObject.CompareTag(MIRRORBULLETTAGNAME))
        {
            //吹き飛ぶ方向のメソッド呼び出し
            SetDirection(_contactNormal, _bossMirrorBulletForce, collision.gameObject);
            //ダメージのレバブルを設定
            _viblration.ViblartionSettingLeftAndRight((Vector2)collision.gameObject.transform.position);
            //ぶつかってきた銃弾を削除
            Destroy(collision.gameObject);
        }
        else if (collision.gameObject.CompareTag(ENEMYBULLETTAGNAME))
        {
            //吹き飛ぶ方向のメソッド呼び出し
            SetDirection(_contactNormal, _force, collision.gameObject);
            //ダメージのレバブルを設定
            _viblration.ViblartionSettingLeftAndRight((Vector2)collision.gameObject.transform.position);
            //ぶつかってきた銃弾を削除
            Destroy(collision.gameObject);
        }
        else if (collision.gameObject.CompareTag(MEDIUMARMORTAGNAME))
        {
            GameObject armor = collision.gameObject;
            ArmorMove armorMove = armor.GetComponent<ArmorMove>();
            if(armorMove.InitState == ArmorState.Rush)
            {
                //ぶつかってきた方向を代入
                _contactNormal = collision.contacts[0].normal;
                StartCoroutine(HitStop(collision,_contactNormal));
            }
            else
            {
                //ぶつかってきた方向を代入
                _contactNormal = collision.contacts[0].normal;
                //吹き飛ぶ方向のメソッド呼び出し
                SetDirection(_contactNormal, _force, collision.gameObject);
                //ダメージのレバブルを設定
                _viblration.ViblartionSettingLeftAndRight((Vector2)collision.gameObject.transform.position);

            }
        }
        else
        {
            //ぶつかってきた方向を代入
            _contactNormal = collision.contacts[0].normal;
            //吹き飛ぶ方向のメソッド呼び出し
            SetDirection(_contactNormal, _force, collision.gameObject);
            //ダメージのレバブルを設定
            _viblration.ViblartionSettingLeftAndRight((Vector2)collision.gameObject.transform.position);
        }
    }

    private IEnumerator HitStop(Collision2D collision,Vector3 direction)
    {
        _trenble.RushTrenble();
        yield return new WaitForSecondsRealtime(_armorHitStopTime);
        //吹き飛ぶ方向のメソッド呼び出し
        SetDirection(direction, _armorRushForce, collision.gameObject);
        //ダメージのレバブルを設定
        _viblration.ViblartionSettingLeftAndRight((Vector2)collision.gameObject.transform.position);

    }

    /// <summary>
    /// ダメージ演出のコルーチン
    /// </summary>
    /// <returns>無敵時間待ってから無敵解除</returns>
    public IEnumerator DamagePerformance()
    {
        _damageCount++;
        _knockBack.TakeDamage();
        // 金を落とす音とダメージ音を再生
        _seManager.PlayMoneyDropSound();
        _seManager.PlayEnemyDamageSound();

        _damageVignette.Damage();
        _hpSprite.ReduceHP();

        if (_scoreData != null)
        {
            _scoreData.TakeDamageCountPlus();
        }

        _trenble.DamageTrenble();

        // コインの情報取得
        int soul = _soulKeep.UseFullSoul;
        int stock = _soulKeep.VStock;

        int dropCoinCount = 0;

        if (soul == 0 && stock > 0)
        {
            // ストックペナルティとして10個ドロップ
            dropCoinCount = 10;
            _soulKeep.ReduceVStock();

        }
        else
        {
            // 通常ドロップ
            dropCoinCount = soul;
            // コインを減らす
            _soulKeep.ReduceCoin();
        }
        // コインをドロップ
        for (int i = 0; i < dropCoinCount; i++)
        {
            GameObject coin = Instantiate(_dropCoinObject, transform.position, Quaternion.identity);
            coin.GetComponent<BoxCollider2D>().enabled = false;
            MoveCoin moveCoin = coin.GetComponent<MoveCoin>();
            if (moveCoin != null)
            {
                moveCoin.MoveStart();
            }
        }

        yield return new WaitForSecondsRealtime(_invincibleTime);
        //無敵解除
        _isInvincible = false;
        this.gameObject.layer = 8;
        switch (_spriteDirection.IsFront)
        {
            case true:
                _frontPlayerSprite.enabled = true;
                break;

            case false:
                _backPlayerSprite.enabled = true;
                break;  
        }

    }
    /// <summary>
    /// 壁にぶつかった時に力を0にする
    /// </summary>
    public void CollisionWall()
    {
        _force = 0;
    }

    /// <summary>
    /// 被弾回数をリセット
    /// </summary>
    public void ResetDamageCount()
    {
        _damageCount = 0;
    }

    public void SetWarpDirection(Vector2 direction)
    {
        _contactNormal = direction;
    }

    /// <summary>
    /// 爆発でノックバックするスクリプト
    /// </summary>
    /// <param name="direction">吹き飛ぶ方向</param>
    /// <param name="power">吹き飛ぶ力</param>
    public void SetDirection(Vector3 direction, float power, GameObject collision)
    {
        //無敵時間中は実行しない
        if (_isInvincible)
        {
            return;
        }
        //無敵判定のフラグをON
        _isInvincible = true;
        this.gameObject.layer = 10;  
        //ダメージの演出コルーチン呼び出し
        StartCoroutine(DamagePerformance());

        //ノックバック中でなければ処理
        if (!_isKnockBacking)
        {
            //前のステートを保存
            _oldState = _playerState.PlayerState;
            //ノックバックのフラグをON
            _isKnockBacking = true;
            //チャージ時間の乗を求める
            float powValue = Mathf.Pow(power, 5f);
            //最低1からpowValueまでの間で線形補正を行い、チャージ値ごとの吹き飛ばしをなめらかに
            _force = Mathf.Lerp(3, powValue, 0.5f);
            _contactNormal = direction;
            _viblration.ViblartionSettingLeftAndRight((Vector2)collision.gameObject.transform.position);
            //見てる方向と反対方向に力を加える
        }
        else
        {
            //前のステートを保存
            _oldState = _playerState.PlayerState;
            //ノックバックのフラグをON
            _isKnockBacking = true;
            //乗算しないように強制的に1の力を与える
            _force = 1;
            //ランダムな方向を生成
            Vector2 randomOffset = Random.insideUnitCircle.normalized * 0.2f;
            //次に吹き飛ぶ方向を設定
            Vector2 newDirection = ((Vector2)direction + randomOffset).normalized;
            //代入
            _contactNormal = newDirection;
        }
    }

    /// <summary>
    /// 落下中に吹き飛ばないように力をリセット
    /// </summary>
    public void Fall()
    {
        _force = 0;
    }

    /// <summary>
    /// 吹き飛びの力をリセットして元の値に戻す
    /// </summary>
    public void ResetForce()
    {
        _force = 2f;
    }

    private void FixedUpdate()
    {
        //落下中は処理を行わない
        if (_playerState.PlayerState == PlayerState.Fall)
        {
            return;
        }
        //ノックバック中だったら処理
        if (_isKnockBacking)
        {
            _playerAnim.KnockBack();
            //吹き飛び方向に移動させる
            transform.position += (Vector3)_contactNormal * _force * Time.fixedDeltaTime;
            //ステートをダメージノックバックにする
            _playerState.DamageKnockBackState();
            //吹き飛び力を減算
            _force -= 0.5f;
            //吹き飛びが0以下になったら実行
            if (_force <= 0)
            {
                Debug.Log(_oldState);

                _playerAnim.Wait();
                //力を最初のやつにリセット
                _force = _originForce;
                //ノックバックのフラグをOFFにする
                _isKnockBacking = false;
                //前のステートがウルトだった場合実行
                if (_oldState == PlayerState.Ultimate)
                {
                    //所有してるコインがウルトの必要枚数を下回ってるかどうか確認
                    if (_soulKeep.VStock <= 0)
                    {
                        //ステートをノーマルに変更
                        _playerState.NormalState();
                        _inputChangeState.ToNormalAnimation();
                    }
                    else
                    {
                        //ウルトを継続
                        _playerState.UltimateState();
                    }
                }
                else
                {
                    //前のステートがウルト以外だったらステートをノーマル継続
                    _playerState.NormalState();
                    _inputChangeState.ToNormalAnimation();
                }
            }
        }

        if (_isInvincible)
        {
            _isShowingSprite = !_isShowingSprite;
            switch (_spriteDirection.IsFront)
            {
                case true:
                    _frontPlayerSprite.enabled = _isShowingSprite;
                    break;

                case false:
                    _backPlayerSprite.enabled = _isShowingSprite;
                    break;
            }
        }
    }
}
