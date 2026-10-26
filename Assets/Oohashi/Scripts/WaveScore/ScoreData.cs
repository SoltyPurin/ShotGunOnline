using UnityEngine;
using UnityEngine.SceneManagement;

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
    #endregion

    #region ゲッター変数群

    private int[] _scoreList = new int[0];
    public int[] ScoreList
    {
        get {  return _scoreList; }
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
        get { return _damageTakenCount;}
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
        _scoreList = new int[_waveManager.WaveList.Count];
        _scoreCharList = new string[_waveManager.WaveList.Count];
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
        Debug.Log("被弾回数増えるメソッド呼び出し");
        WaveManager waveManager = FindAnyObjectByType<WaveManager>();
        if (waveManager != null)
        {
            Debug.Log("ウェーブマネージャーあったよ");

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
        //if (_enemyCountAndCreateTime != null)
        //{
        //    _enemyCountAndCreateTime.TakeDamage();
        //}

    }

    public void SetScore(int timeScore, int hitCount,float clearTime)
    {
        _total = timeScore;

        if(_total >= _sRankValue)
        {
            _scoreCharList[_initListIndex] = "S";
        }
        else if(_total >= _aRankValue)
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
        Debug.Log("取得したウェーブのインデックスは" + waveIndex);
        _scoreUIManager.PopTextClientRpc(clearTime, hitCount, _scoreCharList[_initListIndex],waveIndex);


        _initListIndex++;

        _total = 0;

        _damageTakenCount = 0;

    }


}
