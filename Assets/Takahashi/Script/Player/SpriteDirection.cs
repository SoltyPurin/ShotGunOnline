using UnityEngine;

/// <summary>
/// プレイヤースプライトを、右スティックの入力によって変更する用のクラス
/// </summary>
public class SpriteDirection : MonoBehaviour
{
    //右向きか、左向きかを保存するenum
    private enum Direction
    {
        Right,
        Left,
    }

    //回転軸をどうするか
    private enum Shaft
    {
        X,
        Y,
    }

    [SerializeField, Header("取得系")]
    private PlayerAiming _playerAiming = default;

    [SerializeField, Header("正面")]
    private SpriteRenderer _frontSprite = default;

    [SerializeField, Header("背面")]
    private SpriteRenderer _backSprite = default;

    [SerializeField]
    private Shaft _shaft = Shaft.Y;

    private Vector3 _rotation = Vector3.zero;
    private Vector3 _rotationRe = Vector3.zero;

    private bool _isFront = true;
    public bool IsFront
    {
        get { return _isFront; }
    }

    
    void Start()
    {
        //回転軸の変更
        switch (_shaft)
        {
            case Shaft.X:
                _rotationRe = new Vector3(180f, 0f, 0f);
                break;
            case Shaft.Y:
                _rotationRe = new Vector3(0, 180f, 0f);
                break;
        }
    }

    private void FixedUpdate()
    {
        //エイム方向を取得
        float direction = _playerAiming.Direction.x;
        float verticalDirection = _playerAiming.Direction.y;

        bool isFront = verticalDirection <= 0;

        if (isFront)
        {
            _isFront = true;
            TurnFront();
        }
        else
        {
            _isFront = false;
            TurnBack();
        }

        //エイム方向が現在と反対を向いた時のみスプライトを反転する。

        if(direction < 0)
        {
            TurnFrontLeft(isFront);
        }
        else if(direction >0)
        {
            TurnFrontRight(isFront);
        }

    }

    private void TurnBack()
    {
        _frontSprite.enabled = false;
        _backSprite.enabled = true;

    }

    private void TurnFront()
    {
        _frontSprite.enabled = true;
        _backSprite.enabled = false;
    }

    /// <summary>
    /// 右を向く用のメソッド
    /// </summary>
    private void TurnFrontRight(bool isFront)
    {
        if(isFront)
        {
            _frontSprite.transform.localRotation = Quaternion.Euler(_rotation);
        }
        else
        {
            _backSprite.transform.localRotation = Quaternion.Euler(_rotation);
        }
    }

    /// <summary>
    /// 左を向く用のメソッド
    /// </summary>
    private void TurnFrontLeft(bool isFront)
    {
        if(isFront)
        {
            _frontSprite.transform.localRotation = Quaternion.Euler(_rotationRe);
        }
        else
        {
            _backSprite.transform.localRotation = Quaternion.Euler(_rotationRe);
        }
    }
}
