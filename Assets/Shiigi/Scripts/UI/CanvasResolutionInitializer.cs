using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// FHD(1920x1080)以外の解像度でもUIやアニメーションの位置・サイズが正確に維持されるよう、
/// すべてのScreenSpace CanvasのCanvasScalerおよびフルスクリーンオーバーレイを自動調整する初期化クラス。
/// </summary>
public class CanvasResolutionInitializer : MonoBehaviour
{
    private static CanvasResolutionInitializer s_instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (s_instance != null) return;

        GameObject host = new GameObject("[CanvasResolutionInitializer]");
        DontDestroyOnLoad(host);
        s_instance = host.AddComponent<CanvasResolutionInitializer>();
    }

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        AdjustAllCanvases();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AdjustAllCanvases();

        // リザルト画面でシーン遷移の残留オブジェクト（ソウルなど）が残らないようクリーンアップ
        if (scene.name == "ResultScene")
        {
            CleanupResultSceneOrphans();
        }
    }

    /// <summary>
    /// シーン内のすべてのScreenSpace Canvasを検出し、FHD(1920x1080)基準のScaleWithScreenSizeに設定
    /// </summary>
    public static void AdjustAllCanvases()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null) continue;

            // ワールド空間UI (頭上HPバー等) は対象外
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                continue;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            // CanvasScalerを1920x1080基準に統一設定
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 全画面フェード・暗転用UIのストレッチ対応
            AdjustFullScreenOverlayImages(canvas);
        }
    }

    /// <summary>
    /// フェード画像等の全画面オーバーレイが、16:9以外の縦横比(16:10や21:9等)でも画面端まで確実に覆うよう設定
    /// </summary>
    private static void AdjustFullScreenOverlayImages(Canvas canvas)
    {
        // ResultSceneのUIパネル（Blackなど）を誤って全画面ストレッチしないよう除外
        if (canvas.gameObject.scene.name == "ResultScene")
        {
            return;
        }

        Image[] images = canvas.GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            if (img == null) continue;

            string nameLower = img.gameObject.name.ToLower();
            // 明確な全画面フェード・暗転オブジェクトのみを対象にする
            bool isFadeOverlay = nameLower.StartsWith("fadeimage") ||
                                 nameLower == "wavechangefade" ||
                                 nameLower == "transitionwhite" ||
                                 nameLower == "fade_panel";

            if (isFadeOverlay)
            {
                RectTransform rt = img.rectTransform;
                // 全画面ストレッチ (Anchor: 0..1, Offsets: 0) に設定
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }
    }

    /// <summary>
    /// シーン遷移時に誤って持ち越された可能性のあるソウル等のゲーム内オブジェクトをResultSceneから確実に消去
    /// </summary>
    private static void CleanupResultSceneOrphans()
    {
        MoveCoin[] coins = Object.FindObjectsByType<MoveCoin>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MoveCoin coin in coins)
        {
            if (coin != null)
            {
                Object.Destroy(coin.gameObject);
            }
        }

        CoinControl[] coinCtrls = Object.FindObjectsByType<CoinControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CoinControl ctrl in coinCtrls)
        {
            if (ctrl != null)
            {
                Object.Destroy(ctrl.gameObject);
            }
        }
    }
}
