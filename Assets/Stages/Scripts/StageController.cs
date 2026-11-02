using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 各ステージプレハブのルートにアタッチし、そのステージの地形・スポーン地点・リスポーン地点・Wave・ギミックを一元管理するクラス。
/// 各ステージプレハブは原点 (0, 0, 0) 基準で作成・編集します。
/// </summary>
public class StageController : MonoBehaviour, ISpawnPointProvider
{
    [Serializable]
    public struct NetworkGimmickEntry
    {
        [Tooltip("DefaultNetworkPrefabsに登録済みのネットワークギミックプレハブ（CandleParent / PlateParent 等）")]
        public GameObject NetworkPrefab;

        [Tooltip("ステージ内の配置基準Transform（未指定時はLocalPositionを使用）")]
        public Transform SpawnPoint;

        [Tooltip("SpawnPoint未指定時のステージローカル座標")]
        public Vector3 LocalPosition;

        [Tooltip("SpawnPoint未指定時の回転（オイラー角）")]
        public Vector3 LocalEulerAngles;
    }

    public static StageController ActiveStage { get; private set; }

    [Header("地形ルート")]
    [SerializeField] private Grid _terrainGrid;

    [Header("初期スポーン地点 (P1〜P4)")]
    [Tooltip("Stage 0 開始時や個別テスト時にプレイヤーが生成される地点")]
    [SerializeField] private StageSpawnPoints _initialPlayerSpawns;

    [Header("リスポーン地点リスト")]
    [Tooltip("[0]=このステージへ遷移した時および落下復帰時の地点、[1以降]=ボス戦等で同一ステージ内進行時の更新地点")]
    [SerializeField] private List<Transform> _respawnPoints = new List<Transform>();

    [Header("ウェーブ設定（敵・ウェーブギミック配置）")]
    [SerializeField] private Wave _stageWave;

    [Header("ボス・中ボス用の基準地点（任意）")]
    [Tooltip("ボスの落下復帰や雑魚召喚の基準点（SpawonPoint2 / BossReturnPoint 等）")]
    [SerializeField] private Transform _bossReturnPoint;

    [Header("ステージ常設ネットワークギミック（任意）")]
    [Tooltip("ステージ読み込み時にサーバーから同期Spawnし、ステージ破棄時に自動Despawnするギミック")]
    [SerializeField] private List<NetworkGimmickEntry> _networkGimmicks = new List<NetworkGimmickEntry>();

    private readonly List<NetworkObject> _spawnedStageNetworkObjects = new List<NetworkObject>();
    private bool _networkGimmicksSpawned;

    public Grid TerrainGrid => _terrainGrid;
    public StageSpawnPoints InitialPlayerSpawns => _initialPlayerSpawns;
    public IReadOnlyList<Transform> RespawnPoints => _respawnPoints;
    public Wave StageWave => _stageWave;
    public Transform BossReturnPoint => _bossReturnPoint;

    private void Awake()
    {
        ActiveStage = this;
        AutoResolveReferencesIfNeeded();
    }

    private void OnEnable()
    {
        ActiveStage = this;
        TrySpawnNetworkGimmicks();
    }

    private void OnDestroy()
    {
        DespawnNetworkGimmicks();

        if (ActiveStage == this)
        {
            ActiveStage = null;
        }
    }

    /// <summary>
    /// 外部（WavePrefabOverride使用時など）からステージのWave参照を差し替えます。
    /// </summary>
    public void SetStageWave(Wave wave)
    {
        _stageWave = wave;
    }

    /// <summary>
    /// エディタや実行時に未設定の参照があれば子階層から自動補完します。
    /// </summary>
    public void AutoResolveReferencesIfNeeded()
    {
        if (_terrainGrid == null)
        {
            _terrainGrid = GetComponentInChildren<Grid>(true);
        }

        if (_initialPlayerSpawns == null)
        {
            _initialPlayerSpawns = GetComponentInChildren<StageSpawnPoints>(true);
        }

        if (_stageWave == null)
        {
            _stageWave = GetComponentInChildren<Wave>(true);
        }

        if (_respawnPoints == null || _respawnPoints.Count == 0)
        {
            Transform respawnRoot = transform.Find("SpawnPoints/RespawnPoints") ?? transform.Find("RespawnPoints");
            if (respawnRoot != null)
            {
                _respawnPoints = new List<Transform>();
                for (int i = 0; i < respawnRoot.childCount; i++)
                {
                    _respawnPoints.Add(respawnRoot.GetChild(i));
                }
            }
        }

        if (_bossReturnPoint == null)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "SpawonPoint2" || t.name == "SpawnPoint2" || t.name == "BossReturnPoint")
                {
                    _bossReturnPoint = t;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// ISpawnPointProvider 実装: プレイヤー番号に応じた初期スポーンTransformを返します。
    /// </summary>
    public Transform GetSpawnPoint(int playerIndex)
    {
        if (_initialPlayerSpawns != null)
        {
            return _initialPlayerSpawns.GetSpawnPoint(playerIndex);
        }

        if (_respawnPoints != null && _respawnPoints.Count > 0 && _respawnPoints[0] != null)
        {
            return _respawnPoints[0];
        }

        return transform;
    }

    /// <summary>
    /// プレイヤー番号とステージ内進行インデックスに応じたリスポーン・テレポート先座標を返します。
    /// </summary>
    public Vector2 GetRespawnPosition(int playerNumber, int subStageIndex = 0)
    {
        if (_respawnPoints != null && _respawnPoints.Count > 0)
        {
            int clamped = Mathf.Clamp(subStageIndex, 0, _respawnPoints.Count - 1);
            Transform target = _respawnPoints[clamped];
            if (target != null)
            {
                Vector2 basePos = target.position;
                float offsetX = (playerNumber % 2 == 0 ? -1f : 1f) * (playerNumber < 2 ? 0.8f : 1.6f);
                return basePos + new Vector2(offsetX, 0f);
            }
        }

        if (_initialPlayerSpawns != null)
        {
            Transform initial = _initialPlayerSpawns.GetSpawnPoint(playerNumber);
            if (initial != null)
            {
                return initial.position;
            }
        }

        return transform.position;
    }

    private void TrySpawnNetworkGimmicks()
    {
        if (_networkGimmicksSpawned || _networkGimmicks == null || _networkGimmicks.Count == 0)
        {
            return;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        _networkGimmicksSpawned = true;
        foreach (var entry in _networkGimmicks)
        {
            if (entry.NetworkPrefab == null) continue;

            Vector3 worldPos = entry.SpawnPoint != null
                ? entry.SpawnPoint.position
                : transform.TransformPoint(entry.LocalPosition);
            Quaternion worldRot = entry.SpawnPoint != null
                ? entry.SpawnPoint.rotation
                : transform.rotation * Quaternion.Euler(entry.LocalEulerAngles);

            GameObject go = Instantiate(entry.NetworkPrefab, worldPos, worldRot);
            NetworkObject netObj = go.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn(true);
                _spawnedStageNetworkObjects.Add(netObj);
            }
        }
    }

    public void DespawnNetworkGimmicks()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
        {
            foreach (var netObj in _spawnedStageNetworkObjects)
            {
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn(true);
                }
            }
        }

        _spawnedStageNetworkObjects.Clear();
        _networkGimmicksSpawned = false;
    }

    private void OnDrawGizmos()
    {
        // リスポーン地点を緑色で可視化
        if (_respawnPoints != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < _respawnPoints.Count; i++)
            {
                Transform pt = _respawnPoints[i];
                if (pt == null) continue;
                Gizmos.DrawWireSphere(pt.position, 0.65f);
                Gizmos.DrawLine(pt.position + Vector3.left * 1.6f, pt.position + Vector3.right * 1.6f);
            }
        }

        // ボス復帰・召喚基準地点をマゼンタ色で可視化
        if (_bossReturnPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(_bossReturnPoint.position, new Vector3(1.2f, 1.2f, 0.2f));
        }
    }
}
