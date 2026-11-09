using UnityEngine;
using UnityEngine.UI;

public class FPSLockManager : MonoBehaviour
{
    private float deltaTime = 0.0f;
    [SerializeField] private GameObject _fpsConterObject = default;
    [SerializeField] private Text _fpsText = default;
    private bool _isShowFPSCounter = false;

    private void Awake()
    {
        // FrameRateManagerの設定を反映
        FrameRateManager.RefreshCurrentSettings();
    }

    private void Update()
    {
        // TキーでFPSカウンターの表示/非表示切り替え
        if (Input.GetKeyDown(KeyCode.T))
        {
            _isShowFPSCounter = !_isShowFPSCounter;
            if (_fpsConterObject != null)
            {
                _fpsConterObject.SetActive(_isShowFPSCounter);
            }
        }

        // Yキーでターゲットフレームレートを切り替え (60 -> 120 -> 144 -> Unlimited -> VSync -> 30)
        if (Input.GetKeyDown(KeyCode.Y))
        {
            FrameRateManager.ToggleNextMode();
        }

        if (_isShowFPSCounter && _fpsText != null)
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            float fps = deltaTime > 0.0001f ? 1.0f / deltaTime : 0f;

            string modeStr = FrameRateManager.CurrentMode switch
            {
                FrameRateManager.FrameRateMode.VSync => "VSync",
                FrameRateManager.FrameRateMode.Unlimited => "NoLimit",
                _ => $"{(int)FrameRateManager.CurrentMode}cap"
            };

            _fpsText.text = $"{Mathf.Ceil(fps):F0} FPS [{modeStr}]";
        }
    }
}
