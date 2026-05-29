using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerAiming : MonoBehaviour
{
    [SerializeField, Header("減算中かどうかを取得する")]
    private InputPlayerShot _inputShot = default;
    //右スティックの入力値を代入するための変数
    private Vector2 _rightStickInput;
    //ゲームパッドの変数
    private Gamepad _gamePad;
    [Header("プレイヤーのオブジェクト取得")]
    [SerializeField] private GameObject _playerObject = default;
    [SerializeField, Header("Lerpの補正値")]
    private float _correctionValue = 1f;
    //方向を代入する変数
    private Vector3 _direction = default;

    public Vector3 Direction
    {
        get { return _direction; } //directionを取得できるようにする
    }
    [Header("撃ってないときの向いてる方向表示")]
    [SerializeField] private ShootShape _shape = default;
    //前見てた方向を保存
    private Vector3 _prevDirection = default;

    private PlayerStateManager _state;


    private void Awake()
    {
        //コントローラーが接続されてなければ早期リターン
        if(Gamepad.current == null || _playerObject == null) 
        {
            return;
        }
        _gamePad = Gamepad.current;//ゲームパッド型の変数に現在のコントローラーを代入
        //2つの変数に最初の方向を設定
        _direction = Vector3.right; 
        _prevDirection = Vector3.right;
        _state = GetComponent<PlayerStateManager>();
    }



    private void Update()
    {
        if(_state.PlayerState == PlayerState.Movie)
        {
            return;
        }
        if (!_inputShot.IsAlive)
        {
            return;
        }
        InputAiming(); //エイムのメソッド呼び出し
    }

    private void InputAiming()
    {
        if(_gamePad == null)
        {
            return;
        }
        //右スティックの値を代入
        _rightStickInput = _gamePad.rightStick.ReadValue();
        //右スティックの入力値がゼロチェック値よりでかければ実行
    }

    private void FixedUpdate()
    {
        if (_rightStickInput.sqrMagnitude > 0.04f) // ゼロチェック（小さい揺れ無視）
        {
            Quaternion pRotate = _playerObject.transform.rotation;
            //右スティックのベクトルを角度に変換する
            float angle = Mathf.Atan2(_rightStickInput.y, _rightStickInput.x) * Mathf.Rad2Deg;
            //オイラー角をクオータニオンに変換する
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            //Lerpで線形補正を行い滑らかに回転させる
            _playerObject.transform.rotation = Quaternion.Lerp(pRotate, targetRotation, _correctionValue);
            //方向変数に右スティックの入力値を正規化して代入
            _direction = new Vector3(_rightStickInput.x, _rightStickInput.y, 0).normalized;
            //前の見てる方向に現在の見てる方向を代入するs
            _prevDirection = _direction;
            //射撃方向の設定
            _shape.NotShootTimeDirection = _direction;
        }
        else
        {
            //なにも入力してないときは前回の方向を設定
            _shape.NotShootTimeDirection = _prevDirection;
        }

    }
}
