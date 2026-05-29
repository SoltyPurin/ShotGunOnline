using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// チャージゲージのUIを変化させるスクリプト
/// 【作成者：髙橋英士】
/// </summary>
public class ChargeGageUI : MonoBehaviour
{
    private enum Direction
    {
        Right,
        Left,
    }
    private Direction _state = Direction.Right;

    [SerializeField, Header("変化するゲージの画像")]
    private Image _image = default;

    [SerializeField, Header("プレイヤースクリプト")]
    private InputPlayerShot _inputPlayerShot = default;
    [SerializeField]
    private PlayerAiming _playerAiming = default;
    [SerializeField]
    private PlayerStateManager _playerStateManager = default;

    [SerializeField, Header("アニメーション")]
    private Animator _animator = default;

    [SerializeField, Header("反転させたいオブジェクト")]
    private GameObject _gageObj = default;

    [SerializeField, Header("ウルト時のゲージオブジェクト")]
    private GameObject _ultGage = default;

    private Vector3 _pos;
    private Vector3 _minusPos;

    private void Start()
    {
        //UIのポジションと反対のポジションを設定
        _pos = _gageObj.transform.localPosition;
        float _posX = -_pos.x;
        _minusPos = new Vector3(_posX, _pos.y, _pos.z);

        //ウルト状態でない場合はウルトゲージUIを隠す
        if(_playerStateManager.PlayerState != PlayerState.Ultimate)
        {
            _ultGage.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        //プレイヤーの向いている方向を取得
        float direction = Mathf.Sign(_playerAiming.Direction.x);

        //円形のゲージなので、FillAmountを使用。FillAmountは0～１なので、チャージバリューを調整し画像を変更
        _image.fillAmount = Mathf.Clamp01(_inputPlayerShot.ChargeValue / 2f);

        //チャージバリューが最大の場合はMAX状態のアニメーションを再生
        if(_inputPlayerShot.ChargeValue >= 2)
        {
            _animator.SetBool("Max", true);
        }
        else
        {
            _animator.SetBool("Max", false);
        }

        UltGage();

        //switchでゲージの位置を変更する。
        switch (_state)
        {
            case Direction.Right:
                if (direction < 0)
                {
                    Left();
                }
                break;
            case Direction.Left:
                if (direction > 0)
                {
                    Right();
                }
                break;
        }
    }

    /// <summary>
    /// ウルト状態のゲージを出すかどうかの判定をするメソッド
    /// </summary>
    private void UltGage()
    {
        switch(_playerStateManager.PlayerState)
        {
            case PlayerState.Ultimate:
                if (!_ultGage.activeInHierarchy)
                {
                    _ultGage.SetActive(true);
                }
                break;

            case PlayerState.Normal:
                if (_ultGage.activeInHierarchy)
                {
                    _ultGage.SetActive(false);
                }
                break;
        }
    }

    /// <summary>
    /// 左向いたらUIを反対に向かせるメソッド
    /// </summary>
    private void Left()
    {
        _gageObj.transform.localPosition = _minusPos;
        _state = Direction.Left;
    }

    /// <summary>
    /// 右向いたらUIを反対に向かせるメソッド
    /// </summary>
    private void Right()
    {
        _gageObj.transform.localPosition = _pos;
        _state = Direction.Right;
    }
}
