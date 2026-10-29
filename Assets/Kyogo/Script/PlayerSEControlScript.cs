using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーのSEをコントロールするスクリプト
/// </summary>
public class PlayerSEControlScript : NetworkBehaviour
{
    [SerializeField, Header("効果音のクールタイム")]
    private float _seCoolTime = 0.1f;
    private bool _canPlaySE = true;
    [SerializeField, Header("オブジェクト")]
    private GameObject playerObj;

    [SerializeField, Header("通常攻撃効果音")]
    private AudioClip chargeSE;
    [SerializeField]
    private AudioClip shotSE_Small;
    [SerializeField]
    private AudioClip shotSE_Medium;
    [SerializeField]
    private AudioClip shotSE_Large;
    [SerializeField,Header("チャージマックス音")]
    private AudioClip chargeSE_MaxCharge;
    [SerializeField, Header("最大チャージ待機音")]
    private AudioClip shotSE_WaitWithMaxCharge;
    [SerializeField, Header("必殺技効果音")]
    private AudioClip shotSE_SpecialAttack;
    [SerializeField, Header("オーバーヒートの音")]
    private AudioClip _overHeatSE = default;
    [SerializeField, Header("オーバーヒート時の空撃ちの音")]
    private AudioClip _dryFire = default;
    [SerializeField, Header("ウルト使用可能の時の音")]
    private AudioClip _canUseUltSE = default;
    [SerializeField, Header("ソウルを入手した時の音")]
    private AudioClip _getSoulSE = default;


    [SerializeField]
    private AudioClip ChangeSE_SpecialAttack_ON;
    [SerializeField]
    private AudioClip ChangeSE_SpecialAttack_OFF;
    [SerializeField, Header("その他効果音")]
    private AudioClip[] Walk_SE;
    [SerializeField]
    private AudioClip DamageSE;
    [SerializeField]
    private AudioClip SlideSE;

    [SerializeField, Header("オーディオミキサー")]
    private AudioMixer _mixer = default;
    [SerializeField, Header("audioSource")]
    private AudioSource audioSource;
    [SerializeField,Tooltip("ショットの射撃音再生用オーディオソース")]
    private AudioSource audioSource2;
    [SerializeField]
    private AudioSource audioSource_Walk;
    [SerializeField, Header("最大チャージの待機音出力先")]
    private AudioSource audioSource_ChargeWait;
    [SerializeField, Header("発射音区分下限値")]//SmallはMedium以下の全てなのでなし
    float Shotgun_Shot_Large;
    [SerializeField]
    float Shotgun_Shot_Medium;

    private float _rightTriggerValue = default;
    private float _lastChargeValue = 0f; // 離す直前のチャージ量を保持

    //Script
    InputPlayerShot _inputPlayerShot;
    PlayerStateManager _PlayerStateManager;
    UltimateShot _UltimateShot;
    Rigidbody2D _PlayerRigidbody;
    private bool charge_wait = false; // 前フレームのチャージ状態を記録
    private readonly string FIREBUTTONNAME = "Fire";


    //足音用にタイマーをセット
    [SerializeField, Header("足音の間隔")]
    private float walkingInterval = 1.0f;

    private float walkingTimer = 0f;

    //必殺技のSEが同時にならないようにするロック用変数
    private bool wasShotUltimate = false;

    //SEが再生中かどうか
    private bool _isMaxPlaying = false;

    //直前の状態が通常攻撃か、必殺技の状態かを保存する用の変数
    private PlayerState _prevState;
    //敵にあたったときのタグ管理--------------------------------------------------------------------------------
    string[] HitTarget = { "Enemy", "Armor", "Boss" };

    private bool _canShooting = default;

    void Start()
    {
        //Scriptを持ってきている
        _inputPlayerShot = playerObj.GetComponent<InputPlayerShot>();
        _PlayerStateManager = playerObj.GetComponent<PlayerStateManager>();
        _PlayerRigidbody = playerObj.GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        //ボタン管理--------------------------------------------------------------------------------
        if (!IsOwner)
        {
            return;
        }

        if (_inputPlayerShot != null)
        {
            _canShooting = _inputPlayerShot.IsPushShotButton;
        }
        else
        {
            if (Gamepad.current != null)
            {
                _rightTriggerValue = Gamepad.current.rightTrigger.ReadValue();//ゲームパッドのRTの押し込み具合を見ている
            }
            bool isrightTriggerInput = _rightTriggerValue >= 0.9f;//RTが強く（90%以上)押されているのかを判定する
            _canShooting = (isrightTriggerInput || Input.GetButton(FIREBUTTONNAME));//True=Charge中
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || _inputPlayerShot == null || _PlayerStateManager == null) return;

        float currentCharge = _inputPlayerShot.ChargeValue;

        // チャージ中の数値を毎フレーム保存（ボタンを離してリセットされる前の値を保持）
        if (_canShooting && currentCharge > 0)
        {
            _lastChargeValue = currentCharge;
        }
        //チャージ開始--------------------------------------------------------------------------------

        if (_canShooting && !charge_wait && !_inputPlayerShot.IsDecCharge
            && _PlayerStateManager.PlayerState == PlayerState.Normal
            && _inputPlayerShot.ShootState == ShootState.CanShoot)
        {
            if (audioSource2 != null && chargeSE != null)
            {
                audioSource2.clip = chargeSE;
                audioSource2.loop = false;
                audioSource2.Play();
            }
        }

        // 射撃ボタンを離した際のチャージ音停止
        if (!_canShooting && charge_wait)
        {
            if (audioSource2 != null)
            {
                audioSource2.Stop();
            }
        }

        // 最大チャージ待機音の管理
        if (currentCharge >= 2f && !_isMaxPlaying && _canShooting)
        {
            if (audioSource != null && chargeSE_MaxCharge != null)
            {
                audioSource.PlayOneShot(chargeSE_MaxCharge);
            }
            if (audioSource_ChargeWait != null && shotSE_WaitWithMaxCharge != null)
            {
                audioSource_ChargeWait.clip = shotSE_WaitWithMaxCharge;
                audioSource_ChargeWait.loop = true;
                audioSource_ChargeWait.Play();
            }
            _isMaxPlaying = true;
        }
        else if ((currentCharge < 2f || !_canShooting) && _isMaxPlaying)
        {
            if (audioSource_ChargeWait != null)
            {
                audioSource_ChargeWait.Stop();
            }
            _isMaxPlaying = false;
        }
        charge_wait = _canShooting;

        //必殺技の管理--------------------------------------------------------------------------------
        //ここをできればあたったかどうかで違う音を鳴らしたい。あたったときはそれっぽいけど、当たらなかったときに音がズレてキモい。(6/16清水)
        bool nowShotUltimate = _inputPlayerShot.HasShotUltimate;
        if (nowShotUltimate && !wasShotUltimate)
        {
            if (_UltimateShot)
            {
                audioSource.PlayOneShot(shotSE_SpecialAttack);
            }
            else
            {
                audioSource.PlayOneShot(shotSE_Large);
            }

        }
        wasShotUltimate = nowShotUltimate;


        //通常攻撃と必殺技の切り替え管理--------------------------------------------------------------------------------
        PlayerState currentState = _PlayerStateManager.PlayerState;

        // 【起動音】Normalなど → Ultimate に切り替わった瞬間
        if (_prevState != PlayerState.Ultimate && currentState == PlayerState.Ultimate)
        {
            audioSource.PlayOneShot(ChangeSE_SpecialAttack_ON); // 必殺技 起動音
            audioSource.PlayOneShot(SlideSE);
        }

        // 【解除音】Ultimate → Normal に戻った瞬間
        if (_prevState == PlayerState.Ultimate && currentState == PlayerState.Normal)
        {
            audioSource.PlayOneShot(ChangeSE_SpecialAttack_OFF); // 必殺技 解除音（変数名は仮）
            audioSource.PlayOneShot(SlideSE);
        }

        _prevState = currentState; // 前の状態を更新

        //足音の管理
        // 移動しているか確認（速度が一定以上なら移動中とみなす）
        if (_PlayerRigidbody.linearVelocity.magnitude > 0.1f)
        {
            walkingTimer += Time.deltaTime;

            if (walkingTimer >= walkingInterval)
            {
                walkingTimer = 0f;
                int index = Random.Range(0, Walk_SE.Length);
                audioSource_Walk.PlayOneShot(Walk_SE[index]);
            }
        }
        else
        {
            // 動いていなければタイマーをリセット（連続再生防止）
            walkingTimer = 0f;
        }

    }

    /// <summary>
    /// クールタイム解除
    /// </summary>
    /// <returns></returns>
    private IEnumerator SECoolTime()
    {
        yield return new WaitForSeconds(_seCoolTime);
        _canPlaySE = true;
    }

    /// <summary>
    /// 効果音のクールタイムをスタート
    /// </summary>
    private void CallSECoolTime()
    {
        _canPlaySE = false;
        StartCoroutine(SECoolTime());
    }


    //敵にあたったときの管理--------------------------------------------------------------------------------
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (System.Array.Exists(HitTarget, tag => collision.gameObject.CompareTag(tag)))
        {
            PlayDamageSE();
        }
    }

    /// <summary>
    /// 被ダメージ音を再生する
    /// </summary>
    public void PlayDamageSE()
    {
        if (audioSource != null && DamageSE != null)
        {
            audioSource.PlayOneShot(DamageSE);
        }
    }

    /// <summary>
    /// 射撃音を再生する（InputPlayerShotから直接呼び出し）
    /// </summary>
    /// <param name="chargeValue">発射時のチャージ量</param>
    /// <param name="isCritical">クリティカルまたは最大チャージフラグ</param>
    public void PlayShotSE(float chargeValue, bool isCritical = false)
    {
        if (audioSource2 != null)
        {
            audioSource2.Stop(); // チャージ音を停止
        }
        if (audioSource_ChargeWait != null)
        {
            audioSource_ChargeWait.Stop(); // 最大チャージ待機音を停止
            _isMaxPlaying = false;
        }

        AudioClip clipToPlay;
        float normalizedCharge = chargeValue / 2f;

        if (isCritical || normalizedCharge >= Shotgun_Shot_Large || chargeValue >= 1.8f)
        {
            clipToPlay = shotSE_Large;
        }
        else if (normalizedCharge >= Shotgun_Shot_Medium)
        {
            clipToPlay = shotSE_Medium;
        }
        else
        {
            clipToPlay = shotSE_Small;
        }

        if (audioSource != null && clipToPlay != null)
        {
            if (_inputPlayerShot != null && _inputPlayerShot.BonusMultiplier > 0.01f)
            {
                audioSource.pitch = _inputPlayerShot.BonusMultiplier;
                if (_mixer != null)
                {
                    float pitchCorrection = 1f / _inputPlayerShot.BonusMultiplier;
                    _mixer.SetFloat("Pitch", pitchCorrection);
                }
            }
            else
            {
                audioSource.pitch = 1f;
            }

            audioSource.PlayOneShot(clipToPlay);
        }
    }

    public void PlayTheOverHeat()
    {
        audioSource.PlayOneShot(_overHeatSE);
    }

    public void PlayCanUseUltSound()
    {
        audioSource.PlayOneShot(_canUseUltSE);
    }

    public void PlayGetSoulSE()
    {
        if (!_canPlaySE)
        {
            return;
        }
        audioSource.PlayOneShot(_getSoulSE);
        CallSECoolTime();
    }

    public void PlayDryFire()
    {
        if (!_canPlaySE)
        {
            return;
        }
        audioSource.PlayOneShot(_dryFire);
        CallSECoolTime();
    }

}