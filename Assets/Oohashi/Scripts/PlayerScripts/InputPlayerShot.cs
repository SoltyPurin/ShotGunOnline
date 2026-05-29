using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ShootState
{
    CanShoot, //射撃可能
    OverHeat//オーバーヒート
}

public class InputPlayerShot : MonoBehaviour
{
    [SerializeField,Header("狙ってる方向のスクリプト")]
    private PlayerAiming _playerAiming = default;
    [SerializeField,Header("ノックバックのスクリプト")]
    private PlayerKnockBack _knockBackScriput = default;
    [SerializeField,Header("プレイヤーの移動のスクリプト")]
    private PlayerMove _playerMoveScript = default;
    [SerializeField,Header("ショットガンの判定スクリプト")]
    private ShootRange _shootRange = default;
    [SerializeField,Header("当たり判定を表示するスクリプト")]
    private ShootShape _shootShape = default;
    [SerializeField,Header("レバブルのスクリプト")]
    private ControllerVibelation _vibe = default;
    [SerializeField,Header("ウルトのスクリプト")]
    private UltimateShot _ultShot = default;
    [SerializeField,Header("コインを管理するスクリプト")]
    private SoulKeep _coinKeep = default;
    [SerializeField,Header("プレイヤーのステートを管理")]
    private PlayerStateManager _stateManager = default;
    [SerializeField,Header("銃弾のプール")]
    private BulletPool _bulletPool = default;
    [SerializeField,Header("SEのスクリプト")]
    private PlayerSEControlScript _seScript = default;
    [SerializeField, Header("オーバーヒートのスクリプト")]
    private OverHeat _overHeat = default;
    [Header("プレイヤーのオブジェクト登録")]
    [SerializeField] private GameObject _playerObject;
    [SerializeField, Header("マズルフラッシュのスクリプト登録")]
    private MuzzleFlash _muzzleFlashAnimation = default;
    [SerializeField, Header("ヒットストップのスクリプト")]
    private HitStop _hitstop = default;
    [SerializeField, Header("クリティカル演出のブルーム呼び出しスクリプト")]
    private BloomScript _bloom = default;

    [SerializeField, Header("クリティカルヒットの瞬間の吹き飛び値")]
    private float _criticalPower = 2.4f;

    [SerializeField, Header("クリティカルショットのノックバックの値")]
    private float _criticalKnockBackValue = 1.8f;

    //チャージ減算した後は最低チャージの値まで下げる
    private bool _canDecChargeValue = false;

    [SerializeField, Header("クリティカルショットの猶予時間")]
    private float _criticalGraceTime = 0.1f;

    [SerializeField, Header("ウルト状態遷移のスクリプト")]
    private InputChangeState _inputChengeState = default;

    //瞬間　心重ねて
    private bool _canCriticalShoot = false;

    public bool CanDecChargeValue
    {
        get { return _canDecChargeValue; }
    }

    private float _chargeTime = 0.0f;//チャージしてる時間
    public float ChargeValue
    {
        get { return _chargeTime; }
    }
    //制限されない現在のチャージ時間を測定
    private float _initChargeTime = 0.0f;
    private bool _isCharge = false;//チャージしてるかどうか
    private bool _isDecChargeTime = false; //チャージを減算中かどうか
    public bool IsDecCharge
    {
        get { return _isDecChargeTime; }
    }
    private readonly string FIREBUTTONNAME = "Fire";//撃つボタンの名前
    private bool _hasShotUltimate = false; //ウルトを発動可能か
    public bool HasShotUltimate
    {
        get { return _hasShotUltimate; }
    }
    //射撃ボタンを押したかどうか
    private bool _isPushShootButton = false; 
    public bool IsPushShotButton
    {
        get { return _isPushShootButton; }
    }
    //ステートを所有してる変数
    private ShootState _shootState = ShootState.CanShoot;
    public ShootState ShootState
    {
        set { _shootState = value; } //オーバーヒートのスクリプトから変更
        get { return _shootState; } //SE再生のスクリプトから参照
    }

    //チャージ時間に乗算する値、ご褒美中はこの値が増加する
    private float _bonusMultiplier = 1.0f; 
    public float BonusMultiplier
    {
        get { return _bonusMultiplier; }
    }

    private const float MAXCHARGEVALUE = 2;

    //生存してるか否か
    private bool _isAlive = true;
    public bool IsAlive
    {
        get { return _isAlive; }
    }

    private float _rightTriggerValue = default;
    private void Start()
    {
        _criticalGraceTime += MAXCHARGEVALUE;
    }

    public void DeathMethod()
    {
        _isAlive = false;
    }

    //ウルト射撃後またチャージアニメを表示する時に使用するもの
    private bool _showUltCharge = false;


    private void Update()
    {
        //死んでたら処理は行わない
        if (!_isAlive)
        {
            return;
        }
        //落下中は早期リターンする
        if (_stateManager.PlayerState == PlayerState.Fall) return;
        //MaxChargeDec();
        if(Gamepad.current == null)
        {
            return;
        }
            PadShot();

    }

    private void PadShot()
    {
        //右トリガーから値を取得
        _rightTriggerValue = Gamepad.current.rightTrigger.ReadValue();
        bool isrightTriggerInput = _rightTriggerValue >= 0.9f;
        switch (_stateManager.PlayerState)
        {
            case PlayerState.Normal:
                _isPushShootButton = (isrightTriggerInput || Input.GetButton(FIREBUTTONNAME)) && !_isDecChargeTime /*&& _shootState == ShootState.CanShoot*/;
                break;

            case PlayerState.Ultimate:
                _isPushShootButton = (isrightTriggerInput || Input.GetButtonDown(FIREBUTTONNAME));
                break;

            case PlayerState.Fall:
                break;
        }
        //入力が0.9以上であれば実行判定に移す

    }

    private void Charge()
    {
        _initChargeTime += Time.deltaTime * _bonusMultiplier;
        if (_canCriticalShoot)
        {
            _chargeTime = _criticalPower;
        }
        else
        {
            //チャージ時間に加算
            _chargeTime += Time.deltaTime * _bonusMultiplier;
            //チャージ時間は最大2秒なのでその中に収まるようにする
            _chargeTime = Mathf.Clamp(_chargeTime, 0, MAXCHARGEVALUE);
            //チャージしてる判定にする
            _isCharge = true;

        }
    }


    private void CriticalMethod()
    {
        if(_initChargeTime >= 2 && _initChargeTime < _criticalGraceTime)
        {
            _canCriticalShoot = true;
        }
        else
        {
            _canCriticalShoot = false;
        }
    }
    /// <summary>
    /// ウルトの判定メソッド及びレバブルメソッドを実行
    /// </summary>
    /// <returns>ウルトを撃ってる判定を解除、コイン減算メソッドを実行</returns>
    private IEnumerator UltProtocolWithDelay()
    {
        //レバブル
        _vibe.StartCoroutine(_vibe.UltVibeProtocol());
        //ウルトのスクリプトの判定を出すメソッドを呼ぶ
        _ultShot.UltimateShotProtocol();
        //2秒待たないとコインが連続して減るバグが起きるので2秒待つ
        yield return new WaitForSeconds(2.0f);
        //ウルト発射状態を終了
        _hasShotUltimate = false;
        //ウルト使用した分のコインを減らす
        _coinKeep.UseUltimate();

    }

    private void FixedUpdate()
    {
        //チャージ時間が0以上でチャージ中でない場合
        if (_chargeTime > 0 && !_isCharge)
        {
            ChargeTimeDecrease();
            _isDecChargeTime = true; //チャージ減算中のboolをtrueに
        }
        else
        {
            _isDecChargeTime = false;//チャージ減算終了
        }

        switch (_stateManager.PlayerState)
        {
            case PlayerState.Normal:
                //入力はUpdateで管理する
                //撃つボタンを押しており、チャージ減算中でなく、ステートが射撃可能な時に撃つ
                if (_isPushShootButton && !_canDecChargeValue)
                {
                    if (_shootState == ShootState.OverHeat)
                    {
                        _seScript.PlayDryFire();
                        return;
                    }
                    //当たり判定の計算メソッドを実行
                    _shootRange.CalcChargeAngle(_chargeTime);
                    //クリティカル判定のメソッド
                    CriticalMethod();
                    //チャージするメソッド
                    Charge();
                    _shootRange.StartCharge();
                }
                //撃つボタンを離して、なおかつ現在チャージ中でなく落下中でなかったら解放可能
                else if (!_isPushShootButton && _isCharge && _stateManager.PlayerState != PlayerState.Fall)
                {
                    _initChargeTime = 0;
                    _canDecChargeValue = false;
                    //_initMaxChargetTime = 0;
                    //オーバーヒートの値を加算するメソッド実行
                    _overHeat.ShotCountPlus();
                    //チャージしてる判定を切る
                    _isCharge = false;
                    //ノックバックのスクリプトに現在向いてる方向を渡す
                    //クリティカル判定だった場合プレイヤーの反動はほぼ0
                    if (_canCriticalShoot)
                    {
                        _knockBackScriput.SetDirection(_playerAiming.Direction, _criticalKnockBackValue);
                        _hitstop.CriticalHitStopMethod();
                        _bloom.UseCritical();
                        //当たり判定上の敵にダメージを与えるメソッドを実行
                        _shootRange.ShotgunHitCheck(_chargeTime,true);
                    }
                    else
                    {
                        _shootRange.ShotgunHitCheck(_chargeTime,false   );
                        _knockBackScriput.SetDirection(_playerAiming.Direction, _chargeTime);
                    }
                    //レバブルにチャージ時間を渡して震えさせるメソッドを実行
                    _vibe.ViblationPortocol(_chargeTime);
                    //マズルフラッシュのアニメーション再生メソッドを実行
                    _muzzleFlashAnimation.PlayTheMuzzleFlash();
                    //銃弾(当たり判定無し)の表示
                    _bulletPool.ActiveBullet(_playerAiming.Direction, this.transform.position, _chargeTime);
                }
                break;

            case PlayerState.Ultimate:

                //ウルト使用可能かつウルトチャージエフェクトが非表示なら表示させる。
                if(!_hasShotUltimate && !_showUltCharge)
                {
                    _inputChengeState.ShowUltCharge(true);
                    _showUltCharge = true;
                }

                //ウルトの当たり判定を表示
                _shootShape.UltShape(_playerAiming.Direction);
                //撃つ判定を検知
                //射撃ボタンを押してなおかつウルトを撃ってない時にウルト発射可能
                if (_isPushShootButton && !_hasShotUltimate)
                {
                    //ウルト発射状態に切り替え
                    _hasShotUltimate = true;
                    //ウルトの処理を行うコルーチンを呼び出す
                    StartCoroutine(UltProtocolWithDelay());
                    //マズルフラッシュを再生するメソッドを呼び出す
                    _muzzleFlashAnimation.PlayTheUltFlash();
                    //銃弾のプールにアクティブ化する
                    _bulletPool.ActiveUltBullet(_playerAiming.Direction, this.transform.position);

                    //ウルトチャージエフェクトを非表示
                    _inputChengeState.ShowUltCharge(false);
                    _showUltCharge = false;
                }

                break;

            case PlayerState.Fall:
                //落下中はなにもできない
                break;
        }

    }
    /// <summary>
    /// 発射した後のチャージ時間減算、貯める時間よりも早く戻る
    /// </summary>
    private void ChargeTimeDecrease()
    {
        _chargeTime -= Time.fixedDeltaTime *1.5f;
    }

    /// <summary>
    /// コンボボーナスのチャージ時間短縮の乗算値を変更
    /// </summary>
    /// <param name="bonusValue">コンボスクリプトから渡されるご褒美の値</param>
    public void BonusSet(float bonusValue)
    {
        _bonusMultiplier = bonusValue;
    }

    /// <summary>
    /// チャージ時間の乗算値を元に戻す
    /// </summary>
    public void ResetBonusMultiplier()
    {
        _bonusMultiplier = 1.0f;
    }


}
