using NUnit.Framework.Api;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        _scoreUIManager = FindFirstObjectByType<ScoreUIManager>();
        _waveManager = GameObject.FindWithTag(WAVEMANAGER).GetComponent<WaveManager>();
        _scoreList = new int[_waveManager.WaveList.Count];
        _scoreCharList = new string[_waveManager.WaveList.Count];
    }


    public void SaveMaxCombo(int maxCombo)
    {
        _maxCombo = maxCombo;
    }

    public void TakeDamageCountPlus()
    {
        _damageTakenCount++;
        _enemyCountAndCreateTime.TakeDamage();
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

        _scoreUIManager.PopText(clearTime, hitCount, _scoreCharList[_initListIndex]);


        _initListIndex++;

        _total = 0;

    }
}
