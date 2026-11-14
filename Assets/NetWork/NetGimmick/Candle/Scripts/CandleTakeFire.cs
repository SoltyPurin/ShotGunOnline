using System;
using Unity.Netcode;
using UnityEngine;

public class CandleTakeFire : NetworkBehaviour
{
    #region 変数
    public event Action<bool> IsCandleTakeFire;

    [SerializeField, Header("火が消えるまでの時間")]
    private float _timeUntileItFireOff = 10.0f;
    [SerializeField, Header("火が弱まるまでの時間")]
    private float _timeUntilItWeakens = 5.0f;

    private float _currentFireOnTime = 0;

    private NetworkVariable<bool> _isFireOn = new NetworkVariable<bool>();
    public bool IsFireOn
    {
        get { return _isFireOn.Value; }
    }

    private NetworkVariable<bool> _isCleared = new NetworkVariable<bool>();

    private bool _isFireOff = true;
    private Animator _fireAnimator = default;
    private float _middleShotUnder = 0.6f;
    private float _middleShotCeil = 1.4f;
    #endregion
    #region 定数
    private readonly string FIRE_ON = "FireOn";
    private readonly string FIRE_OFF = "FireOff";
    private readonly string FIRE_WEAKER = "FireWeaker";
    #endregion

    private void Start()
    {
        _fireAnimator = GetComponentInChildren<Animator>();
        _fireAnimator.SetTrigger(FIRE_OFF);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isFireOn.OnValueChanged += OnFireChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _isFireOn.OnValueChanged -= OnFireChanged;
    }

    private void OnFireChanged(bool prevBool,bool curBool)
    {
        IsCandleTakeFire?.Invoke(curBool);
    }
    public void TakeFire(float chargeTime)
    {
        if (_isCleared.Value)
        {
            return;
        }
        //与えられたチャージ時間が中間ショットの値の中に納まってなかった場合は処理をしない
        if(chargeTime < _middleShotUnder || chargeTime > _middleShotCeil)
        {
            return;
        }

        if (IsServer)
        {
            ExecuteTakeFire();
        }
        else
        {
            RequestTakeFireServerRpc();
        }

        _isFireOn.Value = true;
        _isFireOff = false;
        _currentFireOnTime = 0;
        _fireAnimator.ResetTrigger(FIRE_WEAKER);
        _fireAnimator.ResetTrigger(FIRE_OFF);
        _fireAnimator.SetTrigger(FIRE_ON);

    }
    /// <summary>
    /// クライアントからサーバーへ着火を要求する RPC 
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestTakeFireServerRpc()
    {
        ExecuteTakeFire();
    }

    /// <summary>
    /// サーバー上で実行される着火確定処理
    /// </summary>
    private void ExecuteTakeFire()
    {
        // 全プレイヤー端末（ホスト＋クライアント）で一斉に着火アニメーションとタイマーを起動
        StartFireClientRpc();
    }

    /// <summary>
    /// 全端末で着火状態・アニメーション・タイマーを同期開始する ClientRpc
    /// </summary>
    [ClientRpc]
    private void StartFireClientRpc()
    {
        _isFireOn.Value = true;
        _isFireOff = false;
        _currentFireOnTime = 0;

        if (_fireAnimator != null)
        {
            _fireAnimator.ResetTrigger(FIRE_WEAKER);
            _fireAnimator.ResetTrigger(FIRE_OFF);
            _fireAnimator.SetTrigger(FIRE_ON);
        }
    }
    private void FixedUpdate()
    {
        if (_isCleared.Value)
        {
            return;
        }
        if (!_isFireOn.Value)
        {
            return;
        }
        _currentFireOnTime += Time.fixedDeltaTime;

        if(_currentFireOnTime > _timeUntilItWeakens)
        {
            Debug.Log("炎弱化");
            _fireAnimator.ResetTrigger(FIRE_OFF);
            _fireAnimator.SetTrigger(FIRE_WEAKER);
        }

        if (_isFireOff)
        {
            return;
        }
        if(_currentFireOnTime > _timeUntileItFireOff)
        {
            Debug.Log("炎消失");
            _fireAnimator.ResetTrigger(FIRE_WEAKER);
            _fireAnimator.SetTrigger(FIRE_OFF);
            _isFireOff = true;
            _isFireOn.Value = false;
        }
    }
    /// <summary>
    /// 仮に全部着火したら着火しっぱなしなるときはこれが呼び出される
    /// </summary>
    public void ClearJudgementConfirmed()
    {
        if (!IsServer || _isCleared.Value)
        {
            return;
        }

        _isCleared.Value = true;
        if (IsServer)
        {
            ExecuteTakeFire();
        }
        else
        {
            RequestTakeFireServerRpc();
        }
    }
}
