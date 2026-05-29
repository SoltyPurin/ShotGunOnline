using System.Collections;
using System.Collections.Generic;
//using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScoreUIManager : MonoBehaviour
{
    private WaveManager _waveManager = default;

    [SerializeField,Header("合計スコア表示テキスト")]
    private Text _totalScoreText = default;

    [SerializeField, Header("スコア表示UI")]
    private GameObject _scorePrefab = default;

    [SerializeField,Header("項目用テキスト")]
    private Text[] _itemText = new Text[2];

    [SerializeField, Header("スコア用テキスト、上からタイム、緋弾、コンボ")]
    private Text[] _scoreText = new Text[3];

    [SerializeField, Header("ウェーブ数表示用テキスト")]
    private Text _waveText = default;

    //タイムライン
    private PlayableDirector _scoreTimeLine = default;

    private readonly string WAVEMANAGER = "WaveManager";


    private void Start()
    {
        _scoreTimeLine = _scorePrefab.GetComponent<PlayableDirector>();

        _scorePrefab.SetActive(false);

        GameObject waveObj = GameObject.FindWithTag(WAVEMANAGER);
        _waveManager = waveObj.GetComponent<WaveManager>();
    }

    /// <summary>
    /// ウェーブごとのスコア表示する
    /// </summary>
    /// <param name="time">時間</param>
    /// <param name="damage">緋弾のランク</param>
    /// <param name="total">合計のランク</param>
    public void PopText(float time,int damage,string total)
    {
        if(_itemText == null)
        {
            return;
        }
        int nowWave = _waveManager.WaveIndex + 1;

        _scoreText[0].text = time.ToString("F1");
        _scoreText[1].text = damage.ToString();
        _totalScoreText.text = total;
        _waveText.text = "Wave " + nowWave;

        _scorePrefab.SetActive(true);
        _scoreTimeLine.Play();
    }

    private string CalcRank(int score)
    {
        if (score >= 3)
        {
            return "S";
        }
        else if (score >= 2)
        {
            return "A";
        }
        else
        {
            return "B";
        }
    }

    public void HiddenText()
    {
        _scorePrefab.SetActive(false);
    }
}
