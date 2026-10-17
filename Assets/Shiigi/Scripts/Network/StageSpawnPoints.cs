using UnityEngine;

/// <summary>
/// 各ステージシーンに配置し、そのステージ固有のプレイヤー初期位置・向きを提供するクラス
/// </summary>
public class StageSpawnPoints : MonoBehaviour
{
    public static StageSpawnPoints Instance { get; private set; }

    [Header("プレイヤーごとのスポーン位置")]
    [Tooltip("未設定の場合は、このオブジェクトの子オブジェクトが自動的に登録されます")]
    [SerializeField] private Transform[] _spawnPoints;

    private void Awake()
    {
        Instance = this;

        // インスペクター未設定の場合、子オブジェクトのTransformを自動で拾う
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            _spawnPoints = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                _spawnPoints[i] = transform.GetChild(i);
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// プレイヤー番号に応じたスポーン地点を取得
    /// </summary>
    public Transform GetSpawnPoint(int playerIndex)
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogWarning("[StageSpawnPoints] スポーン地点が設定されていません。自身のTransformを返します。");
            return transform;
        }

        // 人数がスポーン地点数を超えた場合はループ
        return _spawnPoints[playerIndex % _spawnPoints.Length];
    }

    /// <summary>
    /// エディタのSceneビューで位置と向きを描画
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        if (_spawnPoints != null)
        {
            foreach (var point in _spawnPoints)
            {
                if (point == null) continue;
                Gizmos.DrawWireSphere(point.position, 0.5f);
                Gizmos.DrawRay(point.position, point.forward * 1.5f);
            }
        }
        else
        {
            // 子オブジェクトのギズモをフォールバック描画
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child == null) continue;
                Gizmos.DrawWireSphere(child.position, 0.5f);
                Gizmos.DrawRay(child.position, child.forward * 1.5f);
            }
        }
    }
}
