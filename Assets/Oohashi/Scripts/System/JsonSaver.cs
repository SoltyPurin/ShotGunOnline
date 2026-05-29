using UnityEngine;
using System.IO;

public class JsonSaver : MonoBehaviour
{
    public static JsonSaver Instance { get; private set; }
    private ALLEnemyJson _enemyJsons;
    public ALLEnemyJson EnemyJson
    {
        get { return _enemyJsons; }
    }

    #region シングルトン化
    private void Awake()
    {
        LoadAllConfigs();
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion


    public void LoadAllConfigs()
    {
        string jsonPath = Path.Combine(Application.persistentDataPath, "EnemyConfigs", "ALLEnemy.json");

        if (!File.Exists(jsonPath))
        {
            _enemyJsons = new ALLEnemyJson(); // 空で初期化してクラッシュ防止
            return;
        }

        string json = File.ReadAllText(jsonPath);
        _enemyJsons = JsonUtility.FromJson<ALLEnemyJson>(json);

        if (_enemyJsons == null)
        {
            _enemyJsons = new ALLEnemyJson();
        }
        GameObject textObj = GameObject.Find("JsonText");
        if(textObj != null)
        {
            JsonReadText showText = textObj.GetComponent<JsonReadText>();
            showText.ShowReadJson();

        }
    }
}
