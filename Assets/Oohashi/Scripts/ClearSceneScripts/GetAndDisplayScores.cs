using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GetAndDisplayScores : MonoBehaviour
{
    [SerializeField, Header("細かいスコアを表示するスクリプト")]
    private CreateWavesTexts _nextScores = default;
    [SerializeField, Header("Sランクのイラスト")]
    private GameObject _sRankImage = default;
    [SerializeField,Header("Aランクのイラスト")]
    private GameObject _aRankImage =default;
    [SerializeField,Header("Bランクのイラスト")]
    private GameObject _bRankImage = default;

    [SerializeField, Header("＋のテキスト")]
    private GameObject _plusText = default;

    [SerializeField, Header("最初に表示するスコア")]
    private GameObject _firstScores = default;
    //sランクを取るのに必要な点数、ウェーブ数から求める
    private int _sRankValue = 0;
    //aランクを取るのに必要な点数
    private int _aRankValue = 0;
    [SerializeField, Header("Sランクを取るのに必要な割合")]
    private float _sRankRatio = 7;
    [SerializeField,Header("Aランクを取るのに必要な割合")]
    private float _aRankRatio = 4;

    [SerializeField, Header("最大スコア")]
    private float _maxScore = 36;

    [SerializeField, Header("スコア表示をするテキスト")]
    private Text _totalScoreShowText = default;
    [SerializeField, Header("被弾数を表示するテキスト")]
    private Text _hitsTakenText = default;
    //[SerializeField,Header("コンボ数を表示するテキスト")]
    //private Text _maxComboText = default;

    [SerializeField,Header("戻るボタン")]
    private GameObject _returnButton = default;

    [SerializeField,Header("詳細を表示するボタン")]
    private GameObject _showDetailButton = default;

    [SerializeField, Header("タイトルに戻るボタン")]
    private Button _titleButton = default;

    private int _maxPoint = 3;
    private ScoreData _scoreData = default;
    private ScoreCircle _scoreCircle = default;
    private readonly string SCOREDATA = "WaveScore";

    private int _scoreSize = 0;

    private int _totalScore = 0;

    private string[] _testList = { "a", "b", "c", "d", "e", "f" };

    /// <summary>
    /// 1ゲームの合計スコアを計算、それと各ウェーブのスコアを表示するスクリプトに生成命令を出す
    /// </summary>
    private void Start()
    {
        _sRankRatio *= 0.1f;
        _aRankRatio *= 0.1f;
        GameObject scoreObject = GameObject.FindWithTag(SCOREDATA);
        if(scoreObject != null)
        {
            _scoreData = scoreObject.GetComponent<ScoreData>();
            _scoreSize = _scoreData.ScoreList.Length;
            _sRankValue = (int)Mathf.Floor(_scoreSize * _maxPoint * _sRankRatio);
            _aRankValue = (int)Mathf.Floor(_scoreSize * _maxPoint * _aRankRatio);
            _nextScores.CreateWaves(_scoreSize, _scoreData.ScoreCharList);
        }
        else
        {
            _scoreSize = _testList.Length;
            _sRankValue = (int)Mathf.Floor(_scoreSize * _maxPoint * _sRankRatio);
            _aRankValue = (int)Mathf.Floor(_scoreSize * _maxPoint * _aRankRatio);
            _nextScores.CreateWaves(6, _testList);
        }
        _scoreCircle = GetComponent<ScoreCircle>();
        for (int i =0; i < _scoreSize; i++)
        {
            _totalScore += _scoreData.ScoreList[i];
        }
        _scoreCircle.Initialize(_maxScore,_totalScore);
        if(_totalScore >= _sRankValue)
        {
            _totalScoreShowText.text = "S";
            _sRankImage.SetActive(true);

            if(_totalScore >= _maxScore)
            {
                _plusText.SetActive(true);
            }
        }
        else if( _totalScore >= _aRankValue)
        {
            _totalScoreShowText.text = "A";
            _aRankImage.SetActive(true);
        }
        else
        {
            _totalScoreShowText.text = "B";
            _bRankImage.SetActive(true);
        }

        _hitsTakenText.text = _scoreData.DamageTakenCount.ToString();

        Destroy(scoreObject);
    }

    /// <summary>
    /// ボタンを押したら実行。
    /// 各ウェーブのスコア表示を行う、最初に表示してるやつらを非表示にして詳細情報を表示する
    /// </summary>
    public void NextScoreShow()
    {
        _returnButton.SetActive(true);
        _firstScores.SetActive(false);
        _nextScores.ShowWaveScores();
        Navigation nav = _titleButton.navigation;
        Button button = _returnButton.GetComponent<Button>();
        nav.selectOnUp = button;
        nav.selectOnDown = button;
        _titleButton.navigation = nav;
        EventSystem.current.SetSelectedGameObject(_returnButton);
    }

    /// <summary>
    /// ボタンを押したら実行。
    /// 各ウェーブのスコアを非表示にして最初のトータルスコアを表示
    /// </summary>
    public void FirstScoreShow()
    {
        _returnButton.SetActive(false);
        _firstScores.SetActive(true);
        _nextScores.HideWaveScores();
        Navigation nav = _titleButton.navigation;
        Button button = _showDetailButton.GetComponent<Button>();
        nav.selectOnUp = button;
        nav.selectOnDown = button;
        _titleButton.navigation = nav;
        EventSystem.current.SetSelectedGameObject(_showDetailButton);
    }
}
