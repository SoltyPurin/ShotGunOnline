using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

//本当にごめんなさいここはあまりに元が難解すぎたためAIフルバーストで記述しました。
public class PlayerAiming : NetworkBehaviour
{
    // エイムの方向ベクトルを同期するネットワーク変数を定義
    // (Ownerが書き込み可能、全員が読み込み可能に設定)
    private NetworkVariable<Vector3> _netAimDirection = new NetworkVariable<Vector3>(
        Vector3.right,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    public Vector3 Direction
    {
        get { return _netAimDirection.Value; } //directionを取得できるようにする
    }


    [SerializeField, Header("減算中かどうかを取得する")]
    private InputPlayerShot _inputShot = default;

    private Vector2 _rightStickInput;
    private Gamepad _gamePad;

    [Header("プレイヤーのオブジェクト取得")]
    [SerializeField] private GameObject _playerObject = default;
    [SerializeField, Header("Lerpの補正値")]
    private float _correctionValue = 1f;

    [Header("撃ってないときの向いてる方向表示")]
    [SerializeField] private ShootShape _shape = default;

    private Vector3 _prevDirection = Vector3.right;


    private void Awake()
    {
        if (_playerObject == null) return;
        if (Gamepad.current != null)
        {
            _gamePad = Gamepad.current;
        }
    }

    private void Update()
    {
        // 入力の取得は「操作している時(Owner)」に行う
        if (this.IsOwner)
        {
            InputAiming();
        }
    }

    private bool _useMouseAim = false;

    private void InputAiming()
    {
        Vector2 stickInput = Vector2.zero;
        if (_gamePad != null)
        {
            stickInput = _gamePad.rightStick.ReadValue();
        }

        if (stickInput.sqrMagnitude > 0.04f)
        {
            _rightStickInput = stickInput;
            _useMouseAim = false;
        }
        else
        {
            _useMouseAim = true;
        }
    }

    private void FixedUpdate()
    {
        // 操作している側（Owner）の場合の処理
        if (this.IsOwner)
        {
            if (!_useMouseAim)
            {
                if (_rightStickInput.sqrMagnitude > 0.04f)
                {
                    _prevDirection = new Vector3(_rightStickInput.x, _rightStickInput.y, 0).normalized;
                }
            }
            else
            {
                // マウスでの照準
                if (Camera.main != null)
                {
                    Vector3 mousePos = Vector3.zero;
                    if (Mouse.current != null)
                    {
                        mousePos = Mouse.current.position.ReadValue();
                    }
                    else
                    {
                        mousePos = Input.mousePosition;
                    }
                    Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Mathf.Abs(Camera.main.transform.position.z - transform.position.z)));
                    Vector3 playerPos = transform.position;
                    Vector3 diff = worldMousePos - playerPos;
                    diff.z = 0;
                    if (diff.sqrMagnitude > 0.01f)
                    {
                        _prevDirection = diff.normalized;
                    }
                }
            }

            // ネットワーク変数に現在の向きを入力（自動的にホスト・クライアント全員に同期）
            _netAimDirection.Value = _prevDirection;
        }

        // 【ホスト・クライアント全員が共通で実行する】処理
        // 同期されて届いた「エイム方向」のデータを使って、各画面のローカルで体とエイム範囲を動かす
        Vector3 currentAimDir = _netAimDirection.Value;

        if (currentAimDir.sqrMagnitude > 0.001f)
        {
            // 体（グラフィック）の回転処理
            Quaternion pRotate = _playerObject.transform.rotation;
            float angle = Mathf.Atan2(currentAimDir.y, currentAimDir.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            // Lerpで滑らかに回転（他人の画面でもカクつかずに綺麗に回ります）
            _playerObject.transform.rotation = Quaternion.Lerp(pRotate, targetRotation, _correctionValue);

            // エイム範囲（見た目の演出）の更新
            // （全員が同じデータを受け取っているので、複雑なelse分岐や逆算が不要になります）
            _shape.NotShootTimeDirection = currentAimDir;
        }
    }
}