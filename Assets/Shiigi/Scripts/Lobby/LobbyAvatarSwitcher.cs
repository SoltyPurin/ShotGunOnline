using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ロビーでの「オフライン分身」と「ネットワークプレイヤー」の切り替えを担当するクラス。
/// ・シーン開始時（未接続）：分身を出現させる
/// ・自端末のネットワークプレイヤーがスポーン：分身の位置・向きを引き継いで分身を破棄
/// ・ルーム退出/切断（OnClientStopped）：最後の位置・向きに分身を再出現させる
/// </summary>
public class LobbyAvatarSwitcher : MonoBehaviour
{
    [Header("オフライン分身プレハブ（NetworkObject なし）")]
    [SerializeField] private LobbyLocalAvatar _localAvatarPrefab;

    [Header("スポーン地点（未設定時は StageSpawnPoints.Instance を使用）")]
    [SerializeField] private StageSpawnPoints _spawnPoints;

    [Header("ロビー用カメラ")]
    [SerializeField] private LobbyCameraFollow _cameraFollow;

    private NetworkManager _networkManager;
    private LobbyLocalAvatar _avatar;
    private LobbyNetworkPlayer _localNetworkPlayer;
    private Vector2 _lastPosition;
    private byte _lastFacingMask;
    private bool _hasLastPosition;

    private void OnEnable()
    {
        LobbyNetworkPlayer.LocalPlayerSpawned += HandleLocalPlayerSpawned;
        LobbyNetworkPlayer.LocalPlayerDespawned += HandleLocalPlayerDespawned;
    }

    private void OnDisable()
    {
        LobbyNetworkPlayer.LocalPlayerSpawned -= HandleLocalPlayerSpawned;
        LobbyNetworkPlayer.LocalPlayerDespawned -= HandleLocalPlayerDespawned;
    }

    private void Start()
    {
        _networkManager = NetworkManager.Singleton;
        if (_networkManager != null)
        {
            _networkManager.OnClientStopped += HandleClientStopped;
        }

        if (_cameraFollow == null && Camera.main != null)
        {
            _cameraFollow = Camera.main.GetComponent<LobbyCameraFollow>();
        }

        // 未接続ならオフライン分身を出す（接続済みの場合はネットワークプレイヤーのスポーン通知を待つ）
        if (_networkManager == null || !_networkManager.IsListening)
        {
            SpawnAvatar(GetInitialPosition(), _lastFacingMask, snapCamera: true);
        }
    }

    private void OnDestroy()
    {
        if (_networkManager != null)
        {
            _networkManager.OnClientStopped -= HandleClientStopped;
        }
    }

    private void Update()
    {
        // 切断時に分身を同じ場所・同じ向きへ戻すため、現在の操作キャラの状態を記録しておく
        if (_localNetworkPlayer != null)
        {
            _lastPosition = _localNetworkPlayer.transform.position;
            _lastFacingMask = _localNetworkPlayer.FacingMask;
            _hasLastPosition = true;
        }
        else if (_avatar != null)
        {
            _lastPosition = _avatar.transform.position;
            _lastFacingMask = _avatar.FacingMask;
            _hasLastPosition = true;
        }
    }

    private void HandleLocalPlayerSpawned(LobbyNetworkPlayer player)
    {
        _localNetworkPlayer = player;

        // 分身がいた場所と向きをネットワークプレイヤーに引き継いでから、分身を消す
        Vector2 position = _avatar != null ? (Vector2)_avatar.transform.position
                         : _hasLastPosition ? _lastPosition
                         : GetInitialPosition();
        byte facing = _avatar != null ? _avatar.FacingMask : _lastFacingMask;

        player.Teleport(position, facing);
        DestroyAvatar();

        if (_cameraFollow != null)
        {
            _cameraFollow.SetTarget(player.transform);
        }
    }

    private void HandleLocalPlayerDespawned(LobbyNetworkPlayer player)
    {
        if (_localNetworkPlayer != player) return;

        _lastPosition = player.transform.position;
        _lastFacingMask = player.FacingMask;
        _hasLastPosition = true;
        _localNetworkPlayer = null;
        // 分身の再出現は OnClientStopped で行う（シーン遷移によるデスポーンでは出さないため）
    }

    private void HandleClientStopped(bool wasHost)
    {
        // シーン破棄中や非ロード時は何もしない
        if (this == null || !isActiveAndEnabled || !gameObject.scene.isLoaded) return;

        _localNetworkPlayer = null;
        if (_avatar == null)
        {
            SpawnAvatar(_hasLastPosition ? _lastPosition : GetInitialPosition(), _lastFacingMask, snapCamera: false);
        }
    }

    private void SpawnAvatar(Vector2 position, byte facingMask, bool snapCamera)
    {
        if (_localAvatarPrefab == null)
        {
            Debug.LogError("[LobbyAvatarSwitcher] _localAvatarPrefab がインスペクターで設定されていません。");
            return;
        }

        _avatar = Instantiate(_localAvatarPrefab, new Vector3(position.x, position.y, 0f), Quaternion.identity);
        _avatar.SetFacingMask(facingMask);

        if (_cameraFollow != null)
        {
            _cameraFollow.SetTarget(_avatar.transform, snapCamera);
        }
    }

    private void DestroyAvatar()
    {
        if (_avatar == null) return;

        Destroy(_avatar.gameObject);
        _avatar = null;
    }

    private Vector2 GetInitialPosition()
    {
        ISpawnPointProvider provider = _spawnPoints != null ? _spawnPoints : StageSpawnPoints.Instance;
        Transform point = provider?.GetSpawnPoint(0);
        return point != null ? (Vector2)point.position : Vector2.zero;
    }
}
