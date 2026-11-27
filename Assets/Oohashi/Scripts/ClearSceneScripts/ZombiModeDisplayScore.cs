using System.Collections;
using System.Net;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
//ゾンビモードの得点は倒した敵の数と被弾回数から求める
//普通に2分回したとき、デバッグモードの敵の理論値は1296体だったため、ここから逆算する
//一体につき2.5秒(フルチャージ+死亡演出で計算)かかると想定すると理論値は518.4体
//ただ別に一体ずつ倒すわけではなく、まとめて倒すため実際の討伐数はもう少し増えるものと予想する
//処理のいくつかはGetAndDisplayScoreからそのまま流用する
//雑魚敵はカウント1,中ボスはカウント5,ボスはカウントを10とする
public class ZombiModeDisplayScore : MonoBehaviour
{
    #region Serialize変数
    [SerializeField, Header("マルチプレイの時のイラスト")]
    private GameObject _multiPlayImage = default;
    [SerializeField, Header("スコア表示をするテキスト")]
    private Text _totalScoreShowText = default;

    [SerializeField, Header("中ボスは雑魚何体分としてカウントするか")]
    private int _middleMultiplier = 5;
    [SerializeField, Header("ボスは雑魚何体分としてカウントするか")]
    private int _bossMultiplier = 10;
    //Sランクを取るには最低限500体倒して被弾数は0
    [SerializeField, Header("Sランクを取るのに必要な討伐数")]
    private int _sRankKillCount = 500;
    [SerializeField, Header("Aランクを取るのに必要な討伐数")]
    private int _aRankKillCount = 300;

    [SerializeField, Header("一回当たりの被弾で減らすスコア")]
    private int _takeDamageDecRankValue = 10;
    #endregion

    #region 通常変数群
    private ScoreData _scoreData = default;
    private ScoreCircle _scoreCircle = default;
    private int _enemyTheoreticalValue = 0;
    #endregion

    #region 定数
    private readonly string SCOREDATA = "WaveScore";
    #endregion
    #region 個別表示用
    private int _normalEnemyKillCount = 0;
    private int _middleEnemyKillCount = 0;
    private int _bossKillCount = 0;
    #endregion

    private void Start()
    {
        StartCoroutine(ScoreCalcDelay());
    }

    private IEnumerator ScoreCalcDelay()
    {
        yield return new WaitForSeconds(0.2f);
        FreezeWatchdogLogger.Checkpoint("GetAndDisplayScores.Start: 開始");
        Debug.Log("スコア計算開始");
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
        GameObject scoreObject = GameObject.FindWithTag(SCOREDATA);
        if (scoreObject != null)
        {
            _scoreData = scoreObject.GetComponent<ScoreData>();
            if (_scoreData != null)
            {
                //ゾンビモードじゃない場合は通常の処理を行わない
                if (_scoreData.IsZombiMode)
                {
                    _normalEnemyKillCount = _scoreData.GetEnemyKillCount.normalEnemyCount;
                    _middleEnemyKillCount = _scoreData.GetEnemyKillCount.middleEnemyCount;
                    _bossKillCount = _scoreData.GetEnemyKillCount.bossEnemyCount;

                    _enemyTheoreticalValue = _scoreData.TotalEnemyCount;

                    int totalScore = _normalEnemyKillCount + (_middleEnemyKillCount * _middleMultiplier) + (_bossKillCount * _bossMultiplier);
                    totalScore -= _scoreData.DamageTakenCount * _takeDamageDecRankValue;
                    Debug.Log("ランク" + totalScore);
                    _scoreCircle = GetComponent<ScoreCircle>();
                    if (_scoreCircle != null)
                    {
                        _scoreCircle.Initialize(_enemyTheoreticalValue, totalScore);
                    }

                    if (totalScore >= _sRankKillCount)
                    {
                        if (_totalScoreShowText != null) _totalScoreShowText.text = "S";
                        Debug.Log("Sランク");
                    }
                    else if (totalScore >= _aRankKillCount)
                    {
                        if (_totalScoreShowText != null) _totalScoreShowText.text = "A";
                        Debug.Log("Aランク");
                    }
                    else
                    {
                        if (_totalScoreShowText != null) _totalScoreShowText.text = "B";
                        Debug.Log("Bランク");
                    }
                    _multiPlayImage.SetActive(true);
                    MultiPlayResultImageChanger imageChanger = _multiPlayImage.GetComponent<MultiPlayResultImageChanger>();
                    imageChanger?.ChangeResutlImage(onlinePlayerCount);

                }

            }


        }


    }

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


}
