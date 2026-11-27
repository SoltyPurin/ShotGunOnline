using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum ShootState
{
    CanShoot, //射撃可能
    OverHeat//オーバーヒート
}

public class InputPlayerShot : Unity.Netcode.NetworkBehaviour
{
    [SerializeField, Header("狙ってる方向のスクリプト")]
    private PlayerAiming _playerAiming = default;
    [SerializeField, Header("ノックバックのスクリプト")]
    private PlayerKnockBack _knockBackScriput = default;
    [SerializeField, Header("プレイヤーの移動のスクリプト")]
    private PlayerMove _playerMoveScript = default;
    [SerializeField, Header("ショットガンの判定スクリプト")]
    private ShootRange _shootRange = default;
    [SerializeField, Header("当たり判定を表示するスクリプト")]
    private ShootShape _shootShape = default;
    [SerializeField, Header("レバブルのスクリプト")]
    private ControllerVibelation _vibe = default;
    [SerializeField, Header("ウルトのスクリプト")]
    private UltimateShot _ultShot = default;
    [SerializeField, Header("コインを管理するスクリプト")]
    private SoulKeep _coinKeep = default;
    [SerializeField, Header("プレイヤーのステートを管理")]
    private PlayerStateManager _stateManager = default;
    [SerializeField, Header("銃弾のプール")]
    private BulletPool _bulletPool = default;
    [SerializeField, Header("SEのスクリプト")]
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
    private float _criticalGraceTime = 0.35f;

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
    ////射撃ボタンを押したかどうか
    //private bool _isPushShootButton = false;
    private NetworkVariable<bool> _netIsPushShootButton = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    public bool IsLocalControl => IsOwner || !IsSpawned;

    public bool IsPushShotButton
    {
        get { return IsLocalControl ? _localPushShootButton : _netIsPushShootButton.Value; }
        //get { return _isPushShootButton; }
    }

    /// <summary>
    /// 通常の射撃ボタン（マウス左、スペース、ゲームパッド右トリガー、Fire）が現在押されているか
    /// </summary>
    public bool IsShootButtonPressed { get; private set; }

    /// <summary>
    /// デバッグ連射モード等で連射入力中かどうか（連射中は通常チャージ・通常発射を抑制する）
    /// </summary>
    public bool IsRapidFireActive { get; set; }
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
    private bool _isKeyboardShootPressed = false;
    private bool _isKeyboardShootDown = false;
    private float _lastShotRpcTime = 0f;
    private const float MIN_SHOT_RPC_INTERVAL = 0.05f;
    private float _lastNormalShotTime = -999f;
    private const float MIN_NORMAL_SHOT_INTERVAL = 0.22f;
    private bool _localPushShootButton = false;
    private bool _suppressMouseFireUntilRelease = false;
    private bool _ultShootRequested = false;

    private void Start()
    {
        _criticalGraceTime += MAXCHARGEVALUE;
        if (_bloom == null)
        {
            var postManager = GameObject.Find("PostManager");
            if (postManager != null) _bloom = postManager.GetComponent<BloomScript>();
        }
        if (_hitstop == null)
        {
            _hitstop = GetComponent<HitStop>();
            if (_hitstop == null) _hitstop = GetComponentInChildren<HitStop>();
        }
        if (_inputChengeState == null)
        {
            _inputChengeState = GetComponent<InputChangeState>();
            if (_inputChengeState == null) _inputChengeState = GetComponentInChildren<InputChangeState>();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // デバッグ用: エディタまたはDevelopment Build時に連射デバッグコンポーネントを自動アタッチ
        if (Debug.isDebugBuild || Application.isEditor)
        {
            if (GetComponent<ShotgunRapidFireDebug>() == null)
            {
                gameObject.AddComponent<ShotgunRapidFireDebug>();
            }
        }
#endif
    }

    public void DeathMethod()
    {
        _isAlive = false;
    }

    //ウルト射撃後またチャージアニメを表示する時に使用するもの
    private bool _showUltCharge = false;

    private void Update()
    {
        // 死んでたら処理は行わない
        if (!_isAlive) return;

        // ★操作しているプレイヤー（Owner、またはオフライン未スポーン時）のみが自身の入力を検出して同期する
        if (IsLocalControl)
        {
            if (LobbyMovement.IsInputBlocked || LobbyViewBase.TopActiveView != null)
            {
                IsShootButtonPressed = false;
                CancelCharge();
                return;
            }

            if (Gamepad.current != null)
            {
                _rightTriggerValue = Gamepad.current.rightTrigger.ReadValue();
            }
            else
            {
                _rightTriggerValue = 0f;
            }

            // キーボード・マウスの射撃入力を取得
            _isKeyboardShootPressed = false;
            _isKeyboardShootDown = false;
            bool isPointerOverUI = IsPointerOverBlockingUI();
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame && isPointerOverUI)
                {
                    _suppressMouseFireUntilRelease = true;
                }
                if (!Mouse.current.leftButton.isPressed)
                {
                    _suppressMouseFireUntilRelease = false;
                }

                // UI上のクリック開始による暴発を防止
                if (!_suppressMouseFireUntilRelease && (_localPushShootButton || !isPointerOverUI))
                {
                    _isKeyboardShootPressed |= Mouse.current.leftButton.isPressed;
                    _isKeyboardShootDown |= Mouse.current.leftButton.wasPressedThisFrame;
                }
            }
            else
            {
                _suppressMouseFireUntilRelease = false;
            }
            if (Keyboard.current != null)
            {
                _isKeyboardShootPressed |= Keyboard.current.spaceKey.isPressed;
                _isKeyboardShootDown |= Keyboard.current.spaceKey.wasPressedThisFrame;
            }

            // 入力判定（旧Input Managerの"Fire"にもマウス左クリックが割り当たるため、UI上クリック時は抑止する）
            bool blockLegacyFire = _suppressMouseFireUntilRelease || (!_localPushShootButton && isPointerOverUI);
            bool isLegacyFirePressed = !blockLegacyFire && Input.GetButton(FIREBUTTONNAME);
            bool isLegacyFireDown = !blockLegacyFire && Input.GetButtonDown(FIREBUTTONNAME);

            bool isrightTriggerInput = _rightTriggerValue >= 0.9f;
            bool isShootPressed = isrightTriggerInput || _isKeyboardShootPressed || isLegacyFirePressed;
            bool isShootDown = isrightTriggerInput || _isKeyboardShootDown || isLegacyFireDown;

            IsShootButtonPressed = isShootPressed;

            bool currentPushState = false;

            switch (_stateManager.PlayerState)
            {
                case PlayerState.Normal:
                    // デバッグ連射中・連射クールタイム中・チャージ減算中は通常チャージ入力を抑制する
                    bool isNormalShotCooldown = (Time.time - _lastNormalShotTime) < MIN_NORMAL_SHOT_INTERVAL;
                    currentPushState = !IsRapidFireActive && !isNormalShotCooldown && isShootPressed && !_isDecChargeTime;
                    break;

                case PlayerState.Ultimate:
                    currentPushState = isShootPressed || isShootDown;
                    if (currentPushState && !_hasShotUltimate)
                    {
                        _ultShootRequested = true;
                    }
                    break;

                case PlayerState.DamageKnockBack:
                case PlayerState.Fall:
                case PlayerState.Movie:
                    currentPushState = false;
                    break;
            }

            _localPushShootButton = currentPushState;

            // 変化時のみネットワーク変数へ書き込み（毎フレーム送信を防止）
            if (IsSpawned && _netIsPushShootButton.Value != currentPushState)
            {
                _netIsPushShootButton.Value = currentPushState;
            }
        }
    }

    private static readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();

    private static bool IsPointerOverBlockingUI()
    {
        if (Mouse.current == null || EventSystem.current == null) return false;
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };
        _uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, _uiRaycastResults);
        for (int i = 0; i < _uiRaycastResults.Count; i++)
        {
            var go = _uiRaycastResults[i].gameObject;
            if (go == null) continue;

            var canvas = go.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                continue;
            }

            var selectable = go.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.IsInteractable())
            {
                return true;
            }
        }
        return false;
    }

    //private void PadShot()
    //{
    //    bool isrightTriggerInput = _rightTriggerValue >= 0.9f;
    //    bool isShootPressed = isrightTriggerInput || _isKeyboardShootPressed || Input.GetButton(FIREBUTTONNAME);
    //    bool isShootDown = isrightTriggerInput || _isKeyboardShootDown || Input.GetButtonDown(FIREBUTTONNAME);

    //    switch (_stateManager.PlayerState)
    //    {
    //        case PlayerState.Normal:
    //            _isPushShootButton = isShootPressed && !_isDecChargeTime;
    //            break;

    //        case PlayerState.Ultimate:
    //            _isPushShootButton = isShootDown;
    //            break;

    //        case PlayerState.Fall:
    //            break;
    //    }
    //}

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
        }
        //チャージしてる判定にする
        _isCharge = true;
    }

    /// <summary>
    /// 被弾時や落下時にチャージ状態を即座にキャンセル・リセットする
    /// </summary>
    public void CancelCharge()
    {
        _isCharge = false;
        _chargeTime = 0f;
        _initChargeTime = 0f;
        _canCriticalShoot = false;
        _canDecChargeValue = false;
        _isDecChargeTime = false;

        if (_showUltCharge)
        {
            if (_inputChengeState != null)
            {
                _inputChengeState.ShowUltCharge(false);
            }
            _showUltCharge = false;
        }

        _localPushShootButton = false;
        _ultShootRequested = false;

        if (this.IsOwner && IsSpawned && _netIsPushShootButton.Value)
        {
            _netIsPushShootButton.Value = false;
        }
    }

    private void CriticalMethod()
    {
        if (_initChargeTime >= 2f && _initChargeTime <= _criticalGraceTime)
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
        if (this.IsOwner)
        {
            //レバブル
            _vibe.StartCoroutine(_vibe.UltVibeProtocol());
            //ウルトのスクリプトの判定を出すメソッドを呼ぶ
            _ultShot.UltimateShotProtocol();
        }
        //2秒待たないとコインが連続して減るバグが起きるので2秒待つ
        yield return new WaitForSeconds(2.0f);
        //ウルト発射状態を終了
        _hasShotUltimate = false;
        if (this.IsOwner)
        {
            //ウルト使用した分のコインを減らす
            _coinKeep.UseUltimate();
        }

    }

    private void FixedUpdate()
    {
        if (_bulletPool == null)
        {
            var poolObj = GameObject.Find("BulletPool");
            if (poolObj != null) _bulletPool = poolObj.GetComponent<BulletPool>();
            if (_bulletPool == null) _bulletPool = FindAnyObjectByType<BulletPool>();
            if (_bulletPool == null) return;
        }

        // ウルト以外のステートになった場合、ウルトチャージエフェクト（青い丸）を確実に非表示にする
        if (_stateManager != null && _stateManager.PlayerState != PlayerState.Ultimate && _showUltCharge)
        {
            if (_inputChengeState != null)
            {
                _inputChengeState.ShowUltCharge(false);
            }
            _showUltCharge = false;
        }

        // 被弾ノックバック中・落下中はチャージをキャンセルして早期リターン
        if (_stateManager.PlayerState == PlayerState.DamageKnockBack || _stateManager.PlayerState == PlayerState.Fall || _stateManager.PlayerState == PlayerState.Movie)
        {
            CancelCharge();
            return;
        }

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

        bool isPushing = IsLocalControl ? _localPushShootButton : _netIsPushShootButton.Value;

        switch (_stateManager.PlayerState)
        {
            case PlayerState.Normal:
                //入力はUpdateで管理する
                //撃つボタンを押しており、チャージ減算中でなく、ステートが射撃可能な時に撃つ
                if (isPushing && !_canDecChargeValue)
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
                    //_shootRange.StartCharge();
                }
                //撃つボタンを離して、なおかつ現在チャージ中で落下中でなかったら解放可能
                else if (!isPushing && _isCharge && _stateManager.PlayerState != PlayerState.Fall)
                {
                    if (IsLocalControl)
                    {
                        float shotCharge = _chargeTime;
                        bool isCrit = _canCriticalShoot;
                        // 最大チャージ（2秒）の少し手前（1.9秒以上）で離しても最大チャージと判定する遊びを持たせる
                        bool isMaxCharge = shotCharge >= (MAXCHARGEVALUE - 0.1f);
                        if (isMaxCharge && shotCharge < MAXCHARGEVALUE)
                        {
                            shotCharge = MAXCHARGEVALUE;
                        }

                        // 通常発射を実行し、非OwnerへワンショットRPCで通知
                        ExecuteShot(shotCharge, isCrit, isMaxCharge);
                        if (IsSpawned)
                        {
                            Vector3 aimDir = _playerAiming != null ? _playerAiming.Direction : Vector3.right;
                            PlayShotEffectsRpc(shotCharge, isCrit, isMaxCharge, aimDir);
                        }
                    }
                    else
                    {
                        // 非Owner側はPlayShotEffectsRpcで正確なタイミング・チャージ量・向きで再生するため、チャージ状態のみリセット
                        _initChargeTime = 0;
                        _canDecChargeValue = false;
                        _canCriticalShoot = false;
                        _isCharge = false;
                    }
                }
                break;

            case PlayerState.Ultimate:

                //ウルト使用可能かつウルトチャージエフェクトが非表示なら表示させる。
                if (IsLocalControl && !_hasShotUltimate && !_showUltCharge)
                {
                    if (_inputChengeState != null) _inputChengeState.ShowUltCharge(true);
                    _showUltCharge = true;
                }

                //ウルトの当たり判定を表示
                if (_shootShape != null && _playerAiming != null)
                {
                    _shootShape.UltShape(_playerAiming.Direction);
                }

                // 操作プレイヤーのみ発射判定を行い、非OwnerへRPCで確実に演出同期（短押し取りこぼし・ステートずれ防止）
                if (IsLocalControl && (isPushing || _ultShootRequested) && !_hasShotUltimate)
                {
                    _ultShootRequested = false;
                    _hasShotUltimate = true;
                    Vector3 aimDir = _playerAiming != null ? _playerAiming.Direction : Vector3.right;

                    StartCoroutine(UltProtocolWithDelay());
                    PlayLocalUltVisuals(aimDir);

                    if (IsSpawned && IsOwner)
                    {
                        PlayUltShotEffectsRpc(aimDir);
                    }
                }

                break;

            case PlayerState.DamageKnockBack:
            case PlayerState.Fall:
                CancelCharge();
                break;
        }

    }
    /// <summary>
    /// 発射した後のチャージ時間減算、貯める時間よりも早く戻る
    /// </summary>
    private void ChargeTimeDecrease()
    {
        _chargeTime = Mathf.Max(0f, _chargeTime - Time.fixedDeltaTime * 1.5f);
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

    /// <summary>
    /// ショットガンの発射処理（通常発射およびデバッグ連射の共通実行メソッド）
    /// </summary>
    public void ExecuteShot(float shotCharge, bool isCrit, bool isMaxCharge, bool ignoreKnockback = false, bool ignoreOverheat = false)
    {
        _initChargeTime = 0;
        _canDecChargeValue = false;
        _canCriticalShoot = false;
        _isCharge = false;
        _lastNormalShotTime = Time.time;

        // オーバーヒート加算
        if (!ignoreOverheat && _overHeat != null)
        {
            _overHeat.ShotCountPlus();
        }

        // 当たり判定・反動・画面エフェクト・振動は撃った本人（Owner、またはオフライン未スポーン時）のみ実行する（複数端末からの2重ダメージ防止）
        if (IsLocalControl)
        {
            if (_bloom == null)
            {
                var postManager = GameObject.Find("PostManager");
                if (postManager != null) _bloom = postManager.GetComponent<BloomScript>();
            }

            // ノックバックのスクリプトに現在向いてる方向を渡す
            // クリティカル判定または最大チャージだった場合、インパクトある演出を行う
            if (isCrit)
            {
                if (!ignoreKnockback && _knockBackScriput != null)
                {
                    _knockBackScriput.SetDirection(_playerAiming.Direction, _criticalKnockBackValue);
                }
                if (!IsRapidFireActive && _hitstop != null) _hitstop.CriticalHitStopMethod();
                if (_bloom != null) _bloom.UseCritical();
                if (_shootRange != null) _shootRange.ShotgunHitCheck(shotCharge, true);
            }
            else
            {
                if (isMaxCharge)
                {
                    if (!IsRapidFireActive && _hitstop != null) _hitstop.CriticalHitStopMethod();
                    if (_bloom != null) _bloom.UseCritical();
                    if (_shootRange != null) _shootRange.ShotgunHitCheck(shotCharge, true);
                }
                else
                {
                    if (_shootRange != null) _shootRange.ShotgunHitCheck(shotCharge, false);
                }

                if (!ignoreKnockback && _knockBackScriput != null)
                {
                    _knockBackScriput.SetDirection(_playerAiming.Direction, shotCharge);
                }
            }

            if (_vibe != null) _vibe.ViblationPortocol(shotCharge);
        }

        // マズルフラッシュのアニメーション再生メソッドを実行
        if (_muzzleFlashAnimation != null)
        {
            _muzzleFlashAnimation.PlayTheMuzzleFlash();
        }

        // 銃弾(当たり判定無し)の表示
        if (_bulletPool != null && _playerAiming != null)
        {
            _bulletPool.ActiveBullet(_playerAiming.Direction, this.transform.position, shotCharge);
        }

        // 発射音を確実に再生（クリティカルまたは最大チャージ時はshotSE_Largeを再生）
        if (_seScript != null)
        {
            _seScript.PlayShotSE(shotCharge, isCrit || isMaxCharge);
        }
    }

    /// <summary>
    /// デバッグ用ショットガン連射実行メソッド
    /// </summary>
    public void FireShotDebug(float chargeTime, bool isCrit, bool isMaxCharge, bool ignoreKnockback, bool ignoreOverheat)
    {
        if (!_isAlive || !IsLocalControl) return;
        if (_stateManager != null && (_stateManager.PlayerState == PlayerState.DamageKnockBack ||
                                      _stateManager.PlayerState == PlayerState.Fall ||
                                      _stateManager.PlayerState == PlayerState.Movie))
        {
            return;
        }

        if (ignoreOverheat && _overHeat != null)
        {
            _overHeat.ResetOverHeat();
            _shootState = ShootState.CanShoot;
        }

        // 発射処理を実行
        ExecuteShot(chargeTime, isCrit, isMaxCharge, ignoreKnockback, ignoreOverheat);

        // マルチプレイ時の非Ownerクライアントへの視覚・音声同期（パケットバースト・キュー枯渇防止のためスロットリング）
        if (IsSpawned)
        {
            if (Time.unscaledTime - _lastShotRpcTime >= MIN_SHOT_RPC_INTERVAL)
            {
                _lastShotRpcTime = Time.unscaledTime;
                Vector3 aimDir = _playerAiming != null ? _playerAiming.Direction : Vector3.right;
                PlayShotEffectsRpc(chargeTime, isCrit, isMaxCharge, aimDir);
            }
        }
    }

    private void PlayLocalUltVisuals(Vector3 aimDirection)
    {
        if (_muzzleFlashAnimation != null)
        {
            _muzzleFlashAnimation.PlayTheUltFlash();
        }

        if (_bulletPool != null)
        {
            Vector3 dir = aimDirection.sqrMagnitude > 0.001f
                ? aimDirection
                : (_playerAiming != null ? _playerAiming.Direction : Vector3.right);
            _bulletPool.ActiveUltBullet(dir, this.transform.position);
        }

        if (_inputChengeState != null)
        {
            _inputChengeState.ShowUltCharge(false);
        }
        _showUltCharge = false;
    }

    [Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Owner)]
    private void PlayUltShotEffectsRpc(Vector3 aimDirection)
    {
        PlayLocalUltVisuals(aimDirection);
    }

    [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Unreliable, InvokePermission = RpcInvokePermission.Owner)]
    private void PlayShotEffectsRpc(float chargeTime, bool isCrit, bool isMaxCharge, Vector3 aimDirection)
    {
        if (_muzzleFlashAnimation != null)
        {
            _muzzleFlashAnimation.PlayTheMuzzleFlash();
        }

        if (_bulletPool != null)
        {
            Vector3 dir = aimDirection.sqrMagnitude > 0.001f
                ? aimDirection
                : (_playerAiming != null ? _playerAiming.Direction : Vector3.right);
            _bulletPool.ActiveBullet(dir, this.transform.position, chargeTime);
        }

        if (_seScript != null)
        {
            _seScript.PlayShotSE(chargeTime, isCrit || isMaxCharge);
        }
    }
}
