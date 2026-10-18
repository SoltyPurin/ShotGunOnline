using UnityEngine;

/// <summary>
/// インゲームの進行状態（GameState）に応じて開始演出CanvasとHUD Canvasの表示・非表示を切り替えるクラス (SRP)
/// </summary>
public class InGameUIStateBinder
{
    private const string FallbackLoadingTextName = "NowLoadingText";

    private GameObject _starterCanvas;
    private readonly GameObject _hudCanvas;

    public GameObject StarterCanvas => _starterCanvas;

    public InGameUIStateBinder(GameObject starterCanvas, GameObject hudCanvas)
    {
        _starterCanvas = starterCanvas;
        _hudCanvas = hudCanvas;
    }

    /// <summary>
    /// _starterCanvas がインスペクター未設定の場合のフォールバック解決
    /// </summary>
    public void ResolveFallbackCanvases()
    {
        if (_starterCanvas != null) return;

        var loadingText = GameObject.Find(FallbackLoadingTextName);
        if (loadingText != null)
        {
            var canvas = loadingText.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _starterCanvas = canvas.gameObject;
            }
        }
    }

    /// <summary>
    /// ゲーム状態に応じたCanvas表示を適用する
    /// </summary>
    public void ApplyStateToUI(MP_GameManager.GameState state)
    {
        // 開始演出Canvas（Now Loading表示および開始ムービーUI）はロード待機中〜開始ムービー再生中まで表示
        if (_starterCanvas != null)
        {
            _starterCanvas.SetActive(
                state == MP_GameManager.GameState.WaitingForPlayers ||
                state == MP_GameManager.GameState.OpeningMovie);
        }

        // HUDはゲームプレイ中のみ表示（ムービー中は非表示）
        if (_hudCanvas != null)
        {
            _hudCanvas.SetActive(state == MP_GameManager.GameState.Playing);
        }
    }
}

