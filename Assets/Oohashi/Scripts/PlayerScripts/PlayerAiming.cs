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
    public bool IsLocalControl => IsOwner || !IsSpawned;

    public Vector3 Direction
    {
        get { return IsLocalControl ? _prevDirection : _netAimDirection.Value; } // Ownerはローカル値を即時参照し遅延を防ぐ
    }

    public void SetDirection(Vector3 aimDir)
    {
        if (aimDir.sqrMagnitude <= 0.001f) return;
        _prevDirection = new Vector3(aimDir.x, aimDir.y, 0f).normalized;
        ApplyAimVisuals(_prevDirection, immediate: true);
        if (IsSpawned && IsOwner)
        {
            _netAimDirection.Value = _prevDirection;
        }
    }


    [SerializeField, Header("減算中かどうかを取得する")]
    private InputPlayerShot _inputShot = default;

    [SerializeField, Header("プレイヤーのステート管理")]
    private PlayerStateManager _playerStateManager = default;

    [SerializeField, Header("カットシーン制御（任意）")]
    private CutsceneControlledPlayer _cutscenePlayer = default;

    private Vector2 _rightStickInput;

    [Header("プレイヤーのオブジェクト取得")]
    [SerializeField] private GameObject _playerObject = default;
    [SerializeField, Header("Lerpの補正値")]
    private float _correctionValue = 1f;

    [Header("撃ってないときの向いてる方向表示")]
    [SerializeField] private ShootShape _shape = default;

    private Vector3 _prevDirection = Vector3.right;
    private bool _useMouseAim = false;
    private Vector2 _lastMousePosition;
    private bool _hasMousePositionInitialized = false;

    private const float StickDeadZoneSqr = 0.04f; // 0.2^2
    private const float MouseMoveThresholdSqr = 9f; // 3px以上動いたらマウス操作とみなす


    private void Awake()
    {
        if (_playerStateManager == null)
        {
            _playerStateManager = GetComponent<PlayerStateManager>();
        }

        if (_cutscenePlayer == null)
        {
            _cutscenePlayer = GetComponent<CutsceneControlledPlayer>();
        }
    }

    /// <summary>
    /// エイム操作・視点移動が可能かどうかを判定
    /// ゲーム開始時のPlay Director再生中やムービーステート中は操作を禁止する
    /// </summary>
    public bool CanAim()
    {
        // 1. PlayerStateManager が Movie ステートの場合は操作不可
        if (_playerStateManager != null && _playerStateManager.PlayerState == PlayerState.Movie)
        {
            return false;
        }

        // 2. CutsceneControlledPlayer がカットシーン実行中の場合は操作不可
        if (_cutscenePlayer != null && _cutscenePlayer.IsInCutscene)
        {
            return false;
        }

        // 3. MP_GameManager がゲーム開始前（待機中または開始ムービー中）の場合は操作不可
        if (MP_GameManager.Instance != null)
        {
            var state = MP_GameManager.Instance.CurrentState;
            if (state == MP_GameManager.GameState.WaitingForPlayers || state == MP_GameManager.GameState.OpeningMovie)
            {
                return false;
            }
        }

        return true;
    }

    private const float AimSyncDotThreshold = 0.9995f; // 約1.8度以上の変化時のみネットワーク同期

    private void Update()
    {
        if (!IsLocalControl)
        {
            UpdateRemoteAimVisuals();
            return;
        }

        if (!CanAim())
        {
            _rightStickInput = Vector2.zero;
            _hasMousePositionInitialized = false;
            if (_playerObject != null)
            {
                _playerObject.transform.localPosition = Vector3.zero;
                _playerObject.transform.rotation = Quaternion.identity;
            }
            if (_shape != null)
            {
                _shape.NotShootTimeDirection = Vector3.right;
            }
            return;
        }

        if (LobbyMovement.IsInputBlocked || LobbyViewBase.TopActiveView != null)
        {
            _hasMousePositionInitialized = false;
            return;
        }

        InputAiming();
        UpdateLocalAimDirection();
        ApplyAimVisuals(_prevDirection, immediate: true);

        // 一定角度以上変化したときだけ NetworkVariable を更新（毎フレーム送信を防止）
        if (IsSpawned && Vector3.Dot(_netAimDirection.Value, _prevDirection) < AimSyncDotThreshold)
        {
            _netAimDirection.Value = _prevDirection;
        }
    }

    private void UpdateLocalAimDirection()
    {
        if (!_useMouseAim)
        {
            if (_rightStickInput.sqrMagnitude > StickDeadZoneSqr)
            {
                _prevDirection = new Vector3(_rightStickInput.x, _rightStickInput.y, 0f).normalized;
            }
            return;
        }

        if (Camera.main == null) return;

        Vector2 mousePos = GetMousePosition();
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Mathf.Abs(Camera.main.transform.position.z - transform.position.z)));
        Vector3 diff = worldMousePos - transform.position;
        diff.z = 0f;
        if (diff.sqrMagnitude > 0.01f)
        {
            _prevDirection = diff.normalized;
        }
    }

    private void InputAiming()
    {
        var gamepad = Gamepad.current;
        Vector2 stickInput = gamepad != null ? gamepad.rightStick.ReadValue() : Vector2.zero;

        if (stickInput.sqrMagnitude > StickDeadZoneSqr)
        {
            _rightStickInput = stickInput;
            _useMouseAim = false;
            SyncMousePosition();
            return;
        }

        _rightStickInput = Vector2.zero;

        // 左スティックやトリガー等のコントローラー操作があった場合もマウスエイムを解除する
        if (IsOtherGamepadInputActive(gamepad))
        {
            _useMouseAim = false;
            SyncMousePosition();
            return;
        }

        // マウスが一定距離以上動いたときだけマウスエイムへ切り替える
        Vector2 currentMousePos = GetMousePosition();
        if (!_hasMousePositionInitialized)
        {
            _lastMousePosition = currentMousePos;
            _hasMousePositionInitialized = true;
            return;
        }

        if (!_useMouseAim)
        {
            if ((currentMousePos - _lastMousePosition).sqrMagnitude > MouseMoveThresholdSqr)
            {
                _useMouseAim = true;
                _lastMousePosition = currentMousePos;
            }
        }
        else
        {
            _lastMousePosition = currentMousePos;
        }
    }

    private bool IsOtherGamepadInputActive(Gamepad gamepad)
    {
        if (gamepad == null) return false;

        return gamepad.leftStick.ReadValue().sqrMagnitude > StickDeadZoneSqr
            || gamepad.rightTrigger.ReadValue() > 0.2f
            || gamepad.leftTrigger.ReadValue() > 0.2f
            || gamepad.rightShoulder.isPressed
            || gamepad.leftShoulder.isPressed
            || gamepad.buttonSouth.isPressed
            || gamepad.buttonEast.isPressed
            || gamepad.buttonWest.isPressed
            || gamepad.buttonNorth.isPressed;
    }

    private Vector2 GetMousePosition()
    {
        return Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;
    }

    private void SyncMousePosition()
    {
        _lastMousePosition = GetMousePosition();
        _hasMousePositionInitialized = true;
    }

    private void UpdateRemoteAimVisuals()
    {
        if (_playerObject == null) return;

        _playerObject.transform.localPosition = Vector3.zero;

        // ムービー・カットシーン再生中は向きをデフォルト（右向き水平）に維持し、視点移動させない
        if (!CanAim())
        {
            _playerObject.transform.rotation = Quaternion.identity;
            if (_shape != null)
            {
                _shape.NotShootTimeDirection = Vector3.right;
            }
            return;
        }

        ApplyAimVisuals(_netAimDirection.Value, immediate: false);
    }

    private void ApplyAimVisuals(Vector3 aimDir, bool immediate)
    {
        if (_playerObject == null || aimDir.sqrMagnitude <= 0.001f) return;

        _playerObject.transform.localPosition = Vector3.zero;
        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
        if (immediate)
        {
            _playerObject.transform.rotation = targetRotation;
        }
        else
        {
            float smoothRate = Mathf.Max(1f, _correctionValue) * 35f;
            float t = 1f - Mathf.Exp(-smoothRate * Time.deltaTime);
            _playerObject.transform.rotation = Quaternion.Slerp(_playerObject.transform.rotation, targetRotation, t);
        }

        if (_shape != null)
        {
            _shape.NotShootTimeDirection = aimDir;
        }
    }
}
