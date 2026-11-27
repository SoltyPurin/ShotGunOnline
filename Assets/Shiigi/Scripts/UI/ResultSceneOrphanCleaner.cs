using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ResultScene への遷移時に誤って持ち越された可能性のあるソウル等の残留オブジェクトを無効化する単一責務クラス (SRP)。
/// CanvasResolutionInitializer から異種責務を分離しています。
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
            if (coins[i] != null)
            {
                DisableOrphanObject(coins[i].gameObject);
            }
        }

        CoinControl[] coinCtrls = Object.FindObjectsByType<CoinControl>(FindObjectsInactive.Include);
        for (int i = 0; i < coinCtrls.Length; i++)
        {
            if (coinCtrls[i] != null)
            {
                DisableOrphanObject(coinCtrls[i].gameObject);
            }
        }
    }

    private static void DisableOrphanObject(GameObject go)
    {
        if (go == null) return;

        var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].enabled = false;
        }

        var behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null) behaviours[i].enabled = false;
        }

        go.SetActive(false);
    }
}
