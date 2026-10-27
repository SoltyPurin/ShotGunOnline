using Unity.Netcode;
using UnityEngine;

public class SoulKeep : NetworkBehaviour
{
    [SerializeField] private SoulMeter _soulMeter;
    [SerializeField, Header("SE管理")]
    private PlayerSEControlScript _seScript = default;
    [SerializeField, Header("プレイヤーアニメーション管理")]
    private PlayerAnimation _playerAnimation = default;
    [SerializeField, Header("プラスカウント表示スクリプト")]
    private SoulCountUI _soulCountUI = default;

    private NetworkVariable<int> _vStock = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int VStock => _vStock.Value;

    private NetworkVariable<int> _useFullSoul = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int UseFullSoul => _useFullSoul.Value;

    private const int USEULTIMATE_NEEDSOUL = 10;
    public int NeedUseUltimateCoin => USEULTIMATE_NEEDSOUL;

    // vStockがMAX（3以上）かどうかプロパティで判定
    public bool IsVStockMax => _vStock.Value >= 3;

    private void Start()
    {
        if (IsOwner)
        {
            ResolveSoulMeter();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _vStock.OnValueChanged += OnVStockChanged;
        _useFullSoul.OnValueChanged += OnSoulChanged;

        // 操作するプレイヤー（IsOwner）の画面のUIを設定
        if (IsOwner)
        {
            ResolveSoulMeter();
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

    /// <summary>
    /// 自端末（IsOwner）のソウルメーターUI参照を安全に解決する。
    /// ゲーム開始前の非アクティブ状態（HUD Canvas）も含めて検索し、必要に応じて遅延取得する。
    /// </summary>
    private SoulMeter ResolveSoulMeter()
    {
        if (_soulMeter != null) return _soulMeter;
        if (!IsOwner) return null;

        _soulMeter = UnityEngine.Object.FindAnyObjectByType<SoulMeter>(FindObjectsInactive.Include);
        return _soulMeter;
    }

    private void OnVStockChanged(int prevValue, int newValue)
    {
        if (!IsOwner) return;
        UpdateVStockUI(newValue);
    }

    private void OnSoulChanged(int prevValue, int newValue)
    {
        if (!IsOwner) return;
        UpdateSoulUI(newValue);
    }

    // UI更新はすべて NetworkVariable の変更通知（OnValueChanged）経由で実行
    private void UpdateSoulUI(int soulValue)
    {
        var meter = ResolveSoulMeter();
        if (meter == null) return;
        meter.SetCount(soulValue);
        meter.AddMeter(soulValue);
    }

    private void UpdateVStockUI(int vStockValue)
    {
        var meter = ResolveSoulMeter();
        if (meter == null) return;
        meter.UpdateVStockDisplay(vStockValue);
    }

    /// <summary>
    /// クライアント・サーバー問わずコイン取得を呼び出す共通メソッド
    /// </summary>
    public void CollectCoin()
    {
        if (IsServer)
        {
            AdditionCoin();
        }
        else if (IsOwner)
        {
            RequestAdditionCoinServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestAdditionCoinServerRpc()
    {
        AdditionCoin();
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
    /// 全クライアント端末で獲得エフェクト・SE・UI演出を発火するRPC
    /// </summary>
    [ClientRpc]
    private void ShowGetSoulEffectsClientRpc()
    {
        if (_playerAnimation != null) _playerAnimation.SoulGet();
        if (_seScript != null) _seScript.PlayGetSoulSE();

        // 操作しているプレイヤー（IsOwner）の画面のUIのみポップアップ表示
        if (IsOwner)
        {
            if (_soulCountUI == null)
            {
                _soulCountUI = GetComponentInChildren<SoulCountUI>(true);
            }

            if (_soulCountUI != null)
            {
                _soulCountUI.SoulPlus();
            }
        }
    }

    public void ReduceCoin()
    {
        if (!IsServer) return; // サーバーのみ
        _useFullSoul.Value = 0; // 値の変更のみ。UI更新は OnValueChanged で行う
    }

    public void ReduceVStock()
    {
        if (!IsServer) return;
        if (_vStock.Value <= 0) return;

        _vStock.Value--;
    }

    public void UseUltimate()
    {
        if (IsServer)
        {
            ConsumeUltimate();
        }
        else if (IsOwner)
        {
            RequestUseUltimateServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestUseUltimateServerRpc()
    {
        ConsumeUltimate();
    }

    private void ConsumeUltimate()
    {
        if (!IsServer) return;
        if (_vStock.Value <= 0) return;

        _vStock.Value--;
    }
}
