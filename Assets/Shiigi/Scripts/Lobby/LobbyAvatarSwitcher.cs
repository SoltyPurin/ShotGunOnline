using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ロビーでの「オフライン分身」と「ネットワークプレイヤー」の切り替えを担当するクラス。
/// プレイシーンと同じプレイヤープレハブ（NetWork Player Variant）をそのまま使用します。
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
    private BulletPool _cachedBulletPool;
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
        EnsureNetworkManagerSubscribed();

        if (_cameraFollow == null && Camera.main != null)
        {
            _cameraFollow = Camera.main.GetComponent<LobbyCameraFollow>();
        }

        if (_networkManager == null || !_networkManager.IsListening)
        {
            SpawnAvatar(GetInitialPosition(), _lastAimDirection, snapCamera: true);
        }
        else if (TryGetSpawnedLocalPlayer(out var existingPlayer))
        {
            HandleLocalPlayerSpawned(existingPlayer);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeNetworkManager();
        DestroyAvatar();
    }

    private void EnsureNetworkManagerSubscribed()
    {
        var current = NetworkManager.Singleton;
        if (_networkManager == current) return;

        UnsubscribeNetworkManager();

        _networkManager = current;
        if (_networkManager != null)
        {
            _networkManager.OnClientStarted += HandleClientStarted;
            _networkManager.OnClientStopped += HandleClientStopped;
        }
    }

    private void UnsubscribeNetworkManager()
    {
        if (_networkManager == null) return;
        _networkManager.OnClientStarted -= HandleClientStarted;
        _networkManager.OnClientStopped -= HandleClientStopped;
        _networkManager = null;
    }

    private void Update()
    {
        if (_networkManager != NetworkManager.Singleton)
        {
            EnsureNetworkManagerSubscribed();
        }

        if (_localNetworkPlayer == null && TryGetSpawnedLocalPlayer(out var spawnedPlayer))
        {
            HandleLocalPlayerSpawned(spawnedPlayer);
        }
    }

    private bool TryGetSpawnedLocalPlayer(out PlayerMove player)
    {
        player = null;
        return _networkManager != null &&
               _networkManager.IsListening &&
               _networkManager.LocalClient != null &&
               _networkManager.LocalClient.PlayerObject != null &&
               _networkManager.LocalClient.PlayerObject.TryGetComponent(out player);
    }

    private void CapturePose(Transform targetTransform, PlayerAiming aiming)
    {
        if (targetTransform == null) return;
        _lastPosition = targetTransform.position;
        if (aiming != null && aiming.Direction.sqrMagnitude > 0.0001f)
        {
            _lastAimDirection = aiming.Direction;
        }
        _hasLastPosition = true;
    }

    private void EnsureBulletPool()
    {
        if (_cachedBulletPool != null) return;

        _cachedBulletPool = FindAnyObjectByType<BulletPool>();
        if (_cachedBulletPool != null || _bulletPoolPrefab == null) return;

        _cachedBulletPool = Instantiate(_bulletPoolPrefab);
        _cachedBulletPool.gameObject.name = "BulletPool";
    }

    private void HandleClientStarted()
    {
        if (_avatar != null)
        {
            CapturePose(_avatar.transform, _avatarAiming);
            DestroyAvatar();
        }
    }

    private void HandleLocalPlayerSpawned(PlayerMove player)
    {
        _localNetworkPlayer = player;
        _localNetworkAiming = player.GetComponent<PlayerAiming>();

        if (_avatar != null)
        {
            CapturePose(_avatar.transform, _avatarAiming);
        }

        Vector2 position = _hasLastPosition ? _lastPosition : GetInitialPosition();
        player.Teleport(position);
        if (_localNetworkAiming != null)
        {
            _localNetworkAiming.SetDirection(_lastAimDirection);
        }

        DestroyAvatar();
        _cameraFollow?.SetTarget(player.transform);
    }

    private void HandleLocalPlayerDespawned(PlayerMove player)
    {
        if (_localNetworkPlayer != player) return;

        CapturePose(player.transform, _localNetworkAiming);
        _localNetworkPlayer = null;
        _localNetworkAiming = null;
    }

    private void HandleClientStopped(bool wasHost)
    {
        if (this == null || !isActiveAndEnabled || !gameObject.scene.isLoaded) return;

        if (_localNetworkPlayer != null)
        {
            CapturePose(_localNetworkPlayer.transform, _localNetworkAiming);
        }

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
        _avatarAiming?.SetDirection(aimDirection);
        _cameraFollow?.SetTarget(_avatar.transform, snapCamera);
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
