using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
public class GetTimeScore : MonoBehaviour
{
    private GameObject _scoreKeeper = default;
    private GetScoreManager _getScoreManager = default;
    [SerializeField, Header("時間のテキスト")]
    private Text _timeText = default;

    private readonly string SCORETAGNAME = "ScoreKeeper";

    private void Start()
    {
        _scoreKeeper = GameObject.FindWithTag(SCORETAGNAME);
        if (_scoreKeeper != null)
        {
            Debug.Log("スコアキーパーはあるよ");
            _getScoreManager = _scoreKeeper.GetComponent<GetScoreManager>();
            int minute = _getScoreManager.Minute;
            float seconds = _getScoreManager.Second;
            Debug.Log("クリアタイムは" + minute + "分：" + seconds + "秒");
            //TimerTextUpdClientRpc(minute, seconds);
            _timeText.text = minute + " : " + seconds.ToString("F2");
            Destroy(_scoreKeeper);
        }
        else
        {
            Debug.Log("スコアキーパーないよ");
        }

    }
}
    //public override void OnNetworkSpawn()
    //{
    //    base.OnNetworkSpawn();


    //    _scoreKeeper = GameObject.FindWithTag(SCORETAGNAME);
    //    if (_scoreKeeper != null)
    //    {
    //        Debug.Log("スコアキーパーはあるよ");
    //        _getScoreManager = _scoreKeeper.GetComponent<GetScoreManager>();
    //        int minute = _getScoreManager.Minute;
    //        float seconds = _getScoreManager.Second;
    //        Debug.Log("クリアタイムは" + minute + "分：" + seconds + "秒");
    //        //TimerTextUpdClientRpc(minute, seconds);
    //        _timeText.text = minute + " : " + seconds.ToString("F2");
    //        Destroy(_scoreKeeper);
    //    }
    //    else
    //    {
    //        Debug.Log("スコアキーパーないよ");
    //    }

    //    //if (IsServer)
    //    //{
    //    //    // 1フレーム遅らせて ScoreKeeper 側のデータセット完了を保証する
    //    //    StartCoroutine(GetAndSyncScoreRoutine());
    //    //}
    //}
    //private IEnumerator GetAndSyncScoreRoutine()
    //{
    //    yield return null;
    //    _scoreKeeper = GameObject.FindWithTag(SCORETAGNAME);
    //    if(_scoreKeeper != null)
    //    {
    //        _getScoreManager = _scoreKeeper.GetComponent<GetScoreManager>();
    //        int minute = _getScoreManager.Minute;
    //        float seconds = _getScoreManager.Second;
    //        Debug.Log("クリアタイムは" + minute + "分：" +  seconds + "秒");
    //        //TimerTextUpdClientRpc(minute, seconds);
    //        _timeText.text = minute + " : " + seconds.ToString("F2");
    //        Destroy(_scoreKeeper);
    //    }
    //}

    ////[ClientRpc]
    ////private void TimerTextUpdClientRpc(int minute ,float seconds)
    ////{
    //    _timeText.text = minute + " : " + seconds.ToString("F2");
    //}
