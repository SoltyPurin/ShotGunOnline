using System.IO;
using UnityEngine;


public class JsonExport : MonoBehaviour
{
    public static bool CanWrite = true;
    void Awake()
    {
        if ((CanWrite))
        {
            WriteProtocol();
        }
    }

    private void WriteProtocol()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "EnemyConfigs");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        ALLEnemyJson allJson = new ALLEnemyJson();
        string jsonPath = Path.Combine(folderPath, "ALLEnemy.json");

        File.WriteAllText(jsonPath, JsonUtility.ToJson(allJson, true));
        CanWrite = false;
    }
}
