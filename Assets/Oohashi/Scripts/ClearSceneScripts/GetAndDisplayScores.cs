using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GetAndDisplayScores : MonoBehaviour
{
    [SerializeField, Header("細かいスコアを表示するスクリプト")]
    private CreateWavesTexts _nextScores = default;
    [SerializeField, Header("Sランクのイラスト")]
    private GameObject _sRankImage = default;
    [SerializeField, Header("Aランクのイラスト")]
    private GameObject _aRankImage = default;
    [SerializeField, Header("Bランクのイラスト")]
    private GameObject _bRankImage = default;

    [SerializeField, Header("マルチプレイの時のイラスト")]
    private GameObject _multiPlayImage = default;
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
    [SerializeField, Header("Aランクを取るのに必要な割合")]
    private float _aRankRatio = 4;

    [SerializeField, Header("最大スコア")]
    private float _maxScore = 36;

    [SerializeField, Header("スコア表示をするテキスト")]
    private Text _totalScoreShowText = default;
    [SerializeField, Header("被弾数を表示するテキスト")]
    private Text _hitsTakenText = default;

    [SerializeField, Header("戻るボタン")]
    private GameObject _returnButton = default;

    [SerializeField, Header("詳細を表示するボタン")]
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
        FreezeWatchdogLogger.Checkpoint("GetAndDisplayScores.Start: 開始");

        // リザルト画面で不要な残存プレイヤーオブジェクトのゲーム動作を停止
        DisableResidualPlayers();

        int onlinePlayerCount = 1;
        bool isMultiplay = false;

        // 1. ネットワークセッションが稼働しているかチェック
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            onlinePlayerCount = Mathf.Max(1, NetworkManager.Singleton.ConnectedClientsIds.Count);
            isMultiplay = (onlinePlayerCount > 1);
        }

        _sRankRatio *= 0.1f;
        _aRankRatio *= 0.1f;
        GameObject scoreObject = GameObject.FindWithTag(SCOREDATA);
        if (scoreObject != null)
        {
            _scoreData = scoreObject.GetComponent<ScoreData>();
            if (_scoreData != null && _scoreData.ScoreList != null && _scoreData.ScoreList.Length > 0)
            {
                _scoreSize = _scoreData.ScoreList.Length;
                _sRankValue = (int)Mathf.Floor(_scoreSize * _maxPoint * _sRankRatio);
                _aRankValue = (int)Mathf.Floor(_scoreSize * _maxPoint * _aRankRatio);
                _nextScores?.CreateWaves(_scoreSize, _scoreData.ScoreCharList);
            }
            else
            {
                UseFallbackScoreData();
            }
        }
        else
        {
            UseFallbackScoreData();
        }

        FreezeWatchdogLogger.Checkpoint("GetAndDisplayScores.Start: スコア集計とScoreCircle初期化");
        _scoreCircle = GetComponent<ScoreCircle>();
        _totalScore = 0;
        if (_scoreData != null && _scoreData.ScoreList != null)
        {
            for (int i = 0; i < Mathf.Min(_scoreSize, _scoreData.ScoreList.Length); i++)
            {
                _totalScore += _scoreData.ScoreList[i];
            }
        }

        if (_scoreCircle != null)
        {
            _scoreCircle.Initialize(_maxScore, _totalScore);
        }

        FreezeWatchdogLogger.Checkpoint("GetAndDisplayScores.Start: ランク判定・UI表示");
        if (_totalScore >= _sRankValue)
        {
            if (_totalScoreShowText != null) _totalScoreShowText.text = "S";
            if (isMultiplay)
            {
                if (_multiPlayImage != null)
                {
                    _multiPlayImage.SetActive(true);
                    MultiPlayResultImageChanger imageChanger = _multiPlayImage.GetComponent<MultiPlayResultImageChanger>();
                    imageChanger?.ChangeResutlImage(onlinePlayerCount);
                }
            }
            else
            {
                if (_sRankImage != null) _sRankImage.SetActive(true);
            }

            if (_totalScore >= _maxScore && _plusText != null)
            {
                _plusText.SetActive(true);
            }
        }
        else if (_totalScore >= _aRankValue)
        {
            if (_totalScoreShowText != null) _totalScoreShowText.text = "A";
            if (isMultiplay)
            {
                if (_multiPlayImage != null)
                {
                    _multiPlayImage.SetActive(true);
                    MultiPlayResultImageChanger imageChanger = _multiPlayImage.GetComponent<MultiPlayResultImageChanger>();
                    imageChanger?.ChangeResutlImage(onlinePlayerCount);
                }
            }
            else
            {
                if (_aRankImage != null) _aRankImage.SetActive(true);
            }
        }
        else
        {
            if (_totalScoreShowText != null) _totalScoreShowText.text = "B";
            if (isMultiplay)
            {
                if (_multiPlayImage != null)
                {
                    _multiPlayImage.SetActive(true);
                    MultiPlayResultImageChanger imageChanger = _multiPlayImage.GetComponent<MultiPlayResultImageChanger>();
                    imageChanger?.ChangeResutlImage(onlinePlayerCount);
                }
            }
            else
            {
                if (_bRankImage != null) _bRankImage.SetActive(true);
            }
        }

        if (_hitsTakenText != null && _scoreData != null)
        {
            _hitsTakenText.text = _scoreData.TotalTakenDamage.ToString();
        }

        if (scoreObject != null)
        {
            Destroy(scoreObject);
        }

        FreezeWatchdogLogger.Checkpoint("GetAndDisplayScores.Start: 完了");
    }

    private void UseFallbackScoreData()
    {
        _scoreSize = _testList.Length;
        _sRankValue = (int)Mathf.Floor(_scoreSize * _maxPoint * _sRankRatio);
        _aRankValue = (int)Mathf.Floor(_scoreSize * _maxPoint * _aRankRatio);
        _nextScores?.CreateWaves(6, _testList);
    }

    /// <summary>
    /// MP_GameScene から DontDestroyOnLoad で持ち越されたプレイヤーオブジェクトを非アクティブ化し、
    /// リザルト画面でのカメラ探索・入力受付・物理演算等の暴走を防止する
    /// </summary>
    private void DisableResidualPlayers()
    {
        FreezeWatchdogLogger.Checkpoint("GetAndDisplayScores: 残存プレイヤー無効化開始");
        try
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
                {
                    if (netObj != null && netObj.IsPlayerObject && netObj.gameObject != null)
                    {
                        netObj.gameObject.SetActive(false);
                    }
                }
            }

            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (var player in players)
            {
                if (player != null)
                {
                    player.SetActive(false);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GetAndDisplayScores] 残存プレイヤー無効化時の軽微な例外: {ex.Message}");
        }
        FreezeWatchdogLogger.Checkpoint("GetAndDisplayScores: 残存プレイヤー無効化完了");
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
