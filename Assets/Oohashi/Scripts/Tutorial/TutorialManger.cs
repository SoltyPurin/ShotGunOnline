using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum TutorialState
{
    Shoot,
    Move,
    Charge,
    Ultimate,
    End,
    Transition,
}

public class TutorialManger : MonoBehaviour
{
    private TutorialState _state = TutorialState.Shoot;
    private TutorialState _nextState = TutorialState.Shoot;
    public TutorialState State
    {
        get { return _state; }
    }

    [SerializeField, Header("ステート変化までの時間")]
    private float _transitionTime = 2.0f;

    private GameObject _shootDecoy = default;

    private Gamepad _gamepad = default;
    private const float INPUTLIMITTIME = 2;
    private float _leftStickInputTime = default;
    private float _timer = 0.0f;
    private bool _isInputLeftStick = false; 
    private float _rightStickInputTime = default;
    private bool _isInputRightStick = false;
    private readonly string DECOYENEMYTAGNAME = "Decoy";
    private readonly string ENEMYTAGNAME = "Enemy";
    private bool _hasFoundChargeEnemy = false; // 一度でも敵を見つけたか？
    private bool _hasFoundUltEnemy = false; // 一度でもウルトをぶち込まれる敵を見つけたか
    private void Start()
    {
        if(Gamepad.current != null)
        {
            _gamepad = Gamepad.current;
        }

        _timer = 0.0f;
        _shootDecoy = null;
    }
    private void Update()
    {
        switch (_state)
        {
            case TutorialState.Shoot:
                _shootDecoy = GameObject.FindWithTag(DECOYENEMYTAGNAME);
                GameObject check = GameObject.Find("Check");
                if (check.activeInHierarchy)
                {
                    check.SetActive(false);
                }
                Slider stickSliderValue = GameObject.Find("Slider").GetComponent<Slider>();
                stickSliderValue.maxValue = INPUTLIMITTIME;

                bool rightStickInput = _gamepad != null && _gamepad.rightStick.ReadValue() != Vector2.zero;
                bool mouseInput = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f;

                if (rightStickInput || mouseInput)
                {
                    _isInputRightStick = true;
                }
                else
                {
                    _isInputRightStick = false;
                }
                if (_isInputRightStick)
                {
                    _rightStickInputTime += Time.deltaTime;
                    stickSliderValue.value = _rightStickInputTime;
                }

                if (_rightStickInputTime > INPUTLIMITTIME)
                {
                    check.gameObject.SetActive(true);
                    _nextState = TutorialState.Move;
                    _state = TutorialState.Transition;
                }
                break;


            case TutorialState.Move:
                _shootDecoy = GameObject.FindWithTag(DECOYENEMYTAGNAME);
                check = GameObject.Find("Check");
                stickSliderValue = GameObject.FindGameObjectWithTag("Slider").GetComponent<Slider>();
                stickSliderValue.maxValue = INPUTLIMITTIME;

                bool leftStickInput = _gamepad != null && _gamepad.leftStick.ReadValue() != Vector2.zero;
                bool keyboardInput = false;
                if (Keyboard.current != null)
                {
                    keyboardInput = Keyboard.current.wKey.isPressed || Keyboard.current.sKey.isPressed ||
                                    Keyboard.current.aKey.isPressed || Keyboard.current.dKey.isPressed ||
                                    Keyboard.current.upArrowKey.isPressed || Keyboard.current.downArrowKey.isPressed ||
                                    Keyboard.current.leftArrowKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
                }
                else
                {
                    keyboardInput = Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
                }

                if (leftStickInput || keyboardInput)
                {
                    _isInputLeftStick = true;
                }
                else
                {
                    _isInputLeftStick = false;
                }
                if (_isInputLeftStick)
                {
                    _leftStickInputTime += Time.deltaTime;
                }
                if(_leftStickInputTime > INPUTLIMITTIME)
                {
                    _state = TutorialState.Transition;
                    _nextState = TutorialState.Charge;
                }
                break;


            case TutorialState.Charge:
                GameObject chargeEnemy = GameObject.FindWithTag(ENEMYTAGNAME);
                if (chargeEnemy != null)
                {
                    _hasFoundChargeEnemy = true; // 一度でも見つけた
                }
                else if (_hasFoundChargeEnemy)
                {
                    _state = TutorialState.Ultimate; // 一度見つけた後にnullになった＝倒された
                }
                break;

            case TutorialState.Ultimate:
                GameObject ultEnemy = GameObject.FindWithTag(ENEMYTAGNAME);
                if (ultEnemy != null)
                {
                    _hasFoundUltEnemy = true;
                }else if (_hasFoundUltEnemy)
                {
                    _state= TutorialState.End;
                }
                break;

            case TutorialState.Transition:
                Transition();
                break;
        }
    }

    private void Transition()
    {
        if(_timer >= _transitionTime)
        {
            _timer = 0;
            _state = _nextState;
            if(_shootDecoy != null)
            {
                Destroy(_shootDecoy.gameObject);
                _shootDecoy = null;
            }
            return;
        }

        _timer += Time.deltaTime;
    }
}
