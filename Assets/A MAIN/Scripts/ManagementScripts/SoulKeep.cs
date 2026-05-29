using UnityEngine;

public class SoulKeep : MonoBehaviour
{
    [SerializeField] private SoulMeter _soulMeter;
    [SerializeField, Header("効果音")]
    private PlayerSEControlScript _seScript = default;
    [SerializeField, Header("プレイヤーアニメ管理")]
    private PlayerAnimation _playerAnimation = default;
    [SerializeField, Header("プラスカウントを表示するスクリプト")]
    private SoulCountUI _soulCountUI = default;

    private int _vStock = 0;
    public int VStock
    {
        get { return _vStock; }
    }
    private int _useFullSoul = 0;
    public int UseFullSoul
    {
        get { return _useFullSoul; }
    }

    private const int USEULTIMATE_NEEDSOUL = 10;
    public int NeedUseUltimateCoin
    {
        get { return USEULTIMATE_NEEDSOUL; }
    }

    private bool _isVstockMax = false;

    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.R))
    //    {
    //        AdditionCoin();
    //    }
    //}





    public void AdditionCoin()
    {
        if (!_isVstockMax)
        {
            _useFullSoul++;
        }
        _playerAnimation.SoulGet();
        _seScript.PlayGetSoulSE();
        while (_useFullSoul >= USEULTIMATE_NEEDSOUL && _vStock < 3)
        {
            _useFullSoul -= USEULTIMATE_NEEDSOUL;
            _vStock++;
            if(_vStock <= 2)
            {
                _soulMeter.ResetMeter();
            }
        }
        if (_vStock >= 3)
        {
            Debug.Log("最大");
            _soulMeter.SetCount(10);
            _useFullSoul = 0;
            _isVstockMax = true;
            return;
        }
        else
        {
            _soulMeter.SetCount(_useFullSoul);
            _soulMeter.AddMeter(_useFullSoul);
            _soulCountUI.SoulPlus();
            _isVstockMax = false;
        }

        UpdateVStockUI();
    }


    public void ReduceCoin()
    {
        Debug.Log("被弾");
        _useFullSoul = 0;
        _soulMeter.SetCount(_useFullSoul);
        _soulMeter.ResetMeter() ;
        UpdateVStockUI();
    }

    public void ReduceVStock()
    {
        if (_vStock <= 0)
        {
            return;
        }
        if (_isVstockMax)
        {
            _soulMeter.SetCount(_useFullSoul);
            _soulMeter.ResetMeter();
            _isVstockMax = false;
        }
        _vStock--;
        UpdateVStockUI();
    }


    public void UseUltimate()
    {
        if (_vStock <= 0) return;
        if(_vStock >= 3)
        {
            _soulMeter.ResetMeter();                                                                                                                                                                                                    
        }
        _vStock--;
        _soulMeter.SetCount(_useFullSoul);
        UpdateVStockUI();
    }


    private void UpdateVStockUI()
    {
        _soulMeter.UpdateVStockDisplay(_vStock);
    }
    //[SerializeField] private PlayerStateManager _stateManager = default;
    //[SerializeField] private InputChangeState _changeState = default;

    //[SerializeField, Header("VStockの所持上限")]
    //private const int VSTOCK_LIMIT = 3;

    //[SerializeField,Header("ソウルのメーターを表示するスクリプト")]
    //private SoulMeter _soulMeter = default;




    ////vストックが3つあるかどうかを判断する
    //private bool _isVStockMax = false;


    //private const int ONE_VSTOCK_NEED_SOUL = 10; //Vストックが1つ溜まるのに必要なソウルの数
    //private int _initPossesionSoul = 0;

    ////ウルトを使用する際に使用するストック、ソウル10個で1つ
    //private int _vStock = 0;

    //public int VStock
    //{
    //    get { return _vStock; }
    //}

    ///// <summary>
    ///// 使用可能なソウル
    ///// </summary>
    //public int UseFullSoul
    //{
    //    get { return _initPossesionSoul; }  
    //}


    ///// <summary>
    ///// ウルトを使用可能になるためのソウルの数
    ///// </summary>
    //public int NeedUseUltimateCoin
    //{
    //    get { return ONE_VSTOCK_NEED_SOUL; }
    //}

    ///// <summary>
    ///// 所持ソウルが規定数を超えたらvストックを1個貯める
    ///// </summary>
    //private void Update()
    //{

    //    if(_initPossesionSoul >= ONE_VSTOCK_NEED_SOUL)
    //    {
    //        _vStock++;
    //        if (_vStock < 3)
    //        {
    //            _vStock = Mathf.Clamp(_vStock, 0, VSTOCK_LIMIT);
    //            _initPossesionSoul = 0;
    //            _soulMeter.ResetMeter();
    //        }
    //        else
    //        {
    //            _initPossesionSoul = 0;
    //        }
    //        _soulMeter.ActiveVStock(_vStock);
    //    }
    //    if (_vStock <= VSTOCK_LIMIT)
    //    {
    //        _isVStockMax = false;
    //    }
    //    else
    //    {
    //        _isVStockMax = true;
    //    }

    //}

    //public void AdditionCoin()
    //{
    //    if (!_isVStockMax)
    //    {
    //        _initPossesionSoul++;

    //        _soulMeter.AddMeter(_initPossesionSoul);


    //        //取得ソウルをカウンターに表示
    //        _soulMeter.SetCount(_initPossesionSoul);

    //    }
    //}

    //public void ReduceCoin()
    //{
    //    if (!this.gameObject.scene.isLoaded)
    //    {
    //        return;
    //    }

    //    if (!this.gameObject.activeInHierarchy)
    //    {
    //        return;
    //    }

    //    if (_vStock >= 1)
    //    {
    //        if(_initPossesionSoul >= 1)
    //        {
    //            _initPossesionSoul = 0;
    //        }
    //        else
    //        {
    //            _vStock--;
    //            _soulMeter.DeActiveVStock();
    //        }
    //    }
    //    else
    //    {
    //        _initPossesionSoul = 0;
    //    }
    //}

    //public void UseUltimate()
    //{

    //    _vStock--;
    //    _soulMeter.DeActiveVStock();
    //    if(_vStock <=0 && _stateManager.PlayerState == PlayerState.Ultimate)
    //    {
    //        _stateManager.NormalState();
    //        _changeState.UltimateActive = false;
    //    }
    //}

    //public void UpdateVStockUI()
    //{

    //}

}
