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
        get { return IsOwner ? _prevDirection : _netAimDirection.Value; } // Ownerはローカル値を即時参照し遅延を防ぐ
    }


    [SerializeField, Header("減算中かどうかを取得する")]
    private InputPlayerShot _inputShot = default;

    [SerializeField, Header("プレイヤーのステート管理")]
    private PlayerStateManager _playerStateManager = default;

    [SerializeField, Header("カットシーン制御（任意）")]
    private CutsceneControlledPlayer _cutscenePlayer = default;

    private Vector2 _rightStickInput;
    private Gamepad _gamePad;

    [Header("プレイヤーのオブジェクト取得")]
    [SerializeField] private GameObject _playerObject = default;
    [SerializeField, Header("Lerpの補正値")]
    private float _correctionValue = 1f;

    [Header("撃ってないときの向いてる方向表示")]
    [SerializeField] private ShootShape _shape = default;

    private Vector3 _prevDirection = Vector3.right;
    private bool _useMouseAim = false;


    private void Awake()
    {
        if (Gamepad.current != null)
        {
            _gamePad = Gamepad.current;
        }

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
        if (!this.IsOwner) return;

        if (!CanAim())
        {
            _rightStickInput = Vector2.zero;
            if (_playerObject != null)
            {
                _playerObject.transform.rotation = Quaternion.identity;
            }
            if (_shape != null)
            {
                _shape.NotShootTimeDirection = Vector3.right;
            }
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
            if (_rightStickInput.sqrMagnitude > 0.04f)
            {
                _prevDirection = new Vector3(_rightStickInput.x, _rightStickInput.y, 0f).normalized;
            }
            return;
        }

        if (Camera.main == null) return;

        Vector3 mousePos = Mouse.current != null
            ? (Vector3)Mouse.current.position.ReadValue()
            : Input.mousePosition;
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
        if (_playerObject == null) return;

        // Owner は Update() 側で入力・描画を即時反映済みのため、ここでは非 Owner のみ補間適用する
        if (this.IsOwner) return;

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

        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
        _playerObject.transform.rotation = immediate
            ? targetRotation
            : Quaternion.Lerp(_playerObject.transform.rotation, targetRotation, _correctionValue);

        if (_shape != null)
        {
            _shape.NotShootTimeDirection = aimDir;
        }
    }
}
