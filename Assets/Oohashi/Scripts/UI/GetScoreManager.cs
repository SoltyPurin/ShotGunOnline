using UnityEngine;
using UnityEngine.SceneManagement;

public class GetScoreManager : MonoBehaviour
{
    [SerializeField, Header("取得系")]
    private ScoreManager _scoreManager = default;

    public static GetScoreManager _this = default;

    private int _minuteScore = 0;

    public int Minute
    {
        get { return _minuteScore; }
    }

    private float _secondScore = 0;
    public float Second
    {
        get { return _secondScore; }
    }

    #region シングルトン化
    private void Awake()
    {
        if (_this != null && _this != this)
        {
            Destroy(gameObject);
            //gameObject.SetActive(false);
            return;
        }

        _this = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (this == null || gameObject == null) return; // 保険
        bool isDestroySceneName = scene.name == "TutorialScene" || scene.name == "Title" || scene.name == "Cushion";
        if (isDestroySceneName && gameObject != null)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Destroy(this.gameObject);
        }

    }
    #endregion

    private void Start()
    {
        _minuteScore = 0;
    }

    /// <summary>
    /// 渡されたスコアを保存する(現在はint型の時間)
    /// </summary>
    /// <param name="minute"></param>
    public void GetScore(int minute , float seconds)
    {
        _minuteScore = minute; 
        _secondScore = seconds;
    }
}
