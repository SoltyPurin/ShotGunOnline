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
        if(_scoreKeeper != null)
        {
            _getScoreManager = _scoreKeeper.GetComponent<GetScoreManager>();
            int minute = _getScoreManager.Minute;
            float seconds = _getScoreManager.Second;
            _timeText.text = minute + " : " +seconds.ToString("F2");
            Destroy(_scoreKeeper);
        }

    }
}
