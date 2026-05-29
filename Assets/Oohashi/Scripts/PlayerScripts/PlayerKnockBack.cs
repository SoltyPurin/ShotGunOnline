using UnityEngine;

public class PlayerKnockBack : MonoBehaviour
{
    //[Header("リジッドボディを設定")]
    //[SerializeField] private Rigidbody2D _rigidbody = default;
    [Header("プレイヤーのオブジェクト")]
    [SerializeField] private GameObject _playerObject = default;

    [Header("スクリプトを設定")]
    [SerializeField] private PlayerMove _playerMove = default;

    [SerializeField, Header("ステートを特定するスクリプト")]
    private PlayerStateManager _playerState = default;

    [SerializeField,Header("プレイヤーのアニメーション再生スクリプト")]
    private PlayerAnimation _animation = default;

    private Rigidbody2D _rigidBody = default;

    private bool _isKnockBacking = false; //ノックバック中か

    private Vector3 _minusDiretion = default; //見てる方向とは逆方向を入れる

    private float _powValue = default; //チャージ時間の乗数を入れる

    private float _force = default; //線形補正した値を入れる

    private bool _isInputLeftStick = false; //左スティックを入力してるかどうか

    //定数
    private readonly float KNOCKBACK_FORCE = 7.0f;


    public bool IsInputLeftStick
    {
        set { _isInputLeftStick = value; }
    }

    private void Start()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
    }

    public void SetDirection(Vector3 direction,float chargeTime)
    {
        //現在見てる方向と逆の方向を出す
        _minusDiretion = (direction * -1).normalized;
        //チャージ時間の乗を求める
        _powValue = Mathf.Pow(chargeTime, 5f);
        //最低1からpowValueまでの間で線形補正を行い、チャージ値ごとの吹き飛ばしをなめらかに
        _force = Mathf.Lerp(3, _powValue, 0.5f);

        _isKnockBacking = true;
        //見てる方向と反対方向に力を加える
        //吹っ飛びが遅いためフワフワに見える
    }

    public void Fall()
    {
        _force = 0;
    }

    public void TakeDamage()
    {
        _force = 0;
    }


    private void FixedUpdate()
    {
        if (!_isKnockBacking)
        {
            return;
        }
        if(_playerState.PlayerState == PlayerState.DamageKnockBack)
        {
            return;
        }
        KnockBacking();
    }


    private void KnockBacking()
    {
        //見てる方向と反対方向に力を加える
        transform.position += _minusDiretion * _force * Time.fixedDeltaTime;
        _playerMove.SaveDirection = _minusDiretion * _force;

        if(_force >= KNOCKBACK_FORCE)
        {
            _animation.KnockBack();
        }

        switch (_isInputLeftStick)
        {
            case true:
                if (_force >= 0)
                {
                    _force -= 0.5f;
                }
                else
                {
                    _animation.Wait();
                    _isKnockBacking = false;
                }

                break;

            case false:
                if (_force >= 0)
                {
                    _force -= 0.2f;
                }
                else
                {
                    _animation.Wait();
                    _isKnockBacking = false;
                }
                break;
        }
    }

}
