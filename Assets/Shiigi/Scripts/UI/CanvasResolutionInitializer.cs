using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 全 ScreenSpace Canvas の CanvasScaler とフルスクリーンオーバーレイを FHD(1920x1080) 基準に調整する。
/// </summary>
public class CanvasResolutionInitializer : MonoBehaviour
{
    private const string ResultSceneName = "ResultScene";
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    private static CanvasResolutionInitializer s_instance;

    private bool _adjustedOnSceneLoad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        if (s_instance != null)
        {
            SceneManager.sceneLoaded -= s_instance.OnSceneLoaded;
            s_instance = null;
        }
    }

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
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    private void Start()
    {
        if (!_adjustedOnSceneLoad)
        {
            AdjustAllCanvases();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _adjustedOnSceneLoad = true;
        AdjustAllCanvases();
    }

    /// <summary>
    /// シーン内のすべてのScreenSpace Canvasを検出し、FHD(1920x1080)基準のScaleWithScreenSizeに設定
    /// </summary>
    public static void AdjustAllCanvases()
    {
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
            {
                continue;
            }

            if (!canvas.TryGetComponent<CanvasScaler>(out var scaler))
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            AdjustFullScreenOverlayImages(canvas);
        }
    }

    /// <summary>
    /// フェード画像等の全画面オーバーレイが、16:9以外の縦横比でも画面端まで確実に覆うよう設定
    /// </summary>
    private static void AdjustFullScreenOverlayImages(Canvas canvas)
    {
        if (canvas.gameObject.scene.name == ResultSceneName)
        {
            return;
        }

        Image[] images = canvas.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];
            if (img == null) continue;

            if (IsFadeOverlayName(img.gameObject.name))
            {
                RectTransform rt = img.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }
    }

    private static bool IsFadeOverlayName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return false;
        return objectName.StartsWith("fadeimage", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(objectName, "wavechangefade", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(objectName, "transitionwhite", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(objectName, "fade_panel", StringComparison.OrdinalIgnoreCase);
    }
}
