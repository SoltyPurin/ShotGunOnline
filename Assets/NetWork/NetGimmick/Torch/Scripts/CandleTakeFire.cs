using Unity.Netcode;
using UnityEngine;

public class CandleTakeFire : NetworkBehaviour
{
    #region 変数
    [SerializeField, Header("火が消えるまでの時間")]
    private float _timeUntileItFireOff = 10.0f;
    [SerializeField, Header("火が弱まるまでの時間")]
    private float _timeUntilItWeakens = 5.0f;

    private float _currentFireOnTime = 0;

    private bool _isFireOn = false;
    public bool IsFireOn
    {
        get { return _isFireOn; }
    }
    private bool _isFireOff = true;
    private bool _isClear = false;
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
    //[ClientRpc]
    public void TakeFire(float chargeTime)
    {
        if (_isClear)
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

        _isFireOn = true;
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
        _isFireOn = true;
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
        if (!_isFireOn)
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
        }
    }
}
