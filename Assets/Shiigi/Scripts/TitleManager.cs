using UnityEngine;

/// <summary>
/// タイトルシーンの初期化およびマルチプレイセッション・ネットワーク状態のクリーンアップを担当するクラス。
/// </summary>
public class TitleManager : MonoBehaviour
{
    [Header("オプションUI参照（未設定時はシーン内から自動検出）")]
    [SerializeField] private FrameRateOptionUI _optionUI;

    private void Start()
    {
        // ポーズ画面などから遷移した際に時間停止状態が残らないよう復帰
        Time.timeScale = 1.0f;
        FrameRateManager.RefreshCurrentSettings();
        EnsureOptionUISetup();

        MultiplayerSessionManager.CleanupOrShutdownStatic();
    }

    /// <summary>
    /// OptionButtonsにFrameRateOptionUIが未設定の場合に自動でアタッチする
    /// </summary>
    private void EnsureOptionUISetup()
    {
        if (_optionUI != null) return;

        _optionUI = FindAnyObjectByType<FrameRateOptionUI>(FindObjectsInactive.Include);
        if (_optionUI != null) return;

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (canvas == null) continue;
            Transform optionButtons = FindChildRecursive(canvas.transform, "OptionButtons");
            if (optionButtons != null)
            {
                _optionUI = optionButtons.gameObject.AddComponent<FrameRateOptionUI>();
                return;
            }
        }
    }

    private static Transform FindChildRecursive(Transform parent, string targetName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == targetName) return child;

            Transform nested = FindChildRecursive(child, targetName);
            if (nested != null) return nested;
        }
        return null;
    }
}
