using UnityEngine;
using UnityEngine.UI;

public class FPSLockManager : MonoBehaviour
{
    float deltaTime = 0.0f;
    [SerializeField] private GameObject _fpsConterObject = default;
    [SerializeField] private Text _fpsText = default;
    private bool _isShowFPSCounter = false;
    private void Awake()
    {
        // Vsync Count Ç 0Ç…Ç∑ÇÈÇ±Ç∆Ç…ÇÊÇËÅAFPS Çå≈íËÇ≈Ç´ÇÈÇÊÇ§Ç…Ç»ÇÈ
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.T) && !_isShowFPSCounter)
        {
            _isShowFPSCounter = true;
        }else if(Input.GetKeyDown(KeyCode.T) && _isShowFPSCounter)
        {
            _isShowFPSCounter = false;
        }
        if (_isShowFPSCounter)
        {
            _fpsConterObject.SetActive(true);
        }
        else
        {
            _fpsConterObject.SetActive(false);
        }
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        _fpsText.text = Mathf.Ceil(fps).ToString() + " FPS";
    }
}
