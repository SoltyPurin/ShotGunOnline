using Unity.Netcode;
using UnityEngine;

public class SoulKeep : NetworkBehaviour
{
    [SerializeField] private SoulMeter _soulMeter;
    [SerializeField, Header("効果音")]
    private PlayerSEControlScript _seScript = default;
    [SerializeField, Header("プレイヤーアニメ管理")]
    private PlayerAnimation _playerAnimation = default;
    [SerializeField, Header("プラスカウントを表示するスクリプト")]
    private SoulCountUI _soulCountUI = default;

    private NetworkVariable<int> _vStock = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int VStock => _vStock.Value;

    private NetworkVariable<int> _useFullSoul = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int UseFullSoul => _useFullSoul.Value;

    private const int USEULTIMATE_NEEDSOUL = 10;
    public int NeedUseUltimateCoin => USEULTIMATE_NEEDSOUL;

    // vStockがMAX（3以上）かどうかをプロパティで判定
    public bool IsVStockMax => _vStock.Value >= 3;

    private void Start()
    {
        //if (_soulMeter == null)
        //{
        //    _soulMeter = GameObject.Find("Soul_Bottle").GetComponent<SoulMeter>();
        //}

        // 自分のプレイヤー（ローカル）である場合のみ画面UIを取得する
        if (IsOwner && _soulMeter == null)
        {
            GameObject bottleObj = GameObject.Find("Soul_Bottle");
            if (bottleObj != null)
            {
                _soulMeter = bottleObj.GetComponent<SoulMeter>();
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        //base.OnNetworkSpawn();
        //_vStock.OnValueChanged += OnVStockChanged;
        //_useFullSoul.OnValueChanged += OnSoulChanged;

        //// 初期表示の同期
        //UpdateSoulUI(_useFullSoul.Value);
        //UpdateVStockUI(_vStock.Value);
        base.OnNetworkSpawn();
        _vStock.OnValueChanged += OnVStockChanged;
        _useFullSoul.OnValueChanged += OnSoulChanged;

        //  自分が操作するプレイヤー（IsOwner）の時だけ画面のUIを初期同期する
        if (IsOwner)
        {
            if (_soulMeter == null)
            {
                GameObject bottleObj = GameObject.Find("Soul_Bottle");
                if (bottleObj != null)
                {
                    _soulMeter = bottleObj.GetComponent<SoulMeter>();
                }
            }
            UpdateSoulUI(_useFullSoul.Value);
            UpdateVStockUI(_vStock.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        _vStock.OnValueChanged -= OnVStockChanged;
        _useFullSoul.OnValueChanged -= OnSoulChanged;
        base.OnNetworkDespawn();
    }

    private void OnVStockChanged(int prevValue, int newValue)
    {
        UpdateVStockUI(newValue);
    }

    private void OnSoulChanged(int prevValue, int newValue)
    {
        UpdateSoulUI(newValue);
    }

    // UI更新はすべて NetworkVariable の変更通知（OnValueChanged）経由で実行
    private void UpdateSoulUI(int soulValue)
    {
        if (_soulMeter == null) return;
        _soulMeter.SetCount(soulValue);
        _soulMeter.AddMeter(soulValue);
    }

    private void UpdateVStockUI(int vStockValue)
    {
        if (_soulMeter == null) return;
        _soulMeter.UpdateVStockDisplay(vStockValue);
    }

    public void AdditionCoin()
    {
        if (!IsServer) return;

        if (!IsVStockMax)
        {
            _useFullSoul.Value++;
        }

        // 演出呼び出し
        ShowGetSoulEffectsClientRpc();
        //if (_playerAnimation != null) _playerAnimation.SoulGet();
        //if (_seScript != null) _seScript.PlayGetSoulSE();
        //if (_soulCountUI != null) _soulCountUI.SoulPlus();

        // 10ソウルごとにVStockへ変換
        while (_useFullSoul.Value >= USEULTIMATE_NEEDSOUL && _vStock.Value < 3)
        {
            _useFullSoul.Value -= USEULTIMATE_NEEDSOUL;
            _vStock.Value++;
        }


        if (_vStock.Value >= 3)
        {
            _useFullSoul.Value = 0;
        }
    }
    /// <summary>
    /// 全クライアント端末上で獲得エフェクト・SE・UI演出を発火するRPC
    /// </summary>
    [ClientRpc]
    private void ShowGetSoulEffectsClientRpc()
    {
        if (_playerAnimation != null) _playerAnimation.SoulGet();
        if (_seScript != null) _seScript.PlayGetSoulSE();

        // 自分が操作しているプレイヤー（IsOwner）の画面のUIのみポップアップを表示
        if (IsOwner)
        {
            if (_soulCountUI != null)
            {
                _soulCountUI.SoulPlus();
            }
        }
    }
    public void ReduceCoin()
    {
        if (!IsServer) return; // サーバー限定
        _useFullSoul.Value = 0; // 値の変更のみ。UI更新は OnValueChanged が自動で行う
    }

    public void ReduceVStock()
    {
        if (!IsServer) return;
        if (_vStock.Value <= 0) return;

        _vStock.Value--;
    }

    public void UseUltimate()
    {
        //Debug.Log("ウルト使用");
        //if (!IsServer) return;
        //if (_vStock.Value <= 0) return;

        //Debug.Log("Vスト消費");
        //_vStock.Value--;

        if (IsServer)
        {
            // ホスト（サーバー）自身なら直接消費実行
            ExecuteUseUltimateServer();
        }
        else
        {
            // クライアントならサーバーへ消費リクエストを送信
            RequestUseUltimateServerRpc();
        }
    }

    /// <summary>
    /// クライアントからサーバーへウルト消費を依頼する RPC
    /// </summary>
    [Rpc(SendTo.Server)]
    private void RequestUseUltimateServerRpc()
    {
        ExecuteUseUltimateServer();
    }
    /// <summary>
    /// サーバー上で実際に VStock または 最大ソウルを消費する処理
    /// </summary>
    private void ExecuteUseUltimateServer()
    {
        // 1. Vストックがある場合は Vストックを 1 減らす
        if (_vStock.Value > 0)
        {
            _vStock.Value--;
            Debug.Log($"[サーバー] Vストック消費成功。残り: {_vStock.Value}");
        }
        //// 2. Vストックは 0 だが、ソウルが 10 以上溜まっている場合はソウルを消費
        //else if (_useFullSoul.Value >= USEULTIMATE_NEEDSOUL)
        //{
        //    _useFullSoul.Value -= USEULTIMATE_NEEDSOUL;
        //    Debug.Log($"[サーバー] 最大ソウル消費成功。残りソウル: {_useFullSoul.Value}");
        //}
        else
        {
            Debug.LogWarning("[サーバー] 消費できるVストックもソウルもありません");
        }
    }

}