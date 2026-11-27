using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ResultScene 遷移時に残留したコイン系オブジェクトを無効化する。
/// </summary>
public static class ResultSceneOrphanCleaner
{
    private const string ResultSceneName = "ResultScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == ResultSceneName)
        {
            CleanupOrphans();
        }
    }

    public static void CleanupOrphans()
    {
        MoveCoin[] coins = Object.FindObjectsByType<MoveCoin>(FindObjectsInactive.Include);
        for (int i = 0; i < coins.Length; i++)
        {
            if (coins[i] != null && coins[i].gameObject.activeSelf)
            {
                coins[i].gameObject.SetActive(false);
            }
        }

        CoinControl[] coinCtrls = Object.FindObjectsByType<CoinControl>(FindObjectsInactive.Include);
        for (int i = 0; i < coinCtrls.Length; i++)
        {
            if (coinCtrls[i] != null && coinCtrls[i].gameObject.activeSelf)
            {
                coinCtrls[i].gameObject.SetActive(false);
            }
        }
    }
}
