using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ロビーでの「オフライン分身」と「ネットワークプレイヤー」の切り替えを担当するクラス。
/// プレイシーンと同じプレイヤープレハブ（NetWork Player Variant）をそのまま使用します。
/// ・シーン開始時（未接続）：オフライン状態でプレイヤープレハブを出現させる
/// ・自端末のネットワークプレイヤーがスポーン：分身の位置・照準向きを引き継いで分身を破棄
/// ・ルーム退出/切断（OnClientStopped）：最後の位置・照準向きに分身を再出現させる
/// </summary>
public class LobbyAvatarSwitcher : MonoBehaviour
{
    [Header("プレイヤープレハブ（NetWork Player Variant）")]
    [SerializeField] private GameObject _playerPrefab;

    [Header("弾プールプレハブ（BulletPool）")]
    [SerializeField] private BulletPool _bulletPoolPrefab;

    [Header("スポーン地点（未設定時は StageSpawnPoints.Instance を使用）")]
    [SerializeField] private StageSpawnPoints _spawnPoints;

    [Header("ロビー用カメラ")]
    [SerializeField] private LobbyCameraFollow _cameraFollow;

    private NetworkManager _networkManager;
    private GameObject _avatar;
    private PlayerAiming _avatarAiming;
    private PlayerMove _localNetworkPlayer;
    private PlayerAiming _localNetworkAiming;
    private Vector2 _lastPosition;
    private Vector3 _lastAimDirection = Vector3.right;
    private bool _hasLastPosition;

    private void OnEnable()
    {
        PlayerMove.LocalPlayerSpawned += HandleLocalPlayerSpawned;
        PlayerMove.LocalPlayerDespawned += HandleLocalPlayerDespawned;
    }

    private void OnDisable()
    {
        PlayerMove.LocalPlayerSpawned -= HandleLocalPlayerSpawned;
        PlayerMove.LocalPlayerDespawned -= HandleLocalPlayerDespawned;
    }

    private void Start()
    {
        EnsureBulletPool();

        _networkManager = NetworkManager.Singleton;
        if (_networkManager != null)
        {
            _networkManager.OnClientStarted += HandleClientStarted;
            _networkManager.OnClientStopped += HandleClientStopped;
        }

        if (_cameraFollow == null && Camera.main != null)
        {
            _cameraFollow = Camera.main.GetComponent<LobbyCameraFollow>();
        }

        // 未接続ならオフライン分身を出す（接続済みの場合はネットワークプレイヤーのスポーン通知を待つ／既にスポーン済みなら即座に紐付ける）
        if (_networkManager == null || !_networkManager.IsListening)
        {
            SpawnAvatar(GetInitialPosition(), _lastAimDirection, snapCamera: true);
        }
        else if (_networkManager.LocalClient != null &&
                 _networkManager.LocalClient.PlayerObject != null &&
                 _networkManager.LocalClient.PlayerObject.TryGetComponent<PlayerMove>(out var existingPlayer))
        {
            HandleLocalPlayerSpawned(existingPlayer);
        }
    }

    private void OnDestroy()
    {
        if (_networkManager != null)
        {
            _networkManager.OnClientStarted -= HandleClientStarted;
            _networkManager.OnClientStopped -= HandleClientStopped;
        }
    }

    private void Update()
    {
        // 万が一スポーン通知を取りこぼしていた場合でもローカルプレイヤーを検出してカメラ追従を紐付ける
        if (_localNetworkPlayer == null &&
            _networkManager != null &&
            _networkManager.IsListening &&
            _networkManager.LocalClient != null &&
            _networkManager.LocalClient.PlayerObject != null &&
            _networkManager.LocalClient.PlayerObject.TryGetComponent<PlayerMove>(out var spawnedPlayer))
        {
            HandleLocalPlayerSpawned(spawnedPlayer);
        }

        // 切断時に分身を同じ場所・同じ向きへ戻すため、現在の操作キャラの状態を記録しておく
        if (_localNetworkPlayer != null)
        {
            _lastPosition = _localNetworkPlayer.transform.position;
            if (_localNetworkAiming != null && _localNetworkAiming.Direction.sqrMagnitude > 0.0001f)
            {
                _lastAimDirection = _localNetworkAiming.Direction;
            }
            _hasLastPosition = true;
        }
        else if (_avatar != null)
        {
            _lastPosition = _avatar.transform.position;
            if (_avatarAiming != null && _avatarAiming.Direction.sqrMagnitude > 0.0001f)
            {
                _lastAimDirection = _avatarAiming.Direction;
            }
            _hasLastPosition = true;
        }
    }

    private void EnsureBulletPool()
    {
        if (FindAnyObjectByType<BulletPool>() != null) return;
        if (_bulletPoolPrefab == null) return;

        BulletPool pool = Instantiate(_bulletPoolPrefab);
        pool.gameObject.name = "BulletPool";
    }

    private void HandleClientStarted()
    {
        // ネットワーク開始時にオフライン分身が残っていれば位置・向きを記録して破棄する
        if (_avatar != null)
        {
            _lastPosition = _avatar.transform.position;
            if (_avatarAiming != null && _avatarAiming.Direction.sqrMagnitude > 0.0001f)
            {
                _lastAimDirection = _avatarAiming.Direction;
            }
            _hasLastPosition = true;
            DestroyAvatar();
        }
    }

    private void HandleLocalPlayerSpawned(PlayerMove player)
    {
        _localNetworkPlayer = player;
        _localNetworkAiming = player.GetComponent<PlayerAiming>();

        // 分身がいた場所と向きをネットワークプレイヤーに引き継いでから、分身を消す
        Vector2 position = _avatar != null ? (Vector2)_avatar.transform.position
                         : _hasLastPosition ? _lastPosition
                         : GetInitialPosition();
        Vector3 aimDir = (_avatar != null && _avatarAiming != null && _avatarAiming.Direction.sqrMagnitude > 0.0001f)
                         ? _avatarAiming.Direction
                         : _lastAimDirection;

        player.Teleport(position);
        if (_localNetworkAiming != null)
        {
            _localNetworkAiming.SetDirection(aimDir);
        }

        DestroyAvatar();

        if (_cameraFollow != null)
        {
            _cameraFollow.SetTarget(player.transform);
        }
    }

    private void HandleLocalPlayerDespawned(PlayerMove player)
    {
        if (_localNetworkPlayer != player) return;

        _lastPosition = player.transform.position;
        if (_localNetworkAiming != null && _localNetworkAiming.Direction.sqrMagnitude > 0.0001f)
        {
            _lastAimDirection = _localNetworkAiming.Direction;
        }
        _hasLastPosition = true;
        _localNetworkPlayer = null;
        _localNetworkAiming = null;
        // 分身の再出現は OnClientStopped で行う（シーン遷移によるデスポーンでは出さないため）
    }

    private void HandleClientStopped(bool wasHost)
    {
        // シーン破棄中や非ロード時は何もしない
        if (this == null || !isActiveAndEnabled || !gameObject.scene.isLoaded) return;

        _localNetworkPlayer = null;
        _localNetworkAiming = null;
        if (_avatar == null)
        {
            SpawnAvatar(_hasLastPosition ? _lastPosition : GetInitialPosition(), _lastAimDirection, snapCamera: false);
        }
    }

    private void SpawnAvatar(Vector2 position, Vector3 aimDirection, bool snapCamera)
    {
        if (_playerPrefab == null)
        {
            Debug.LogError("[LobbyAvatarSwitcher] _playerPrefab がインスペクターで設定されていません。");
            return;
        }

        EnsureBulletPool();

        _avatar = Instantiate(_playerPrefab, new Vector3(position.x, position.y, 0f), Quaternion.identity);
        _avatar.name = "LobbyOfflinePlayer";
        _avatarAiming = _avatar.GetComponent<PlayerAiming>();
        if (_avatarAiming != null)
        {
            _avatarAiming.SetDirection(aimDirection);
        }

        if (_cameraFollow != null)
        {
            _cameraFollow.SetTarget(_avatar.transform, snapCamera);
        }
    }

    private void DestroyAvatar()
    {
        if (_avatar == null) return;

        Destroy(_avatar);
        _avatar = null;
        _avatarAiming = null;
    }

    private Vector2 GetInitialPosition()
    {
        ISpawnPointProvider provider = _spawnPoints != null ? _spawnPoints : StageSpawnPoints.Instance;
        Transform point = provider?.GetSpawnPoint(0);
        return point != null ? (Vector2)point.position : Vector2.zero;
    }
}
