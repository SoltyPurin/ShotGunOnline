using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
public struct EnemyKillCounts
{
    [Tooltip("通常の敵のカウント")]
    public int normalEnemyCount;
    [Tooltip("中ボスのカウント")]
    public int middleEnemyCount;
    [Tooltip("ボスのキルカウント")]
    public int bossEnemyCount;
}

public class ScoreData : MonoBehaviour
{
    #region 変数
    public static ScoreData _scoreData = default;

    private int _total = 0;

    private ScoreUIManager _scoreUIManager = default;

    private WaveManager _waveManager = default;

    private EnemyCountAndCreateTime _enemyCountAndCreateTime = default;

    [SerializeField, Header("合計Sランクの点数")]
    private int _sRankValue = 7;
    [SerializeField, Header("合計Aランクの点数")]
    private int _aRankValue = 5;


    private EnemyKillCounts _enemyKillCounts;
    private bool _isZombiMode = false;

    private int _totalEnemyCount = 0;
    #endregion

    #region ゲッター変数群

    private int[] _scoreList = new int[0];
    public int[] ScoreList
    {
        get { return _scoreList; }
    }

    private string[] _scoreCharList = new string[0];
    public string[] ScoreCharList
    {
        get { return _scoreCharList; }
    }

    private int _initListIndex = 0;

    private int _maxCombo = 0;
    public int MaxCombo
    {
        get { return _maxCombo; }
    }

    private int _damageTakenCount = 0;

    public int DamageTakenCount
    {
        get { return _damageTakenCount; }
    }

    private int _totalTakenDamage = 0;
    public int TotalTakenDamage
    {
        get { return _totalTakenDamage; }
    }

    public EnemyKillCounts GetEnemyKillCount
    {
        get { return _enemyKillCounts; }
    }
    public bool IsZombiMode
    {
        get { return _isZombiMode; }
    }
    public int TotalEnemyCount
    {
        get { return _totalEnemyCount; }
    }


    #endregion
    #region 定数
    private readonly string WAVEMANAGER = "WaveManager";
    #endregion

    #region シングルトン化
    private void Awake()
    {
        if (_scoreData != null && _scoreData != this)
        {
            Destroy(gameObject);
            return;
        }

        _scoreData = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (this == null || gameObject == null) return; // 保険
        bool destroyScene = scene.name == "TutorialScene" || scene.name == "Title" || scene.name == "Cushion";
        if (destroyScene && gameObject != null)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Destroy(this.gameObject);
        }
    }
    #endregion

    private void Start()
    {
        _enemyCountAndCreateTime = GetComponent<EnemyCountAndCreateTime>();
        _scoreUIManager = FindAnyObjectByType<ScoreUIManager>();
        _waveManager = GameObject.FindWithTag(WAVEMANAGER).GetComponent<WaveManager>();
        int waveCount = _waveManager.TotalWaveCount;
        _scoreList = new int[waveCount];
        _scoreCharList = new string[waveCount];
    }

    public void SaveMaxCombo(int maxCombo)
    {
        _maxCombo = maxCombo;
    }

    /// <summary>
    /// 被弾時にプレイヤーから呼ばれるメソッド
    /// </summary>
    public void TakeDamageCountPlus()
    {
        WaveManager waveManager = FindAnyObjectByType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.NotifyTakeDamageServerRpc();
        }
        else
        {
            AddDamageCount();
        }
    }

    /// <summary>
    /// サーバー（ホスト）側で ServerRpc 受領後に呼ばれる、純粋な加算処理
    /// </summary>
    public void AddDamageCount()
    {
        _damageTakenCount++;
        _totalTakenDamage++;
    }

    public void SetScore(int timeScore, int hitCount, float clearTime)
    {
        _total = timeScore;

        if (_initListIndex < _scoreCharList.Length)
        {
            if (_total >= _sRankValue)
            {
                _scoreCharList[_initListIndex] = "S";
            }
            else if (_total >= _aRankValue)
            {
                _scoreCharList[_initListIndex] = "A";
            }
            else
            {
                _scoreCharList[_initListIndex] = "B";
            }

            _scoreList[_initListIndex] = _total;

            int waveIndex = 0;
            WaveManager waveManager = FindAnyObjectByType<WaveManager>();
            if (waveManager)
            {
                waveIndex = waveManager.WaveIndex;
            }
            if (_scoreUIManager == null)
            {
                _scoreUIManager = FindAnyObjectByType<ScoreUIManager>();
            }
            if (_scoreUIManager != null)
            {
                // SyncScoreClientRpc によって全端末で SetScore が実行されているため、直接ローカルUIを表示
                _scoreUIManager.PopText(clearTime, hitCount, _scoreCharList[_initListIndex], waveIndex);
            }

            _initListIndex++;
        }

        _total = 0;
        _damageTakenCount = 0;
    }

    /// <summary>
    /// 敵のキル数を数える
    /// </summary>
    /// <param name="enemyType"></param>
    public void GetEnemyCount(EnemyType enemyType)
    {
        switch (enemyType)
        {
            case EnemyType.Normal:
                _enemyKillCounts.normalEnemyCount += 1;
                break;

            case EnemyType.Middle:
                _enemyKillCounts.middleEnemyCount += 1;
                break;

            case EnemyType.Boss:
                _enemyKillCounts.bossEnemyCount += 1;
                break;
        }

        _isZombiMode = true;
    }

    public void GetTotalEnemyCount(int count)
    {
        _totalEnemyCount += count;
    }

    /// <summary>
    /// (クライアント側の場合)現在の値をホストから送られた値に上書きし、同期する
    /// </summary>
    /// <param name="normalCount"></param>
    /// <param name="middleCount"></param>
    /// <param name="bossCount"></param>
    /// <param name="totalEnemyCount"></param>
    /// <param name="damageTakenCount"></param>
    /// <param name="totalTakenDamage"></param>
    public void SetZombieScoreFromHost(
    int normalCount,
    int middleCount,
    int bossCount,
    int totalEnemyCount)
    {
        _enemyKillCounts.normalEnemyCount = normalCount;
        _enemyKillCounts.middleEnemyCount = middleCount;
        _enemyKillCounts.bossEnemyCount = bossCount;

        _totalEnemyCount = totalEnemyCount;

        _isZombiMode = true;

    }
    /// <summary>
    /// ホストから最終リザルト用に通算被弾数を全端末へ同期させるためのメソッド
    /// </summary>
    public void SyncTotalDamageFromHost(int totalDamage)
    {
        _totalTakenDamage = totalDamage;
    }
}
