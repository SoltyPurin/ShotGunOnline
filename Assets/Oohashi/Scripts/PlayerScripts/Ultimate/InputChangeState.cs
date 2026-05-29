using UnityEngine;
using UnityEngine.InputSystem;

public class InputChangeState : MonoBehaviour
{
    #region Serializeの変数
    [SerializeField, Header("モードチェンジエフェクトのオブジェクト")]
    private GameObject _toNormal = default;
    [SerializeField]
    private GameObject _toUlt = default;

    [SerializeField, Header("エフェクトのアニメータ")]
    private Animator _toNormalAnim = default;
    [SerializeField]
    private Animator _toUltAnim = default;

    [SerializeField, Header("ウルト中のオーラ")]
    private GameObject _ultAura = default;

    [SerializeField, Header("ウルト可能かどうかのオーラ")]
    private GameObject _canUltAura = default;

    [SerializeField, Header("ウルトフラッシュ")]
    private Animator _flashAnimator = default;

    [Header("ウルトチャージ")]
    [SerializeField, Tooltip("チャージのエフェクト")]
    private GameObject _ultCharge = default;
    [SerializeField, Tooltip("チャージエフェクト出現のエフェクト")]
    private Animator _showUltChargeEffect = default;

    [SerializeField, Header("効果音")]
    private PlayerSEControlScript _seScript = default;

    [SerializeField, Header("コイン管理のスクリプト取得")] 
    private SoulKeep _soulKeep = default;
    [SerializeField,Header("プレイヤーのステート取得")] 
    private PlayerStateManager _stateManager = default;

    #endregion

    #region 内部変数
    //ウルトを使用可能か
    private bool _canUseUltimate = false; 
    //ウルトのボタン
    private readonly string USE_ULTIMATE_BUTTON = "Ultimate";
    //ウルトが現在アクティブかどうか
    private bool _ultimateActive = false;
    //左トリガーを押したかどうか
    private bool _isLeftTriggerPressed = false;
    public bool UltimateActive
    {
        set { _ultimateActive = value; }
    }

    private float _leftTriggerValue = default;

    private InputPause _pause = default;
    #endregion

    private void Start()
    {
        //ウルトのオーラを表示する
        if(_ultAura != null)
        {
            _ultAura.SetActive(false);
        }

        if(_canUltAura != null)
        {
            _canUltAura.SetActive(false);
        }

        _pause = GameObject.Find("PauseVision").GetComponent<InputPause>();
    }

    private void Update()
    {
        if (_pause.IsPauseing || _stateManager.PlayerState == PlayerState.Movie)
        {
            return;
        }

        //Vストックが1個以上の時にウルト使用可能
        if (_soulKeep.VStock >= 1)
        {
            //ウルトを使用してないとき
            if (!_canUseUltimate)
            {
                _flashAnimator.SetTrigger("Flash");
                _seScript.PlayCanUseUltSound();
            }
            _canUseUltimate = true;


            if (!_canUltAura.activeInHierarchy && !_ultimateActive)
            {
                _canUltAura.SetActive(true);
            }
        }// ウルト使用可能状態が解除された瞬間の処理(ウルト撃った後にコインがなくなったとき)
        else if(_canUseUltimate)
        {
            _canUseUltimate = false;
            _ultimateActive = false;
            _canUltAura.SetActive(false);
            ToNormalAnimation();
            _stateManager.NormalState();
        }
        if (Gamepad.current == null)
        {
            return;
        }
        _leftTriggerValue = Gamepad.current.leftTrigger.ReadValue();
        bool isLeftTriggerInput = _leftTriggerValue >= 0.9f;

        //ウルトボタンが押されたときかつ左トリガーが入力した時点でまだ押されてない時に実行
        if (((Input.GetButtonDown(USE_ULTIMATE_BUTTON)) || isLeftTriggerInput) && !_isLeftTriggerPressed)
        {
            if (_stateManager.PlayerState == PlayerState.Movie)
            {
                return;
            }

            _isLeftTriggerPressed = true;
            //ウルトがまだアクティブではなくかつウルトをまだ使ってないとき
            if (!_ultimateActive && _canUseUltimate)
            {
                _stateManager.UltimateState();
                _ultimateActive = true;
                ToUltAnimation();
            }//ウルトの判定の時に切り替えボタンを押したときの処理
            else if (_ultimateActive)
            {
                _stateManager.NormalState();
                _ultimateActive = false;
                ToNormalAnimation();
            }

        }
        //左トリガーのフラグを切り替え
        if (!isLeftTriggerInput)
        {
            _isLeftTriggerPressed = false;
        }

    }

    /// <summary>
    /// ノーマルのアニメーションに切り替え、ウルトのオーラを非アクティブにするなど
    /// </summary>
    public void ToNormalAnimation()
    {
        if (_toNormal == null) return;
        _toNormal.transform.position = this.transform.position;
        _toNormalAnim.SetTrigger("Change");
        _ultAura.SetActive(false);

        if(_ultCharge.activeInHierarchy)
        {
            ShowUltCharge(false);
        }
    }

    /// <summary>
    /// ウルトのアニメーションに切り替え、オーラバトラーにする
    /// </summary>
    public void ToUltAnimation()
    {
        if(_toUlt == null) return;
        _toUlt.transform.position = this.transform.position;
        _toUltAnim.SetTrigger("Change");
        _ultAura.SetActive(true);
        _canUltAura.SetActive(false);

        if (!_ultCharge.activeInHierarchy)
        {
            ShowUltCharge(true);
        }
    }

    public void ShowUltCharge(bool ultBool)
    {
        _ultCharge.SetActive(ultBool);
        _showUltChargeEffect.SetTrigger("Trigger");
    }
}
