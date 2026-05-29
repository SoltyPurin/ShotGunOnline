using UnityEngine;

public class ChargeAnimation : MonoBehaviour
{
    [SerializeField, Header("プレイヤーのチャージ値を取得するスクリプト")]
    private InputPlayerShot _inputPlayerShot = default;
    [SerializeField, Header("アニメーターを登録")]
    private Animator _animator = default;
    //現在のチャージ時間
    private float _chargeTime = 0;  
    //チャージが最大かどうかのフラグ
    private bool _isChargeMax = false;
    //現在チャージしてるかどうかのフラグ
    private bool _isCharging = false;

    private bool _canPlayinganimation = default;
    private void Update()
    {
        //射撃ボタンを押していてチャージ時間が減算中でなくウルトを撃ってる状態でもなく現在チャージ中でなければtrue
        _canPlayinganimation = _inputPlayerShot.IsPushShotButton && !_inputPlayerShot.IsDecCharge && !_inputPlayerShot.HasShotUltimate && !_isCharging;
    }

    private void FixedUpdate()
    {
        if (_canPlayinganimation)
        {
            _animator.SetTrigger("Charge");
            _isCharging = true;
            //射撃ボタンを離したら処理
        }
        else if (!_inputPlayerShot.IsPushShotButton)
        {
            _animator.SetTrigger("Wait");
            _isCharging = false;
            _isChargeMax = false;
            _chargeTime = 0;
        }

        if (_isCharging)
        {
            _chargeTime += Time.deltaTime;
            //一回しか最大チャージの音がならないようにする
            if (_chargeTime >= 2 && !_isChargeMax)
            {
                _animator.SetTrigger("MAX");
                _isChargeMax = true;
            }
        }

    }
}
