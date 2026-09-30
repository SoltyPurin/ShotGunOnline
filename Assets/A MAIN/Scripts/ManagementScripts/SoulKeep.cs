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

    private NetworkVariable<int> _vStock = new NetworkVariable<int>
        (0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
    public int VStock
    {
        get { return _vStock.Value; }
    }
    private NetworkVariable<int> _useFullSoul = new NetworkVariable<int>
        (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public int UseFullSoul
    {
        get { return _useFullSoul.Value; }
    }

    private const int USEULTIMATE_NEEDSOUL = 10;
    public int NeedUseUltimateCoin
    {
        get { return USEULTIMATE_NEEDSOUL; }
    }

    private bool _isVstockMax = false;


    private void Start()
    {
        if (_soulMeter == null)
        {
            _soulMeter = GameObject.Find("Soul_Bottle").GetComponent<SoulMeter>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _vStock.OnValueChanged += OnVStockChanged;
        _useFullSoul.OnValueChanged += OnSoulChanged;

        UpdateSoulUI(_useFullSoul.Value);
        UpdateVStockUI(_vStock.Value);
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

    private void OnSoulChanged(int prevValue,int newValue)
    {
        UpdateSoulUI(newValue);
    }

    private void UpdateSoulUI(int soulValue)
    {
        if(_soulMeter == null)
        {
            return;
        }

        _soulMeter.SetCount(soulValue);
        _soulMeter.AddMeter(soulValue);
    }

    private void UpdateVStockUI(int vStockValue)
    {
        if(_soulMeter == null)
        {
            return;
        }
        _soulMeter.UpdateVStockDisplay(vStockValue);
    }
    public void AdditionCoin()
    {
        if (!IsServer)
        {
            return;
        }
        if (!_isVstockMax)
        {
            _useFullSoul.Value++;
        }

        //ソウル入手時の演出
        _playerAnimation.SoulGet();
        _seScript.PlayGetSoulSE();

        //ソウルが10以上でVストックに変換
        while (_useFullSoul.Value >= USEULTIMATE_NEEDSOUL && _vStock.Value < 3)
        {
            _useFullSoul.Value -= USEULTIMATE_NEEDSOUL;
            _vStock.Value++;
            if (_vStock.Value <= 2)
            {
                _soulMeter.ResetMeter();
            }
        }
        if (_vStock.Value >= 3)
        {
            _soulMeter.SetCount(10);
            _useFullSoul.Value = 0;
            _isVstockMax = true;
            return;
        }
        else
        {
            _soulMeter.SetCount(_useFullSoul.Value);
            _soulMeter.AddMeter(_useFullSoul.Value);
            _soulCountUI.SoulPlus();
            _isVstockMax = false;
        }

    }


    public void ReduceCoin()
    {
        _useFullSoul.Value = 0;
        _soulMeter.SetCount(_useFullSoul.Value);
        _soulMeter.ResetMeter();
    }

    public void ReduceVStock()
    {
        if (_vStock.Value <= 0)
        {
            return;
        }
        if (_isVstockMax)
        {
            _soulMeter.SetCount(_useFullSoul.Value);
            _soulMeter.ResetMeter();
            _isVstockMax = false;
        }
        _vStock.Value--;
    }


    public void UseUltimate()
    {
        if (_vStock.Value <= 0) return;
        if (_vStock.Value >= 3)
        {
            _soulMeter.ResetMeter();
        }
        _vStock.Value--;
        _soulMeter.SetCount(_useFullSoul.Value);
    }
}
